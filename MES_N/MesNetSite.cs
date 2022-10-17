using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MES_N
{
    //操作MES各站點獨立類別
    class MesNetSite
    {
        public DataTable dt = new DataTable();

        public String String_TID = "";
        public String String_Dline = "";
        public String String_DIP = "";
        public String String_Port = "";
        public String String_Address = "";
        public String String_NOTE = "";
        public String Str_Portid = "";
        public String String_SID = "";
        public String String_Sclass = "";

        public String[] str_Command = new string[] { "S1F1", "S1F2", "S1F3", "S1F4", "S1F5", "S1F6", "S1F7" };        

        public Dictionary<String, List<String>> dicDeviceList = new Dictionary<String, List<String>>();

        public Boolean bool_AutoRun = false;
        public Boolean bool_isThreadSet = false;     

        private int index = 0;
        public int int_ThreadNum = 0;
        public int int_timeOutMsec = 0;
        public int int_ReaderSleep = 0;
        public int int_ReaderSleepSET = 0;

        //17122601
        public string str_Barcode = "";
        public string str_BarcodeLight = "";
        public System.Net.Sockets.NetworkStream NetworkStream_Reader;
        public System.Net.Sockets.TcpClient TcpClient_Reader;

        byte[] Byte_Command_Sent_082 = new byte[8];
        byte[] Byte_Command_Sent_083 = new byte[9];
        void CommandSet()
        {
            // 02 03 02 4C 00 04 84 55 讀取第1台 4通道 ID02
            // 03 03 02 4C 00 04 85 84 讀取第2台 4通道 ID03 
            StrArrayToByteArray("02 03 02 4C 00 04 84 55", Byte_Command_Sent_082);

            StrArrayToByteArray("03 03 02 4C 00 04 85 84", Byte_Command_Sent_083);

            StrArrayToByteArray("01 03 00 00 00 08 44 0C", Byte_Command_Sent);
        }

        void StrArrayToByteArray(String StringArray, byte[] ByteArray)
        {
            for (int i = 0; i <= StringArray.Split(' ').Length - 1; i++)
            {
                ByteArray[i] = Convert.ToByte(StringArray.Split(' ')[i], 16);
            }
        }

        //以防其他執行緒未完成，專門為TCP連結用
        Boolean bool_tcpclientconnect_Action = false;
        /// <summary>
        /// 與網路盒建立連結
        /// </summary>
        public void TcpClientConnect()
        {
            if (bool_tcpclientconnect_Action == false && bool_AutoRun == true)
            {
                bool_tcpclientconnect_Action = true;

                //宣告tcp連接 
                byte[] Bytes_IP = new byte[4];
                for (int i = 0; i <= 3; i++)
                {
                    Bytes_IP[i] = Convert.ToByte(String_DIP.Split('.')[i]);
                }

                try
                {
                    if (TcpClient_Reader != null)
                    {
                        TcpClient_Reader.Close();
                        TcpClient_Reader = null;
                        Thread.Sleep(100);
                    }

                    TcpClient_Reader = TimeOutSocket.Connect(new System.Net.IPEndPoint(new System.Net.IPAddress(Bytes_IP), Convert.ToInt16(String_Port)), 100);

                    //
                    if (!(TcpClient_Reader == null) && TcpClient_Reader.Connected)
                    {
                        //連結成功
                        NetworkStream_Reader = TcpClient_Reader.GetStream();

                        TcpClient_Reader.ReceiveTimeout = 100;

                        TcpClient_Reader.SendTimeout = 100;

                        TcpClient_Reader.ReceiveTimeout = 100;

                        TcpClient_Reader.SendTimeout = 100;

                        NetworkStream_Reader.WriteTimeout = 100;

                        NetworkStream_Reader.ReadTimeout = 100;

                        //Int_GetCount = 0;

                        TcpClient_Reader.ReceiveBufferSize = 1024;

                        //成功啟動後，把執行緒休息時間改為設計值
                        int_ReaderSleep = int_ReaderSleepSET;

                        CommandSet();

                    }
                }
                catch (Exception EX)
                {
                    if (EX.Source != null)
                    {
                        Console.WriteLine("M0091:Exception source: {0}", String_DIP + "[" + EX.Message + "]");

                        if (EX.Message == "TimeOut Exception (TimeOutSocket-0040)")
                        {
                            //初此啟動之後，把此執行緒休息時間延長至10秒
                            int_ReaderSleep = 1000;
                            //System.Threading.Thread.Sleep(10000);
                        }
                    }

                }

                bool_tcpclientconnect_Action = false;
            }
        }

        //製作一個boolean 代表只有單一個動作在執行
        Boolean boolMESnetISrun = false;

        Boolean[] FirstRec = new Boolean[] { };

        bool bool_reconnect = true;

        int int_Reconnect = 0;
        int F11_port = 0;
        int F15_port = 0;

        StringBuilder sbSQL = new StringBuilder();

        private delegate void InvokeDelegate();
        //執行緒主要執行區塊
        public void MesNetSiteRunning()
        {
            // String_TID = "111";
            while (bool_AutoRun)
            {
                try
                {
                    //byte[] testRecByte = new byte[1];
                    ////使用Peek，測試client是否還有連線
                    //if (!(TcpClient_Reader != null && TcpClient_Reader.Connected))
                    //{
                    //    if (int_Reconnect > 10)
                    //    {
                    //        int_Reconnect = 0;
                    //        TcpClientConnect();
                    //    }
                    //    int_Reconnect++;
                    //}

                    //161208 加入判斷tcpclient是否在連結中，如果在連結中就不要做其他動作。
                    if (boolMESnetISrun == false)
                    {
                        boolMESnetISrun = true;

                        // 清除舊的SQL語法
                        sbSQL.Clear();

                        Array.Resize(ref String_ReData, Convert.ToInt32(dicDeviceList[String_Port].Count));
                        Array.Resize(ref FirstRec, Convert.ToInt32(dicDeviceList[String_Port].Count));

                        for (int i = 0; i < dicDeviceList[String_Port].Count; i++)     
                        {
                            try
                            {
                                F11_port = 0;
                                F15_port = 0;
                                index = i;

                                String_TID = MPU.dt_MainTable.Rows[int_ThreadNum + index]["TID"].ToString();
                                String_SID = MPU.dt_MainTable.Rows[int_ThreadNum + index]["SID"].ToString();
                                String_NOTE = MPU.dt_MainTable.Rows[int_ThreadNum + index]["NOTE"].ToString();

                                //連結成功才動作
                                //if ((TcpClient_Reader != null) && TcpClient_Reader.Connected)
                                if ((TcpClient_Reader != null) && !TcpClient_Reader.Client.Poll(0, SelectMode.SelectRead))
                                {
                                    switch (dicDeviceList[String_Port][i].Split('_')[0])
                                    {
                                        case "1":
                                            function_01();
                                            break;
                                        case "2": //條碼
                                            function_02();
                                            break;
                                        case "3": //控制燈號模組
                                            function_WLS_LCS();
                                            break;
                                        case "4":
                                            function_DTS();
                                            break;
                                        case "5":
                                            function_KPS();
                                            break;
                                        case "6":
                                            function_06();
                                            break;
                                        case "7":
                                            function_07();
                                            break;
                                        case "8":
                                            function_DTS_8();
                                            break;
                                        case "9": //正壓
                                            function_09_positive();
                                            break;
                                        case "10": //負壓
                                            function_10_negative();
                                            break;
                                        case "11": //流量計
                                            if (dicDeviceList[String_Port][i].Split('_').Length == 2)
                                                F11_port = Convert.ToInt32(dicDeviceList[String_Port][i].Split('_')[1]);
                                            function_11_flow(F11_port);
                                            //if (dicDeviceList[String_Port][i].Split('_').Length == 1)
                                            //    F11_index++;
                                            break;
                                        case "12": //燈號判斷
                                            function_12_light();
                                            break;
                                        case "13": //溫度
                                            function_13();
                                            break;
                                        case "14": //燈號控制
                                            function_14();
                                            break;
                                        case "15": //正壓、負壓(8_1、8_2、8_5、8_6)
                                            if (dicDeviceList[String_Port][i].Split('_').Length == 2)
                                                F15_port = Convert.ToInt32(dicDeviceList[String_Port][i].Split('_')[1]);
                                            function_15(F15_port);
                                            break;
                                        case "16": //PC S1F1 燈號
                                            function_16_pc_light();
                                            break;
                                        case "17": //PC S1F2 壓力
                                            function_17_pc_pressure();
                                            break;
                                        case "18": //PC S1F3 流量
                                            function_18_pc_flow();
                                            break;
                                        case "19": //PC S1F4 溫度
                                            function_19_pc_temperature();
                                            break;
                                        case "20": //PC S1F5 吸嘴阻值
                                            function_20_pc_resistance();
                                            break;
                                        case "21": //PC S1F6 H-Judge讀值
                                            function_21_pc_H_Judge();
                                            break;
                                        case "22": //PC S1F7 螢幕辨識參數(Temperature, Power, force, Time)
                                            function_22_pc_shoucut();
                                            break;
                                        case "23": //BrainChild溫度
                                            function_23_brainchild();
                                            break;
                                        case "24": //PC S1F8 舉離機(NMPA, NMPB)
                                            function_24_pc_NMPA_NMPB();
                                            break;
                                        case "25":
                                            function_25(Convert.ToInt32(dicDeviceList[String_Port][i].Split('_')[1]));
                                            break;
                                        default:
                                            //SetStatus();
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (dicDeviceList[String_Port][i].Split('_')[0])
                                    {
                                        case "26": //只PING設備IP，連線成功就回傳黃燈
                                            function_26_PingIP();
                                            break;
                                        default: //TCPclient連線失敗
                                            String_ReData[index] = MPU.str_ErrorMessage[1];
                                            if (bool_reconnect)
                                            {
                                                bool_reconnect = false;
                                                new Thread(Reconnect).Start();
                                            }
                                            break;
                                    }

                                    #region 舊的
                                    //// 30秒重新連線一次
                                    //if ((DateTime.Now - IOdisTime).Seconds >= 10)
                                    //{
                                    //    isConnect = false;
                                    //}

                                    //if (isConnect == false && bool_tcpclientconnect_Action == false)
                                    //{
                                    //    IOdisTime = DateTime.Now;
                                    //    isConnect = true;
                                    //    //重新建立連結
                                    //    TcpClientConnect();
                                    //}

                                    //161208 超出時間後再連一次。
                                    //byte_function_03_loop_index += 1;

                                    //當10次沒有連結的時候，重新連線一次。
                                    //if (byte_function_03_loop_index > 5)
                                    //{
                                    //    //重新建立連結
                                    //    TcpClientConnect();

                                    //    byte_function_03_loop_index = 0;

                                    //}
                                    #endregion
                                }
                            }
                            catch (System.NullReferenceException EXnull)
                            {
                                if (EXnull.Source != null)
                                {
                                    Console.WriteLine("M0182:空值處理，重新連線[" + String_DIP + "] Exception source: {0}", EXnull.Source + ":" + EXnull.Message);
                                    String_ReData[index] = MPU.str_ErrorMessage[1];
                                    if (bool_reconnect)
                                    {
                                        bool_reconnect = false;
                                        new Thread(Reconnect).Start();
                                    }

                                    #region 舊的
                                    //// 30秒重新連線一次
                                    //if ((DateTime.Now - IOdisTime).Seconds >= 30)
                                    //{
                                    //    isConnect = false;
                                    //}

                                    //if (isConnect == false)
                                    //{
                                    //    IOdisTime = DateTime.Now;
                                    //    isConnect = true;
                                    //    //重新建立連結
                                    //    new Thread(TcpClientConnect).Start();
                                    //}

                                    //161208 超出時間後再連一次。
                                    //byte_function_03_loop_index += 1;

                                    //當10次沒有連結的時候，重新連線一次。
                                    //if (byte_function_03_loop_index > 5)
                                    //{
                                    //    //重新建立連結
                                    //    TcpClientConnect();

                                    //    byte_function_03_loop_index = 0;

                                    //}
                                    #endregion
                                }
                            }
                            catch (Exception ex)
                            {
                                String_ReData[index] = MPU.str_ErrorMessage[1];
                                if (ex.Source != null)
                                {
                                    Console.WriteLine("M0192:Exception source: {0}", ex.Source + ":" + ex.Message);
                                }

                                if (bool_reconnect)
                                {
                                    bool_reconnect = false;
                                    new Thread(Reconnect).Start();
                                }

                                #region 舊的
                                //// 30秒重新連線一次
                                //if ((DateTime.Now - IOdisTime).Seconds >= 30)
                                //{
                                //    isConnect = false;
                                //}

                                //if (isConnect == false)
                                //{
                                //    IOdisTime = DateTime.Now;
                                //    isConnect = true;
                                //    //重新建立連結
                                //    new Thread(TcpClientConnect).Start();
                                //}
                                #endregion
                            }


                            boolMESnetISrun = false;

                            if (bool_AutoRun && MPU.Ethernet)
                            {
                                if (!string.IsNullOrEmpty(MPU.dic_ReceiveMessage[int_ThreadNum + index]))
                                {
                                    // 如果狀態出現連線失敗或catch的錯誤訊息
                                    if (MPU.dic_ReceiveMessage[int_ThreadNum + index].Contains(MPU.str_ErrorMessage[0]) 
                                        || MPU.dic_ReceiveMessage[int_ThreadNum + index].Contains(MPU.str_ErrorMessage[1]))
                                    {                                        
                                        // IF判斷如果不存在斷線紀錄
                                        sbSQL.AppendFormat(@"IF NOT EXISTS (SELECT * FROM tb_connectlog 
                                                            WHERE DIP = '{0}' and 
                                                            ADDRESS = {1} and 
                                                            SID = '{2}' and 
                                                            DVALUE = '{3}' and 
                                                            CONTIME IS NULL)",
                                                            String_DIP, String_Address, String_SID, String_NOTE);
                                        sbSQL.AppendLine();

                                        // 新增斷線紀錄
                                        sbSQL.Append("  INSERT INTO ");
                                        sbSQL.AppendFormat(@"tb_connectlog (DIP, ADDRESS, SID, DVALUE, DISTIME, SYSTIME) 
                                                            VALUES ('{0}', {1}, '{2}', '{3}', '{4}', '{5}')",
                                                            String_DIP, String_Address, String_SID, String_NOTE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                        sbSQL.AppendLine();                                        
                                    }

                                    //  如果狀態不存在任何錯誤訊息，代表正常連線
                                    if (!MPU.str_ErrorMessage.Contains(MPU.dic_ReceiveMessage[int_ThreadNum + index]))
                                    {
                                        // IF判斷如果存在斷線紀錄，且沒有重新連線紀錄
                                        sbSQL.AppendFormat(@"IF EXISTS (SELECT * FROM tb_connectlog 
                                                                WHERE DIP = '{0}' and 
                                                                ADDRESS = {1} and 
                                                                SID = '{2}' and 
                                                                DVALUE = '{3}' and 
                                                                CONTIME IS NULL)",
                                                                String_DIP, String_Address, String_SID, String_NOTE);
                                        sbSQL.AppendLine();

                                        // 更新原本斷線紀錄，將連線時間更新上去
                                        sbSQL.Append("  UPDATE ");
                                        sbSQL.AppendFormat(@"tb_connectlog SET CONTIME = '{0}' 
                                                                WHERE DIP = '{1}' and 
                                                                ADDRESS = {2} and   
                                                                SID = '{3}' and 
                                                                DVALUE = '{4}' 
                                                                and CONTIME IS NULL",
                                                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), String_DIP, String_Address, String_SID, String_NOTE);
                                        sbSQL.AppendLine();
                                    }
                                }
                            }

                            // 刷新設備狀態
                            SetStatus();

                            // 回寫資料庫
                            sbSQL.AppendLine(String_SQLcommand);
                            if (!string.IsNullOrWhiteSpace(sbSQL.ToString()))
                                MPU.ReadSQL(sbSQL.ToString());

                            if (!string.IsNullOrWhiteSpace(String_SQLcommand_old))
                                MPU.ReadSQL_old(String_SQLcommand_old);


                            //System.Threading.Thread.Sleep(int_ReaderSleep / dicDeviceList[String_Port].Count);
                        }
                    }

                    #region 舊的
                    // 如果資料庫斷線，就每30秒重新連線一次
                    //if (MPU.Ethernet == false)
                    //{
                    //    if ((DateTime.Now - DBdisTime).Seconds >= 30)
                    //    {
                    //        isDBreConnect = true;
                    //    }

                    //    if (isDBreConnect == true)
                    //    {
                    //        DBdisTime = DateTime.Now;
                    //        isDBreConnect = false;
                    //        Check_Connection.CheckConnaction();
                    //    }
                    //}
                    #endregion

                    System.Threading.Thread.Sleep(int_ReaderSleep);
                }
                catch (Exception ex)
                {
                    MPU.WriteErrorCode("", "[MesNetSiteRunning] " + ex.Message);
                    Console.WriteLine("[MesNetSiteRunning] " + ex.Message);
                }
            } 
        }

        private void Reconnect()
        {
            while (!bool_reconnect)
            {
                int_Reconnect++;
                try
                {
                    if (int_Reconnect >= 10)
                    {
                        int_Reconnect = 0;
                        //if (TcpClient_Reader != null)
                        //{
                        //    TcpClient_Reader.Close();
                        //    TcpClient_Reader = null;
                        //}
                        TcpClientConnect();
                        bool_reconnect = true;
                    }
                }
                catch (Exception ex)
                {
                    bool_reconnect = false;
                }
                Thread.Sleep(1000);
            }
        }

        bool bool_firstrun = true;
        List<int> int_connecting = new List<int>();
        private static readonly object D_Lock = new object();
        private void SetStatus()
        {
            try
            {
                if (String_ReData[index] != null)
                {
                    if (String_ReData[index].Contains(MPU.str_ErrorMessage[3]) || String_ReData[index] == "")
                    {
                        if (bool_firstrun)
                        {
                            for (int i = 0; i < Convert.ToInt32(dicDeviceList[String_Port].Count); i++)
                            {
                                int_connecting.Add(0);
                            }
                            bool_firstrun = false;
                        }

                        if (int_connecting[index] >= 10)
                        {
                            String_ReData[index] = MPU.str_ErrorMessage[1];
                            int_connecting[index] = 0;
                        }
                        else
                        {
                            int_connecting[index]++;
                        }
                    }
                }
                else
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                if (String_ReData[index] != null)
                    MPU.dic_ReceiveMessage.AddOrUpdate(int_ThreadNum + index, String_ReData[index], (k, v) => String_ReData[index]);

                #region 舊的
                //if (Form1.form1.InvokeRequired)
                //{
                //    Form1.form1.Invoke(new Action(SetStatus), new object[] { });
                //}
                //else
                //{
                //    if (bool_AutoRun)
                //    {
                //        if (!(TcpClient_Reader != null && TcpClient_Reader.Connected))
                //        {
                //            String_ReData[intDiveceIndex] = DateTime.Now.ToString("HH:mm:ss") + " " + MPU.str_ErrorMessage[1];
                //            Form1.form1.dgvLog[0].Rows[int_ThreadNum + intDiveceIndex].DefaultCellStyle.BackColor = System.Drawing.Color.MistyRose;
                //        }
                //        else
                //        {
                //            Form1.form1.dgvLog[0].Rows[int_ThreadNum + intDiveceIndex].DefaultCellStyle.BackColor = System.Drawing.Color.White;
                //        }

                //        if (MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.str_ErrorMessage[0]) ||
                //            MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.str_ErrorMessage[1]) ||
                //            MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.str_ErrorMessage[2]) ||
                //            MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.str_ErrorMessage[3]))
                //        {
                //            Form1.form1.dgvLog[0].Rows[int_ThreadNum + intDiveceIndex].DefaultCellStyle.BackColor = System.Drawing.Color.MistyRose;
                //        }

                //        if (MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.str_ErrorMessage[3]))
                //        {
                //            if (FirstRec[intDiveceIndex])
                //            {
                //                FirstRec[intDiveceIndex] = false;
                //                noDataTime[intDiveceIndex] = DateTime.Now;
                //            }

                //            if ((DateTime.Now - noDataTime[intDiveceIndex]).Seconds >= 30)
                //            {
                //                MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"] = MPU.str_ErrorMessage[2];
                //                String_ReData[intDiveceIndex] = MPU.str_ErrorMessage[2];
                //                noDataTime[intDiveceIndex] = DateTime.Now;
                //                FirstRec[intDiveceIndex] = true;
                //            }
                //        }

                //        MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"] = String_ReData[intDiveceIndex];

                //        Application.DoEvents();
                //    }
                //}
                #endregion
            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "[MesNetSite SetStatus] " + ex.StackTrace);
                Console.WriteLine("[MesNetSite SetStatus] " + ex.StackTrace);
            }
        }

        #region 舊 - 更新連線狀態用法
        private void UpdateDTToSQL()
        {
            if (bool_AutoRun)
            {
                try
                {
                    if (!string.IsNullOrEmpty(MPU.dic_ReceiveMessage[int_ThreadNum + index]))
                    {
                        //Form1.form1.BeginInvoke(new InvokeDelegate(ChangeDatagridviewColor));
                        if (MPU.dic_ReceiveMessage[int_ThreadNum + index].Contains(MPU.str_ErrorMessage[0]) || MPU.dic_ReceiveMessage[int_ThreadNum + index].Contains(MPU.str_ErrorMessage[1]))
                        {
                            if (MPU.Ethernet == true)
                            {
                                dt = MPU.ReadSQLToDT(string.Format("SELECT * FROM tb_connectlog WHERE DIP = '{0}' and ADDRESS = '{1}' and DVALUE = '{2}' and CONTIME IS NULL ORDER BY SYSTIME DESC", String_DIP, String_Address, String_NOTE));
                                if (dt.Rows.Count > 0)
                                {
                                    if (!string.IsNullOrEmpty(dt.Rows[0]["CONTIME"].ToString()))
                                    {
                                        MPU.ReadSQL(string.Format("INSERT INTO tb_connectlog (DIP, ADDRESS, DVALUE, DISTIME, SYSTIME) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", String_DIP, String_Address, String_NOTE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                                    }
                                }
                                else
                                {
                                    MPU.ReadSQL(string.Format("INSERT INTO tb_connectlog (DIP, ADDRESS, DVALUE, DISTIME, SYSTIME) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", String_DIP, String_Address, String_NOTE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                                }
                            }
                        }
                        else if (!MPU.dic_ReceiveMessage[int_ThreadNum + index].Contains(MPU.str_ErrorMessage[2]) && !MPU.dic_ReceiveMessage[int_ThreadNum + index].Contains(MPU.str_ErrorMessage[3]))
                        {
                            if (MPU.Ethernet == true)
                            {
                                dt = MPU.ReadSQLToDT(string.Format("SELECT TOP (1) * FROM tb_connectlog WHERE DIP = '{0}' and ADDRESS = '{1}' and DVALUE = '{2}' ORDER BY SYSTIME DESC", String_DIP, String_Address, String_NOTE));
                                if (dt.Rows.Count > 0)
                                {
                                    if (string.IsNullOrEmpty(dt.Rows[0]["CONTIME"].ToString()))
                                    {
                                        //ReadSQLToDT(string.Format("INSERT INTO tb_connectlog (DIP, ADDRESS, DVALUE, CONTIME, SYSTIME) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", String_DIP, String_Address, String_NOTE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                                        MPU.ReadSQL(string.Format("UPDATE tb_connectlog SET CONTIME = '{0}' WHERE DIP = '{1}' and ADDRESS = '{2}' and DVALUE = '{3}' and CONTIME IS NULL ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), String_DIP, String_Address, String_NOTE));
                                    }
                                }
                            }
                        }

                        #region 更新即時警報(舊的用法)
                        //// 30秒更新一次即時警報一次
                        //if (isUpdate)
                        //{
                        //    isUpdate = false;
                        //    // 記錄更新當下時間
                        //    UpdateTime = DateTime.Now;
                        //    string concat_str = "";
                        //    for (int j = 0; j < MPU.dt_MainTable.Rows.Count; j++)
                        //    {
                        //        concat_str += "'" + MPU.dt_MainTable.Rows[j]["DIP"].ToString() + "," + MPU.dt_MainTable.Rows[j]["ADDRESS"].ToString() + "," + MPU.dt_MainTable.Rows[j]["NOTE"].ToString() + "'" + ",";
                        //    }
                        //    concat_str = concat_str.Trim(',');
                        //    MPU.dt_CurrentLog = null;

                        //    if (MPU.Ethernet == true)
                        //    {
                        //        MPU.dt_CurrentLog = ReadSQLToDT(string.Format("SELECT DIP IP, ADDRESS 站號, DVALUE Note, FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間 FROM tb_connectlog WHERE CONTIME IS NULL AND CONCAT(TRIM(DIP),',',ADDRESS,',',TRIM(DVALUE)) IN ({0}) ORDER BY DISTIME DESC", concat_str));

                        //        Form1.form1.dgvLog[0].DataSource = MPU.dt_CurrentLog;

                        //        Form1.form1.dgvLog[0].Columns[0].Width = 100;
                        //        Form1.form1.dgvLog[0].Columns[1].Width = 60;
                        //        Form1.form1.dgvLog[0].Columns[2].Width = 859;
                        //        Form1.form1.dgvLog[0].Columns[3].Width = 224;
                        //    }
                        //}

                        //if ((DateTime.Now - UpdateTime).Seconds >= 30)
                        //{
                        //    isUpdate = true;
                        //}
                        #endregion
                    }

                    #region 舊的
                    //if (bool_isThreadSet)
                    //{

                    //    if (MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.static_msg[0]) || MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.static_msg[1]))
                    //    {
                    //        Form1.form1.dgvLog[0].Rows[int_ThreadNum].DefaultCellStyle.BackColor = System.Drawing.Color.MistyRose;
                    //    }
                    //    else
                    //    {
                    //        Form1.form1.dgvLog[0].Rows[int_ThreadNum].DefaultCellStyle.BackColor = System.Drawing.Color.White;
                    //    }
                    //    //Form1.form1.ChangeColor(int_ThreadNum);
                    //}
                    #endregion

                    Application.DoEvents();
                }
                catch (Exception ex)
                {
                    MPU.WriteErrorCode("", "[MesNetSite UpdateDTToSQL] " + ex.StackTrace);
                    Console.WriteLine(ex);
                }

                #region 舊的
                //if (Form1.form1.InvokeRequired)
                //{
                //    Form1.form1.Invoke(new Action(UpdateDTToSQL), new object[] { });
                //}
                //else
                //{
                //    try
                //    {
                //        string static_str = "";
                //        if (!string.IsNullOrEmpty(MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString()))
                //        {
                //            // 若同IP、同PORT時，只讓第一組IP去更新DataTable
                //            if (String_ReData.Length == Convert.ToInt32(address_index.Split(',')[1]) && Convert.ToInt32(address_index.Split(',')[0]) == 1)
                //            {
                //                for (int i = 0; i < Convert.ToInt32(address_index.Split(',')[1]); i++)
                //                {
                //                    MPU.dt_MainTable.Rows[int_ThreadNum + i]["Static"] = String_ReData[intDiveceIndex];
                //                }
                //            }
                //            ChangeDatagridviewColor();

                //           static_str = String_DIP + " " + String_NOTE + MPU.static_msg[1];
                //            if (MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.static_msg[0]) || MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.static_msg[1]))
                //            {
                //                if (MPU.Ethernet == true)
                //                {
                //                    dt = MPU.ReadSQLToDT(string.Format("SELECT * FROM tb_connectlog WHERE DIP = '{0}' and ADDRESS = '{1}' and DVALUE = '{2}' and CONTIME IS NULL ORDER BY SYSTIME DESC", String_DIP, String_Address, String_NOTE));
                //                    if (dt.Rows.Count > 0)
                //                    {
                //                        if (!string.IsNullOrEmpty(dt.Rows[0]["CONTIME"].ToString()))
                //                        {
                //                            MPU.ReadSQLToDT(string.Format("INSERT INTO tb_connectlog (DIP, ADDRESS, DVALUE, DISTIME, SYSTIME) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", String_DIP, String_Address, String_NOTE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                //                        }
                //                    }
                //                    else
                //                    {
                //                        MPU.ReadSQLToDT(string.Format("INSERT INTO tb_connectlog (DIP, ADDRESS, DVALUE, DISTIME, SYSTIME) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", String_DIP, String_Address, String_NOTE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                //                    }
                //                }
                //            }
                //            else if (!MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.static_msg[2]) && !MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.static_msg[3]))
                //            {
                //                if (MPU.Ethernet == true)
                //                {
                //                    dt = MPU.ReadSQLToDT(string.Format("SELECT TOP (1) * FROM tb_connectlog WHERE DIP = '{0}' and ADDRESS = '{1}' and DVALUE = '{2}' ORDER BY SYSTIME DESC", String_DIP, String_Address, String_NOTE));
                //                    if (dt.Rows.Count > 0)
                //                    {
                //                        if (string.IsNullOrEmpty(dt.Rows[0]["CONTIME"].ToString()))
                //                        {
                //                            //ReadSQLToDT(string.Format("INSERT INTO tb_connectlog (DIP, ADDRESS, DVALUE, CONTIME, SYSTIME) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", String_DIP, String_Address, String_NOTE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                //                            MPU.ReadSQLToDT(string.Format("UPDATE tb_connectlog SET CONTIME = '{0}' WHERE DIP = '{1}' and ADDRESS = '{2}' and DVALUE = '{3}' and CONTIME IS NULL ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), String_DIP, String_Address, String_NOTE));
                //                        }
                //                    }
                //                }
                //            }

                //            #region 更新即時警報
                //            //// 30秒更新一次即時警報一次
                //            //if (isUpdate)
                //            //{
                //            //    isUpdate = false;
                //            //    // 記錄更新當下時間
                //            //    UpdateTime = DateTime.Now;
                //            //    string concat_str = "";
                //            //    for (int j = 0; j < MPU.dt_MainTable.Rows.Count; j++)
                //            //    {
                //            //        concat_str += "'" + MPU.dt_MainTable.Rows[j]["DIP"].ToString() + "," + MPU.dt_MainTable.Rows[j]["ADDRESS"].ToString() + "," + MPU.dt_MainTable.Rows[j]["NOTE"].ToString() + "'" + ",";
                //            //    }
                //            //    concat_str = concat_str.Trim(',');
                //            //    MPU.dt_CurrentLog = null;

                //            //    if (MPU.Ethernet == true)
                //            //    {
                //            //        MPU.dt_CurrentLog = ReadSQLToDT(string.Format("SELECT DIP IP, ADDRESS 站號, DVALUE Note, FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間 FROM tb_connectlog WHERE CONTIME IS NULL AND CONCAT(TRIM(DIP),',',ADDRESS,',',TRIM(DVALUE)) IN ({0}) ORDER BY DISTIME DESC", concat_str));

                //            //        Form1.form1.dgvLog[0].DataSource = MPU.dt_CurrentLog;

                //            //        Form1.form1.dgvLog[0].Columns[0].Width = 100;
                //            //        Form1.form1.dgvLog[0].Columns[1].Width = 60;
                //            //        Form1.form1.dgvLog[0].Columns[2].Width = 859;
                //            //        Form1.form1.dgvLog[0].Columns[3].Width = 224;
                //            //    }
                //            //}

                //            //if ((DateTime.Now - UpdateTime).Seconds >= 30)
                //            //{
                //            //    isUpdate = true;
                //            //}
                //            #endregion                        
                //        }

                //        if (MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"].ToString().Contains(MPU.static_msg[3]))
                //        {
                //            //MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"] = String_ReData[intDiveceIndex];
                //            // 30秒內若撈不到I/O的資料就更改狀態
                //            if ((DateTime.Now - noDataTime).Seconds >= 30)
                //            {
                //                MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"] = MPU.static_msg[2];
                //            }
                //            else
                //            {
                //                MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"] = MPU.static_msg[3];
                //                //MPU.dt_MainTable.Rows[int_ThreadNum + intDiveceIndex]["Static"] = MPU.static_msg[3];
                //            }
                //        }

                //        if (bool_isThreadSet)
                //        {
                //            Form1.form1.ChangeColor(int_ThreadNum);
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        MPU.WriteErrorCode("", "MesNetSite UpdateDTToSQL : " + ex.Message);
                //        Console.WriteLine(ex);
                //        throw ex;
                //    }
                //}
                #endregion
            }
        }
        #endregion

        #region 舊 - 更新DataGridView顏色
        private void ChangeDatagridviewColor()
        {
            if (MPU.dt_MainTable.Rows[int_ThreadNum + index]["Static"].ToString().Contains(MPU.str_ErrorMessage[0]) || MPU.dt_MainTable.Rows[int_ThreadNum + index]["Static"].ToString().Contains(MPU.str_ErrorMessage[1]))
            {
                Form1.form1.dgvLog[0].Rows[int_ThreadNum].DefaultCellStyle.BackColor = System.Drawing.Color.MistyRose;
            }
            else
            {
                Form1.form1.dgvLog[0].Rows[int_ThreadNum].DefaultCellStyle.BackColor = System.Drawing.Color.White;
            }
        }
        #endregion

        #region 計算CRC檢查碼
        /// <summary>
        /// 計算CRC檢查碼(請確認CRC指令是否正確)
        /// </summary>
        /// <param name="CRC">CRC指令</param>
        public String CRC16LH(String CRC)
        {
            try
            {
                byte[] CRC_Bytes = new byte[CRC.Split(' ').Length];
                for (int i = 0; i < CRC_Bytes.Length; i++)
                {
                    CRC_Bytes[i] = Convert.ToByte(Convert.ToInt32(CRC.Split(' ')[i], 16));
                }
                ushort crc = 0xffff;
                ushort polynom = 0xA001;
                for (int i = 0; i < CRC_Bytes.Length; i++)
                {
                    crc ^= CRC_Bytes[i];
                    for (int j = 0; j < 8; j++)
                    {
                        if ((crc & 0x01) == 0x01)
                        {
                            crc >>= 1;
                            crc ^= polynom;
                        }
                        else
                        {
                            crc >>= 1;
                        }
                    }
                }
                String CRC_Hex = crc.ToString("X4");//A84
                CRC = CRC + " " + CRC_Hex.Substring(2, 2) + " " + CRC_Hex.Substring(0, 2);
                return CRC;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        #endregion

        #region 計算完整Modbus指令
        /// <summary>
        /// 輸入除站號以及檢查碼以外的指令，並計算完整Modbus指令(請確認除站號以及檢查碼以外的指令是否正確)
        /// </summary>
        /// <param name="address">I/O站號</param>
        /// <param name="command">無站號和檢查碼的原始指令</param>
        private string GetModbusCommand(string address, string command)
        {
            return Convert.ToInt32(address).ToString("00") + " " + command;
        }
        #endregion

        public String String_SQLcommand = "";

        public String String_SQLcommand_old = "";

        public String String_SQLcommand_values = "";

        public String[] String_ReData = new String[] { };

        public string test_str = "";

        //public String[] String_SeData13 = { "", "02 03 02 4C 00 04 84 55" };

        //public String[] String_SeData13 = { "", "01 03 02 4C 00 04 84 66" };

        // 不含開頭一碼站號，以及結尾兩碼檢查碼
        public String[] String_SeData13 = { "", "03 02 4C 00 04" };

        public String[] String_SeData = { "", "00 00 00 00 00 00 01 03 00 00 00 02" };

        public String[] String_SeData05 = { "", "01 03 00 00 00 08 44 0C" };

        byte[] Byte_Command_Sent = new byte[100];

        byte[] Byte_Command_Re = new byte[100];

        //各項感測器工作對應模組

        //step1：set步驟，可能要傳碼，或者不用傳

        //step2：等候回應，

        //step3：解碼

        //感測器類別編號相對應函數庫
        //1 環境溫濕度 2條碼 3燈號 4溫度 5空壓
        //
        string str_values_temp = "";

        //1 環境溫濕度
        /// <summary>
        /// 環境溫濕度
        /// </summary>
        void function_01()
        {
            try
            {
                for (int i = 0; i <= String_SeData[1].Split(' ').Length - 1; i++)
                {
                    Byte_Command_Sent[i] = Convert.ToByte(String_SeData[1].Split(' ')[i], 16);
                }
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, String_SeData[1].Split(' ').Length);
                for (int i = 0; i <= 50; i++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                if (int_Net_Available == 13)
                {
                    string str_T = Convert.ToInt32(String.Format("{0:X2}", Byte_Command_Re[9]) + String.Format("{0:X2}", Byte_Command_Re[10]), 16).ToString().PadLeft(4, '0'); ;

                    string str_H = Convert.ToInt32(String.Format("{0:X2}", Byte_Command_Re[11]) + String.Format("{0:X2}", Byte_Command_Re[12]), 16).ToString().PadLeft(4, '0'); ;

                    // str_T += str_H;

                    if (str_values_temp != str_T + str_H)
                    {
                        String_SQLcommand = "INSERT INTO [dbo].[tb_recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[0].ToString() + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "') ";

                        String_SQLcommand += ", ('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[1].ToString() + "','" + str_H + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";

                        str_values_temp = str_T + str_H;
                    }
                    else
                    {
                        String_SQLcommand = "";
                    }

                    String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "..." + str_T + ":" + str_H + String_Sclass;
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "...Ex:" + EX.Source;

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }

        //2條碼 
        void function_02()
        {
            try {
                int int_Net_Available = TcpClient_Reader.Available;
                //有條碼時才動作。

                //判斷是否有資料以及是否可以讀取，並且此連結資料通道可以抓取資料時才能動作。

                if (int_Net_Available > 0 && NetworkStream_Reader.CanRead == true)
                {
                    string str_T = "";

                    //抓取資料之後轉為字元
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    for (int j = 0; j <= int_Net_Available - 2; j++)
                    {
                        str_T += Convert.ToChar(Byte_Command_Re[j]);
                    }

                    String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;

                    //
                    if (str_T.Length > 16)
                    {
                        str_T = str_T.Substring(0, 16);
                    }

                    //如果非空字串的話
                    if (str_T != "")
                    {
                        if (String_Dline == "P2")
                        {
                            String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog] ([DID]           ,[DIP]           ,[SID]           ,[DVALUE]           ,[SYSTIME])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "') ; ";
                        }
                        if (String_Dline == "P3")
                        {
                            String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog] ([DID]           ,[DIP]           ,[SID]           ,[DVALUE]           ,[SYSTIME])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "') ;";
                        }

                        //171119 

                        String light_Str;

                        light_Str = "0100";

                        String Str_SQL_sid_X_WLS = "SELECT S_ID FROM [dbMES].[dbo].[tb_sensors_rules] WHERE d_ID =  (SELECT d_ID FROM   [dbMES].[dbo].[tb_sensors_rules]  WHERE s_ID = '" + String_SID + "') AND S_ID LIKE '%WLS%'";

                        if (String_Dline == "P2")
                        {
                            String_SQLcommand += "INSERT INTO [dbo].[tb_P2recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "',(" + Str_SQL_sid_X_WLS + "),'" + light_Str + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ;";
                        }
                        else if (String_Dline == "P3")
                        {
                            String_SQLcommand += "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "',(" + Str_SQL_sid_X_WLS + "),'" + light_Str + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ;";
                        }

                        //

                        str_Barcode = str_T;

                        //str_BarcodeLight = str_Barcode;
                        MPU.str_Barcode = str_Barcode;

                        str_Barcode = "";

                    }
                    else
                    {
                        String_SQLcommand = "";
                    }
                }
                else
                {
                    //沒有資料仍要提示訊息
                    String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "...Available[" + int_Net_Available + "]:[" + NetworkStream_Reader.CanRead.ToString() + "]@無資料:" + String_Sclass;

                    //沒有資料的時候丟個資料過去不要讓他斷線

                    byte[] byte_Command_wlslcs = new byte[5];

                    //byte_Command_wlslcs[0] = 48;
                    //byte_Command_wlslcs[1] = 56;
                    //byte_Command_wlslcs[2] = 48;
                    //byte_Command_wlslcs[3] = 10;
                    //byte_Command_wlslcs[4] = 13;

                    //NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);

                    //INSERT 一筆
                    if (booleanInsert == true)
                    {

                        // String_SQLcommand = "INSERT INTO [dbo].[tb_recordslogTEST] ([DID]           ,[DIP]           ,[SID]           ,[DVALUE]           ,[SYSTIME])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','RUN-161103756-00','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "') ";

                    }

                    booleanInsert = false;
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "...Ex:" + EX.Source;

                    Console.WriteLine("M0375:Exception source: {0}", EX.Source + EX.Message);
                }
            }
        }

        Boolean booleanInsert = true;

        int byte_function_03_loop_index = 0;

        string str_ttt = "";

        //記錄上次燈號是否為閃爍用

        string Str_lcs = "";

        //記錄insert過的燈號，重覆的話不傳
        string Str_WLSLCS_F = "";

        int int_WLSLCS_Count = 0;

        //
        void SetOrangeLight()
        {
            byte[] byte_Command_wlslcs = new byte[5];

            //byte_Command_wlslcs[0] = 48;
            //byte_Command_wlslcs[1] = 54;
            //byte_Command_wlslcs[2] = 49;
            //byte_Command_wlslcs[3] = 10;
            //byte_Command_wlslcs[4] = 13;

            byte_Command_wlslcs[0] = Convert.ToByte("30", 16);
            byte_Command_wlslcs[1] = Convert.ToByte("38", 16);
            byte_Command_wlslcs[2] = Convert.ToByte("31", 16);
            byte_Command_wlslcs[3] = Convert.ToByte("0D", 16);
            byte_Command_wlslcs[4] = Convert.ToByte("0A", 16);
            NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);

            //byte_Command_wlslcs[0] = 48;
            //byte_Command_wlslcs[1] = 53;
            //byte_Command_wlslcs[2] = 49;
            //byte_Command_wlslcs[3] = 10;
            //byte_Command_wlslcs[4] = 13;
            //NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);

            //byte_Command_wlslcs[0] = 48;
            //byte_Command_wlslcs[1] = 52;
            //byte_Command_wlslcs[2] = 48;
            //byte_Command_wlslcs[3] = 10;
            //byte_Command_wlslcs[4] = 13;
            //NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);

            //byte_Command_wlslcs[0] = 48;
            //byte_Command_wlslcs[1] = 51;
            //byte_Command_wlslcs[2] = 48;
            //byte_Command_wlslcs[3] = 10;
            //byte_Command_wlslcs[4] = 13;
            //NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);
        }

        //3燈號函數 
        void function_WLS_LCS()
        {
            try

            {
                //17122601
                str_BarcodeLight = "00000000";
                if (str_BarcodeLight != "" && String_SID.Substring(0, 3) == "WLS")
                {
                    byte[] byte_Command_wlslcs = new byte[5];

                    if (str_BarcodeLight == "00000000")
                    {
                        //傳送橘燈訊號
                        SetOrangeLight();
                    }
                    else
                    {
                        //16進位
                        byte_Command_wlslcs[0] = 48;
                        byte_Command_wlslcs[1] = 54;
                        byte_Command_wlslcs[2] = 49;
                        byte_Command_wlslcs[3] = 10;
                        byte_Command_wlslcs[4] = 13;
                        NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);

                        byte_Command_wlslcs[0] = 48;
                        byte_Command_wlslcs[1] = 53;
                        byte_Command_wlslcs[2] = 48;
                        byte_Command_wlslcs[3] = 10;
                        byte_Command_wlslcs[4] = 13;
                        NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);

                        byte_Command_wlslcs[0] = 48;
                        byte_Command_wlslcs[1] = 52;
                        byte_Command_wlslcs[2] = 49;
                        byte_Command_wlslcs[3] = 10;
                        byte_Command_wlslcs[4] = 13;
                        NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);

                        byte_Command_wlslcs[0] = 48;
                        byte_Command_wlslcs[1] = 51;
                        byte_Command_wlslcs[2] = 48;
                        byte_Command_wlslcs[3] = 10;
                        byte_Command_wlslcs[4] = 13;
                        NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);
                    }

                    str_BarcodeLight = "";
                }

                //不會一直回傳燈號，只會傳一次，只會在有變動產生回傳，這樣子會有潛藏性的BUG。
                //所有在傳遞一定次數之後，必須重新連線一次。
                //在燈號的運作中，如果n分鐘沒有資料回傳的話，就重來一次連結，產生目前狀態

                int_WLSLCS_Count += 1;

                if (int_WLSLCS_Count > 2500)
                {
                    int_WLSLCS_Count = 0;

                    Str_WLSLCS_F = "";
                }

                //200227 當燈號計數器歸零時，送一個橘燈訊號, 必須是自製燈號的
                //
                if (int_WLSLCS_Count == 0 && String_SID.Substring(0, 3) == "WLS")
                {
                    SetOrangeLight();
                }

                //取得目前網路盒的訊號資料量大小
                int int_Net_Available = TcpClient_Reader.Available;

                //判斷是否有資料以及是否可以讀取。
                if (int_Net_Available >= 10 && NetworkStream_Reader.CanRead == true)
                {
                    byte_function_03_loop_index = 0;

                    string str_T = "";

                    NetworkStream_Reader.Read(Byte_Command_Re, 0, 10);

                    for (int j = 0; j <= 10 - 2; j++)
                    {
                        str_T += Convert.ToChar(Byte_Command_Re[j]);
                    }

                    //和之前的燈號一樣的話，就不用做處理了。
                    if (Str_WLSLCS_F != str_T)
                    {
                        //記錄這一次的燈號
                        Str_WLSLCS_F = str_T;

                        //判斷是何種燈號解析，WLS為自訂義燈號，LCS為燈號解析模組。
                        //WLS 00BGYR00 RGYW
                        if (str_T.Length > 8 && String_SID.Substring(0, 3) == "WLS")
                        {
                            //17122601 

                            if (String_SID == "WLS002") //水洗機的燈號模組其中一組訊號拿來做段數開關的偵測。
                            {
                                if (str_T.Substring(2, 1) == "1")
                                {
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','SWD001','4','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "'); ";
                                }
                                else
                                {
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','SWD001','0','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "'); ";
                                }
                            }

                            //因為紅燈為反相，所以需要把紅燈的顯示相反過來。
                            if (str_T.Substring(5, 1) == "1")
                            {
                                str_T = "0" + str_T.Substring(3, 1) + str_T.Substring(4, 1) + str_T.Substring(2, 1);
                            }
                            else
                            {
                                str_T = "1" + str_T.Substring(3, 1) + str_T.Substring(4, 1) + str_T.Substring(2, 1);
                            }
                        }
                        else if (str_T.Length > 8 && String_SID.Substring(0, 3) == "LCS")
                        {
                            //LCS 00RWYG00 RGYW
                            //LCS 如果有0001 純白燈的話 => 回傳黃燈 0011
                            //LCS 如果有0000 全滅的話 => 回傳黃燈 0010
                            str_T = str_T.Substring(2, 1) + str_T.Substring(5, 1) + str_T.Substring(4, 1) + str_T.Substring(3, 1);
                            //LCS001 LCS002 燈號不同，為特殊型。
                            //當為1100時，判斷為綠燈(0100)，當為0011時，判斷為黃燈(0010)，其他狀況時為紅燈(1000)
                            if (String_SID == "LCS001" || String_SID == "LCS002")
                            {
                                Str_lcs = str_T;
                                str_T = "1000";
                                if (Str_lcs == "1100")
                                {
                                    str_T = "0100";
                                }
                                else if (Str_lcs == "0011")
                                {
                                    str_T = "0010";
                                }
                            }
                            else
                            {
                                Str_lcs = str_T;

                                if (Str_lcs == "0001")
                                {
                                    str_T = "0011";
                                }
                                else if (Str_lcs == "0000")
                                {
                                    str_T = "0010";
                                }
                                if (Str_lcs == "1100" || Str_lcs == "1110" || Str_lcs == "1101" || Str_lcs == "1111")
                                {
                                    str_T = "0100";
                                }
                            }
                        }

                        //燈號結果處理完畢
                        str_ttt = str_T;

                        String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;

                        //有資料時再處理
                        if (str_T != "" && String_SID != "WLS002")
                        {
                            //依製二製三放置不同資料表。
                            //20161230 insert會一直加下去，所以insert指令會變很大。稍微修改一下，變成只有一筆insert into
                            //如果String_SQLcommand 不是空字串的話，直接加入values的值到字串尾巴即可。
                            if (String_SQLcommand == "")
                            {
                                if (String_Dline == "P2")
                                {
                                    String_SQLcommand += "INSERT INTO [dbo].[tb_P2recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                                }
                                else if (String_Dline == "P3")
                                {
                                    String_SQLcommand += "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                                }
                            }
                            else
                            {
                                String_SQLcommand += ", ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                            }
                        }
                        else if (String_SID == "WLS002")
                        {
                            if (String_Dline == "P2")
                            {
                                String_SQLcommand += "INSERT INTO [dbo].[tb_P2recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";

                            }
                            else if (String_Dline == "P3")
                            {
                                String_SQLcommand += "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                            }
                        }
                        else
                        {
                            String_SQLcommand = "";
                        }
                    }
                }
                else
                {
                    //當沒有資料的時候，利用發送指令取得回傳值

                    byte[] byte_Command_wlslcs = new byte[5];

                    byte_Command_wlslcs[0] = 48;
                    byte_Command_wlslcs[1] = 56;
                    byte_Command_wlslcs[2] = 48;
                    byte_Command_wlslcs[3] = 10;
                    byte_Command_wlslcs[4] = 13;

                    NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);

                    for (int i = 0; i <= 50; i++)
                    {
                        System.Threading.Thread.Sleep(5);

                        System.Windows.Forms.Application.DoEvents();
                    }

                    int int_Net_Availablexxx = TcpClient_Reader.Available;

                    //如果真的都沒有值回傳的話，再準備進行資料重連的動作。
                    if (int_Net_Availablexxx == 0)
                    {
                        byte_function_03_loop_index += 1;

                        String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "...<468>" + int_Net_Available + "@" + str_ttt + "，未解碼次數(" + byte_function_03_loop_index + "):" + String_Sclass;

                        //當燈號解析超出10次沒有資料的時候，重新連線一次。
                        if (byte_function_03_loop_index > 50)
                        {
                            //161208 加入一個共有變數，同一時間只能一個tcpip連線
                            if (MPU.TOSrun == false)
                            {
                                MPU.TOSrun = true;

                                byte_function_03_loop_index = 0;

                                str_ttt = "";

                                NetworkStream_Reader.Close();

                                TcpClient_Reader.Close();

                                //NetworkStream_Reader = null;

                                //TcpClient_Reader = null;

                                for (int int_t = 0; int_t < 100; int_t++)
                                {
                                    System.Windows.Forms.Application.DoEvents();

                                    System.Threading.Thread.Sleep(5);
                                }

                                //重新建立連結
                                TcpClientConnect();

                                MPU.TOSrun = false;
                            }
                        }
                    }
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "...[" + byte_function_03_loop_index + "]Ex:" + EX.Source + EX.Message;

                    Console.WriteLine("M0516:燈號異常:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss[{0}]"), EX.Source + EX.Message);

                    byte_function_03_loop_index += 1;
                }
            }
        }

        public static byte[] crc16(byte[] data, byte dataSize)
        {
            if (dataSize == 0)
                throw new Exception("Exception");
            byte[] temdata = new byte[dataSize + 2];
            int xda, xdapoly;
            int i, j, xdabit;
            xda = 0xFFFF;
            xdapoly = 0xA001;
            for (i = 0; i < dataSize; i++)
            {
                xda ^= data[i];
                for (j = 0; j < 8; j++)
                {
                    xdabit = (int)(xda & 0x01);
                    xda >>= 1;
                    if (xdabit == 1)
                        xda ^= xdapoly;
                }
            }
            temdata = new byte[2] { (byte)(xda & 0xFF), (byte)(xda >> 8) };
            return temdata;
        }

        byte[] temCRC = new byte[2];

        string[] str_DTS_temp = new string[8];

        int int_DTS_temp_count = 0;

        //4溫度 
        void function_DTS()
        {
            try
            {
                string str_T = "";

                int_DTS_temp_count += 1;

                for (int i = 0; i <= String_SeData05[1].Split(' ').Length - 1; i++)
                {
                    Byte_Command_Sent[i] = Convert.ToByte(String_SeData05[1].Split(' ')[i], 16);
                }

                //發送指令前先清完記憶體

                NetworkStream_Reader.Write(Byte_Command_Sent, 0, String_SeData05[1].Split(' ').Length);

                for (int i = 0; i <= 50; i++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                // NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                //按照位置抓取資料，空壓的都採電壓式， 
                //例位置1 ，抓 第4+第5 加總為16進製 4字元， 轉換成 10進製後得 x
                // X 乘以 10 之後 再除以 4095， 取小數點後2位

                String str_hex = "";

                double double_DCvi = 0;

                if (int_Net_Available > 20)
                {
                    //一次只取21筆
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, 21);

                    //判斷是否符合CRC
                    temCRC = crc16(Byte_Command_Re, 19);

                    //相符的話才能執行
                    if (temCRC[0] == Byte_Command_Re[19] && temCRC[1] == Byte_Command_Re[20])

                    {
                        str_hex = "";

                        String_SQLcommand = "";

                        //先了解此機台有幾組溫度
                        int int_X = Convert.ToInt16(Str_Portid.Split(';')[0]);

                        for (int xx = 0; xx < int_X; xx++)
                        {
                            //按照設定值抓取溫度
                            int int_GetPortid = Convert.ToInt16(Str_Portid.Split(';')[1].Split('.')[xx]);

                            //轉換成16進制文字
                            str_hex = Convert.ToString(Byte_Command_Re[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[1 + ((int_GetPortid * 2) + 1)], 16).PadLeft(2, '0');

                            int int_dec = 0;

                            int_dec = Convert.ToInt16(str_hex, 16);

                            double_DCvi = Convert.ToDouble(int_dec) * 10 / 4095;

                            double double_Mpa = 0;

                            //DTS016
                            //0~250
                            //0.039840637

                            //if (String_SID.Split('.')[xx] == "DTS016" || String_SID.Split('.')[xx] == "DTS008" || String_SID.Split('.')[xx] == "DTS009")
                            if (String_SID.Split('.')[xx] != "DTS010" &&
                                String_SID.Split('.')[xx] != "DTS011" &&
                                String_SID.Split('.')[xx] != "DTS012" &&
                                String_SID.Split('.')[xx] != "DTS013" &&
                                String_SID.Split('.')[xx] != "DTS014" &&
                                String_SID.Split('.')[xx] != "DTS005")

                            {//-43
                                //double_Mpa = double_DCvi / 0.039840637 + Convert.ToDouble(String_Sclass.Split(';')[1]);
                                //if (String_SID.Split('.')[xx] == "DTS008")
                                //{
                                ///    double_Mpa = double_DCvi / 0.0194 + Convert.ToDouble(String_Sclass.Split(';')[1]);
                                //}
                                //else
                                //{
                                if (String_SID.Split('.')[xx] == "KPS022" || String_SID.Split('.')[xx] == "KPS023")
                                {
                                    double_Mpa = double_DCvi;

                                    str_DTS_temp[xx] = "";

                                    int_DTS_temp_count = 0;
                                }
                                else
                                {
                                    double_Mpa = double_DCvi / 0.02 + Convert.ToDouble(String_Sclass.Split(';')[1]) - 99;
                                }
                                //}
                            }
                            else
                            {
                                //double_Mpa = double_DCvi / 0.014306152 + Convert.ToDouble(String_Sclass.Split(';')[1]);

                                double_Mpa = double_DCvi * 156.5 - 200;
                            }

                            // DTS001 DTS001,溫度,銀膠冰箱

                            if (String_SID.Split('.')[xx] == "DTS001")
                            {
                                //DTS001 銀膠冰箱， 值為 VI / 0.1776 - 99 
                                //170215 更換參數為 -0.388394674 
                                //double_Mpa = (double_DCvi - 0.53) / -0.2 + Convert.ToDouble(String_Sclass.Split(';')[1]);
                                double_Mpa = double_DCvi / -0.388394674 + Convert.ToDouble(String_Sclass.Split(';')[1]);

                                str_T += "(" + int_GetPortid + ")[" + double_DCvi + "]" + double_Mpa.ToString("F3");
                            }
                            else
                            {
                                str_T += "[" + str_hex.ToUpper() + "], (" + int_GetPortid + ")[" + double_DCvi + "]" + double_Mpa.ToString("F3");
                            }

                            if (String_SQLcommand == "" && str_DTS_temp[xx] != double_Mpa.ToString("F3") && String_Dline == "P2")
                            {
                                String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                            }
                            if (String_SQLcommand == "" && str_DTS_temp[xx] != double_Mpa.ToString("F3") && String_Dline == "P3")
                            {
                                String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                            }

                            if (String_SQLcommand != "" && str_DTS_temp[xx] != double_Mpa.ToString("F3") && xx != 0)
                            {
                                String_SQLcommand += ",";
                            }

                            //降低精準度至小數點1位
                            if (str_DTS_temp[xx] != double_Mpa.ToString("F1"))
                            {
                                str_DTS_temp[xx] = double_Mpa.ToString("F1");

                                String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + double_Mpa.ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                            }

                            if (int_DTS_temp_count > 200)
                            {
                                str_DTS_temp[xx] = "";

                                int_DTS_temp_count = 0;
                            }
                        }

                        String_SQLcommand += ";";
                    }
                }

                if (double_DCvi == 0 || double_DCvi == 10)
                {
                    String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + "vi資料異常:" + String_Sclass;

                    //資料異常就不要進資料庫 
                    String_SQLcommand = "";
                }
                else
                {
                    String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    Console.WriteLine("M0733:Exception source: {0}", EX.Source);

                    String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "...[" + byte_function_03_loop_index + "]Ex:" + EX.Source + EX.Message;
                }
            }

        }

        string[] str_KPS_temp = new string[8];

        //5空壓
        //DC 解碼 01 03 00 00 00 08 44 0C
        //2.24V~2.25V = 0.352 2.23~2.22 = 0.351 
        void function_KPS()
        {
            try

            {
                string str_T = "";

                for (int i = 0; i <= String_SeData05[1].Split(' ').Length - 1; i++)
                {
                    Byte_Command_Sent[i] = Convert.ToByte(String_SeData05[1].Split(' ')[i], 16);
                }

                //發送讀取指令。
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, String_SeData05[1].Split(' ').Length);

                for (int i = 0; i <= 50; i++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                //取得回傳大小。
                int int_Net_Available = TcpClient_Reader.Available;

                // NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                //按照位置抓取資料，空壓的都採電壓式， 
                //例位置1 ，抓 第4+第5 加總為16進製 4字元， 轉換成 10進製後得 x
                // X 乘以 10 之後 再除以 4095， 取小數點後2位

                String str_hex = "";

                //至少需要21碼才做解碼動作
                if (int_Net_Available >= 21)
                {
                    String_SQLcommand = "";
                    //一次只取21筆
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, 21);

                    //判斷是否符合CRC
                    temCRC = crc16(Byte_Command_Re, 19);

                    //檢查回傳碼是否相符的話才能執行
                    if (temCRC[0] == Byte_Command_Re[19] && temCRC[1] == Byte_Command_Re[20])

                    {
                        str_hex = "";

                        //先了解此機台有幾組空壓

                        int int_X = Convert.ToInt16(Str_Portid.Split(';')[0]);

                        String_SQLcommand_values = "";

                        for (int xx = 0; xx < int_X; xx++)
                        {
                            //按照設定值抓取空壓
                            int int_GetPortid = Convert.ToInt16(Str_Portid.Split(';')[1].Split('.')[xx]);

                            //轉換成16進制文字
                            str_hex = Convert.ToString(Byte_Command_Re[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[1 + ((int_GetPortid * 2) + 1)], 16).PadLeft(2, '0');
                            //String.Format("{0:X4}", iTemp)
                            int int_dec = 0;

                            int_dec = Convert.ToInt16(str_hex, 16);

                            double double_DCvi = 0;

                            double_DCvi = Convert.ToDouble(int_dec) * 10 / 4095;

                            double double_Mpa = 0;

                            double_Mpa = (double_DCvi - 1) / 4 + 0.0512503;

                            if (String_SID.Split('.')[xx] == "KPS004" || String_SID.Split('.')[xx] == "KPS006" || String_SID.Split('.')[xx] == "KPS021" || String_SID.Split('.')[xx] == "KPS019")
                            {
                                if ((double_Mpa * 100) < 0)
                                {
                                    if (String_SID.Split('.')[xx] == "KPS004")  //-26
                                    {
                                        str_T += "(" + int_GetPortid + ")" + (double_Mpa * 100 + 26).ToString("F1");
                                    }
                                    else if (String_SID.Split('.')[xx] == "KPS019") //-20
                                    {
                                        str_T += "(" + int_GetPortid + ")" + (double_Mpa * 100 + 20).ToString("F1");
                                    }
                                    else
                                    {
                                        str_T += "(" + int_GetPortid + ")" + (double_Mpa * 100).ToString("F1");
                                    }
                                }
                                else
                                {
                                    if (String_SID.Split('.')[xx] == "KPS004")  //-26
                                    {
                                        str_T += "(" + int_GetPortid + ")-" + (double_Mpa * 100 + 25.9).ToString("F1");
                                    }
                                    else if (String_SID.Split('.')[xx] == "KPS019") //-20
                                    {
                                        str_T += "(" + int_GetPortid + ")-" + (double_Mpa * 100 + 20.2).ToString("F1");
                                    }
                                    else
                                    {
                                        str_T += "(" + int_GetPortid + ")-" + (double_Mpa * 100).ToString("F1");
                                    }
                                }
                            }
                            else if (String_SID.Split('.')[xx] == "GRA001" || String_SID.Split('.')[xx] == "GRA002")
                            {
                                str_T += "(" + int_GetPortid + ")" + double_DCvi.ToString("F3") + "@" + str_hex;
                            }
                            else
                            {
                                if ((double_Mpa * 100) < 0)
                                {
                                    if (String_SID.Split('.')[xx] == "KPS005")
                                    {
                                        str_T += "(" + int_GetPortid + ")-" + (double_Mpa + 0.18).ToString("F1");
                                    }
                                    else if (String_SID.Split('.')[xx] == "KPS018")
                                    {
                                        str_T += "(" + int_GetPortid + ")-" + (double_Mpa + 0.23).ToString("F1");
                                    }
                                    else
                                    {
                                        str_T += "(" + int_GetPortid + ")-" + (double_Mpa).ToString("F1");
                                    }
                                }
                                else
                                {
                                    if (String_SID.Split('.')[xx] == "KPS005")
                                    {
                                        str_T += "(" + int_GetPortid + ")" + (double_Mpa + 0.166).ToString("F3");
                                    }
                                    else if (String_SID.Split('.')[xx] == "KPS018")
                                    {
                                        str_T += "(" + int_GetPortid + ")" + (double_Mpa + 0.1765).ToString("F3");
                                    }
                                    else
                                    {
                                        str_T += "(" + int_GetPortid + ")" + (double_Mpa * 100).ToString("F1");
                                    }
                                }

                                //str_T += "(" + int_GetPortid + ")" + double_Mpa.ToString("F3");
                            }

                            //開頭先加上insert 指令起頭
                            //降低精準度至小數點後1位。
                            if (String_SQLcommand == "" && str_KPS_temp[xx] != (double_Mpa * 100).ToString("F1"))
                            {
                                //str_KPS_temp[xx] = (double_Mpa * 100).ToString("F1");
                                if (String_Dline == "P2")
                                {
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                                }
                                if (String_Dline == "P3")
                                {
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                                }
                            }

                            //跟上一筆的不一樣的才儲存
                            if (str_KPS_temp[xx] != (double_Mpa * 100).ToString("F1"))
                            {
                                str_KPS_temp[xx] = (double_Mpa * 100).ToString("F1");
                                //161208 因為發現有空values的狀況，加上一個String_SQLcommand_values來暫存是否有資料填入，來做為是否加入[ , ]的區隔符號。
                                if (String_SQLcommand != "" && xx != 0 && String_SQLcommand_values != "")
                                {
                                    String_SQLcommand += ",";
                                }
                                else
                                {
                                    if (String_TID == "2048-001")
                                    {
                                        String_SQLcommand += "";
                                    }
                                }

                                String_SQLcommand_values = "";

                                str_KPS_temp[xx] = (double_Mpa * 100).ToString("F1");

                                if (String_SID.Split('.')[xx] == "KPS004" || String_SID.Split('.')[xx] == "KPS006" || String_SID.Split('.')[xx] == "KPS021" || String_SID.Split('.')[xx] == "KPS019")
                                {
                                    if ((double_Mpa * 100) < 0)
                                    {
                                        if (String_SID.Split('.')[xx] == "KPS004")  //-26
                                        {
                                            String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + (double_Mpa * 100 - 26).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";

                                            String_SQLcommand_values = "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + (double_Mpa * 100 - 26).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";
                                        }
                                        else if (String_SID.Split('.')[xx] == "KPS019") //-20
                                        {
                                            String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + (double_Mpa * 100 - 20).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";

                                            String_SQLcommand_values = "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + (double_Mpa * 100 - 20).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";
                                        }
                                        else
                                        {
                                            String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + (double_Mpa * 100).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";

                                            String_SQLcommand_values = "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + (double_Mpa * 100).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";
                                        }
                                    }
                                    else
                                    {
                                        if (String_SID.Split('.')[xx] == "KPS004") //-26
                                        {
                                            String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','-" + (double_Mpa * 100 + 25.9).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";

                                            String_SQLcommand_values = "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','-" + (double_Mpa * 100 + 25.9).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";

                                        }
                                        else if (String_SID.Split('.')[xx] == "KPS019") //-20
                                        {
                                            String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','-" + (double_Mpa * 100 + 20.2).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";

                                            String_SQLcommand_values = "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','-" + (double_Mpa * 100 + 20.2).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";
                                        }
                                        else
                                        {
                                            String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','-" + (double_Mpa * 100).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";

                                            String_SQLcommand_values = "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','-" + (double_Mpa * 100).ToString("F1") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";
                                        }
                                    }
                                }
                                else if (String_SID.Split('.')[xx] == "GRA001" || String_SID.Split('.')[xx] == "GRA002")
                                {
                                    //str_T += "(" + int_GetPortid + ")-" + double_DCvi.ToString("F1");

                                    String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + double_DCvi.ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";

                                    String_SQLcommand_values = "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + double_DCvi.ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";
                                }
                                else
                                {
                                    //////

                                    if (String_SID.Split('.')[xx] == "KPS005")
                                    {
                                        String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + (double_Mpa + 0.166).ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";

                                        String_SQLcommand_values = "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + (double_Mpa + 0.166).ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";
                                    }
                                    else if (String_SID.Split('.')[xx] == "KPS018")
                                    {
                                        String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + (double_Mpa + 0.1765).ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";

                                        String_SQLcommand_values = "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + (double_Mpa + 0.1765).ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";
                                    }
                                    else
                                    {
                                        String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + double_Mpa.ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";

                                        String_SQLcommand_values = "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + double_Mpa.ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "[" + str_hex + "]') ";
                                    }
                                }
                            }
                        }

                        if (String_SQLcommand != "")
                            String_SQLcommand += ";";
                    }
                }

                String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                    Console.WriteLine("M0930:Exception source: {0}", EX.Source + "[" + EX.Message + "]");
            }

        }

        //6水阻值 採用 ma解碼
        //DC 解碼 01 03 00 00 00 08 44 0C
        //2.24V~2.25V = 0.352 2.23~2.22 = 0.351 
        void function_06()
        {
            try
            {
                Random rnd = new Random();

                string str_T = "";

                for (int i = 0; i <= String_SeData05[1].Split(' ').Length - 1; i++)
                {
                    Byte_Command_Sent[i] = Convert.ToByte(String_SeData05[1].Split(' ')[i], 16);
                }

                NetworkStream_Reader.Write(Byte_Command_Sent, 0, String_SeData05[1].Split(' ').Length);

                for (int i = 0; i <= 50; i++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                String str_hex = "";

                if (int_Net_Available > 20)
                {
                    if (String_Dline == "P2")
                    {
                        String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                    }
                    if (String_Dline == "P3")
                    {
                        String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                    }

                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    str_hex = "";

                    //先了解此機台有幾組感測器

                    int int_X = Convert.ToInt16(Str_Portid.Split(';')[0]);

                    for (int xx = 0; xx < int_X; xx++)
                    {
                        //按照埠設定值抓取基本資料
                        int int_GetPortid = Convert.ToInt16(Str_Portid.Split(';')[1].Split('.')[xx]);

                        //轉換成16進制文字
                        str_hex = Convert.ToString(Byte_Command_Re[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[1 + ((int_GetPortid * 2) + 1)], 16).PadLeft(2, '0');

                        int int_dec = 0;

                        int_dec = Convert.ToInt16(str_hex, 16);

                        double double_DCma = 0;

                        double_DCma = Convert.ToDouble(int_dec) * 20 / 4095;

                        double double_Mpa = 0;

                        if (xx != 0)
                        {
                            String_SQLcommand += ",";
                        }

                        //溫度的兩台不同
                        if (String_SID.Split('.')[xx] == "DTS017" || String_SID.Split('.')[xx] == "DTS018")
                        {
                            double_Mpa = double_DCma / 0.0599018347362718 - 100;

                            if (String_SID.Split('.')[xx] == "DTS018")
                            {
                                //double_Mpa += (rnd.NextDouble() * (0.78 - 0.12) + 1);
                                double_Mpa = double_DCma / 0.0599018347362718 - 100;

                            }

                            str_T += "(" + int_GetPortid + ")" + (double_Mpa).ToString("F3");

                            String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + double_Mpa.ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        }
                        else
                        { //M001 //M002 //M003 
                            //4.5  0.61
                            //5.8  2.23
                            //5.9  2.41
                            //6    2.48

                            //ai_1	5.94	2.41	2.46473029
                            //ai_1	6	    2.48	2.419354839
                            //ai_2	17.48	17.45	1.001719198
                            //ai_2	18.23	18.2	1.001648352
                            //ai_2	18.25	18.21	1.002196595
                            //ai_2	18.17	18.15	1.001101928
                            //ai_3	7.91	4.91	1.610997963
                            //ai_3	7.86	4.86	1.617283951

                            if (String_SID.Split('.')[xx] == "M001")
                            {
                                //double_Mpa = double_DCma / 4.623524623524623;
                                double_Mpa = (double_DCma - 4) / 0.806;
                            }
                            else if (String_SID.Split('.')[xx] == "M002")
                            {
                                double_Mpa = (double_DCma - 4) / 0.78;
                            }
                            else if (String_SID.Split('.')[xx] == "M003")
                            {
                                double_Mpa = (double_DCma - 4) / 1.256;
                            }

                            str_T += "(" + int_GetPortid + ")" + double_Mpa.ToString("F3");

                            String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + double_Mpa.ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        }



                    }

                    String_SQLcommand += ";";
                }

                String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;

                if (int_Net_Available == 13)
                {
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                    Console.WriteLine("M1137:Exception source: {0}", EX.Source);
            }

        }

        //7 暫時套用 
        void function_07()
        {

        }

        byte[] Byte_Command_Sent_DTS8 = new byte[100];

        //記錄是否有填入values了
        Boolean bool_add_Values = false;

        //8 溫度4通道模組解碼
        void function_DTS_8()
        {
            for (int i = 0; i <= 10; i++)
            {
                System.Threading.Thread.Sleep(10);

                System.Windows.Forms.Application.DoEvents();
            }

            try
            {
                string str_T = "";

                int_DTS_temp_count += 1;

                //設備ID(slave address/ID) + 0x03 + 讀取起始位置(word) + 讀取的數量(word) + CRC16
                //"01 03 00 00 00 08 44 0C"

                // 02 03 02 4C 00 04 84 55 讀取第1台 4通道 ID02
                // 03 03 02 4C 00 04 85 84 讀取第2台 4通道 ID03 
                //StrArrayToByteArray("02 03 02 4C 00 04 84 55", Byte_Command_Sent_082);
                //StrArrayToByteArray("03 03 02 4C 00 04 85 84", Byte_Command_Sent_083);

                //發送指令前先清完記憶體
                if (TcpClient_Reader.Available > 0)
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, TcpClient_Reader.Available);

                NetworkStream_Reader.Write(Byte_Command_Sent_082, 0, 8);

                //清空讀取記憶體
                for (int i = 0; i <= 50; i++)
                {
                    Byte_Command_Re[i] = 0;
                }

                //休息約 4秒
                for (int i = 0; i <= 400; i++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                //判斷是否有網路資料
                int int_Net_Available = TcpClient_Reader.Available;

                String str_hex = "";

                //有資料大於13個位元組的再處理。
                if (int_Net_Available >= 13)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);
                    //判斷是否符合CRC
                    temCRC = crc16(Byte_Command_Re, 11);
                    //相符CRC的話才能執行  //20200311 加上判斷開頭 為 02 03

                    // 02 03 02 4C 00 04 84 55 讀取第1台 4通道 ID02
                    // 03 03 02 4C 00 04 85 84 讀取第2台 4通道 ID03 
                    if (temCRC[0] == Byte_Command_Re[11] && temCRC[1] == Byte_Command_Re[12] && Byte_Command_Re[0] == 2 && Byte_Command_Re[1] == 3)
                    {
                        str_hex = "";
                        String_SQLcommand = "";

                        //先做4台
                        int int_X = 4;
                        for (int xx = 0; xx < int_X; xx++)
                        {
                            //按照設定值抓取溫度
                            int int_GetPortid = Convert.ToInt16(Str_Portid.Split(';')[1].Split('.')[xx]);

                            //轉換成16進制文字
                            str_hex = Convert.ToString(Byte_Command_Re[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[1 + ((int_GetPortid * 2) + 1)], 16).PadLeft(2, '0');

                            int int_dec = 0;

                            int_dec = Convert.ToInt16(str_hex, 16);

                            double double_Mpa = 0;

                            if (String_SQLcommand == "" && String_Dline == "P2")
                            {
                                String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                                bool_add_Values = false;
                            }
                            if (String_SQLcommand == "" && String_Dline == "P3")
                            {
                                String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                                bool_add_Values = false;
                            }

                            if (String_SQLcommand != "" && xx != 0)
                            {
                                String_SQLcommand += ",";
                            }

                            str_DTS_temp[xx] = int_dec.ToString();

                            bool_add_Values = true;

                            String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + int_dec.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";

                            str_T += "(" + int_GetPortid + ")" + int_dec.ToString();

                            if (int_DTS_temp_count > 1)
                            {
                                str_DTS_temp[xx] = "";

                                int_DTS_temp_count = 0;
                            }
                        }

                        //String_SQLcommand += ";";

                    }
                }

                ////////第二台4ch
                //發送指令前先清完記憶體

                if (TcpClient_Reader.Available > 0)
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, TcpClient_Reader.Available);

                NetworkStream_Reader.Write(Byte_Command_Sent_083, 0, 8);

                for (int i = 0; i <= 50; i++)
                {
                    Byte_Command_Re[i] = 0;
                }

                for (int i = 0; i <= 400; i++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                int_Net_Available = TcpClient_Reader.Available;

                str_hex = "";

                if (int_Net_Available >= 13)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    //判斷是否符合CRC
                    temCRC = crc16(Byte_Command_Re, 11);

                    //相符的話才能執行  //20200311 加上判斷開頭 為 03 03

                    // 02 03 02 4C 00 04 84 55 讀取第1台 4通道 ID02
                    // 03 03 02 4C 00 04 85 84 讀取第2台 4通道 ID03 
                    if (temCRC[0] == Byte_Command_Re[11] && temCRC[1] == Byte_Command_Re[12] && Byte_Command_Re[0] == 3 && Byte_Command_Re[1] == 3)
                    {
                        str_hex = "";
                        //String_SQLcommand = "";

                        //後做4台
                        int int_X = 8;
                        for (int xx = 4; xx < int_X; xx++)
                        {
                            //按照設定值抓取溫度
                            int int_GetPortid = Convert.ToInt16(Str_Portid.Split(';')[1].Split('.')[xx]);

                            //轉換成16進制文字
                            str_hex = Convert.ToString(Byte_Command_Re[1 + ((int_GetPortid - 4) * 2)], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[1 + (((int_GetPortid - 4) * 2) + 1)], 16).PadLeft(2, '0');

                            int int_dec = 0;
                            int_dec = Convert.ToInt16(str_hex, 16);

                            double double_Mpa = 0;

                            //看那個製造部門 資料表
                            if (String_SQLcommand == "" && String_Dline == "P2")
                            {
                                String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                                bool_add_Values = false;
                            }
                            if (String_SQLcommand == "" && String_Dline == "P3")
                            {
                                String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                                bool_add_Values = false;
                            }

                            if (String_SQLcommand != "" && xx != 4)
                            {
                                String_SQLcommand += ",";
                            }

                            str_DTS_temp[xx] = int_dec.ToString();

                            bool_add_Values = true;

                            //儲存的值：為 String_SID int_dec
                            String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[xx] + "','" + int_dec.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";

                            str_T += "(" + int_GetPortid + ")" + int_dec.ToString();

                            if (int_DTS_temp_count > 1)
                            {
                                str_DTS_temp[xx] = "";
                                int_DTS_temp_count = 0;
                            }
                        }

                        //String_SQLcommand += ";";

                    }
                }

                if (TcpClient_Reader.Available > 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, TcpClient_Reader.Available);
                }

                /////////抓取速度模組
                //發送讀取指令。

                for (int i = 0; i <= 10; i++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                if (TcpClient_Reader.Available > 0)
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, TcpClient_Reader.Available);

                for (int i = 0; i <= 50; i++)
                {
                    Byte_Command_Re[i] = 0;
                }

                NetworkStream_Reader.Write(Byte_Command_Sent, 0, String_SeData05[1].Split(' ').Length);

                for (int i = 0; i <= 150; i++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                //取得回傳大小。
                int_Net_Available = TcpClient_Reader.Available;

                // NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                //按照位置抓取資料，空壓的都採電壓式， 
                //例位置1 ，抓 第4+第5 加總為16進製 4字元， 轉換成 10進製後得 x
                // X 乘以 10 之後 再除以 4095， 取小數點後2位

                str_hex = "";

                //至少需要21碼才做解碼動作
                if (int_Net_Available >= 21)
                {
                    //String_SQLcommand = "";
                    //一次只取21筆
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, 21);

                    //判斷是否符合CRC
                    temCRC = crc16(Byte_Command_Re, 19);

                    //檢查回傳碼是否相符的話才能執行
                    if (temCRC[0] == Byte_Command_Re[19] && temCRC[1] == Byte_Command_Re[20])
                    {
                        //按照設定值抓取溫度
                        int int_GetPortid = 1;

                        //轉換成16進制文字
                        str_hex = Convert.ToString(Byte_Command_Re[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[1 + ((int_GetPortid * 2) + 1)], 16).PadLeft(2, '0');

                        int int_dec = 0;

                        int_dec = Convert.ToInt16(str_hex, 16);

                        double double_DCvi = Convert.ToDouble(int_dec) * 10 / 4095;

                        //03/12調整現場失敗，改用後台補充值+2
                        str_T += "(速度)" + double_DCvi.ToString() + "[" + (double_DCvi / MPU.MPU_int_numericUpDown1 + 2) + "]";

                        if (String_SQLcommand == "" && String_Dline == "P2")
                        {
                            String_SQLcommand = "; INSERT INTO [dbo].[tb_P2recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                            bool_add_Values = false;
                        }
                        if (String_SQLcommand == "" && String_Dline == "P3")
                        {
                            String_SQLcommand = "; INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                            bool_add_Values = false;
                        }

                        if (String_SQLcommand != "" && bool_add_Values == true)
                        {
                            String_SQLcommand += ",";
                        }

                        //03/12調整現場失敗，改用後台補充值+2
                        String_SQLcommand += "('" + String_TID + "','" + String_DIP + "','PLS001','" + (double_DCvi / MPU.MPU_int_numericUpDown1 + 2).ToString("F3") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','德邦PSK-8000-速度'); ";

                    }

                    String_SQLcommand += ";";
                }

                // if (double_DCvi == 0 || double_DCvi == 10)

                //else
                {
                    String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    Console.WriteLine("M0733:Exception source: {0}", EX.Source);

                    String_ReData[index] = DateTime.Now.ToString("HH:mm:ss") + "...[" + byte_function_03_loop_index + "]Ex:" + EX.Source + EX.Message;
                }
            }
        }

        string[] eight1_1 = { "01", "03", "00", "00", "00", "01", "84", "0A" };
        string[] eight2_1 = { "01", "03", "00", "01", "00", "01", "D5", "CA" };
        string[] eight3_1 = { "01", "03", "00", "02", "00", "01", "25", "CA" };
        string[] eight4_1 = { "01", "03", "00", "03", "00", "01", "74", "0A" };
        string[] eight5_1 = { "01", "03", "00", "04", "00", "01", "C5", "CB" };
        string[] eight6_1 = { "01", "03", "00", "05", "00", "01", "94", "0B" };

        //正壓
        void function_09_positive()
        {
            try
            {
                string str_T = "";
                for (int i = 0; i < 8; i++)
                {
                    //if氣壓ad位置為1
                    {
                        Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(eight1_1[i], 16));
                    }

                    #region if設備為IPA
                    
                        //正壓ad位置為5
                        Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(eight5_1[i], 16));
                        //正壓ad位置為6
                        Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(eight6_1[i], 16));
                    
                    #endregion
                }
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, 8);

                for (int i = 0; i <= 50; i++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                String str_hex = "";
                double double_Mpa = 0;

                str_hex = Convert.ToString(Byte_Command_Re[4], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[5], 16).PadLeft(2, '0');

                int[] eight = new int[str_hex.Length];
                for (int i = 0; i < eight.Length; i = i + 4)
                {
                    eight[i / 4] = Convert.ToInt32(str_hex.Substring(i, 4), 16);
                    //感測值結果
                    double_Mpa = ((Convert.ToDouble(eight[0])) * 20 / 4095 - 4) / 16;
                }
            }
            catch
            {
            }
        }

        //負壓
        void function_10_negative()
        {
            try
            {
                string str_T = "";
                for (int i = 0; i < 8; i++)
                {
                    Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(eight2_1[i], 16));
                }
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, 8);

                for (int i = 0; i <= 50; i++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                String str_hex = "";
                double double_Mpa = 0;

                str_hex = Convert.ToString(Byte_Command_Re[4], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[5], 16).PadLeft(2, '0');

                int[] eight = new int[str_hex.Length];
                for (int i = 0; i < eight.Length; i = i + 4)
                {
                    eight[i / 4] = Convert.ToInt32(str_hex.Substring(i, 4), 16);
                    //感測值結果
                    double_Mpa = ((Convert.ToDouble(eight[0])) * 20 / 4095 - 4) * 101 / (-16);
                }
            }
            catch
            {
            }
        }

        #region function_11_flow 流量計(8_3、8_4)
        //流量計
        void function_11_flow(int port)
        {
            try
            {
                switch (port)
                {
                    case 3:
                        for (int j = 0; j < 8; j++)
                        {
                            Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(eight3_1[j], 16));
                        }
                        break;
                    case 4:
                        for (int j = 0; j < 8; j++)
                        {
                            Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(eight4_1[j], 16));
                        }
                        break;
                    default:
                        break;
                }

                NetworkStream_Reader.Write(Byte_Command_Sent, 0, Byte_Command_Sent.Length);

                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                if (int_Net_Available > 0)
                {

                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    String str_hex = "";
                    double double_Mpa = 0;
                    str_hex = Convert.ToString(Byte_Command_Re[4], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[5], 16).PadLeft(2, '0');

                    int[] eight = new int[str_hex.Length];
                    for (int j = 0; j < eight.Length; j = j + 4)
                    {
                        eight[j / 4] = Convert.ToInt32(str_hex.Substring(j, 4), 16);
                        //感測值結果
                        double_Mpa = (((Convert.ToDouble(eight[0]) * 20 / 4095 - 4) / 0.16) * 50) / 100;
                        //double_Mpa = ((Convert.ToDouble(eight[0]) * 20 / 4095 - 4) / 0.16) * (50 / 100);
                    }

                    double_Mpa = Math.Round(double_Mpa, 2);

                    String_ReData[index] = "流量 (" + double_Mpa.ToString() + ")";

                    String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region function_12_light 燈號判斷
        //紅燈
        string[] light1_1 = { "02", "03", "00", "00", "00", "01", "84", "39" };
        //綠燈
        string[] light2_1 = { "02", "03", "00", "01", "00", "01", "D5", "F9" };
        //燈號
        void function_12_light()
        {
            try
            {
                int int_Net_Available = 0;
                double double_Mpa_1 = 0;
                double double_Mpa_2 = 0;

                //紅燈
                string str_T_1 = "";
                for (int j = 0; j < 8; j++)
                {
                    Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(light1_1[j], 16));
                }
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, 8);

                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                int_Net_Available = TcpClient_Reader.Available;

                if (int_Net_Available > 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    String str_hex_1 = "";
                    double_Mpa_1 = 0;

                    str_hex_1 = Convert.ToString(Byte_Command_Re[4], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[5], 16).PadLeft(2, '0');

                    int[] eight1 = new int[str_hex_1.Length];
                    for (int j = 0; j < eight1.Length; j = j + 4)
                    {
                        eight1[j / 4] = Convert.ToInt32(str_hex_1.Substring(j, 4), 16);
                        //感測值結果
                        double_Mpa_1 = eight1[0];
                    }

                    //綠燈
                    string str_T_2 = "";
                    for (int j = 0; j < 8; j++)
                    {
                        Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(light2_1[j], 16));
                    }
                    NetworkStream_Reader.Write(Byte_Command_Sent, 0, 8);

                    for (int j = 0; j <= 50; j++)
                    {
                        System.Threading.Thread.Sleep(10);
                        System.Windows.Forms.Application.DoEvents();
                    }

                    //int int_Net_Available = TcpClient_Reader.Available;

                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    String str_hex_2 = "";
                    double_Mpa_2 = 0;

                    str_hex_2 = Convert.ToString(Byte_Command_Re[4], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[5], 16).PadLeft(2, '0');

                    int[] eight2 = new int[str_hex_2.Length];
                    for (int j = 0; j < eight2.Length; j = j + 4)
                    {
                        eight2[j / 4] = Convert.ToInt32(str_hex_2.Substring(j, 4), 16);
                        //感測值結果
                        double_Mpa_2 = eight2[0];
                    }

                    string str_light = "";
                    if (double_Mpa_1 > 20000)
                    {
                        //紅燈(停止中)
                        String_ReData[index] = "紅燈";
                        str_light = "1000";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_light + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_light + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else if (double_Mpa_2 > 20000)
                    {
                        //綠燈(運行中)
                        String_ReData[index] = "綠燈";
                        str_light = "0100";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_light + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_light + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else if ((double_Mpa_1 < 20000 && double_Mpa_2 < 20000) && (double_Mpa_1 > 0 && double_Mpa_2 > 0))
                    {
                        //黃燈(暫停中)
                        String_ReData[index] = "黃燈";
                        str_light = "0010";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_light + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_light + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else
                    {
                        //黃燈(暫停中)
                        String_ReData[index] = "黃燈";
                        str_light = "0010";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_light + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "";
                    }
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("Exception source: {0}", EX.Source + "," + EX.Message);
                }
            }
        }
        #endregion

        #region function_13 溫度
        void function_13()
        {
            try
            {
                String_SeData13[1] = "03 02 4C 00 04";
                String_SeData13[1] = CRC16LH(GetModbusCommand(String_Address.ToString(), String_SeData13[1]));
                Array.Resize(ref Byte_Command_Sent, String_SeData13[1].Split(' ').Length);
                for (int j = 0; j <= String_SeData13[1].Split(' ').Length - 1; j++)
                {
                    Byte_Command_Sent[j] = Convert.ToByte(String_SeData13[1].Split(' ')[j], 16);
                }

                NetworkStream_Reader.Write(Byte_Command_Sent, 0, String_SeData13[1].Split(' ').Length);


                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                if (int_Net_Available > 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    if (int_Net_Available == 14)
                    {
                        string str_1 = Convert.ToInt32(String.Format("{0:X2}", Byte_Command_Re[5]), 16).ToString();

                        string str_2 = Convert.ToInt32(String.Format("{0:X2}", Byte_Command_Re[7]), 16).ToString();

                        string str_3 = Convert.ToInt32(String.Format("{0:X2}", Byte_Command_Re[9]), 16).ToString();

                        string str_4 = Convert.ToInt32(String.Format("{0:X2}", Byte_Command_Re[11]), 16).ToString();

                        // str_T += str_H;

                        //if (str_values_temp != str_T + str_H)
                        //{

                        //    String_SQLcommand = "INSERT INTO [dbo].[tb_recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[0].ToString() + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "') ";

                        //    String_SQLcommand += ", ('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[1].ToString() + "','" + str_H + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";

                        //    str_values_temp = str_T + str_H;
                        //}
                        //else
                        //{
                        //    String_SQLcommand = "";
                        //}

                        str_1 = str_1.Replace("88", "N/A");
                        str_2 = str_2.Replace("88", "N/A");
                        str_3 = str_3.Replace("88", "N/A");
                        str_4 = str_4.Replace("88", "N/A");
                        //Console.WriteLine(String_ReData);
                        String_ReData[index] = str_1 + " : " + str_2 + " : " + str_3 + " : " + str_4;

                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + String_ReData[index].Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else
                    {
                        String_ReData[index] = MPU.str_ErrorMessage[5];

                        String_SQLcommand = "";
                        String_SQLcommand_old = "";
                    }
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region function_14 燈號控制
        void function_14()
        {
            try

            {
                //17122601
                if (MPU.str_Barcode != "")
                {
                    byte[] byte_Command_wlslcs = new byte[5];

                    if (MPU.str_Barcode == "00000000")
                    {
                        //傳送橘燈訊號
                        SetOrangeLight();
                        //byte_Command_wlslcs[0] = Convert.ToByte("32", 16);
                        //NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 1);
                        //byte_Command_wlslcs[0] = Convert.ToByte("30", 16);
                        //byte_Command_wlslcs[1] = Convert.ToByte("38", 16);
                        //byte_Command_wlslcs[2] = Convert.ToByte("31", 16);
                        //byte_Command_wlslcs[3] = Convert.ToByte("0D", 16);
                        //byte_Command_wlslcs[4] = Convert.ToByte("0A", 16);
                        NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);
                    }
                    else
                    {
                        //16進位
                        byte_Command_wlslcs[0] = Convert.ToByte("30", 16);
                        byte_Command_wlslcs[1] = Convert.ToByte("38", 16);
                        byte_Command_wlslcs[2] = Convert.ToByte("32", 16);
                        byte_Command_wlslcs[3] = Convert.ToByte("0D", 16);
                        byte_Command_wlslcs[4] = Convert.ToByte("0A", 16);
                        NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);
                    }

                    MPU.str_Barcode = "";
                }

                //不會一直回傳燈號，只會傳一次，只會在有變動產生回傳，這樣子會有潛藏性的BUG。
                //所有在傳遞一定次數之後，必須重新連線一次。
                //在燈號的運作中，如果n分鐘沒有資料回傳的話，就重來一次連結，產生目前狀態

                int_WLSLCS_Count += 1;

                if (int_WLSLCS_Count > 2500)
                {
                    int_WLSLCS_Count = 0;

                    Str_WLSLCS_F = "";
                }

                //200227 當燈號計數器歸零時，送一個橘燈訊號, 必須是自製燈號的
                //
                if (int_WLSLCS_Count == 0 && String_SID.Substring(0, 3) == "WLS")
                {
                    SetOrangeLight();
                }

                //取得目前網路盒的訊號資料量大小
                int int_Net_Available = TcpClient_Reader.Available;

                //判斷是否有資料以及是否可以讀取。
                if (int_Net_Available >= 10 && NetworkStream_Reader.CanRead == true)
                {
                    byte_function_03_loop_index = 0;

                    string str_T = "";

                    NetworkStream_Reader.Read(Byte_Command_Re, 0, 10);

                    for (int j = 0; j <= 10 - 2; j++)
                    {
                        str_T += Convert.ToChar(Byte_Command_Re[j]);
                    }

                    //和之前的燈號一樣的話，就不用做處理了。
                    if (Str_WLSLCS_F != str_T)
                    {
                        //記錄這一次的燈號
                        Str_WLSLCS_F = str_T;

                        //判斷是何種燈號解析，WLS為自訂義燈號，LCS為燈號解析模組。
                        //WLS 00BGYR00 RGYW
                        if (str_T.Length > 8 && String_SID.Substring(0, 3) == "WLS")
                        {
                            //17122601 

                            if (String_SID == "WLS002") //水洗機的燈號模組其中一組訊號拿來做段數開關的偵測。
                            {
                                if (str_T.Substring(2, 1) == "1")
                                {
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','SWD001','4','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "'); ";
                                }
                                else
                                {
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','SWD001','0','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "'); ";
                                }
                            }

                            //因為紅燈為反相，所以需要把紅燈的顯示相反過來。
                            if (str_T.Substring(5, 1) == "1")
                            {
                                str_T = "0" + str_T.Substring(3, 1) + str_T.Substring(4, 1) + str_T.Substring(2, 1);
                            }
                            else
                            {
                                str_T = "1" + str_T.Substring(3, 1) + str_T.Substring(4, 1) + str_T.Substring(2, 1);
                            }
                        }
                        else if (str_T.Length > 8 && String_SID.Substring(0, 3) == "LCS")
                        {
                            //LCS 00RWYG00 RGYW
                            //LCS 如果有0001 純白燈的話 => 回傳黃燈 0011
                            //LCS 如果有0000 全滅的話 => 回傳黃燈 0010
                            str_T = str_T.Substring(2, 1) + str_T.Substring(5, 1) + str_T.Substring(4, 1) + str_T.Substring(3, 1);
                            //LCS001 LCS002 燈號不同，為特殊型。
                            //當為1100時，判斷為綠燈(0100)，當為0011時，判斷為黃燈(0010)，其他狀況時為紅燈(1000)
                            if (String_SID == "LCS001" || String_SID == "LCS002")
                            {
                                Str_lcs = str_T;
                                str_T = "1000";
                                if (Str_lcs == "1100")
                                {
                                    str_T = "0100";
                                }
                                else if (Str_lcs == "0011")
                                {
                                    str_T = "0010";
                                }
                            }
                            else
                            {
                                Str_lcs = str_T;

                                if (Str_lcs == "0001")
                                {
                                    str_T = "0011";
                                }
                                else if (Str_lcs == "0000")
                                {
                                    str_T = "0010";
                                }
                                if (Str_lcs == "1100" || Str_lcs == "1110" || Str_lcs == "1101" || Str_lcs == "1111")
                                {
                                    str_T = "0100";
                                }
                            }
                        }

                        //燈號結果處理完畢
                        str_ttt = str_T;

                        String_ReData[index] = int_Net_Available + "@" + str_T + ":" + String_Sclass;

                        //有資料時再處理
                        if (str_T != "" && String_SID != "WLS002")
                        {
                            //依製二製三放置不同資料表。
                            //20161230 insert會一直加下去，所以insert指令會變很大。稍微修改一下，變成只有一筆insert into
                            //如果String_SQLcommand 不是空字串的話，直接加入values的值到字串尾巴即可。
                            if (String_SQLcommand == "")
                            {
                                if (String_Dline == "P2")
                                {
                                    String_SQLcommand += "INSERT INTO [dbo].[tb_P2recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                                }
                                else if (String_Dline == "P3")
                                {
                                    String_SQLcommand += "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                                }
                            }
                            else
                            {
                                String_SQLcommand += ", ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                            }
                        }
                        else if (String_SID == "WLS002")
                        {
                            if (String_Dline == "P2")
                            {
                                String_SQLcommand += "INSERT INTO [dbo].[tb_P2recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";

                            }
                            else if (String_Dline == "P3")
                            {
                                String_SQLcommand += "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                            }
                        }
                        else
                        {
                            String_SQLcommand = "";
                        }
                    }
                }
                else
                {
                    //當沒有資料的時候，利用發送指令取得回傳值

                    byte[] byte_Command_wlslcs = new byte[5];

                    byte_Command_wlslcs[0] = 48;
                    byte_Command_wlslcs[1] = 56;
                    byte_Command_wlslcs[2] = 48;
                    byte_Command_wlslcs[3] = 10;
                    byte_Command_wlslcs[4] = 13;
                    //byte_Command_wlslcs[0] = Convert.ToByte("", 16);
                    //byte_Command_wlslcs[0] = Convert.ToByte("", 16);
                    //byte_Command_wlslcs[0] = Convert.ToByte("", 16);
                    //byte_Command_wlslcs[0] = Convert.ToByte("", 16);
                    //byte_Command_wlslcs[0] = Convert.ToByte("", 16);

                    NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);

                    for (int i = 0; i <= 50; i++)
                    {
                        System.Threading.Thread.Sleep(5);

                        System.Windows.Forms.Application.DoEvents();
                    }

                    int int_Net_Availablexxx = TcpClient_Reader.Available;

                    //如果真的都沒有值回傳的話，再準備進行資料重連的動作。
                    if (int_Net_Availablexxx == 0)
                    {
                        byte_function_03_loop_index += 1;

                        String_ReData[index] = "<468>" + int_Net_Available + "@" + str_ttt + "，未解碼次數(" + byte_function_03_loop_index + "):" + String_Sclass;

                        //當燈號解析超出10次沒有資料的時候，重新連線一次。
                        if (byte_function_03_loop_index > 50)
                        {
                            //161208 加入一個共有變數，同一時間只能一個tcpip連線
                            if (MPU.TOSrun == false)
                            {
                                MPU.TOSrun = true;

                                byte_function_03_loop_index = 0;

                                str_ttt = "";

                                NetworkStream_Reader.Close();

                                TcpClient_Reader.Close();

                                //NetworkStream_Reader = null;

                                //TcpClient_Reader = null;

                                for (int int_t = 0; int_t < 100; int_t++)
                                {
                                    System.Windows.Forms.Application.DoEvents();

                                    System.Threading.Thread.Sleep(5);
                                }

                                //重新建立連結
                                TcpClientConnect();

                                MPU.TOSrun = false;
                            }
                        }
                    }
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = "[" + byte_function_03_loop_index + "]Ex:" + EX.Source + EX.Message;

                    Console.WriteLine("M0516:燈號異常:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss[{0}]"), EX.Source + EX.Message);

                    byte_function_03_loop_index += 1;
                }
            }
        }
        #endregion

        #region function_15 正壓、負壓(8_1、8_2、8_5、8_6)
        void function_15(int port)
        {
            try
            {
                switch (port)
                {
                    case 1:
                        for (int j = 0; j < 8; j++)
                        {
                            Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(eight1_1[j], 16));
                        }
                        break;
                    case 2:
                        for (int j = 0; j < 8; j++)
                        {
                            Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(eight2_1[j], 16));
                        }
                        break;
                    case 5:
                        for (int j = 0; j < 8; j++)
                        {
                            Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(eight5_1[j], 16));
                        }
                        break;
                    case 6:
                        for (int j = 0; j < 8; j++)
                        {
                            Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(eight6_1[j], 16));
                        }
                        break;
                    default:
                        break;
                }
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, 8);


                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                if (int_Net_Available > 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    String str_hex = "";
                    double double_Mpa = 0;

                    str_hex = Convert.ToString(Byte_Command_Re[4], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[5], 16).PadLeft(2, '0');

                    int[] eight = new int[str_hex.Length];

                    for (int j = 0; j < eight.Length; j = j + 4)
                    {
                        eight[j / 4] = Convert.ToInt32(str_hex.Substring(j, 4), 16);
                        //感測值結果
                        if (F15_port != 2)
                            double_Mpa = ((Convert.ToDouble(eight[0])) * 20 / 4095 - 4) / 16;
                        else
                            double_Mpa = ((Convert.ToDouble(eight[0])) * 20 / 4095 - 4) * 101 / (-16);
                    }

                    double_Mpa = Math.Round(double_Mpa, 2);

                    if (double_Mpa > 0)
                    {
                        String_ReData[index] = "正壓 (" + double_Mpa.ToString() + ")";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else if (double_Mpa < 0)
                    {
                        String_ReData[index] = "負壓 (" + double_Mpa.ToString() + ")";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else
                    {
                        String_ReData[index] = "(" + double_Mpa.ToString() + ")";
                        String_SQLcommand = "";
                        String_SQLcommand_old = "";
                    }
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", ex.Source);
                }
            }
        }
        #endregion

        #region function_16_pc_light S1F1 燈號
        void function_16_pc_light()
        {
            try
            {
                int int_Net_Available = 0;

                Byte_Command_Sent = Encoding.ASCII.GetBytes("S1F1");
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, Byte_Command_Sent.Length);

                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                int_Net_Available = TcpClient_Reader.Available;
                if (int_Net_Available > 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    string temp = Encoding.ASCII.GetString(Byte_Command_Re, 0, int_Net_Available).Trim(';').Split(',')[1];

                    if (temp == "Green")
                    {
                        //綠燈(運行中)
                        String_ReData[index] = "綠燈";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + "0100" + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + "0100" + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else if (temp == "Yellow")
                    {
                        //黃燈(暫停中)
                        String_ReData[index] = "黃燈";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + "0010" + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + "0010" + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else if (temp == "Red")
                    {
                        //紅燈(停止中)
                        String_ReData[index] = "紅燈";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + "1000" + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + "1000" + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else
                    {
                        String_ReData[index] = MPU.str_ErrorMessage[5];
                        String_SQLcommand = "";
                        String_SQLcommand_old = "";
                    }
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region function_17_pc_pressure S1F2 氣壓
        void function_17_pc_pressure()
        {
            try
            {
                Byte_Command_Sent = Encoding.ASCII.GetBytes("S1F2");
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, Byte_Command_Sent.Length);

                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                if (int_Net_Available != 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    string temp = Encoding.ASCII.GetString(Byte_Command_Re, 0, int_Net_Available).Trim(';').Split(',')[F15_port + 1];
                    if (temp != "NA")
                    {
                        if (Convert.ToDouble(temp) > 0)
                            String_ReData[index] = "正壓 (" + temp + ")";
                        else
                            String_ReData[index] = "負壓 (" + temp + ")";

                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else
                    {
                        String_SQLcommand = "";
                        String_SQLcommand_old = "";
                    }
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", ex.Source);
                }
            }
        }
        #endregion

        #region function_18_pc_flow S1F3 流量
        void function_18_pc_flow()
        {
            try
            {
                Byte_Command_Sent = Encoding.ASCII.GetBytes("S1F3");

                NetworkStream_Reader.Write(Byte_Command_Sent, 0, Byte_Command_Sent.Length);

                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;
                if (int_Net_Available != 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    double double_Mpa = 0;

                    double_Mpa = Convert.ToDouble(Encoding.ASCII.GetString(Byte_Command_Re, 0, int_Net_Available).Trim(';').Split(',')[F11_port + 1]);

                    String_ReData[index] = "流量 (" + double_Mpa.ToString() + ")";

                    String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region function_19_pc_temperature S1F4 溫度
        void function_19_pc_temperature()
        {
            try
            {
                Byte_Command_Sent = Encoding.ASCII.GetBytes("S1F4");
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, Byte_Command_Sent.Length);


                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;
                if (int_Net_Available != 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);
                    string temp = Encoding.ASCII.GetString(Byte_Command_Re, 0, int_Net_Available).Trim(';').Split(',')[1];

                    temp = temp.Replace("NA", "0");

                    String_ReData[index] = "溫度 (" + temp + ")";

                    String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }

            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region function_20_pc_resistance S1F5 種晶吸嘴阻值
        void function_20_pc_resistance()
        {
            try
            {
                Byte_Command_Sent = Encoding.ASCII.GetBytes("S1F5");
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, Byte_Command_Sent.Length);


                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                if (int_Net_Available > 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);
                    string temp = Encoding.ASCII.GetString(Byte_Command_Re, 0, int_Net_Available).Trim(';').Split(',')[1];

                    temp = temp.Replace("NA", "0");

                    String_ReData[index] = "種晶吸嘴阻值 (" + temp + ")";

                    String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }

            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region function_21_pc_H_Judge S1F6 H-Judge讀值
        void function_21_pc_H_Judge()
        {
            try
            {
                Byte_Command_Sent = Encoding.ASCII.GetBytes("S1F6");
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, Byte_Command_Sent.Length);


                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                if (int_Net_Available != 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);
                    string temp = Encoding.ASCII.GetString(Byte_Command_Re, 0, int_Net_Available).Trim(';').Split(',')[1];

                    temp = temp.Replace("NA", "0");

                    String_ReData[index] = "H-Judge讀值 (" + temp + ")";

                    String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }

            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region function_22_pc_shoucut S1F7 螢幕辨識參數(Temperature, Power, force, Time)
        void function_22_pc_shoucut()
        {
            try
            {
                Byte_Command_Sent = Encoding.ASCII.GetBytes("S1F7");
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, Byte_Command_Sent.Length);


                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;
                NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                if (int_Net_Available != 0)
                {
                    string[] temp = Encoding.ASCII.GetString(Byte_Command_Re, 0, int_Net_Available).Trim(';').Split(',');
                    if (temp[1] != "NA")
                    {
                        temp[1] = temp[1].TrimStart('0');

                        String_ReData[index] = "溫度 (" + temp[1] + ")";

                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp[1] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp[1] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }

            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region function_23_brainchild 溫度
        void function_23_brainchild()
        {
            try
            {
                String_SeData13[1] = "03 03 00 80 00 01 84 00";
                for (int j = 0; j <= String_SeData13[1].Split(' ').Length - 1; j++)
                {
                    Byte_Command_Sent[j] = Convert.ToByte(String_SeData13[1].Split(' ')[j], 16);
                }

                NetworkStream_Reader.Write(Byte_Command_Sent, 0, String_SeData13[1].Split(' ').Length);


                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                if (int_Net_Available > 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);
                    double SH = 4553.6, SL = -1999.9;   
                    double temp;
                    string hex = Convert.ToString(Byte_Command_Re[4], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[5], 16).PadLeft(2, '0');
                    temp = (((SH - SL) / 65535) * Convert.ToInt32(hex, 16)) + SL - 1;
                    if (temp > 0)
                    {
                        String_ReData[index] = "溫度(" + Math.Round(temp, 0).ToString() + ")";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + String_ReData[index].Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + String_ReData[index].Trim() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else
                    {
                        String_ReData[index] = "異常";
                        String_SQLcommand = "";
                        String_SQLcommand_old = "";
                    }

                    
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region function_24_pc_NMPA_NMPB S1F8 舉離機(NMPA, NMPB)
        void function_24_pc_NMPA_NMPB()
        {
            try
            {
                int int_Net_Available = 0;

                Byte_Command_Sent = Encoding.ASCII.GetBytes("S1F8");
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, Byte_Command_Sent.Length);

                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                int_Net_Available = TcpClient_Reader.Available;
                if (int_Net_Available > 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    string temp = Encoding.ASCII.GetString(Byte_Command_Re, 0, int_Net_Available).Trim(';');

                    if (!string.IsNullOrWhiteSpace(temp))
                    {
                        //綠燈(運行中)
                        String_ReData[index] = "NMPA, NMPB (" + temp + ")";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + temp + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else
                    {
                        String_ReData[index] = MPU.str_ErrorMessage[5];
                        String_SQLcommand = "";
                        String_SQLcommand_old = "";
                    }
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region function_25 混合壓(8_1、8_5)
        void function_25(int port)
        {
            try
            {
                switch (port)
                {
                    case 1:
                        for (int j = 0; j < 8; j++)
                        {
                            Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(eight1_1[j], 16));
                        }
                        break;
                    case 2:
                        for (int j = 0; j < 8; j++)
                        {
                            Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(eight2_1[j], 16));
                        }
                        break;
                    case 5:
                        for (int j = 0; j < 8; j++)
                        {
                            Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(eight5_1[j], 16));
                        }
                        break;
                    case 6:
                        for (int j = 0; j < 8; j++)
                        {
                            Byte_Command_Sent[j] = Convert.ToByte(Convert.ToInt32(eight6_1[j], 16));
                        }
                        break;
                    default:
                        break;
                }
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, 8);


                for (int j = 0; j <= 50; j++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                if (int_Net_Available > 0)
                {
                    NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                    String str_hex = "";
                    double double_Mpa = 0;

                    str_hex = Convert.ToString(Byte_Command_Re[4], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[5], 16).PadLeft(2, '0');

                    int[] eight = new int[str_hex.Length];

                    for (int j = 0; j < eight.Length; j = j + 4)
                    {
                        eight[j / 4] = Convert.ToInt32(str_hex.Substring(j, 4), 16);
                        //感測值結果
                        double mA = Convert.ToDouble(eight[0]) * 20 / 4095;
                        double_Mpa = 0.00003 * Math.Pow(mA, 2) + 0.068 * mA - 0.3725;
                    }

                    double_Mpa = Math.Round(double_Mpa, 2);

                    if (double_Mpa > 0)
                    {
                        String_ReData[index] = "正壓 (" + double_Mpa.ToString() + ")";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else if (double_Mpa < 0)
                    {
                        String_ReData[index] = "負壓 (" + double_Mpa.ToString() + ")";
                        String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                        String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + double_Mpa.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    }
                    else
                    {
                        String_ReData[index] = "(" + double_Mpa.ToString() + ")";
                        String_SQLcommand = "";
                        String_SQLcommand_old = "";
                    }
                }
                else
                {
                    String_ReData[index] = MPU.str_ErrorMessage[5];
                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", ex.Source);
                }
            }
        }
        #endregion

        #region function_26_PingIP 只PING設備IP，連線成功就回傳黃燈
        void function_26_PingIP()
        {

            Ping ping = new Ping();
            try
            {
                //PING設備IP
                PingReply reply = ping.Send(String_DIP, 100);
                if (reply.Status == IPStatus.Success)
                {
                    //連線成功則顯示黃燈
                    String_ReData[index] = "黃燈";
                    string str_light = "0010";
                    String_SQLcommand = "INSERT INTO [dbo].[tb_CSPrecordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_light + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                    String_SQLcommand_old = "INSERT INTO [dbo].[tb_CSPrecordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_light + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";
                }
                else
                {
                    //連線失敗則顯示錯誤訊息
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                {
                    String_ReData[index] = MPU.str_ErrorMessage[1];

                    String_SQLcommand = "";
                    String_SQLcommand_old = "";

                    Console.WriteLine("M0312:Exception source: {0}", ex.Source);
                }
            }
        }
        #endregion
    }
}
