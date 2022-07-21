using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

        public String address_index = "";

        public Boolean bool_AutoRun = false;

        public Boolean bool_isThreadSet = false;     

        public int index = 0;

        public int int_ThreadNum = 0;

        public int int_timeOutMsec = 0;

        public int int_ReaderSleep = 0;

        public int int_ReaderSleepSET = 0;

        //17122601
        public string str_Barcode = "";

        public string str_BarcodeLight = "";

        System.Net.Sockets.NetworkStream NetworkStream_Reader;

        System.Net.Sockets.TcpClient TcpClient_Reader;

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
            if (bool_tcpclientconnect_Action == false)
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
                    if (!(TcpClient_Reader == null) && TcpClient_Reader.Connected)
                    {
                        TcpClient_Reader.Close();
                    }

                    TcpClient_Reader = TimeOutSocket.Connect(new System.Net.IPEndPoint(new System.Net.IPAddress(Bytes_IP), Convert.ToInt16(String_Port)), 100);
                    string ss = "";
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
                            int_ReaderSleep = 200;
                            //System.Threading.Thread.Sleep(10000);
                        }

                    }


                }

                bool_tcpclientconnect_Action = false;
            }

        }

        //製作一個boolean 代表只有單一個動作在執行
        Boolean boolMESnetISrun = false;

        Boolean isConnect = false;


        DateTime disTime = DateTime.Now;
        //執行緒主要執行區塊
        public void MesNetSiteRunning()
        {
            // String_TID = "111";
            do
            {
                //161208 加入判斷tcpclient是否在連結中，如果在連結中就不要做其他動作。
                if (boolMESnetISrun == false)
                {
                    boolMESnetISrun = true;

                    // 若同IP、同PORT時，只讓第一組IP連線
                    if (Convert.ToInt32(address_index.Split(',')[0]) == 1)
                    {
                        System.Threading.Thread.Sleep(int_ReaderSleep);
                        try
                        {
                            //連結成功才動作
                            if ((TcpClient_Reader != null) && TcpClient_Reader.Connected)
                            {
                                switch (String_Sclass.Split(';')[0])
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

                                    //正壓
                                    case "9":
                                        function_09_positive();
                                        break;
                                    //負壓
                                    case "10":
                                        function_10_negative();
                                        break;
                                    //流量計
                                    case "11":
                                        function_11_flow();
                                        break;
                                    //燈號
                                    case "12":
                                        function_12_light();
                                        break;
                                    case "13":
                                        function_13();
                                        break;
                                    case "14":
                                        function_14();
                                        break;
                                    default:
                                        SetErrorStatic();
                                        break;
                                }

                            }
                            else
                            {
                                //連結失敗
                                SetErrorStatic();

                                // 30秒重新連線一次
                                if ((DateTime.Now - disTime).Seconds >= 10)
                                {
                                    isConnect = false;
                                }

                                if (isConnect == false && bool_tcpclientconnect_Action == false)
                                {
                                    disTime = DateTime.Now;
                                    isConnect = true;
                                    //重新建立連結
                                    TcpClientConnect();
                                }

                                //161208 超出時間後再連一次。
                                //byte_function_03_loop_index += 1;

                                //當10次沒有連結的時候，重新連線一次。
                                //if (byte_function_03_loop_index > 5)
                                //{
                                //    //重新建立連結
                                //    TcpClientConnect();

                                //    byte_function_03_loop_index = 0;

                                //}
                            }
                        }
                        catch (System.NullReferenceException EXnull)
                        {
                            if (EXnull.Source != null)
                            {
                                Console.WriteLine("M0182:空值處理，重新連線[" + String_DIP + "] Exception source: {0}", EXnull.Source + ":" + EXnull.Message);

                                // 30秒重新連線一次
                                if ((DateTime.Now - disTime).Seconds >= 10)
                                {
                                    isConnect = false;
                                }

                                if (isConnect == false && bool_tcpclientconnect_Action == false)
                                {
                                    disTime = DateTime.Now;
                                    isConnect = true;
                                    //重新建立連結
                                    TcpClientConnect();
                                }

                                //161208 超出時間後再連一次。
                                //byte_function_03_loop_index += 1;

                                //當10次沒有連結的時候，重新連線一次。
                                //if (byte_function_03_loop_index > 5)
                                //{
                                //    //重新建立連結
                                //    TcpClientConnect();

                                //    byte_function_03_loop_index = 0;

                                //}
                            }
                        }
                        catch (Exception EX)
                        {

                            if (EX.Source != null)
                            {

                                Console.WriteLine("M0192:Exception source: {0}", EX.Source + ":" + EX.Message);

                            }
                        }
                    }

                    boolMESnetISrun = false;

                    UpdateValue();

                }

            } while (bool_AutoRun);
        }

        private void SetErrorStatic()
        {
            if (Form1.form1.InvokeRequired)
            {
                Form1.form1.Invoke(new Action(SetErrorStatic), new object[] { });
            }
            else
            {
                Array.Resize(ref String_ReData, Convert.ToInt32(address_index.Split(',')[1]));

                for (int j = 0; j < Convert.ToInt32(address_index.Split(',')[1]); j++)
                {
                    String_ReData[j] = DateTime.Now.ToString("HH:mm:ss") + " " + MPU.static_msg[1];
                    MPU.DataTable_Threads.Rows[int_ThreadNum + j]["Static"] = String_ReData[j];
                }
            }
        }


        DateTime noDataTime = DateTime.Now;
        DateTime UpdateTime = DateTime.Now;
        Boolean isUpdate = true;
        private void UpdateValue()
        {
            if (Form1.form1.InvokeRequired)
            {
                Form1.form1.Invoke(new Action(UpdateValue), new object[] { });
            }
            else
            {
                try
                {
                    string static_str = "";
                    if (!string.IsNullOrEmpty(MPU.DataTable_Threads.Rows[int_ThreadNum]["Static"].ToString()))
                    { 
                        // 若同IP、同PORT時，只讓第一組IP去更新DataTable
                        if (String_ReData.Length == Convert.ToInt32(address_index.Split(',')[1]) && Convert.ToInt32(address_index.Split(',')[0]) == 1)
                        {
                            for (int i = 0; i < Convert.ToInt32(address_index.Split(',')[1]); i++)
                            {
                                MPU.DataTable_Threads.Rows[int_ThreadNum + i]["Static"] = String_ReData[i];
                            }
                        }

                        static_str = String_DIP + " " + String_NOTE + MPU.static_msg[1];
                        if (MPU.DataTable_Threads.Rows[int_ThreadNum]["Static"].ToString().Contains(MPU.static_msg[0]) || MPU.DataTable_Threads.Rows[int_ThreadNum]["Static"].ToString().Contains(MPU.static_msg[1]))
                        {
                            dt = ReadSQLToDT(string.Format("SELECT * FROM tb_connectlog WHERE DIP = '{0}' and ADDRESS = '{1}' and DVALUE = '{2}' and CONTIME IS NULL ORDER BY SYSTIME DESC", String_DIP, String_Address, String_NOTE));
                            if (dt.Rows.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(dt.Rows[0]["CONTIME"].ToString()))
                                {
                                    ReadSQLToDT(string.Format("INSERT INTO tb_connectlog (DIP, ADDRESS, DVALUE, DISTIME, SYSTIME) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", String_DIP, String_Address, String_NOTE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                                }
                            }
                            else
                            {
                                ReadSQLToDT(string.Format("INSERT INTO tb_connectlog (DIP, ADDRESS, DVALUE, DISTIME, SYSTIME) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", String_DIP, String_Address, String_NOTE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                            }
                        }
                        else if (!MPU.DataTable_Threads.Rows[int_ThreadNum]["Static"].ToString().Contains(MPU.static_msg[2]) && !MPU.DataTable_Threads.Rows[int_ThreadNum]["Static"].ToString().Contains(MPU.static_msg[3]))
                        {
                            dt = ReadSQLToDT(string.Format("SELECT TOP (1) * FROM tb_connectlog WHERE DIP = '{0}' and ADDRESS = '{1}' and DVALUE = '{2}' ORDER BY SYSTIME DESC", String_DIP, String_Address, String_NOTE));
                            if (dt.Rows.Count > 0)
                            {
                                if (string.IsNullOrEmpty(dt.Rows[0]["CONTIME"].ToString()))
                                {
                                    //ReadSQLToDT(string.Format("INSERT INTO tb_connectlog (DIP, ADDRESS, DVALUE, CONTIME, SYSTIME) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", String_DIP, String_Address, String_NOTE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                                    ReadSQLToDT(string.Format("UPDATE tb_connectlog SET CONTIME = '{0}' WHERE DIP = '{1}' and ADDRESS = '{2}' and DVALUE = '{3}' and CONTIME IS NULL ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), String_DIP, String_Address, String_NOTE));
                                }
                            }
                        }

                        #region 更新即時警報
                        // 30秒更新一次即時警報一次
                        if (isUpdate)
                        {
                            // 記錄更新當下時間
                            UpdateTime = DateTime.Now;
                            string concat_str = "";
                            for (int j = 0; j < MPU.DataTable_Threads.Rows.Count; j++)
                            {
                                concat_str += "'" + MPU.DataTable_Threads.Rows[j]["DIP"].ToString() + "   " + MPU.DataTable_Threads.Rows[j]["ADDRESS"].ToString() + MPU.DataTable_Threads.Rows[j]["NOTE"].ToString() + "  '" + ",";
                            }
                            concat_str = concat_str.Trim(',');
                            MPU.DataTable_CurrLog = null;
                            MPU.DataTable_CurrLog = ReadSQLToDT(string.Format("SELECT DIP IP, ADDRESS 站號, DVALUE Note, FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間 FROM tb_connectlog WHERE CONTIME IS NULL AND CONCAT(DIP,ADDRESS,DVALUE) IN ({0}) ORDER BY DISTIME DESC", concat_str));
                            Form1.form1.Datagridview_Log[0].DataSource = MPU.DataTable_CurrLog;

                            Form1.form1.Datagridview_Log[0].Columns[0].Width = 100;
                            Form1.form1.Datagridview_Log[0].Columns[1].Width = 60;
                            Form1.form1.Datagridview_Log[0].Columns[2].Width = 859;
                            Form1.form1.Datagridview_Log[0].Columns[3].Width = 224;
                            isUpdate = false;
                        }

                        if ((DateTime.Now - UpdateTime).Seconds >= 30)
                        {
                            isUpdate = true;
                        }
                        #endregion                        
                    }

                    if (MPU.DataTable_Threads.Rows[int_ThreadNum]["Static"].ToString().Contains(MPU.static_msg[3]))
                    {
                        //MPU.DataTable_Threads.Rows[int_ThreadNum]["Static"] = String_ReData[0];
                        // 30秒內若撈不到I/O的資料就更改狀態
                        if ((DateTime.Now - noDataTime).Seconds >= 30)
                        {
                            MPU.DataTable_Threads.Rows[int_ThreadNum]["Static"] = MPU.static_msg[2];
                        }
                        else
                        {
                            MPU.DataTable_Threads.Rows[int_ThreadNum]["Static"] = MPU.static_msg[3];
                            //MPU.DataTable_Threads.Rows[int_ThreadNum]["Static"] = MPU.static_msg[3];
                        }
                    }

                    if (bool_isThreadSet)
                    {
                        Form1.form1.ChangeColor(int_ThreadNum);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw ex;
                }
            }
        }

        #region 讀取SQL資料庫
        /// <summary>
        /// 讀取SQL資料庫(請確認SQL指令是否正確)
        /// </summary>
        /// <param name="pSQL">SQL指令</param>
        private DataTable ReadSQLToDT(string pSQL)
        {
            DataTable dtSource = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection("server=192.168.0.180;Initial Catalog=dbMES;Persist Security Info=True;User ID=sa;Password=28921148"))
                {
                    SqlCommand cmd = new SqlCommand(pSQL, conn);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dtSource);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dtSource;
        }
        #endregion

        #region 寫入資料
        /// <summary>
        /// 寫入資料(請確認路徑是否正確)
        /// </summary>
        /// <param name="path">路徑</param>
        /// <param name="name">檔名</param>
        /// <param name="content">寫入內容(若未找到指定檔案則會生成)</param>
        public void WriteData(string path, string name, string content)
        {
            try
            {
                string write_path = path + @"\" + name;

                using (System.IO.StreamWriter wf = new System.IO.StreamWriter(write_path, true))
                {

                    wf.WriteLine(content);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        #endregion

        #region 計算CRC檢查碼
        /// <summary>
        /// 計算CRC檢查碼(請確認CRC指令是否正確)
        /// </summary>
        /// <param name="CRC">CRC指令</param>
        public static String CRC16LH(String CRC)
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

        #region 溫度程式碼
        void function_13()
        {
            for (int i = 0; i < Convert.ToInt32(address_index.Split(',')[1]); i++)
            {
                try
                {

                    String_SeData13[1] = "03 02 4C 00 04";
                    String_SeData13[1] = CRC16LH(GetModbusCommand((i + 1).ToString(), String_SeData13[1]));
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
                        Array.Resize(ref String_ReData, Convert.ToInt32(address_index.Split(',')[1]));
                        String_ReData[i] = DateTime.Now.ToString("HH:mm:ss") + " ... " + str_1 + " : " + str_2 + " : " + str_3 + " : " + str_4;

                        String_SQLcommand = "INSERT INTO [dbo].[tb_recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID.Split('.')[0].ToString() + "','" + str_1 + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "') ";

                    }
                    else
                    {
                        String_ReData[i] = DateTime.Now.ToString("HH:mm:ss") + " " + MPU.static_msg[1];
                    }
                }
                catch (Exception EX)
                {

                    if (EX.Source != null)
                    {

                        String_ReData[i] = DateTime.Now.ToString("HH:mm:ss") + " " + MPU.static_msg[1];

                        Console.WriteLine("M0312:Exception source: {0}", EX.Source);

                    }
                }
            }
        }
        #endregion

        void function_14()
        {
            try

            {
                //17122601
                if (MPU.str_Barcode != "" )
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
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','SWD001','4','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "'); ";

                                }
                                else
                                {
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','SWD001','0','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "'); ";

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

                        String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;

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

                                    String_SQLcommand += "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";

                                }
                                else if (String_Dline == "P3")
                                {
                                    String_SQLcommand += "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";

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

                                String_SQLcommand += "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";


                            }
                            else if (String_Dline == "P3")
                            {
                                String_SQLcommand += "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";

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

                        String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "...<468>" + int_Net_Available + "@" + str_ttt + "，未解碼次數(" + byte_function_03_loop_index + "):" + String_Sclass;

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

                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "...[" + byte_function_03_loop_index + "]Ex:" + EX.Source + EX.Message;

                    Console.WriteLine("M0516:燈號異常:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss[{0}]"), EX.Source + EX.Message);

                    byte_function_03_loop_index += 1;

                }
            }
        }

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

                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "..." + str_T + ":" + str_H + String_Sclass;

                }

            }
            catch (Exception EX)
            {

                if (EX.Source != null)
                {

                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "...Ex:" + EX.Source;

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

                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;

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
                            String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID]           ,[DIP]           ,[SID]           ,[DVALUE]           ,[SYSTIME])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "') ; ";

                        }
                        if (String_Dline == "P3")
                        {
                            String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID]           ,[DIP]           ,[SID]           ,[DVALUE]           ,[SYSTIME])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "') ;";

                        }


                        //171119 


                        String light_Str;

                        light_Str = "0100";

                        String Str_SQL_sid_X_WLS = "SELECT S_ID FROM [dbMES].[dbo].[tb_sensors_rules] WHERE d_ID =  (SELECT d_ID FROM   [dbMES].[dbo].[tb_sensors_rules]  WHERE s_ID = '" + String_SID + "') AND S_ID LIKE '%WLS%'";

                        if (String_Dline == "P2")
                        {

                            String_SQLcommand += "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "',(" + Str_SQL_sid_X_WLS + "),'" + light_Str + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ;";

                        }
                        else if (String_Dline == "P3")
                        {
                            String_SQLcommand += "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "',(" + Str_SQL_sid_X_WLS + "),'" + light_Str + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ;";

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
                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "...Available[" + int_Net_Available + "]:[" + NetworkStream_Reader.CanRead.ToString() + "]@無資料:" + String_Sclass;

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

                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "...Ex:" + EX.Source;

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
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','SWD001','4','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "'); ";

                                }
                                else
                                {
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','SWD001','0','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "'); ";

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

                        String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;

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

                                    String_SQLcommand += "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";

                                }
                                else if (String_Dline == "P3")
                                {
                                    String_SQLcommand += "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";

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

                                String_SQLcommand += "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";


                            }
                            else if (String_Dline == "P3")
                            {
                                String_SQLcommand += "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + String_NOTE + "') ";

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

                        String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "...<468>" + int_Net_Available + "@" + str_ttt + "，未解碼次數(" + byte_function_03_loop_index + "):" + String_Sclass;

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

                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "...[" + byte_function_03_loop_index + "]Ex:" + EX.Source + EX.Message;

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
                                String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";

                            }
                            if (String_SQLcommand == "" && str_DTS_temp[xx] != double_Mpa.ToString("F3") && String_Dline == "P3")
                            {
                                String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";

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
                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + "vi資料異常:" + String_Sclass;

                    //資料異常就不要進資料庫 
                    String_SQLcommand = "";
                }
                else
                {
                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;
                }
            }

            catch (Exception EX)
            {

                if (EX.Source != null)
                {

                    Console.WriteLine("M0733:Exception source: {0}", EX.Source);

                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "...[" + byte_function_03_loop_index + "]Ex:" + EX.Source + EX.Message;

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
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";

                                }
                                if (String_Dline == "P3")
                                {
                                    String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";

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

                String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;

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
                        String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";

                    }
                    if (String_Dline == "P3")
                    {
                        String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";

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

                String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;

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

                                String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                                bool_add_Values = false;
                            }
                            if (String_SQLcommand == "" && String_Dline == "P3")
                            {

                                String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
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
                                String_SQLcommand = "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                                bool_add_Values = false;
                            }
                            if (String_SQLcommand == "" && String_Dline == "P3")
                            {
                                String_SQLcommand = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
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

                            String_SQLcommand = "; INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                            bool_add_Values = false;
                        }
                        if (String_SQLcommand == "" && String_Dline == "P3")
                        {

                            String_SQLcommand = "; INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
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
                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "..." + int_Net_Available + "@" + str_T + ":" + String_Sclass;
                }
            }

            catch (Exception EX)
            {

                if (EX.Source != null)
                {

                    Console.WriteLine("M0733:Exception source: {0}", EX.Source);

                    String_ReData[0] = DateTime.Now.ToString("HH:mm:ss") + "...[" + byte_function_03_loop_index + "]Ex:" + EX.Source + EX.Message;

                }
            }


        }

        //紅燈
        string[] light1_1 = { "02", "03", "00", "00", "00", "01", "84", "39" };
        //綠燈
        string[] light2_1 = { "02", "03", "00", "01", "00", "01", "D5", "F9" };

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
                    //if設備為IPA
                    {
                        //正壓ad位置為5
                        Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(eight5_1[i], 16));
                        //正壓ad位置為6
                        Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(eight6_1[i], 16));
                    }
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

        //流量計
        void function_11_flow()
        {
            try
            {
                string str_T = "";
                for (int i = 0; i < 8; i++)
                {
                    //if流量計ad位置為3
                    {
                        Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(eight1_1[i], 16));
                        //Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(eight2_1[i], 16));
                        //Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(eight3_1[i], 16));
                    }
                    //if流量計ad位置為4
                    {
                    //    Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(eight4_1[i], 16));
                    }
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
                    //if設備是SST(100L)
                    {
                        double_Mpa = (((Convert.ToDouble(eight[0])) * 20 / 4095 - 4) / 0.16) * (100 / 100);
                    }
                    //else(50L)
                    {
                        double_Mpa = (((Convert.ToDouble(eight[0])) * 20 / 4095 - 4) / 0.16) * (50 / 100);
                    }
                }
            }
            catch
            {

            }
        }

        //燈號
        void function_12_light()
        {
            try
            {
                //紅燈
                string str_T_1 = "";
                for (int i = 0; i < 8; i++)
                {
                    Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(light1_1[i], 16));
                }
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, 8);

                for (int i = 0; i <= 50; i++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = TcpClient_Reader.Available;

                NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                String str_hex_1 = "";
                double double_Mpa_1 = 0;

                str_hex_1 = Convert.ToString(Byte_Command_Re[4], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[5], 16).PadLeft(2, '0');

                int[] eight1 = new int[str_hex_1.Length];
                for (int i = 0; i < eight1.Length; i = i + 4)
                {
                    eight1[i / 4] = Convert.ToInt32(str_hex_1.Substring(i, 4), 16);
                    //感測值結果
                    double_Mpa_1 = eight1[0];
                }

                //綠燈
                string str_T_2 = "";
                for (int i = 0; i < 8; i++)
                {
                    Byte_Command_Sent[i] = Convert.ToByte(Convert.ToInt32(light2_1[i], 16));
                }
                NetworkStream_Reader.Write(Byte_Command_Sent, 0, 8);

                for (int i = 0; i <= 50; i++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                //int int_Net_Available = TcpClient_Reader.Available;

                NetworkStream_Reader.Read(Byte_Command_Re, 0, int_Net_Available);

                String str_hex_2 = "";
                double double_Mpa_2 = 0;

                str_hex_2 = Convert.ToString(Byte_Command_Re[4], 16).PadLeft(2, '0') + Convert.ToString(Byte_Command_Re[5], 16).PadLeft(2, '0');

                int[] eight2 = new int[str_hex_2.Length];
                for (int i = 0; i < eight2.Length; i = i + 4)
                {
                    eight2[i / 4] = Convert.ToInt32(str_hex_2.Substring(i, 4), 16);
                    //感測值結果
                    double_Mpa_2 = eight2[0];
                }

                if (double_Mpa_1 > 10000)
                {
                    //紅燈(停止中)
                }
                else if (double_Mpa_2 > 10000)
                {
                    //綠燈(運行中)
                }
                else
                {
                    //橘燈(暫停中)
                }
            }
            catch
            {

            }
        }
    }


}
