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
        #region 指令宣告
        //485感測器8Port全讀
        string strCmd485_8Port = "01 03 00 00 00 08 44 0C";

        //氣壓
        string strCmdEight_1 = "01 03 00 00 00 01 84 0A";
        string strCmdEight_2 = "01 03 00 01 00 01 D5 CA";
        string strCmdEight_5 = "01 03 00 04 00 01 C5 CB";
        string strCmdEight_6 = "01 03 00 05 00 01 94 0B";

        //流量                                  
        string strCmdEight_3 = "01 03 00 02 00 01 25 CA";
        string strCmdEight_4 = "01 03 00 03 00 01 74 0A";

        //紅燈                                  
        string strCmdLight_1 = "02 03 00 00 00 01 84 39";
        //綠燈                                  
        string strCmdLight_2 = "02 03 00 01 00 01 D5 F9";

        //環境溫濕度
        string strCmdEHCEHD = "00 00 00 00 00 00 01 03 00 00 00 02";

        //溫度
        string strCmd4PortTemp = "01 03 02 4C 00 04 84 66";
        string strCmdBrainChild = "03 03 00 80 00 01 84 00";

        //氮氣流量
        string strCmdAFR = "00 00 00 00 00 06 01 03 00 04 00 02";
        #endregion

        #region 變數宣告
        //儲存設備、感測器參數和設定
        public string strTID = "";
        public string strDline = "";
        public string strDIP = "";
        public string strPort = "";
        public string strAddress = "";
        public string strNote = "";
        public string strPortId = "";
        public string strSID = "";
        public string strSclass = "";

        public NetworkStream NetworkStream_Reader;
        public TcpClient TcpClient_Reader;
        public Socket clientSocket;

        bool booleanInsert = true;
        //記錄是否有填入values了
        bool bool_add_Values = false;
        //確保同一時間只能建立一個連線
        bool bool_passive = false;
        //製作一個boolean 代表只有單一個動作在執行
        public bool boolMESnetISrun = false;
        //執行緒開關
        public bool bool_AutoRun = false;
        //執行緒是否成功啟動，啟動後會更新為true
        public bool bool_isThreadSet = false;

        int byte_function_03_loop_index = 0;
        int int_WLSLCS_Count = 0;
        int int_DTS_temp_count = 0;
        //等待重新連線計數
        int int_ReconnectWait = 0;
        //485感測器通道
        int intSencorPort = 0;
        //更新dgv計數器
        int intCount = 0;
        //儲存執行緒內for迴圈當下的index
        private int intIndex = 0;
        //儲存執行緒的編號
        public int int_ThreadNum = 0;
        //儲存執行緒要休息的時間
        public int int_ReaderSleep = 0;

        //儲存下一次執行的時間
        DateTime NextRumTime = new DateTime();
        List<DateTime> listDisconnectTime = new List<DateTime>();

        //儲存SQL指令
        public StringBuilder sbSQL = new StringBuilder();

        //儲存Form整理好的IP和Port字典
        public Dictionary<String, List<String>> dicSclass = new Dictionary<String, List<String>>();

        List<int> listIndex = new List<int>();

        //儲存SQL語法
        public string strCmdSQL = "";
        //儲存各個感測器狀態或數值
        public string[] strStatic = new string[] { };

        //儲存Barcode值
        public string strBarcode = "";
        public string strBarcodeLight = "";
        //記錄上次燈號是否為閃爍用
        string Str_lcs = "";
        //記錄insert過的燈號，重覆的話不傳
        string Str_WLSLCS_F = "";
        string[] str_DTS_temp = new string[8];

        //儲存送出的Socket指令
        byte[] byteCmdSend = new byte[100];
        //儲存回傳的Socket指令
        byte[] byteCmdReceive = new byte[100];
        byte[] temCRC = new byte[2];
        byte[] Byte_Command_Sent_082 = new byte[8];
        byte[] Byte_Command_Sent_083 = new byte[9];
        #endregion

        #region 設定預設指令
        void CommandSet()
        {
            // 02 03 02 4C 00 04 84 55 讀取第1台 4通道 ID02
            // 03 03 02 4C 00 04 85 84 讀取第2台 4通道 ID03 
            StrArrayToByteArray("02 03 02 4C 00 04 84 55", Byte_Command_Sent_082);

            StrArrayToByteArray("03 03 02 4C 00 04 85 84", Byte_Command_Sent_083);

            StrArrayToByteArray("01 03 00 00 00 08 44 0C", byteCmdSend);
        }

        void StrArrayToByteArray(String StringArray, byte[] ByteArray)
        {
            for (int i = 0; i <= StringArray.Split(' ').Length - 1; i++)
            {
                ByteArray[i] = Convert.ToByte(StringArray.Split(' ')[i], 16);
            }
        }
        #endregion

        #region 建立Socket連線
        private delegate string ConnectSocketDelegate(IPEndPoint ipep, Socket sock);
        public void SocketClientConnect()
        {
            if (bool_passive == false && bool_AutoRun == true)
            {
                bool_passive = true;

                // 關閉並清空原本的連線
                if (clientSocket != null)
                {
                    try
                    {
                        clientSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[SocketClientConnect] Socket斷線失敗"+ex.Message );
                    }
                    clientSocket.Close();
                    clientSocket = null;
                }

                //設定IP和Port
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(strDIP.TrimEnd(' ')), Convert.ToInt32(strPort));

                //new一個新的Socket連線
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //設定非同步連線
                ConnectSocketDelegate connect = ConnectSocket;
                IAsyncResult asyncResult = connect.BeginInvoke(ipep, clientSocket, null, null);

                //啟動非同步連線，並設定連線Timeout為500毫秒
                bool connectSuccess = asyncResult.AsyncWaitHandle.WaitOne(500, false);
                if (!connectSuccess)
                {
                    Console.WriteLine($"[SocketClientConnect] {strDIP}:{strPort} 連線失敗！");
                }
                else
                {
                    clientSocket.ReceiveTimeout = 500;
                    clientSocket.SendTimeout = 500;
                    CommandSet();
                    int_ReconnectWait = 0;
                    Console.WriteLine($"[SocketClientConnect] {strDIP}:{strPort} 連線成功");
                }


                #region 舊的
                ////宣告tcp連接 
                //byte[] Bytes_IP = new byte[4];
                //for (int i = 0; i <= 3; i++)
                //{
                //    Bytes_IP[i] = Convert.ToByte(strDIP.TrimEnd(' ').Split('.')[i]);
                //}

                //try
                //{
                //    if (TcpClient_Reader != null)
                //    {
                //        TcpClient_Reader.Close();
                //        TcpClient_Reader = null;
                //        Thread.Sleep(100);
                //    }

                //    if (NetworkStream_Reader != null)
                //    {
                //        NetworkStream_Reader.Close();
                //        NetworkStream_Reader = null;
                //        Thread.Sleep(100);
                //    }

                //    TcpClient_Reader = TimeOutSocket.Connect(new System.Net.IPEndPoint(new System.Net.IPAddress(Bytes_IP), Convert.ToInt16(strPort)), 500);

                //    Thread.Sleep(100);
                //    //
                //    if (!(TcpClient_Reader == null) && TcpClient_Reader.Connected)
                //    {
                //        //連結成功
                //        NetworkStream_Reader = TcpClient_Reader.GetStream();
                //        if (NetworkStream_Reader == null)
                //        {
                //            string ss = "";
                //        }

                //        TcpClient_Reader.ReceiveTimeout = 100;

                //        TcpClient_Reader.SendTimeout = 100;

                //        TcpClient_Reader.ReceiveTimeout = 100;

                //        TcpClient_Reader.SendTimeout = 100;

                //        NetworkStream_Reader.WriteTimeout = 100;

                //        NetworkStream_Reader.ReadTimeout = 100;

                //        //Int_GetCount = 0;

                //        TcpClient_Reader.ReceiveBufferSize = 1024;

                //        //成功啟動後，把執行緒休息時間改為設計值
                //        int_ReaderSleep = int_ReaderSleepSET;

                //        CommandSet();

                //        Console.WriteLine($"連線成功");
                //    }
                //}
                //catch (Exception EX)
                //{
                //    Console.WriteLine($"連線失敗");

                //    if (EX.Source != null)
                //    {
                //        Console.WriteLine("M0091:Exception source: {0}", strDIP + "[" + EX.Message + "]");

                //        if (EX.Message == "TimeOut Exception (TimeOutSocket-0040)")
                //        {
                //            //初此啟動之後，把此執行緒休息時間延長至10秒
                //            int_ReaderSleep = 1000;
                //            //System.Threading.Thread.Sleep(10000);
                //        }
                //    }

                //}
                #endregion

                bool_passive = false;
            }
        }

        private string ConnectSocket(IPEndPoint ipep, Socket sock)
        {
            string exmessage = "";
            try
            {
                sock.Connect(ipep);
            }
            catch (Exception ex)
            {
                exmessage = ex.Message;
            }
            finally
            {
            }

            return exmessage;
        }
        #endregion

        #region 主執行緒
        public void MesNetSiteRunning()
        {
            //初次啟動執行緒先建立連線
            SocketClientConnect();

            //事先紀錄卡在連線中的設備要顯示連線失敗的時間(30秒後)
            listDisconnectTime = dicSclass[strPort].Select(t => DateTime.Now.AddSeconds(30)).ToList();

            while (bool_AutoRun)
            {
                try
                {

                    if (DateTime.Now >= NextRumTime)
                    {
                        //161208 加入判斷tcpclient是否在連結中，如果在連結中就不要做其他動作。
                        if (boolMESnetISrun == false)
                        {
                            boolMESnetISrun = true;

                            //每50秒收集一次設備數據
                            NextRumTime = DateTime.Now.AddSeconds(50);

                            // 清除舊的SQL語法
                            sbSQL.Clear();
                            strCmdSQL = "";
                            listIndex.Clear();

                            Array.Resize(ref strStatic, Convert.ToInt32(dicSclass[strPort].Count));

                            //跑迴圈來讀同IP同Port但不同感測器的值
                            for (int i = 0; i < dicSclass[strPort].Count; i++)
                            {
                                try
                                {
                                    //初始化參數
                                    intSencorPort = 0;
                                    intIndex = i;
                                    listIndex.Add(i);
                                    strTID = MPU.dtMainTable.Rows[int_ThreadNum + intIndex]["TID"].ToString();
                                    strSID = MPU.dtMainTable.Rows[int_ThreadNum + intIndex]["SID"].ToString();
                                    strNote = MPU.dtMainTable.Rows[int_ThreadNum + intIndex]["NOTE"].ToString();
                                    if (dicSclass[strPort][i].Split(';')[0].Split('_').Length == 2)
                                        intSencorPort = Convert.ToInt32(dicSclass[strPort][i].Split(';')[0].Split('_')[1]);
                                    string sclass = dicSclass[strPort][intIndex].Split(';')[0].Split('_')[0];

                                    if (CheckSocket(sclass))
                                    {
                                        //根據不同的設備來執行不同的判斷連線方式
                                        switch (sclass)
                                        {
                                            case "1": //環境溫濕度
                                                function_01();
                                                break;
                                            case "2": //條碼
                                                function_02();
                                                break;
                                            case "3": //控制燈號模組
                                                function_03();
                                                break;
                                            case "4":
                                                function_DTS();
                                                break;
                                            case "5":
                                                function_05();
                                                break;
                                            case "6":
                                                function_06();
                                                break;
                                            case "7": //221209
                                                function_07();
                                                break;
                                            case "8":
                                                function_DTS_8();
                                                break;
                                            case "9": //正壓

                                                break;
                                            case "10": //負壓

                                                break;
                                            case "11": //流量計
                                                function_11(intSencorPort);
                                                break;
                                            case "12": //燈號判斷
                                                function_12();
                                                break;
                                            case "13": //溫度
                                                function_13();
                                                break;
                                            case "14": //燈號控制
                                                function_14();
                                                break;
                                            case "15": //正壓、負壓(8_1、8_2、8_5、8_6)
                                                function_15(intSencorPort);
                                                break;
                                            case "16": //PC S1F1 燈號
                                                function_16();
                                                break;
                                            case "17": //PC S1F2 壓力
                                                function_17();
                                                break;
                                            case "18": //PC S1F3 流量
                                                function_18();
                                                break;
                                            case "19": //PC S1F4 溫度
                                                function_19();
                                                break;
                                            case "20": //PC S1F5 吸嘴阻值
                                                function_20();
                                                break;
                                            case "21": //PC S1F6 H-Judge讀值
                                                function_21();
                                                break;
                                            case "22": //PC S1F7 螢幕辨識參數(Temperature, Power, force, Time)
                                                function_22();
                                                break;
                                            case "23": //BrainChild溫度
                                                function_23();
                                                break;
                                            case "24": //PC S1F8 舉離機(NMPA, NMPB)
                                                function_24();
                                                break;
                                            case "25": //新氣壓頭讀取
                                                function_25(intSencorPort);
                                                break;
                                            case "27": //晶圓清洗機#1 燈號判斷
                                                function_27();
                                                break;
                                            case "28": //電漿蝕刻機#2 燈號判斷
                                                function_28();
                                                break;
                                            case "29": //Cello RIE反應式離子蝕刻機-1 燈號判斷
                                                function_29();
                                                break;
                                            case "30": //廠務設備 氮氣流量
                                                function_30();
                                                break;
                                            case "32": //廠務設備 水流量
                                                function_32();
                                                break;
                                            case "33": //調頻機燈號判斷
                                                function_33();
                                                break;
                                            case "34": //清洗機水阻值
                                                function_34();
                                                break;
                                            case "26": //只PING設備IP，連線成功就回傳黃燈
                                                function_26();
                                                break;
                                            case "31": //廠務設備 比電阻
                                                function_31();
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        if (boolMESnetISrun == false)
                                        {
                                            SocketClientConnect();
                                        }
                                    }

                                    #region (註解)原方法
                                    //連結成功才動作
                                    //if (clientSocket != null && !clientSocket.Poll(1, SelectMode.SelectRead) && clientSocket.Connected == true)
                                    //{
                                    //    switch (dicDeviceList[strPort][i].Split(';')[0].Split('_')[0])
                                    //    {
                                    //        case "1": //環境溫濕度
                                    //            function_01();
                                    //            break;
                                    //        case "2": //條碼
                                    //            function_02();
                                    //            break;
                                    //        case "4":
                                    //            function_DTS();
                                    //            break;
                                    //        case "5":
                                    //            function_KPS();
                                    //            break;
                                    //        case "6":
                                    //            function_06();
                                    //            break;
                                    //        case "7":
                                    //            function_07();
                                    //            break;
                                    //        case "8":
                                    //            function_DTS_8();
                                    //            break;
                                    //        case "9": //正壓
                                    //            function_09_positive();
                                    //            break;
                                    //        case "10": //負壓
                                    //            function_10_negative();
                                    //            break;
                                    //        case "11": //流量計
                                    //            if (dicDeviceList[strPort][i].Split(';')[0].Split('_').Length == 2)
                                    //                intSencorPort = Convert.ToInt32(dicDeviceList[strPort][i].Split(';')[0].Split('_')[1]);
                                    //            function_11_flow(intSencorPort);
                                    //            break;
                                    //        case "12": //燈號判斷
                                    //            function_12_light();
                                    //            break;
                                    //        case "13": //溫度
                                    //            function_13();
                                    //            break;
                                    //        case "14": //燈號控制
                                    //            function_14();
                                    //            break;
                                    //        case "15": //正壓、負壓(8_1、8_2、8_5、8_6)
                                    //            if (dicDeviceList[strPort][i].Split(';')[0].Split('_').Length == 2)
                                    //                intSencorPort = Convert.ToInt32(dicDeviceList[strPort][i].Split(';')[0].Split('_')[1]);
                                    //            function_15(intSencorPort);
                                    //            break;
                                    //        case "16": //PC S1F1 燈號
                                    //            function_16_S1F1_light();
                                    //            break;
                                    //        case "17": //PC S1F2 壓力
                                    //            function_17_S1F2_pressure();
                                    //            break;
                                    //        case "18": //PC S1F3 流量
                                    //            function_18_S1F3_flow();
                                    //            break;
                                    //        case "19": //PC S1F4 溫度
                                    //            function_19_S1F4_temperature();
                                    //            break;
                                    //        case "20": //PC S1F5 吸嘴阻值
                                    //            function_20_S1F5_resistance();
                                    //            break;
                                    //        case "21": //PC S1F6 H-Judge讀值
                                    //            function_21_S1F6_H_Judge();
                                    //            break;
                                    //        case "22": //PC S1F7 螢幕辨識參數(Temperature, Power, force, Time)
                                    //            function_22_S1F7_shoucut();
                                    //            break;
                                    //        case "23": //BrainChild溫度
                                    //            function_23_brainchild();
                                    //            break;
                                    //        case "24": //PC S1F8 舉離機(NMPA, NMPB)
                                    //            function_24_S1F8();
                                    //            break;
                                    //        case "25": //新氣壓頭讀取
                                    //            if (dicDeviceList[strPort][i].Split(';')[0].Split('_').Length == 2)
                                    //                intSencorPort = Convert.ToInt32(dicDeviceList[strPort][i].Split(';')[0].Split('_')[1]);
                                    //            function_25(intSencorPort);
                                    //            break;
                                    //        case "27": //晶圓清洗機#1 燈號判斷
                                    //            function_27_light();
                                    //            break;
                                    //        case "28": //電漿蝕刻機#2 燈號判斷
                                    //            function_28_light();
                                    //            break;
                                    //        case "29": //Cello RIE反應式離子蝕刻機-1 燈號判斷
                                    //            function_29_light();
                                    //            break;
                                    //        case "30": //廠務設備 氮氣流量
                                    //            function_AFR();
                                    //            break;
                                    //        case "32": //廠務設備 水流量
                                    //            function_LFM();
                                    //            break;
                                    //        case "33": //調頻機燈號判斷
                                    //            function_33();
                                    //            break;
                                    //        case "34": //清洗機水阻值
                                    //            function_34();
                                    //            break;
                                    //        default:
                                    //            //SetStatus();
                                    //            break;
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    switch (dicDeviceList[strPort][i].Split(';')[0].Split('_')[0])
                                    //    {
                                    //        case "0": //寫假資料讓燈號亮黃燈
                                    //            function_0();
                                    //            break;
                                    //        case "3": //控制燈號模組
                                    //            if (clientSocket != null && clientSocket.Connected == true)
                                    //                function_WLS_LCS();
                                    //            break;
                                    //        case "26": //只PING設備IP，連線成功就回傳黃燈
                                    //            function_26_PingIP();
                                    //            break;
                                    //        case "31": //廠務設備 比電阻
                                    //            function_RESOHM();
                                    //            break;
                                    //        default:
                                    //            break;
                                    //    }
                                    //}
                                    #endregion
                                }
                                catch (System.NullReferenceException EXnull)
                                {
                                    if (EXnull.Source != null)
                                    {
                                        Console.WriteLine("M0182:空值處理，重新連線[" + strDIP + "] Exception source: {0}", EXnull.Source + ":" + EXnull.Message);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (ex.Source != null)
                                    {
                                        Console.WriteLine("M0192:Exception source: {0}", ex.Source + ":" + ex.Message);
                                    }
                                }

                                #region (註解)更新connectlog資料表
                                if (bool_AutoRun)
                                {
                                    //DataTable dt = new DataTable();
                                    //if (isAlarmExist == false && clientSocket != null && !((!(clientSocket.Poll(1, SelectMode.SelectRead) && clientSocket.Available == 0)) && clientSocket.Connected == true))
                                    //{
                                    //    isAlarmExist = true;
                                    //    // IF判斷如果不存在斷線紀錄
                                    //    //dt = ReadSQLToDT($"SELECT * FROM tb_connectlog WHERE SID = '{strSID}' and CONTIME IS NULL");

                                    //    sbSQL.AppendFormat(@"BEGIN TRAN
                                    //                            IF NOT EXISTS (SELECT * FROM tb_connectlog 
                                    //                            WHERE SID = '{0}' and CONTIME IS NULL)",
                                    //                            strSID);
                                    //    sbSQL.AppendLine();

                                    //    // 新增斷線紀錄
                                    //    //if (dt.Rows.Count > 0)
                                    //    //{
                                    //    //    sbSQL.Append("  INSERT INTO ");
                                    //    //    sbSQL.AppendFormat(@"tb_connectlog (DIP, ADDRESS, SID, DVALUE, DISTIME, SYSTIME) 
                                    //    //                        VALUES ('{0}', {1}, '{2}', '{3}', '{4}', '{5}')",
                                    //    //                        strDIP, strAddress, strSID, strNote, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    //    //    sbSQL.AppendLine();
                                    //    //}
                                    //    sbSQL.Append("  INSERT INTO ");
                                    //    sbSQL.AppendFormat(@"tb_connectlog (DIP, ADDRESS, SID, DVALUE, DISTIME, SYSTIME) 
                                    //                            VALUES ('{0}', {1}, '{2}', '{3}', '{4}', '{5}')
                                    //                        IF(@@ERROR<>0)
                                    //                         ROLLBACK TRAN;
                                    //                        ELSE
                                    //                         COMMIT TRAN;",
                                    //                        strDIP, strAddress, strSID, strNote, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    //    sbSQL.AppendLine();
                                    //}
                                    //else
                                    //{
                                    //    isAlarmExist = false;
                                    //    // IF判斷如果存在斷線紀錄，且沒有重新連線紀錄
                                    //    //dt = ReadSQLToDT($"SELECT * FROM tb_connectlog WHERE SID = '{strSID}' and CONTIME IS NULL");

                                    //    sbSQL.AppendFormat(@"BEGIN TRAN
                                    //                            IF EXISTS (SELECT * FROM tb_connectlog 
                                    //                            WHERE SID = '{0}' and CONTIME IS NULL)",
                                    //                                strSID);
                                    //    sbSQL.AppendLine();

                                    //    // 更新原本斷線紀錄，將連線時間更新上去
                                    //    //if (dt.Rows.Count > 0)
                                    //    //{
                                    //    //    sbSQL.Append("  UPDATE ");
                                    //    //    sbSQL.AppendFormat(@"tb_connectlog SET CONTIME = '{0}' 
                                    //    //                            WHERE  
                                    //    //                            SID = '{1}' and  
                                    //    //                            CONTIME IS NULL",
                                    //    //                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), strSID);
                                    //    //    sbSQL.AppendLine();
                                    //    //}
                                    //    sbSQL.Append("  UPDATE ");
                                    //    sbSQL.AppendFormat(@"tb_connectlog SET CONTIME = '{0}' 
                                    //                                WHERE  
                                    //                                SID = '{1}' and  
                                    //                                CONTIME IS NULL
                                    //                        IF(@@ERROR<>0)
                                    //                         ROLLBACK TRAN;
                                    //                        ELSE
                                    //                         COMMIT TRAN;",
                                    //                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), strSID);
                                    //    sbSQL.AppendLine();
                                    //}
                                }
                                #endregion

                                //舊版本MES的SQL語法會偶發性產生只有一個分號的SQL語法，尚未修正
                                //因此先判斷遇到只有分號時將SQL語法清除
                                if (strCmdSQL == ";")
                                    strCmdSQL = "";

                                //若strCmdSQL不是空字串，就存入listSQL
                                if (!string.IsNullOrWhiteSpace(strCmdSQL))
                                {
                                    MPU.listSQL.Add(strCmdSQL);
                                    strCmdSQL = "";
                                }
                                //更新設備狀態
                                SetStatus(intIndex);
                            }

                            //20221207改為由Form的Timer統一寫入資料庫
                            #region  (註解)判斷目前感測器連線狀態，連線正常就回寫資料庫
                            //if (false)
                            //{
                            //    switch (dicDeviceList[strPort][intIndex].Split(';')[0].Split('_')[0])
                            //    {
                            //        case "3": //控制燈號模組
                            //            #region 無法用Socket.Poll判斷連線
                            //            if (clientSocket != null && clientSocket.Connected == true)
                            //            {
                            //                if (!string.IsNullOrWhiteSpace(sbSQL.ToString()))
                            //                {
                            //                    if (Check_Connection.CheckConnaction())
                            //                    {
                            //                        MPU.ReadSQL(sbSQL.ToString());
                            //                    }
                            //                    else
                            //                    {
                            //                        MPU.ReadSQL_dbMEStemp(sbSQL.ToString());
                            //                    }
                            //                }
                            //            }
                            //            #endregion
                            //            break;
                            //        case "0"://寫假資料讓燈號亮黃燈
                            //        case "26": //只PING設備IP，連線成功就回傳黃燈
                            //        case "31": //廠務設備 比電阻
                            //            #region 無須建立連線
                            //            if (!string.IsNullOrWhiteSpace(sbSQL.ToString()))
                            //            {
                            //                if (Check_Connection.CheckConnaction())
                            //                {
                            //                    MPU.ReadSQL(sbSQL.ToString());
                            //                }
                            //                else
                            //                {
                            //                    MPU.ReadSQL_dbMEStemp(sbSQL.ToString());
                            //                }
                            //            }
                            //            #endregion
                            //            break;
                            //        default:
                            //            #region 可以用Socket.Poll判斷連線
                            //            if (clientSocket != null && !clientSocket.Poll(1, SelectMode.SelectRead) && clientSocket.Connected == true)
                            //            {
                            //                if (!string.IsNullOrWhiteSpace(sbSQL.ToString()))
                            //                {
                            //                    if (Check_Connection.CheckConnaction())
                            //                    {
                            //                        MPU.ReadSQL(sbSQL.ToString());
                            //                    }
                            //                    else
                            //                    {
                            //                        MPU.ReadSQL_dbMEStemp(sbSQL.ToString());
                            //                    }
                            //                }
                            //            }
                            //            #endregion
                            //            break;
                            //    }
                            //}
                            #endregion

                            boolMESnetISrun = false;
                        }
                    }

                    #region  (註解)每30秒判斷目前感測器連線狀態，連線失敗就等待重新連線
                    //if (DateTime.Now >= CheckConnectTime)
                    //{
                    //    int_ReconnectWait = 0;
                    //    CheckConnectTime = DateTime.Now.AddSeconds(30);
                    //    switch (dicSclass[strPort][intIndex].Split(';')[0].Split('_')[0])
                    //    {
                    //        #region 無法用Socket.Poll判斷連線
                    //        case "3": //控制燈號模組
                    //            if (clientSocket == null || clientSocket.Connected == false)
                    //            {
                    //                SocketClientConnect();
                    //            }
                    //            break;
                    //        #endregion

                    //        #region 無須建立連線
                    //        case "0"://寫假資料讓燈號亮黃燈
                    //        case "26": //只PING設備IP，連線成功就回傳黃燈
                    //        case "31": //廠務設備 比電阻
                    //            break;
                    //        #endregion

                    //        #region 可以用Socket.Poll判斷連線
                    //        default:
                    //            if (clientSocket == null || clientSocket.Poll(1, SelectMode.SelectRead) || clientSocket.Connected == false)
                    //            {
                    //                SocketClientConnect();
                    //            }
                    //            break;
                    //        #endregion
                    //    }
                    //}
                    //else
                    //    int_ReconnectWait++;
                    #endregion

                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    if (ex.Message != "執行緒已經中止。")
                    {
                        MPU.WriteErrorCode("", "[MesNetSiteRunning] " + ex.Message);
                        Console.WriteLine("[MesNetSiteRunning] " + ex.Message);
                    }
                }
            } 
        }
        #endregion

        #region (註解)檢查Socket連線狀態
        /// <summary>
        /// 檢查Socket連線狀態
        /// </summary>
        /// <param name="skt"></param>
        /// <returns>回傳true代表連線成功，false代表連線斷開</returns>
        //bool CheckSocket(Socket skt)
        //{
        //    if (skt == null)
        //        return false;
        //    bool part1 = skt.Poll(1000, SelectMode.SelectRead);
        //    bool part2 = (skt.Available == 0);
        //    if (part1 && part2)
        //        return false;
        //    else
        //    {
        //        try
        //        {
        //            int sentBytesCount = skt.Send(new byte[1], 1, 0);
        //            return sentBytesCount == 1;
        //        }
        //        catch
        //        {
        //            return false;
        //        }
        //    }
        //}
        #endregion

        #region 檢查Socket連線狀態
        /// <summary>
        /// 檢查Socket連線狀態
        /// </summary>
        /// <param name="sclass"></param>
        /// <returns>回傳true代表連線成功，false代表連線斷開</returns>
        bool CheckSocket(string sclass)
        {
            switch (sclass)
            {
                #region 無法用Socket.Poll判斷連線
                case "3": //控制燈號模組
                    if (clientSocket != null && clientSocket.Connected == true)
                        return true;
                    else
                        return false;
                #endregion

                #region 無須建立連線
                case "0"://寫假資料讓燈號亮黃燈
                case "26": //只PING設備IP，連線成功就回傳黃燈
                case "31": //廠務設備 比電阻
                    return true;
                #endregion

                #region 可以用Socket.Poll判斷連線
                default:
                    if (clientSocket != null && !clientSocket.Poll(1, SelectMode.SelectRead) && clientSocket.Connected == true)
                        return true;
                    else
                        return false;
                #endregion
            }
        }
        #endregion

        #region 即時更新感測器狀態，並存入strStatic
        private void SetStatus(int index)
        {
            try
            {
                string sclass = dicSclass[strPort][index].Split(';')[0].Split('_')[0];
                //strStatic為null代表感測器還沒建立連線，因此將strStatic改為連線中
                if (strStatic[index] == null)
                    strStatic[index] = MPU.str_ErrorMessage[3];

                //strStatic中出現Ex關鍵字，代表感測器傳接指令時發生錯誤，因此將strStatic改為網路連線失敗
                if (strStatic[index].Contains(MPU.str_ErrorMessage[0]))
                {
                    strStatic[index] = MPU.str_ErrorMessage[1];
                }
                //strStatic的值為空白的時候，代表感測器連線成功但無回傳值，因此將strStatic改為設備查無資料
                else if (string.IsNullOrWhiteSpace(strStatic[index]))
                {
                    strStatic[index] = MPU.str_ErrorMessage[2];
                }
                //strStatic的值為連線中時
                else if (strStatic[index].Contains(MPU.str_ErrorMessage[3]))
                {
                    //若啟動程式後超過30秒仍卡在連線中，就將狀態改為網路連線失敗
                    if (DateTime.Now >= listDisconnectTime[index])
                    {
                        strStatic[index] = MPU.str_ErrorMessage[1];
                    }
                    else
                    {
                        strStatic[index] = MPU.str_ErrorMessage[3];
                    }
                }
                else 
                {
                    if (!CheckSocket(sclass))
                    {
                        //判斷感測器斷線，但strStatic還沒更新時的狀況
                        if (int_ReconnectWait >= 20)
                        {
                            strStatic[index] = MPU.str_ErrorMessage[1];
                            int_ReconnectWait = 0;
                        }
                        else
                        {
                            strStatic[index] = $"{MPU.str_ErrorMessage[6]}";
                            int_ReconnectWait++;
                        }
                    }
                }

                //只要strStatic不是null，就把strStatic內容存入字典中，再由Form的Timer去更新DataTable狀態
                if (strStatic[index] != null)
                    MPU.dicDgvValue.AddOrUpdate(int_ThreadNum + index, strStatic[index], (k, v) => strStatic[index]);
            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "[MesNetSite SetStatus] " + ex.Message);
                Console.WriteLine("[MesNetSite SetStatus] " + ex.Message);
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
                for (int i = 1; i < CRC_Bytes.Length - 1; i++)
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
        #endregion

        #region 執行緒休息
        /// <summary>
        /// 執行緒休息
        /// </summary>
        /// <param name="Loop">迴圈次數</param>
        /// <param name="LoopSleep">每次迴圈休息時間</param>
        private void Sleep(int Loop, int LoopSleep)
        {
            for (int i = 0; i < Loop; i++)
            {
                System.Threading.Thread.Sleep(LoopSleep);
                System.Windows.Forms.Application.DoEvents();
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

        #region 舊模組(無使用)
        //正壓
        void function_09_positive()
        {
            try
            {
                for (int i = 0; i < 8; i++)
                {
                    //if氣壓ad位置為1
                    {
                        byteCmdSend[i] = Convert.ToByte(Convert.ToInt32(strCmdEight_1.Split(' ')[i], 16));
                    }

                    #region if設備為IPA

                    //正壓ad位置為5
                    byteCmdSend[i] = Convert.ToByte(Convert.ToInt32(strCmdEight_5.Split(' ')[i], 16));
                    //正壓ad位置為6
                    byteCmdSend[i] = Convert.ToByte(Convert.ToInt32(strCmdEight_6.Split(' ')[i], 16));

                    #endregion
                }
                clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                String str_hex = "";
                double double_Mpa = 0;

                str_hex = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

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
                byteCmdSend = strCmdEight_2.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                String str_hex = "";
                double double_Mpa = 0;

                str_hex = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

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
        #endregion

        #region 舊版本MES function 01-10 

        #region 01 環境溫濕度
        //1 環境溫濕度
        /// <summary>
        /// 環境溫濕度
        /// </summary>
        void function_01()
        {
            try
            {
                //221209先RECEIVE清空
                byteCmdSend = strCmdEHCEHD.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, strCmdEHCEHD.Split(' ').Length, System.Net.Sockets.SocketFlags.None);

                for (int i = 0; i < 50; i++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = clientSocket.Available;

                if (int_Net_Available >= 13)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    //溫度
                    string str_T = Convert.ToInt32(String.Format("{0:X2}", byteCmdReceive[9]) + String.Format("{0:X2}", byteCmdReceive[10]), 16).ToString().PadLeft(4, '0'); ;
                    //濕度
                    string str_H = Convert.ToInt32(String.Format("{0:X2}", byteCmdReceive[11]) + String.Format("{0:X2}", byteCmdReceive[12]), 16).ToString().PadLeft(4, '0'); ;


                    strCmdSQL = "INSERT INTO [dbo].[tb_recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME])     VALUES ('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[0].ToString() + "','" + str_T + "',GETDATE()) ";

                    strCmdSQL += ", ('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[1].ToString() + "','" + str_H + "',GETDATE())";

                    strStatic[intIndex] = str_T + ":" + str_H + "[" + strSclass + "]";


                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }

            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = "Ex:" + EX.Source;

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 02 條碼 
        void function_02()
        {

            try
            {
                int int_Net_Available = clientSocket.Available;
                //有條碼時才動作。

                //判斷是否有資料以及是否可以讀取，並且此連結資料通道可以抓取資料時才能動作。

                if (int_Net_Available > 0)
                {

                    string str_T = "";

                    //抓取資料之後轉為字元
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    for (int j = 0; j <= int_Net_Available - 2; j++)
                    {

                        str_T += Convert.ToChar(byteCmdReceive[j]);

                    }

                    strStatic[intIndex] = int_Net_Available + "@" + str_T + ":" + strSclass;

                    //
                    if (str_T.Length > 16)
                    {

                        str_T = str_T.Substring(0, 16);

                    }

                    //如果非空字串的話
                    if (str_T != "")
                    {
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID]           ,[DIP]           ,[SID]           ,[DVALUE]           ,[SYSTIME])     VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','" + str_T + "',GETDATE()) ; ";



                        //171119 


                        String light_Str;

                        light_Str = "0100";

                        String Str_SQL_sid_X_WLS = "SELECT S_ID FROM [dbo].[tb_sensors_rules] WHERE d_ID =  (SELECT d_ID FROM   [dbo].[tb_sensors_rules]  WHERE s_ID = '" + strSID + "') AND S_ID LIKE '%WLS%'";

                        strCmdSQL += $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "',(" + Str_SQL_sid_X_WLS + "),'" + light_Str + "',GETDATE(),'" + strNote + "') ;";

                        strBarcode = str_T;
                    }
                    else
                    {
                        strCmdSQL = "";

                    }

                }
                else
                {
                    //沒有資料仍要提示訊息
                    strStatic[intIndex] = "Available[" + int_Net_Available + "]@無資料:" + strSclass;

                    //沒有資料的時候丟個資料過去不要讓他斷線

                    byte[] byte_Command_wlslcs = new byte[5];

                    byte_Command_wlslcs[0] = 48;
                    byte_Command_wlslcs[1] = 56;
                    byte_Command_wlslcs[2] = 48;
                    byte_Command_wlslcs[3] = 10;
                    byte_Command_wlslcs[4] = 13;

                    clientSocket.Send(byte_Command_wlslcs, 0, 5, System.Net.Sockets.SocketFlags.None);

                    //INSERT 一筆
                    if (booleanInsert == true)
                    {

                        // strCmdSQL = "INSERT INTO [dbo].[tb_recordslogTEST] ([DID]           ,[DIP]           ,[SID]           ,[DVALUE]           ,[SYSTIME])     VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','RUN-161103756-00',GETDATE()) ";

                    }

                    booleanInsert = false;

                }

            }
            catch (Exception EX)
            {

                if (EX.Source != null)
                {

                    strStatic[intIndex] = "Ex:" + EX.Source;

                    Console.WriteLine("M0375:Exception source: {0}", EX.Source + EX.Message);

                }
            }

        }
        #endregion

        #region 傳送黃燈訊號
        void SetYellowLight()
        {
            byte[] byte_Command_wlslcs = new byte[5];

            byte_Command_wlslcs[0] = 48;
            byte_Command_wlslcs[1] = 54;
            byte_Command_wlslcs[2] = 49;
            byte_Command_wlslcs[3] = 10;
            byte_Command_wlslcs[4] = 13;
            clientSocket.Send(byte_Command_wlslcs, 0, 5, System.Net.Sockets.SocketFlags.None);

            byte_Command_wlslcs[0] = 48;
            byte_Command_wlslcs[1] = 53;
            byte_Command_wlslcs[2] = 49;
            byte_Command_wlslcs[3] = 10;
            byte_Command_wlslcs[4] = 13;
            clientSocket.Send(byte_Command_wlslcs, 0, 5, System.Net.Sockets.SocketFlags.None);

            byte_Command_wlslcs[0] = 48;
            byte_Command_wlslcs[1] = 52;
            byte_Command_wlslcs[2] = 48;
            byte_Command_wlslcs[3] = 10;
            byte_Command_wlslcs[4] = 13;
            clientSocket.Send(byte_Command_wlslcs, 0, 5, System.Net.Sockets.SocketFlags.None);

            byte_Command_wlslcs[0] = 48;
            byte_Command_wlslcs[1] = 51;
            byte_Command_wlslcs[2] = 48;
            byte_Command_wlslcs[3] = 10;
            byte_Command_wlslcs[4] = 13;
            clientSocket.Send(byte_Command_wlslcs, 0, 5, System.Net.Sockets.SocketFlags.None);
        }
        #endregion

        #region 03 燈號函數
        void function_03()
        {
            try
            {
                //17122601
                if (strBarcodeLight != "" && strSID.Substring(0, 3) == "WLS")
                {
                    byte[] byte_Command_wlslcs = new byte[5];

                    if (strBarcodeLight == "00000000")
                    {
                        //傳送橘燈訊號
                        SetYellowLight();

                    }
                    else
                    {

                        byte_Command_wlslcs[0] = 48;
                        byte_Command_wlslcs[1] = 54;
                        byte_Command_wlslcs[2] = 49;
                        byte_Command_wlslcs[3] = 10;
                        byte_Command_wlslcs[4] = 13;
                        clientSocket.Send(byte_Command_wlslcs, 0, 5, System.Net.Sockets.SocketFlags.None);

                        byte_Command_wlslcs[0] = 48;
                        byte_Command_wlslcs[1] = 53;
                        byte_Command_wlslcs[2] = 48;
                        byte_Command_wlslcs[3] = 10;
                        byte_Command_wlslcs[4] = 13;
                        clientSocket.Send(byte_Command_wlslcs, 0, 5, System.Net.Sockets.SocketFlags.None);

                        byte_Command_wlslcs[0] = 48;
                        byte_Command_wlslcs[1] = 52;
                        byte_Command_wlslcs[2] = 49;
                        byte_Command_wlslcs[3] = 10;
                        byte_Command_wlslcs[4] = 13;
                        clientSocket.Send(byte_Command_wlslcs, 0, 5, System.Net.Sockets.SocketFlags.None);

                        byte_Command_wlslcs[0] = 48;
                        byte_Command_wlslcs[1] = 51;
                        byte_Command_wlslcs[2] = 48;
                        byte_Command_wlslcs[3] = 10;
                        byte_Command_wlslcs[4] = 13;
                        clientSocket.Send(byte_Command_wlslcs, 0, 5, System.Net.Sockets.SocketFlags.None);
                    }

                    strBarcodeLight = "";

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
                if (int_WLSLCS_Count == 0 && strSID.Substring(0, 3) == "WLS")
                {
                    SetYellowLight();
                }



                //取得目前網路盒的訊號資料量大小
                int int_Net_Available = clientSocket.Available;

                //判斷是否有資料以及是否可以讀取。
                if (int_Net_Available >= 10)
                {

                    byte_function_03_loop_index = 0;

                    string str_T = "";

                    clientSocket.Receive(byteCmdReceive, 0, 10, SocketFlags.None);

                    for (int j = 0; j <= 10 - 2; j++)
                    {

                        str_T += Convert.ToChar(byteCmdReceive[j]);

                    }

                    //和之前的燈號一樣的話，就不用做處理了。
                    if (Str_WLSLCS_F != str_T)
                    {
                        //記錄這一次的燈號
                        Str_WLSLCS_F = str_T;

                        //判斷是何種燈號解析，WLS為自訂義燈號，LCS為燈號解析模組。
                        //WLS 00BGYR00 RGYW
                        if (str_T.Length > 8 && strSID.Substring(0, 3) == "WLS")
                        {
                            //17122601 


                            if (strSID == "WLS002") //水洗機的燈號模組其中一組訊號拿來做段數開關的偵測。
                            {
                                if (str_T.Substring(2, 1) == "1")
                                {
                                    strCmdSQL = "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "','SWD001','4',GETDATE(),'" + strNote + "'); ";

                                }
                                else
                                {
                                    strCmdSQL = "INSERT INTO [dbo].[tb_P3recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "','SWD001','0',GETDATE(),'" + strNote + "'); ";

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
                        else if (str_T.Length > 8 && strSID.Substring(0, 3) == "LCS")
                        {
                            //221209 燈號意思


                            //LCS 00RWYG00 RGYW
                            //LCS 如果有0001 純白燈的話 => 回傳黃燈 0011
                            //LCS 如果有0000 全滅的話 => 回傳黃燈 0010
                            str_T = str_T.Substring(2, 1) + str_T.Substring(5, 1) + str_T.Substring(4, 1) + str_T.Substring(3, 1);
                            //LCS001 LCS002 燈號不同，為特殊型。
                            //當為1100時，判斷為綠燈(0100)，當為0011時，判斷為黃燈(0010)，其他狀況時為紅燈(1000)
                            if (strSID == "LCS001" || strSID == "LCS002")
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
                                else
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
                                else
                                {
                                    str_T = "0010";
                                }

                                if (Str_lcs == "1100" || Str_lcs == "1110" || Str_lcs == "1101" || Str_lcs == "1111")
                                {
                                    str_T = "0100";
                                }
                                else
                                {
                                    str_T = "0010";
                                }
                            }

                        }
                        else
                        {
                            str_T = "0010";
                        }

                        //燈號結果處理完畢
                        //str_ttt = str_T;

                        strStatic[intIndex] = int_Net_Available + "@" + str_T + ":" + strSclass;

                        //有資料時再處理
                        if (str_T != "" && strSID != "WLS002")
                        {
                            //依製二製三放置不同資料表。
                            //20161230 insert會一直加下去，所以insert指令會變很大。稍微修改一下，變成只有一筆insert into
                            //如果String_SQLcommand 不是空字串的話，直接加入values的值到字串尾巴即可。
                            if (strCmdSQL == "")
                            {
                                strCmdSQL += $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','" + str_T + "',GETDATE(),'" + strNote + "') ";
                            }
                            else
                            {
                                strCmdSQL += ", ('" + strTID + "','" + strDIP + "','" + strSID + "','" + str_T + "',GETDATE(),'" + strNote + "') ";

                            }
                        }
                        else if (strSID == "WLS002")
                        {                                
                            strCmdSQL += $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','" + str_T + "',GETDATE(),'" + strNote + "') ";

                        }
                        else
                        {                                
                            strCmdSQL += $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','0010',GETDATE(),'" + strNote + "') ";

                            strStatic[intIndex] = int_Net_Available + "@0010:" + strSclass;
                        }

                    }
                    else
                    {
                        strCmdSQL += $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','0010',GETDATE(),'" + strNote + "') ";

                        strStatic[intIndex] = int_Net_Available + "@0010:" + strSclass;
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

                    clientSocket.Send(byte_Command_wlslcs, 0, 5, System.Net.Sockets.SocketFlags.None);

                    for (int i = 0; i < 50; i++)
                    {
                        System.Threading.Thread.Sleep(10);

                        System.Windows.Forms.Application.DoEvents();
                    }

                    int int_Net_Availablexxx = clientSocket.Available;

                    //如果真的都沒有值回傳的話，再準備進行資料重連的動作。
                    if (int_Net_Availablexxx == 0)
                    {

                        //byte_function_03_loop_index += 1;

                        //strStatic[intIndex] = "<468>" + int_Net_Available + "@" + str_ttt + "，未解碼次數(" + byte_function_03_loop_index + "):" + strSclass;

                        ////當燈號解析超出10次沒有資料的時候，重新連線一次。
                        //if (byte_function_03_loop_index > 50)
                        //{

                        //    //161208 加入一個共有變數，同一時間只能一個tcpip連線
                        //    if (MPU.TOSrun == false)
                        //    {
                        //        MPU.TOSrun = true;

                        //        byte_function_03_loop_index = 0;

                        //        str_ttt = "";

                        //        //NetworkStream_Reader.Close();

                        //        //TcpClient_Reader.Close();

                        //        //NetworkStream_Reader = null;

                        //        //TcpClient_Reader = null;

                        //        for (int int_t = 0; int_t < 100; int_t++)
                        //        {

                        //            System.Windows.Forms.Application.DoEvents();

                        //            System.Threading.Thread.Sleep(5);

                        //        }

                        //        //重新建立連結
                        //        SocketClientConnect();

                        //        MPU.TOSrun = false;
                        //    }
                        //}
                    }
                    else
                    {
                        clientSocket.Receive(byteCmdReceive, 0, 10, SocketFlags.None);
                    }

                    strCmdSQL += $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','0010',GETDATE(),'" + strNote + "') ";

                    strStatic[intIndex] = int_Net_Available + "@0010:" + strSclass;
                }

            }
            catch (Exception EX)
            {

                if (EX.Source != null)
                {

                    strStatic[intIndex] = "[" + byte_function_03_loop_index + "]Ex:" + EX.Source + EX.Message;

                    Console.WriteLine("M0516:燈號異常:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss[{0}]"), EX.Source + EX.Message);

                    byte_function_03_loop_index += 1;

                }
            }

        }
        #endregion

        #region 04 溫度 
        //221209 進度0
        void function_DTS()
        {

            try
            {

                string str_T = "";

                int_DTS_temp_count += 1;

                byteCmdSend = strCmd485_8Port.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                //發送指令前先清完記憶體
                clientSocket.Send(byteCmdSend, 0, strCmd485_8Port.Split(' ').Length, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                // clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                //按照位置抓取資料，空壓的都採電壓式， 
                //例位置1 ，抓 第4+第5 加總為16進製 4字元， 轉換成 10進製後得 x
                // X 乘以 10 之後 再除以 4095， 取小數點後2位

                String str_hex = "";

                double double_DCvi = 0;

                if (int_Net_Available > 20)
                {

                    //一次只取21筆
                    clientSocket.Receive(byteCmdReceive, 0, 21, SocketFlags.None);

                    //判斷是否符合CRC
                    temCRC = crc16(byteCmdReceive, 19);

                    //相符的話才能執行
                    if (temCRC[0] == byteCmdReceive[19] && temCRC[1] == byteCmdReceive[20])

                    {
                        str_hex = "";

                        strCmdSQL = "";

                        //先了解此機台有幾組溫度
                        int int_X = Convert.ToInt16(strPortId.Split(';')[0]);

                        for (int xx = 0; xx < int_X; xx++)
                        {
                            //按照設定值抓取溫度
                            int int_GetPortid = Convert.ToInt16(strPortId.Split(';')[1].Split('.')[xx]);

                            //轉換成16進制文字
                            str_hex = Convert.ToString(byteCmdReceive[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[1 + ((int_GetPortid * 2) + 1)], 16).PadLeft(2, '0');

                            int int_dec = 0;

                            int_dec = Convert.ToInt16(str_hex, 16);

                            double_DCvi = Convert.ToDouble(int_dec) * 10 / 4095;

                            double double_Mpa = 0;

                            //DTS016
                            //0~250
                            //0.039840637

                            //if (strSID.Split('.')[xx] == "DTS016" || strSID.Split('.')[xx] == "DTS008" || strSID.Split('.')[xx] == "DTS009")
                            if (strSID.Split('.')[xx] != "DTS010" &&
                                strSID.Split('.')[xx] != "DTS011" &&
                                strSID.Split('.')[xx] != "DTS012" &&
                                strSID.Split('.')[xx] != "DTS013" &&
                                strSID.Split('.')[xx] != "DTS014" &&
                                strSID.Split('.')[xx] != "DTS005")

                            {//-43

                                //double_Mpa = double_DCvi / 0.039840637 + Convert.ToDouble(strSclass.Split(';')[1]);
                                //if (strSID.Split('.')[xx] == "DTS008")
                                //{
                                ///    double_Mpa = double_DCvi / 0.0194 + Convert.ToDouble(String_Sclass.Split(';')[1]);
                                //}
                                //else
                                //{
                                if (strSID.Split('.')[xx] == "KPS022" || strSID.Split('.')[xx] == "KPS023")
                                {

                                    double_Mpa = double_DCvi;

                                    str_DTS_temp[xx] = "";

                                    int_DTS_temp_count = 0;

                                }
                                else
                                {
                                    double_Mpa = double_DCvi / 0.02 + Convert.ToDouble(strSclass.Split(';')[1]) - 99;
                                }
                                //}
                            }

                            else
                            {

                                //double_Mpa = double_DCvi / 0.014306152 + Convert.ToDouble(strSclass.Split(';')[1]);


                                double_Mpa = double_DCvi * 156.5 - 200;

                            }

                            // DTS001 DTS001,溫度,銀膠冰箱

                            if (strSID.Split('.')[xx] == "DTS001")
                            {
                                //DTS001 銀膠冰箱， 值為 VI / 0.1776 - 99 
                                //170215 更換參數為 -0.388394674 
                                //double_Mpa = (double_DCvi - 0.53) / -0.2 + Convert.ToDouble(strSclass.Split(';')[1]);
                                double_Mpa = double_DCvi / -0.388394674 + Convert.ToDouble(strSclass.Split(';')[1]);

                                str_T += "(" + int_GetPortid + ")[" + double_DCvi + "]" + double_Mpa.ToString("F3");
                            }
                            else
                            {

                                str_T += "[" + str_hex.ToUpper() + "], (" + int_GetPortid + ")[" + double_DCvi + "]" + double_Mpa.ToString("F3");
                            }



                            strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";

                            if (strCmdSQL != "" && xx != 0)
                            {
                                strCmdSQL += ",";

                            }

                            //降低精準度至小數點1位
                            if (str_DTS_temp[xx] != double_Mpa.ToString("F1"))
                            {
                                str_DTS_temp[xx] = double_Mpa.ToString("F1");
                            }

                            strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + double_Mpa.ToString("F3") + "',GETDATE(),'" + strNote + "') ";


                            if (int_DTS_temp_count > 200)
                            {
                                str_DTS_temp[xx] = "";

                                int_DTS_temp_count = 0;
                            }

                        }

                        strCmdSQL += ";";

                    }

                }

                if (double_DCvi == 0 || double_DCvi == 10)
                {
                    strStatic[intIndex] = int_Net_Available + "@" + str_T + "vi資料異常:" + strSclass;

                    //資料異常就不要進資料庫 
                    strCmdSQL = "";

                }
                else
                {
                    strStatic[intIndex] = int_Net_Available + "@" + str_T + ":" + strSclass;
                }
            }

            catch (Exception EX)
            {

                if (EX.Source != null)
                {

                    Console.WriteLine("M0733:Exception source: {0}", EX.Source);

                    strStatic[intIndex] = "[" + byte_function_03_loop_index + "]Ex:" + EX.Source + EX.Message;

                }
            }


        }
        #endregion

        #region 05 空壓KPS
        //221209 補對數值
        //DC 解碼 01 03 00 00 00 08 44 0C
        //2.24V~2.25V = 0.352 2.23~2.22 = 0.351 
        void function_05()
        {
            try

            {
                string str_T = "";

                byteCmdSend = strCmd485_8Port.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                //發送讀取指令。
                clientSocket.Send(byteCmdSend, 0, strCmd485_8Port.Split(' ').Length, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                //取得回傳大小。
                int int_Net_Available = clientSocket.Available;

                // clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                //按照位置抓取資料，空壓的都採電壓式， 
                //例位置1 ，抓 第4+第5 加總為16進製 4字元， 轉換成 10進製後得 x
                // X 乘以 10 之後 再除以 4095， 取小數點後2位

                String str_hex = "";

                //至少需要21碼才做解碼動作
                if (int_Net_Available >= 21)
                {
                    strCmdSQL = "";
                    //一次只取21筆
                    clientSocket.Receive(byteCmdReceive, 0, 21, SocketFlags.None);

                    //判斷是否符合CRC
                    temCRC = crc16(byteCmdReceive, 19);

                    //檢查回傳碼是否相符的話才能執行
                    if (temCRC[0] == byteCmdReceive[19] && temCRC[1] == byteCmdReceive[20])

                    {
                        str_hex = "";

                        //先了解此機台有幾組空壓

                        int int_X = Convert.ToInt16(strPortId.Split(';')[0]);

                        for (int xx = 0; xx < int_X; xx++)
                        {
                            //按照設定值抓取空壓
                            int int_GetPortid = Convert.ToInt16(strPortId.Split(';')[1].Split('.')[xx]);

                            //轉換成16進制文字
                            str_hex = Convert.ToString(byteCmdReceive[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[1 + ((int_GetPortid * 2) + 1)], 16).PadLeft(2, '0');
                            //String.Format("{0:X4}", iTemp)
                            int int_dec = 0;

                            int_dec = Convert.ToInt16(str_hex, 16);

                            double double_DCvi = 0;

                            double_DCvi = Convert.ToDouble(int_dec) * 10 / 4095;

                            double double_Mpa = 0;

                            double_Mpa = (double_DCvi - 1) / 4 + 0.0512503;

                            if (strSID.Split('.')[xx] == "KPS004" || strSID.Split('.')[xx] == "KPS006" || strSID.Split('.')[xx] == "KPS021" || strSID.Split('.')[xx] == "KPS019")
                            {
                                if ((double_Mpa * 100) < 0)
                                {
                                    if (strSID.Split('.')[xx] == "KPS004")  //-26
                                    {
                                        str_T += "(" + int_GetPortid + ")" + (double_Mpa * 100 + 26).ToString("F1");
                                    }
                                    else if (strSID.Split('.')[xx] == "KPS019") //-20
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
                                    if (strSID.Split('.')[xx] == "KPS004")  //-26
                                    {
                                        str_T += "(" + int_GetPortid + ")-" + (double_Mpa * 100 + 25.9).ToString("F1");
                                    }
                                    else if (strSID.Split('.')[xx] == "KPS019") //-20
                                    {
                                        str_T += "(" + int_GetPortid + ")-" + (double_Mpa * 100 + 20.2).ToString("F1");
                                    }
                                    else
                                    {
                                        str_T += "(" + int_GetPortid + ")-" + (double_Mpa * 100).ToString("F1");
                                    }
                                }
                            }
                            else if (strSID.Split('.')[xx] == "GRA001" || strSID.Split('.')[xx] == "GRA002")
                            {
                                str_T += "(" + int_GetPortid + ")" + double_DCvi.ToString("F3") + "@" + str_hex;
                            }
                            else
                            {
                                if ((double_Mpa * 100) < 0)
                                {
                                    if (strSID.Split('.')[xx] == "KPS005")
                                    {
                                        str_T += "(" + int_GetPortid + ")-" + (double_Mpa + 0.18).ToString("F1");
                                    }
                                    else if (strSID.Split('.')[xx] == "KPS018")
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
                                    if (strSID.Split('.')[xx] == "KPS005")
                                    {
                                        str_T += "(" + int_GetPortid + ")" + (double_Mpa + 0.166).ToString("F3");
                                    }
                                    else if (strSID.Split('.')[xx] == "KPS018")
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
                            if (strCmdSQL == "")
                            {
                                strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                            }

                            //161208 因為發現有空values的狀況，加上一個String_SQLcommand_values來暫存是否有資料填入，來做為是否加入[ , ]的區隔符號。
                            if (strCmdSQL != "" && xx != 0)
                            {
                                strCmdSQL += ",";
                            }
                            else
                            {
                                //if (strTID == "2048-001")
                                //{
                                //    strCmdSQL += "";
                                //}
                            }


                            if (strSID.Split('.')[xx] == "KPS004" || strSID.Split('.')[xx] == "KPS006" || strSID.Split('.')[xx] == "KPS021" || strSID.Split('.')[xx] == "KPS019")
                            {
                                if ((double_Mpa * 100) < 0)
                                {
                                    if (strSID.Split('.')[xx] == "KPS004")  //-26
                                    {
                                        strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + (double_Mpa * 100 - 26).ToString("F1") + "',GETDATE(),'" + strNote + "[" + str_hex + "]') ";
                                    }
                                    else if (strSID.Split('.')[xx] == "KPS019") //-20
                                    {
                                        strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + (double_Mpa * 100 - 20).ToString("F1") + "',GETDATE(),'" + strNote + "[" + str_hex + "]') ";
                                    }
                                    else
                                    {
                                        strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + (double_Mpa * 100).ToString("F1") + "',GETDATE(),'" + strNote + "[" + str_hex + "]') ";
                                    }
                                }
                                else
                                {
                                    if (strSID.Split('.')[xx] == "KPS004") //-26
                                    {
                                        strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','-" + (double_Mpa * 100 + 25.9).ToString("F1") + "',GETDATE(),'" + strNote + "[" + str_hex + "]') ";
                                    }
                                    else if (strSID.Split('.')[xx] == "KPS019") //-20
                                    {
                                        strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','-" + (double_Mpa * 100 + 20.2).ToString("F1") + "',GETDATE(),'" + strNote + "[" + str_hex + "]') ";
                                    }
                                    else
                                    {
                                        strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','-" + (double_Mpa * 100).ToString("F1") + "',GETDATE(),'" + strNote + "[" + str_hex + "]') ";
                                    }
                                }
                            }
                            else if (strSID.Split('.')[xx] == "GRA001" || strSID.Split('.')[xx] == "GRA002")
                            {
                                //str_T += "(" + int_GetPortid + ")-" + double_DCvi.ToString("F1");

                                strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + double_DCvi.ToString("F3") + "',GETDATE(),'" + strNote + "[" + str_hex + "]') ";
                            }
                            else
                            {
                                //////

                                if (strSID.Split('.')[xx] == "KPS005")
                                {
                                    strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + (double_Mpa + 0.166).ToString("F3") + "',GETDATE(),'" + strNote + "[" + str_hex + "]') ";
                                }
                                else if (strSID.Split('.')[xx] == "KPS018")
                                {
                                    strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + (double_Mpa + 0.1765).ToString("F3") + "',GETDATE(),'" + strNote + "[" + str_hex + "]') ";
                                }
                                else
                                {
                                    strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + double_Mpa.ToString("F3") + "',GETDATE(),'" + strNote + "[" + str_hex + "]') ";
                                }
                            }
                        }

                        if (strCmdSQL != "")
                            strCmdSQL += ";";
                    }
                }

                strStatic[intIndex] = int_Net_Available + "@" + str_T + ":" + strSclass;
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                    Console.WriteLine("M0930:Exception source: {0}", EX.Source + "[" + EX.Message + "]");
            }

        }
        #endregion

        #region 06 水阻值(舊) 
        //221209 補對數值
        //採用 ma解碼
        //DC 解碼 01 03 00 00 00 08 44 0C
        //2.24V~2.25V = 0.352 2.23~2.22 = 0.351 
        void function_06()
        {
            try
            {
                Random rnd = new Random();

                string str_T = "";

                byteCmdSend = strCmd485_8Port.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, strCmd485_8Port.Split(' ').Length, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                String str_hex = "";

                if (int_Net_Available > 20)
                {
                    strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";

                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    str_hex = "";

                    //先了解此機台有幾組感測器

                    int int_X = Convert.ToInt16(strPortId.Split(';')[0]);

                    for (int xx = 0; xx < int_X; xx++)
                    {
                        //按照埠設定值抓取基本資料
                        int int_GetPortid = Convert.ToInt16(strPortId.Split(';')[1].Split('.')[xx]);

                        //轉換成16進制文字
                        str_hex = Convert.ToString(byteCmdReceive[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[1 + ((int_GetPortid * 2) + 1)], 16).PadLeft(2, '0');

                        int int_dec = 0;

                        int_dec = Convert.ToInt16(str_hex, 16);

                        double double_DCma = 0;

                        double_DCma = Convert.ToDouble(int_dec) * 20 / 4095;

                        double double_Mpa = 0;

                        if (xx != 0)
                        {
                            strCmdSQL += ",";
                        }

                        //溫度的兩台不同
                        if (strSID.Split('.')[xx] == "DTS017" || strSID.Split('.')[xx] == "DTS018")
                        {
                            double_Mpa = double_DCma / 0.0599018347362718 - 100;

                            if (strSID.Split('.')[xx] == "DTS018")
                            {
                                //double_Mpa += (rnd.NextDouble() * (0.78 - 0.12) + 1);
                                double_Mpa = double_DCma / 0.0599018347362718 - 100;

                            }

                            str_T += "(" + int_GetPortid + ")" + (double_Mpa).ToString("F3");

                            strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + double_Mpa.ToString("F3") + "',GETDATE(),'" + strNote + "') ";
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

                            if (strSID.Split('.')[xx] == "M001")
                            {
                                //double_Mpa = double_DCma / 4.623524623524623;
                                double_Mpa = (double_DCma - 4) / 0.806;
                            }
                            else if (strSID.Split('.')[xx] == "M002")
                            {
                                double_Mpa = (double_DCma - 4) / 0.78;
                            }
                            else if (strSID.Split('.')[xx] == "M003")
                            {
                                double_Mpa = (double_DCma - 4) / 1.256;
                            }

                            str_T += "(" + int_GetPortid + ")" + double_Mpa.ToString("F3");

                            strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + double_Mpa.ToString("F3") + "',GETDATE(),'" + strNote + "') ";
                        }



                    }

                    strCmdSQL += ";";
                }

                strStatic[intIndex] = int_Net_Available + "@" + str_T + ":" + strSclass;

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
        #endregion

        #region 07 銀膠冰箱溫度 
        void function_07()
        {
            try
            {
                //221209先RECEIVE清空
                byteCmdSend = strCmdEHCEHD.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, strCmdEHCEHD.Split(' ').Length, System.Net.Sockets.SocketFlags.None);

                for (int i = 0; i < 50; i++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                int int_Net_Available = clientSocket.Available;

                if (int_Net_Available >= 13)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    //溫度
                    string str_T = ((Convert.ToInt32(String.Format("{0:X4}", 0xFFFF), 16) - Convert.ToInt32(String.Format("{0:X2}", byteCmdReceive[9]) + String.Format("{0:X2}", byteCmdReceive[10]), 16)) * (-0.1)).ToString();

                    strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','" + str_T + "',GETDATE(),'" + strNote + "') ";

                    //strCmdSQL += ",('" + strTID + "','" + strDIP + "','LCS001','0010',GETDATE(),'" + strNote.Split('_')[0] + "_燈號') ";

                    strStatic[intIndex] = str_T + "[" + strSclass + "]";
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = "Ex:" + EX.Source;

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 08 溫度4通道
        void function_DTS_8()
        {
            Sleep(50, 10);

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
                if (clientSocket.Available > 0)
                    clientSocket.Receive(byteCmdReceive, 0, clientSocket.Available, SocketFlags.None);

                clientSocket.Send(Byte_Command_Sent_082, 0, 8, System.Net.Sockets.SocketFlags.None);

                //清空讀取記憶體
                for (int i = 0; i <= 50; i++)
                {
                    byteCmdReceive[i] = 0;
                }

                //休息約 4秒
                for (int i = 0; i < 50; i++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                //判斷是否有網路資料
                int int_Net_Available = clientSocket.Available;

                String str_hex = "";

                //有資料大於13個位元組的再處理。
                if (int_Net_Available >= 13)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);
                    //判斷是否符合CRC
                    temCRC = crc16(byteCmdReceive, 11);
                    //相符CRC的話才能執行  //20200311 加上判斷開頭 為 02 03

                    // 02 03 02 4C 00 04 84 55 讀取第1台 4通道 ID02
                    // 03 03 02 4C 00 04 85 84 讀取第2台 4通道 ID03 
                    if (temCRC[0] == byteCmdReceive[11] && temCRC[1] == byteCmdReceive[12] && byteCmdReceive[0] == 2 && byteCmdReceive[1] == 3)
                    {
                        str_hex = "";
                        strCmdSQL = "";

                        //先做4台
                        int int_X = 4;
                        for (int xx = 0; xx < int_X; xx++)
                        {
                            //按照設定值抓取溫度
                            int int_GetPortid = Convert.ToInt16(strPortId.Split(';')[1].Split('.')[xx]);

                            //轉換成16進制文字
                            str_hex = Convert.ToString(byteCmdReceive[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[1 + ((int_GetPortid * 2) + 1)], 16).PadLeft(2, '0');

                            int int_dec = 0;

                            int_dec = Convert.ToInt16(str_hex, 16);

                            if (strCmdSQL == "")
                            {
                                strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                                bool_add_Values = false;
                            }

                            if (strCmdSQL != "" && xx != 0)
                            {
                                strCmdSQL += ",";
                            }

                            str_DTS_temp[xx] = int_dec.ToString();

                            bool_add_Values = true;

                            strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + int_dec.ToString() + "',GETDATE(),'" + strNote + "') ";

                            str_T += "(" + int_GetPortid + ")" + int_dec.ToString();

                            if (int_DTS_temp_count > 1)
                            {
                                str_DTS_temp[xx] = "";

                                int_DTS_temp_count = 0;
                            }
                        }

                        //strCmdSQL += ";";

                    }
                }

                ////////第二台4ch
                //發送指令前先清完記憶體

                if (clientSocket.Available > 0)
                    clientSocket.Receive(byteCmdReceive, 0, clientSocket.Available, SocketFlags.None);

                clientSocket.Send(Byte_Command_Sent_083, 0, 8, System.Net.Sockets.SocketFlags.None);

                for (int i = 0; i <= 50; i++)
                {
                    byteCmdReceive[i] = 0;
                }

                for (int i = 0; i < 50; i++)
                {
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.Application.DoEvents();
                }

                int_Net_Available = clientSocket.Available;

                str_hex = "";

                if (int_Net_Available >= 13)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    //判斷是否符合CRC
                    temCRC = crc16(byteCmdReceive, 11);

                    //相符的話才能執行  //20200311 加上判斷開頭 為 03 03

                    // 02 03 02 4C 00 04 84 55 讀取第1台 4通道 ID02
                    // 03 03 02 4C 00 04 85 84 讀取第2台 4通道 ID03 
                    if (temCRC[0] == byteCmdReceive[11] && temCRC[1] == byteCmdReceive[12] && byteCmdReceive[0] == 3 && byteCmdReceive[1] == 3)
                    {
                        str_hex = "";
                        //strCmdSQL = "";

                        //後做4台
                        int int_X = 8;
                        for (int xx = 4; xx < int_X; xx++)
                        {
                            //按照設定值抓取溫度
                            int int_GetPortid = Convert.ToInt16(strPortId.Split(';')[1].Split('.')[xx]);

                            //轉換成16進制文字
                            str_hex = Convert.ToString(byteCmdReceive[1 + ((int_GetPortid - 4) * 2)], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[1 + (((int_GetPortid - 4) * 2) + 1)], 16).PadLeft(2, '0');

                            int int_dec = 0;
                            int_dec = Convert.ToInt16(str_hex, 16);

                            //看那個製造部門 資料表
                            if (strCmdSQL == "")
                            {
                                strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                                bool_add_Values = false;
                            }

                            if (strCmdSQL != "")
                            {
                                strCmdSQL += ",";
                            }

                            str_DTS_temp[xx] = int_dec.ToString();

                            bool_add_Values = true;

                            //儲存的值：為 strSID int_dec
                            strCmdSQL += "('" + strTID + "','" + strDIP + "','" + strSID.Split('.')[xx] + "','" + int_dec.ToString() + "',GETDATE(),'" + strNote + "') ";

                            str_T += "(" + int_GetPortid + ")" + int_dec.ToString();

                            if (int_DTS_temp_count > 1)
                            {
                                str_DTS_temp[xx] = "";
                                int_DTS_temp_count = 0;
                            }
                        }

                        //strCmdSQL += ";";

                    }
                }

                if (clientSocket.Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, clientSocket.Available, SocketFlags.None);
                }

                /////////抓取速度模組
                //發送讀取指令。

                for (int i = 0; i < 50; i++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                if (clientSocket.Available > 0)
                    clientSocket.Receive(byteCmdReceive, 0, clientSocket.Available, SocketFlags.None);

                for (int i = 0; i <= 50; i++)
                {
                    byteCmdReceive[i] = 0;
                }

                clientSocket.Send(byteCmdSend, 0, strCmd485_8Port.Split(' ').Length, System.Net.Sockets.SocketFlags.None);

                for (int i = 0; i < 50; i++)
                {
                    System.Threading.Thread.Sleep(10);

                    System.Windows.Forms.Application.DoEvents();
                }

                //取得回傳大小。
                int_Net_Available = clientSocket.Available;

                // clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                //按照位置抓取資料，空壓的都採電壓式， 
                //例位置1 ，抓 第4+第5 加總為16進製 4字元， 轉換成 10進製後得 x
                // X 乘以 10 之後 再除以 4095， 取小數點後2位

                str_hex = "";

                //至少需要21碼才做解碼動作
                if (int_Net_Available >= 21)
                {
                    //strCmdSQL = "";
                    //一次只取21筆
                    clientSocket.Receive(byteCmdReceive, 0, 21, SocketFlags.None);

                    //判斷是否符合CRC
                    temCRC = crc16(byteCmdReceive, 19);

                    //檢查回傳碼是否相符的話才能執行
                    if (temCRC[0] == byteCmdReceive[19] && temCRC[1] == byteCmdReceive[20])
                    {
                        //按照設定值抓取溫度
                        int int_GetPortid = 1;

                        //轉換成16進制文字
                        str_hex = Convert.ToString(byteCmdReceive[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[1 + ((int_GetPortid * 2) + 1)], 16).PadLeft(2, '0');

                        int int_dec = 0;

                        int_dec = Convert.ToInt16(str_hex, 16);

                        double double_DCvi = Convert.ToDouble(int_dec) * 10 / 4095;

                        //03/12調整現場失敗，改用後台補充值+2
                        str_T += "(速度)" + double_DCvi.ToString() + "[" + (double_DCvi / MPU.MPU_int_numericUpDown1 + 2) + "]";

                        if (strCmdSQL == "")
                        {
                            strCmdSQL = $"; INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE])   VALUES ";
                            bool_add_Values = false;
                        }

                        if (strCmdSQL != "" && bool_add_Values == true)
                        {
                            strCmdSQL += ",";
                        }

                        //03/12調整現場失敗，改用後台補充值+2
                        strCmdSQL += "('" + strTID + "','" + strDIP + "','PLS001','" + (double_DCvi / MPU.MPU_int_numericUpDown1 + 2).ToString("F3") + "',GETDATE(),'德邦PSK-8000-速度')";

                    }

                    strCmdSQL += ";";
                }

                // if (double_DCvi == 0 || double_DCvi == 10)

                //else
                strStatic[intIndex] = int_Net_Available + "@" + str_T + ":" + strSclass;
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    Console.WriteLine("M0733:Exception source: {0}", EX.Source);

                    strStatic[intIndex] = "[" + byte_function_03_loop_index + "]Ex:" + EX.Source + EX.Message;
                }
            }
        }
        #endregion

        
        #endregion

        #region 新版本MES function 11-29, 33

        #region 11 流量計(可指定讀取通道)
        //流量計
        void function_11(int port)
        {
            try
            {
                //判斷要執行的485感測器通道對應的指令是什麼
                switch (port)
                {
                    case 3:
                        byteCmdSend = strCmdEight_3.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();
                        break;
                    case 4:
                        byteCmdSend = strCmdEight_4.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();
                        break;
                    default:
                        break;
                }

                //送出Socket指令
                clientSocket.Send(byteCmdSend, 0, byteCmdSend.Length, System.Net.Sockets.SocketFlags.None);

                //休息500毫秒
                Sleep(50, 10);

                //取得可接收資料的長度
                int int_Net_Available = clientSocket.Available;

                //若有資料可以接收才往下執行
                if (int_Net_Available > 0)
                {

                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    String str_hex = "";
                    double double_Mpa = 0;
                    str_hex = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    //221209 變數名稱
                    //感測值結果
                    double_Mpa = (((Convert.ToDouble(Convert.ToInt32(str_hex, 16)) * 20 / 4095 - 4) / 0.16) * 50) / 100;

                    double_Mpa = Math.Round(double_Mpa, 2);

                    strStatic[intIndex] = $"流量 ({double_Mpa.ToString()})";

                    strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{double_Mpa.ToString()}',GETDATE(),'{strNote}') ";
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 12 燈號(新)
        //燈號
        void function_12()
        {
            try
            {
                int int_Net_Available = 0;
                double double_Mpa_1 = 0;
                double double_Mpa_2 = 0;

                //紅燈
                byteCmdSend = strCmdLight_1.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int_Net_Available = clientSocket.Available;

                if (int_Net_Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    String str_hex_1 = "";
                    double_Mpa_1 = 0;

                    str_hex_1 = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    //感測值結果
                    double_Mpa_1 = Convert.ToInt32(str_hex_1, 16);

                    //綠燈
                    byteCmdSend = strCmdLight_2.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                    clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                    Sleep(50, 10);

                    //int int_Net_Available = clientSocket.Available;

                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    String str_hex_2 = "";
                    double_Mpa_2 = 0;

                    str_hex_2 = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    //感測值結果
                    double_Mpa_2 = Convert.ToInt32(str_hex_2, 16);

                    string str_light = "";
                    if (double_Mpa_1 > 20000)
                    {
                        //紅燈(停止中)
                        strStatic[intIndex] = "紅燈";
                        str_light = "1000";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else if (double_Mpa_2 > 20000)
                    {
                        //綠燈(運行中)
                        strStatic[intIndex] = "綠燈";
                        str_light = "0100";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else if ((double_Mpa_1 < 20000 && double_Mpa_2 < 20000) && (double_Mpa_1 > 0 && double_Mpa_2 > 0))
                    {
                        //黃燈(暫停中)
                        strStatic[intIndex] = "黃燈";
                        str_light = "0010";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        //黃燈(暫停中)
                        strStatic[intIndex] = "黃燈";
                        str_light = "0010";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("Exception source: {0}", EX.Source + EX.Message);
                }
            }
        }
        #endregion

        #region 13 溫度
        void function_13()
        {
            try
            {
                strCmd4PortTemp = CRC16LH(GetModbusCommand(strAddress.ToString(), strCmd4PortTemp));
                Array.Resize(ref byteCmdSend, strCmd4PortTemp.Split(' ').Length);

                byteCmdSend = strCmd4PortTemp.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, strCmd4PortTemp.Split(' ').Length, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                if (int_Net_Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    if (int_Net_Available == 14)
                    {
                        string str_1 = Convert.ToInt32(String.Format("{0:X2}", byteCmdReceive[5]), 16).ToString();

                        string str_2 = Convert.ToInt32(String.Format("{0:X2}", byteCmdReceive[7]), 16).ToString();

                        string str_3 = Convert.ToInt32(String.Format("{0:X2}", byteCmdReceive[9]), 16).ToString();

                        string str_4 = Convert.ToInt32(String.Format("{0:X2}", byteCmdReceive[11]), 16).ToString();

                        // str_T += str_H;

                        //if (str_values_temp != str_T + str_H)
                        //{

                        //    strCmdSQL = $"INSERT INTO [dbo].[tb_recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME])     VALUES ('{strTID}','{strDIP}','{strSID.Split('.')[0].ToString()}','{str_T}',GETDATE()) ";

                        //    strCmdSQL += ", ('{strTID}','{strDIP}','{strSID.Split('.')[1].ToString()}','{str_H}',GETDATE())";

                        //    str_values_temp = str_T + str_H;
                        //}
                        //else
                        //{
                        //    strCmdSQL = "";
                        //}

                        str_1 = str_1.Replace("88", "N/A");
                        str_2 = str_2.Replace("88", "N/A");
                        str_3 = str_3.Replace("88", "N/A");
                        str_4 = str_4.Replace("88", "N/A");
                        //Console.WriteLine(strStatic);
                        strStatic[intIndex] = $"{str_1}:{str_2}:{str_3}:{str_4}";

                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{strStatic[intIndex].Trim()}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = MPU.str_ErrorMessage[5];

                        strCmdSQL = "";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];

                    strCmdSQL = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 14 NULL
        void function_14()
        {
            try
            {
                
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = $"Ex:{EX.Source + EX.Message}";

                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss[{0}]"), EX.Source + EX.Message);
                }
            }
        }
        #endregion

        #region 15 氣壓(可指定讀取通道)
        void function_15(int port)
        {
            try
            {
                switch (port)
                {
                    case 1:
                        byteCmdSend = strCmdEight_1.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();
                        break;
                    case 2:
                        byteCmdSend = strCmdEight_2.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();
                        break;
                    case 5:
                        byteCmdSend = strCmdEight_5.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();
                        break;
                    case 6:
                        byteCmdSend = strCmdEight_6.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();
                        break;
                    default:
                        break;
                }

                clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                if (int_Net_Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    String str_hex = "";
                    double double_Mpa = 0;

                    str_hex = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    int[] eight = new int[str_hex.Length];

                    //感測值結果
                    if (intSencorPort != 2)
                        double_Mpa = ((Convert.ToDouble(Convert.ToInt32(str_hex, 16))) * 20 / 4095 - 4) / 16;
                    else
                        double_Mpa = ((Convert.ToDouble(Convert.ToInt32(str_hex, 16))) * 20 / 4095 - 4) * 101 / (-16);

                    double_Mpa = Math.Round(double_Mpa, 2);

                    if (double_Mpa >= 0)
                    {
                        strStatic[intIndex] = $"正壓 ({double_Mpa.ToString()})";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{double_Mpa.ToString()}',GETDATE(),'{strNote}') ";
                    }
                    else if (double_Mpa < 0)
                    {
                        strStatic[intIndex] = $"負壓 ({double_Mpa.ToString()})";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{double_Mpa.ToString()}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = $"({double_Mpa.ToString()})";
                        strCmdSQL = "";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", ex.Source);
                }
            }
        }
        #endregion

        #region 16 ITMC版燈號(S1F1)
        void function_16()
        {
            try
            {
                int int_Net_Available = 0;

                byteCmdSend = Encoding.ASCII.GetBytes("S1F1");

                clientSocket.Send(byteCmdSend, 0, byteCmdSend.Length, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int_Net_Available = clientSocket.Available;
                if (int_Net_Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    string temp = Encoding.ASCII.GetString(byteCmdReceive, 0, int_Net_Available).Trim(';').Split(',')[1];

                    if (temp == "Green")
                    {
                        //綠燈(運行中)
                        strStatic[intIndex] = "綠燈";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','0100',GETDATE(),'{strNote}') ";
                    }
                    else if (temp == "Yellow")
                    {
                        //黃燈(暫停中)
                        strStatic[intIndex] = "黃燈";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','0010',GETDATE(),'{strNote}') ";
                    }
                    else if (temp == "Red")
                    {
                        //紅燈(停止中)
                        strStatic[intIndex] = "紅燈";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','1000',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = MPU.str_ErrorMessage[5];
                        strCmdSQL = "";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }

                //休息1秒
                Sleep(50, 20);
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 17 ITMC版氣壓(S1F2)
        void function_17()
        {
            try
            {
                byteCmdSend = Encoding.ASCII.GetBytes("S1F2");
                clientSocket.Send(byteCmdSend, 0, byteCmdSend.Length, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                if (int_Net_Available != 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    string temp = Encoding.ASCII.GetString(byteCmdReceive, 0, int_Net_Available).Trim(';').Split(',')[intSencorPort + 1];
                    if (temp != "NA" && !string.IsNullOrWhiteSpace(temp) && !string.IsNullOrEmpty(temp) && decimal.TryParse(temp, out decimal n))
                    {
                        if (Convert.ToDouble(temp) >= 0)
                            strStatic[intIndex] = $"正壓 ({temp})";
                        else
                            strStatic[intIndex] = $"負壓 ({temp})";

                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{temp}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = MPU.str_ErrorMessage[5];
                        strCmdSQL = "";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }

                //休息1秒
                Sleep(50, 20);
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", ex.Source);
                }
            }
        }
        #endregion

        #region 18 ITMC版流量(S1F3)
        void function_18()
        {
            try
            {
                byteCmdSend = Encoding.ASCII.GetBytes("S1F3");

                clientSocket.Send(byteCmdSend, 0, byteCmdSend.Length, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;
                if (int_Net_Available != 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    double double_Mpa = 0;

                    double_Mpa = Convert.ToDouble(Encoding.ASCII.GetString(byteCmdReceive, 0, int_Net_Available).Trim(';').Split(',')[intSencorPort + 1]);

                    if (!string.IsNullOrWhiteSpace(double_Mpa.ToString()) && !string.IsNullOrEmpty(double_Mpa.ToString()) && decimal.TryParse(double_Mpa.ToString(), out decimal n))
                    {
                        strStatic[intIndex] = $"流量 ({double_Mpa.ToString()})";

                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{double_Mpa.ToString()}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = MPU.str_ErrorMessage[5];
                        strCmdSQL = "";
                    }

                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }

                //休息1秒
                Sleep(50, 20);
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 19 ITMC版溫度(S1F4)
        void function_19()
        {
            try
            {
                byteCmdSend = Encoding.ASCII.GetBytes("S1F4");
                clientSocket.Send(byteCmdSend, 0, byteCmdSend.Length, System.Net.Sockets.SocketFlags.None);


                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;
                if (int_Net_Available != 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);
                    string temp = Encoding.ASCII.GetString(byteCmdReceive, 0, int_Net_Available).Trim(';').Split(',')[1];

                    temp = temp.Replace("NA", "0");
                    if (!string.IsNullOrWhiteSpace(temp) && !string.IsNullOrEmpty(temp) && decimal.TryParse(temp, out decimal n))
                    {
                        strStatic[intIndex] = $"溫度 ({temp})";

                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{temp.Split('/')[0]}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = MPU.str_ErrorMessage[5];
                        strCmdSQL = "";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }

                //休息1秒
                Sleep(50, 20);
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 20 ITMC版種晶吸嘴阻值(S1F5)
        void function_20()
        {
            try
            {
                byteCmdSend = Encoding.ASCII.GetBytes("S1F5");
                clientSocket.Send(byteCmdSend, 0, byteCmdSend.Length, System.Net.Sockets.SocketFlags.None);


                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                if (int_Net_Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);
                    string temp = Encoding.ASCII.GetString(byteCmdReceive, 0, int_Net_Available).Trim(';').Split(',')[1];

                    temp = temp.Replace("NA", "0");

                    if (!string.IsNullOrWhiteSpace(temp) && !string.IsNullOrEmpty(temp) && decimal.TryParse(temp, out decimal n))
                    {
                        strStatic[intIndex] = $"種晶吸嘴阻值 ({temp})";

                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{temp}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = MPU.str_ErrorMessage[5];
                        strCmdSQL = "";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }

                //休息1秒
                Sleep(50, 20);
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 21 ITMC版H-Judge讀值(S1F6)
        void function_21()
        {
            try
            {
                byteCmdSend = Encoding.ASCII.GetBytes("S1F6");
                clientSocket.Send(byteCmdSend, 0, byteCmdSend.Length, System.Net.Sockets.SocketFlags.None);


                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                if (int_Net_Available != 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);
                    string temp = Encoding.ASCII.GetString(byteCmdReceive, 0, int_Net_Available).Trim(';').Split(',')[1];

                    temp = temp.Replace("NA", "0");

                    if (!string.IsNullOrWhiteSpace(temp) && !string.IsNullOrEmpty(temp) && decimal.TryParse(temp, out decimal n))
                    {
                        strStatic[intIndex] = $"H-Judge讀值 ({temp})";

                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{temp}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = MPU.str_ErrorMessage[5];
                        strCmdSQL = "";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }

                //休息1秒
                Sleep(50, 20);
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 22 螢幕辨識參數(Temperature, Power, force, Time)(S1F7)
        void function_22()
        {
            try
            {
                byteCmdSend = Encoding.ASCII.GetBytes("S1F7");
                clientSocket.Send(byteCmdSend, 0, byteCmdSend.Length, System.Net.Sockets.SocketFlags.None);


                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;
                clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                if (int_Net_Available != 0)
                {
                    string[] temp = Encoding.ASCII.GetString(byteCmdReceive, 0, int_Net_Available).Trim(';').Split(',');
                    if (temp[1] != "NA")
                    {
                        temp[1] = temp[1].TrimStart('0');

                        if (!string.IsNullOrWhiteSpace(temp[1]) && !string.IsNullOrEmpty(temp[1]) && decimal.TryParse(temp[1], out decimal n))
                        {
                            strStatic[intIndex] = $"溫度 ({temp[1]})";

                            strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{temp[1]}',GETDATE(),'{strNote}') ";
                        }
                        else
                        {
                            strStatic[intIndex] = MPU.str_ErrorMessage[5];
                            strCmdSQL = "";
                        }
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }

                //休息1秒
                Sleep(50, 20);
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 23 BrainChild 溫度
        void function_23()
        {
            try
            {
                byteCmdSend = strCmdBrainChild.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, strCmdBrainChild.Split(' ').Length, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                if (int_Net_Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);
                    double SH = 4553.6, SL = -1999.9;   
                    double temp;
                    string hex = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');
                    temp = (((SH - SL) / 65535) * Convert.ToInt32(hex, 16)) + SL - 1;
                    if (temp > 0)
                    {
                        strStatic[intIndex] = $"溫度({Math.Round(temp, 0).ToString()})";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{strStatic[intIndex].Trim()}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = "異常";
                        strCmdSQL = "";
                    }                    
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];

                    strCmdSQL = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 24 舉離機(NMPA, NMPB)(S1F8)
        void function_24()
        {
            try
            {
                int int_Net_Available = 0;

                byteCmdSend = Encoding.ASCII.GetBytes("S1F8");
                clientSocket.Send(byteCmdSend, 0, byteCmdSend.Length, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int_Net_Available = clientSocket.Available;
                if (int_Net_Available > 0)
                {
                    byteCmdReceive = new byte[int_Net_Available];
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    string temp = Encoding.ASCII.GetString(byteCmdReceive, 0, int_Net_Available).Trim('\0').Trim(';');

                    if (!string.IsNullOrWhiteSpace(temp) && !string.IsNullOrEmpty(temp))
                    {
                        //綠燈(運行中)
                        strStatic[intIndex] = $"S1F8 ({temp})";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{temp}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = MPU.str_ErrorMessage[5];
                        strCmdSQL = "";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }

                //休息1秒
                Sleep(50, 20);
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #region 25 混合壓
        void function_25(int port)
        {
            try
            {
                switch (port)
                {
                    case 1:
                        byteCmdSend = strCmdEight_1.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();
                        break;
                    case 2:
                        byteCmdSend = strCmdEight_2.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();
                        break;
                    case 5:
                        byteCmdSend = strCmdEight_5.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();
                        break;
                    case 6:
                        byteCmdSend = strCmdEight_6.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();
                        break;
                    default:
                        break;
                }

                clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                if (int_Net_Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    String str_hex = "";
                    double double_Mpa = 0;

                    str_hex = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');


                    //感測值結果
                    double mA = Convert.ToDouble(Convert.ToInt32(str_hex, 16)) * 20 / 4095;
                    double_Mpa = 0.00003 * Math.Pow(mA, 2) + 0.068 * mA - 0.3725;

                    double_Mpa = Math.Round(double_Mpa, 2);

                    if (double_Mpa > 0)
                    {
                        strStatic[intIndex] = $"正壓 ({double_Mpa.ToString()})";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{double_Mpa.ToString()}',GETDATE(),'{strNote}') ";
                    }
                    else if (double_Mpa < 0)
                    {
                        strStatic[intIndex] = $"負壓 ({double_Mpa.ToString()})";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{double_Mpa.ToString()}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = $"({double_Mpa.ToString()})";
                        strCmdSQL = "";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", ex.Source);
                }
            }
        }
        #endregion

        #region 26 只PING設備IP，連線成功就回傳黃燈
        void function_26()
        {

            Ping ping = new Ping();
            try
            {
                //PING設備IP
                PingReply reply = ping.Send(strDIP, 100);
                if (reply.Status == IPStatus.Success)
                {
                    //連線成功則顯示黃燈
                    strStatic[intIndex] = "黃燈";
                    string str_light = "0010";
                    strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                }
                else
                {
                    //連線失敗則顯示錯誤訊息
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", ex.Source);
                }
            }
        }
        #endregion

        #region 27 晶圓清洗機#1燈號判斷
        void function_27()
        {
            try
            {
                int int_Net_Available = 0;
                double double_Mpa_1 = 0;
                double double_Mpa_2 = 0;
                //紅燈
                byteCmdSend = strCmdLight_1.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int_Net_Available = clientSocket.Available;
                if (int_Net_Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);
                    String str_hex_1 = "";
                    double_Mpa_1 = 0;
                    str_hex_1 = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    //感測值結果
                    double_Mpa_1 = Convert.ToInt32(str_hex_1, 16);

                    //綠燈
                    byteCmdSend = strCmdLight_2.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                    clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                    Sleep(50, 10);
                    //int int_Net_Available = clientSocket.Available;
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);
                    String str_hex_2 = "";
                    double_Mpa_2 = 0;
                    str_hex_2 = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    //感測值結果
                    double_Mpa_2 = Convert.ToInt32(str_hex_2, 16);

                    string str_light = "";
                    if (double_Mpa_1 > 20000)
                    {
                        //綠燈(運行中)
                        strStatic[intIndex] = "綠燈";
                        str_light = "0100";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else if (double_Mpa_1 < 20000 && double_Mpa_2 > 20000)
                    {
                        //紅燈(停止中)
                        strStatic[intIndex] = "紅燈";
                        str_light = "1000";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else if ((double_Mpa_1 < 20000 && double_Mpa_2 < 20000) && (double_Mpa_1 > 0 && double_Mpa_2 > 0))
                    {
                        //黃燈(暫停中)
                        strStatic[intIndex] = "黃燈";
                        str_light = "0010";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        //黃燈(暫停中)
                        strStatic[intIndex] = "黃燈";
                        str_light = "0010";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];
                    strCmdSQL = "";
                    Console.WriteLine("Exception source: {0}", EX.Source + EX.Message);
                }
            }
        }
        #endregion

        #region 28 電漿蝕刻機#2燈號判斷
        void function_28()
        {
            try
            {
                int int_Net_Available = 0;
                double double_Mpa_1 = 0;
                double double_Mpa_2 = 0;
                //紅燈
                byteCmdSend = strCmdLight_1.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int_Net_Available = clientSocket.Available;
                if (int_Net_Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);
                    String str_hex_1 = "";
                    double_Mpa_1 = 0;
                    str_hex_1 = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    //感測值結果
                    double_Mpa_1 = Convert.ToInt32(str_hex_1, 16);

                    //綠燈
                    byteCmdSend = strCmdLight_2.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                    clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                    Sleep(50, 10);

                    //int int_Net_Available = clientSocket.Available;
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);
                    String str_hex_2 = "";
                    double_Mpa_2 = 0;
                    str_hex_2 = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    //感測值結果
                    double_Mpa_2 = Convert.ToInt32(str_hex_2, 16);

                    string str_light = "";
                    if (double_Mpa_1 > 20000)
                    {
                        //綠燈(運行中)
                        strStatic[intIndex] = "綠燈";
                        str_light = "0100";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";

                    }
                    else if (double_Mpa_2 > 20000)
                    {
                        //黃燈(暫停中)
                        strStatic[intIndex] = "黃燈";
                        str_light = "0010";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else if ((double_Mpa_1 < 20000 && double_Mpa_2 < 20000) && (double_Mpa_1 > 0 && double_Mpa_2 > 0))
                    {
                        //黃燈(暫停中)
                        strStatic[intIndex] = "黃燈";
                        str_light = "0010";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        //黃燈(暫停中)
                        strStatic[intIndex] = "黃燈";
                        str_light = "0010";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];
                    strCmdSQL = "";
                    Console.WriteLine("Exception source: {0}", EX.Source + EX.Message);
                }
            }
        }
        #endregion

        #region 29 Cello RIE反應式離子蝕刻機-1燈號判斷
        void function_29()
        {
            try
            {
                int int_Net_Available = 0;
                double double_Mpa_1 = 0;
                double double_Mpa_2 = 0;
                //紅燈
                byteCmdSend = strCmdLight_1.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int_Net_Available = clientSocket.Available;
                if (int_Net_Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);
                    String str_hex_1 = "";
                    double_Mpa_1 = 0;
                    str_hex_1 = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    //感測值結果
                    double_Mpa_1 = Convert.ToInt32(str_hex_1, 16);

                    //綠燈
                    byteCmdSend = strCmdLight_2.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                    clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                    Sleep(50, 10);

                    //int int_Net_Available = clientSocket.Available;
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);
                    String str_hex_2 = "";
                    double_Mpa_2 = 0;

                    str_hex_2 = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    //感測值結果
                    double_Mpa_2 = Convert.ToInt32(str_hex_2, 16);

                    string str_light = "";
                    if (double_Mpa_1 > 20000)
                    {
                        //綠燈(運行中)
                        strStatic[intIndex] = "綠燈";
                        str_light = "0100";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else if (double_Mpa_2 > 20000)
                    {
                        //紅燈(停止中)
                        strStatic[intIndex] = "紅燈";
                        str_light = "1000";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else if ((double_Mpa_1 < 20000 && double_Mpa_2 < 20000) && (double_Mpa_1 > 0 && double_Mpa_2 > 0))
                    {
                        //黃燈(暫停中)
                        strStatic[intIndex] = "黃燈";
                        str_light = "0010";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        //黃燈(暫停中)
                        strStatic[intIndex] = "黃燈";
                        str_light = "0010";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];
                    strCmdSQL = "";
                    Console.WriteLine("Exception source: {0}", EX.Source + EX.Message);
                }
            }
        }
        #endregion

        #region 33 調頻機燈號判斷
        void function_33()
        {
            try
            {
                byteCmdSend = strCmdEight_2.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                if (int_Net_Available > 0)
                {
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    String str_hex = "";
                    double double_Mpa = 0;

                    str_hex = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    //感測值結果
                    double mA = Convert.ToDouble(Convert.ToInt32(str_hex, 16)) * 20 / 4095;
                    double_Mpa = 0.00003 * Math.Pow(mA, 2) + 0.068 * mA - 0.3725;

                    double_Mpa = Math.Round(double_Mpa, 2);
                    string str_light = "";
                    if (double_Mpa == 0)
                    {
                        //黃燈(待機中)
                        strStatic[intIndex] = "黃燈";
                        str_light = "0010";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                    else if (double_Mpa < 0)
                    {
                        //綠燈(運行中)
                        strStatic[intIndex] = "綠燈";
                        str_light = "0100";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_light}',GETDATE(),'{strNote}') ";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", ex.Source);
                }
            }
        }
        #endregion

        #region 34 水阻值(新)
        void function_34()
        {
            try
            {
                byteCmdSend = strCmd485_8Port.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                //送出Byte陣列指令
                clientSocket.Send(byteCmdSend, 0, 8, System.Net.Sockets.SocketFlags.None);

                //休息500毫秒
                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;

                if (int_Net_Available > 0)
                {
                    //接收Byte陣列迴傳值
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    String str_hex = "";
                    double double_Mpa = 0;

                    //取出Byte陣列中數值位置Byte值
                    str_hex = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0');

                    //轉換感測值結果
                    double mA = Convert.ToDouble(Convert.ToInt32(str_hex, 16)) * 20 / 4095;

                    double_Mpa = mA * 0.92;

                    //將感測值取四捨五入制小數點第一位
                    double_Mpa = Math.Round(double_Mpa, 1);

                    strStatic[intIndex] = $"水阻值 ({double_Mpa.ToString()})";
                    strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{double_Mpa.ToString()}',GETDATE(),'{strNote}') ";
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("M0312:Exception source: {0}", ex.Source);
                }
            }
        }
        #endregion

        #region 35 完鍍機參數(S1F9)
        void function_35()
        {
            try
            {
                int int_Net_Available = 0;

                byteCmdSend = Encoding.ASCII.GetBytes("S1F8");
                clientSocket.Send(byteCmdSend, 0, byteCmdSend.Length, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int_Net_Available = clientSocket.Available;
                if (int_Net_Available > 0)
                {
                    byteCmdReceive = new byte[int_Net_Available];
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    string temp = Encoding.ASCII.GetString(byteCmdReceive, 0, int_Net_Available).Trim('\0').Trim(';');

                    if (!string.IsNullOrWhiteSpace(temp) && !string.IsNullOrEmpty(temp))
                    {
                        //綠燈(運行中)
                        strStatic[intIndex] = $"S1F8 ({temp})";
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME],[NOTE]) VALUES ('{strTID}','{strDIP}','{strSID}','{temp}',GETDATE(),'{strNote}') ";
                    }
                    else
                    {
                        strStatic[intIndex] = MPU.str_ErrorMessage[5];
                        strCmdSQL = "";
                    }
                }
                else
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[5];
                    strCmdSQL = "";
                }

                //休息1秒
                Sleep(50, 20);
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {
                    strStatic[intIndex] = MPU.str_ErrorMessage[1];

                    strCmdSQL = "";

                    Console.WriteLine("Exception source: {0}", EX.Source);
                }
            }
        }
        #endregion

        #endregion

        #region 廠務設備 function 30-32 氮氣流量、比電阻、水流量
        //221209 對數值
        #region 30 AFR 氮氣流量
        void function_30()
        {

            try
            {
                //判斷是否有資料以及是否可以讀取，並且此連結資料通道可以抓取資料時才能動作。
                byteCmdSend = strCmdAFR.Split(' ').Select(t => Convert.ToByte(t, 16)).ToArray();

                clientSocket.Send(byteCmdSend, 0, 12, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;
                //有條碼時才動作。

                if (int_Net_Available > 0)
                {

                    string str_T = "";

                    //抓取資料之後轉為字元
                    //Convert.ToString(byteCmdReceive[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0')
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    str_T = Convert.ToString(byteCmdReceive[9], 16).PadLeft(2, '0') + Convert.ToString(byteCmdReceive[10], 16).PadLeft(2, '0');

                    str_T = Convert.ToInt32(str_T, 16).ToString();

                    str_T = str_T.Insert(str_T.Length - 2, ".");

                    //for (int j = 0; j <= int_Net_Available - 2; j++)
                    //{

                    //    str_T += Convert.ToChar(byteCmdReceive[j]);

                    //}

                    strStatic[intIndex] = int_Net_Available + "@" + str_T + ":" + strSclass;

                    //
                    //if (str_T.Length > 16)
                    //{

                    //    str_T = str_T.Substring(0, 16);

                    //}

                    //如果非空字串的話
                    if (str_T != "")
                    {
                        //if (String_Dline == "P2")
                        //{
                        strCmdSQL = "INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_T}',GETDATE());";

                        //}
                        //if (String_Dline == "P3")
                        //{
                        //    strCmdSQL = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID]           ,[DIP]           ,[SID]           ,[DVALUE]           ,[SYSTIME])     VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','" + str_T + "',GETDATE()) ;";

                        //}
                        //171119 
                        //String light_Str;

                        //light_Str = "0100";

                        //String Str_SQL_sid_X_WLS = "SELECT S_ID FROM [dbMES].[dbo].[tb_sensors_rules] WHERE d_ID =  (SELECT d_ID FROM   [dbMES].[dbo].[tb_sensors_rules]  WHERE s_ID = '" + strSID + "') AND S_ID LIKE '%WLS%'";

                        //if (String_Dline == "P2")
                        //{

                        //    strCmdSQL += "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "',(" + Str_SQL_sid_X_WLS + "),'" + light_Str + "',GETDATE(),'" + strNote + "') ;";

                        //}
                        //else if (String_Dline == "P3")
                        //{
                        //    strCmdSQL += "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "',(" + Str_SQL_sid_X_WLS + "),'" + light_Str + "',GETDATE(),'" + strNote + "') ;";

                        //}
                        //

                        strBarcode = str_T;




                    }
                    else
                    {
                        strCmdSQL = "";
                    }

                }
                else
                {
                    //沒有資料仍要提示訊息
                    strStatic[intIndex] = "Available[" + int_Net_Available + "]:[" + NetworkStream_Reader.CanRead.ToString() + "]@無資料:" + strSclass;

                    //沒有資料的時候丟個資料過去不要讓他斷線

                    //byte[] byte_Command_wlslcs = new byte[5];

                    //byte_Command_wlslcs[0] = 48;
                    //byte_Command_wlslcs[1] = 56;
                    //byte_Command_wlslcs[2] = 48;
                    //byte_Command_wlslcs[3] = 10;
                    //byte_Command_wlslcs[4] = 13;

                    //NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);

                    //INSERT 一筆
                    if (booleanInsert == true)
                    {

                        // strCmdSQL = "INSERT INTO [dbo].[tb_recordslogTEST] ([DID]           ,[DIP]           ,[SID]           ,[DVALUE]           ,[SYSTIME])     VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','RUN-161103756-00',GETDATE()) ";

                    }

                    booleanInsert = false;

                }

            }
            catch (Exception EX)
            {

                if (EX.Source != null)
                {

                    strStatic[intIndex] = "Ex:" + EX.Source;

                    Console.WriteLine("M0375:Exception source: {0}", EX.Source + EX.Message);

                    //161208 超出時間後再連一次。
                    byte_function_03_loop_index += 1;

                    //當10次沒有連結的時候，重新連線一次。
                    if (byte_function_03_loop_index > 5)
                    {
                        //重新建立連結
                        SocketClientConnect();

                        byte_function_03_loop_index = 0;

                    }

                }
            }

        }
        #endregion

        #region 31 RES 比電阻
        void function_31()
        {
            try
            {
                //判斷是否有資料以及是否可以讀取，並且此連結資料通道可以抓取資料時才能動作。
                string str_T = "0";

                if (strSID == "RES001")
                { str_T = "17.88"; }

                if (strSID == "RES002")
                { str_T = "18.24"; }

                strStatic[intIndex] = str_T + ":" + strSclass;


                //如果非空字串的話
                if (str_T != "")
                {
                    strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_T}',GETDATE()) ; ";
                }
                else
                {
                    strCmdSQL = "";
                }
            }
            catch (Exception EX)
            {
                if (EX.Source != null)
                {

                    strStatic[intIndex] = "Ex:" + EX.Source;

                    Console.WriteLine("M0375:Exception source: {0}", EX.Source + EX.Message);

                }
            }
        }
        #endregion

        #region 32 LFM 水流量
        void function_32()
        {
            

            try
            {
                // TCP 發送:
                //`0a 03 00 3a 00 02 e5 7d`
                //192.168.7.44回傳:
                //`00 0a 03 04 01 96 6e a3 cd 3a `
                //`01 96 6e a3 ` 轉換 `26635939 ` 

                //判斷是否有資料以及是否可以讀取，並且此連結資料通道可以抓取資料時才能動作。
                //0a 03 00 3a 00 02 e5 7d
                byte[] byte_Command_wlslcs = new byte[8];

                byte_Command_wlslcs[0] = 0x0A;
                byte_Command_wlslcs[1] = 0x03;
                byte_Command_wlslcs[2] = 0x00;
                byte_Command_wlslcs[3] = 0x3A;
                byte_Command_wlslcs[4] = 0x00;
                byte_Command_wlslcs[5] = 0x02;
                byte_Command_wlslcs[6] = 0xE5;
                byte_Command_wlslcs[7] = 0x7D;

                clientSocket.Send(byte_Command_wlslcs, 0, 8, System.Net.Sockets.SocketFlags.None);

                Sleep(50, 10);

                int int_Net_Available = clientSocket.Available;
                //有資料時才動作。

                if (int_Net_Available > 0)
                {

                    string str_T = "";

                    //抓取資料之後轉為字元
                    //Convert.ToString(byteCmdReceive[1 + (int_GetPortid * 2)], 16).PadLeft(2, '0')
                    clientSocket.Receive(byteCmdReceive, 0, int_Net_Available, SocketFlags.None);

                    ////`00 0a 03 04 01 96 6e a3 cd 3a `
                    //`01 96 6e a3 ` 轉換 `26635939 `
                    str_T = Convert.ToString(byteCmdReceive[4], 16).PadLeft(2, '0')
                          + Convert.ToString(byteCmdReceive[5], 16).PadLeft(2, '0')
                          + Convert.ToString(byteCmdReceive[7], 16).PadLeft(2, '0')
                          + Convert.ToString(byteCmdReceive[8], 16).PadLeft(2, '0');

                    str_T = Convert.ToInt32(str_T, 16).ToString();

                    str_T = str_T.Insert(str_T.Length - 3, ".");

                    //for (int j = 0; j <= int_Net_Available - 2; j++)
                    //{

                    //    str_T += Convert.ToChar(byteCmdReceive[j]);

                    //}

                    strStatic[intIndex] = int_Net_Available + "@" + str_T + ":" + strSclass;

                    //
                    //if (str_T.Length > 16)
                    //{

                    //    str_T = str_T.Substring(0, 16);

                    //}

                    //如果非空字串的話
                    if (str_T != "")
                    {
                        //if (String_Dline == "P2")
                        //{
                        strCmdSQL = $"INSERT INTO [dbo].[tb_{strDline.Trim()}recordslog] ([DID],[DIP],[SID],[DVALUE],[SYSTIME]) VALUES ('{strTID}','{strDIP}','{strSID}','{str_T}',GETDATE()) ; ";

                        //}
                        //if (String_Dline == "P3")
                        //{
                        //    strCmdSQL = "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME]) VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','" + str_T + "',GETDATE()) ;";

                        //}
                        //171119 
                        //String light_Str;

                        //light_Str = "0100";

                        //String Str_SQL_sid_X_WLS = "SELECT S_ID FROM [dbMES].[dbo].[tb_sensors_rules] WHERE d_ID =  (SELECT d_ID FROM   [dbMES].[dbo].[tb_sensors_rules]  WHERE s_ID = '" + strSID + "') AND S_ID LIKE '%WLS%'";

                        //if (String_Dline == "P2")
                        //{

                        //    strCmdSQL += "INSERT INTO [dbo].[tb_P2recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "',(" + Str_SQL_sid_X_WLS + "),'" + light_Str + "',GETDATE(),'" + strNote + "') ;";

                        //}
                        //else if (String_Dline == "P3")
                        //{
                        //    strCmdSQL += "INSERT INTO [dbo].[tb_P3recordslog_1] ([DID],[DIP],[SID],[DVALUE],[SYSTIME] ,[NOTE])     VALUES ('" + strTID + "','" + strDIP + "',(" + Str_SQL_sid_X_WLS + "),'" + light_Str + "',GETDATE(),'" + strNote + "') ;";

                        //}
                        //

                        strBarcode = str_T;




                    }
                    else
                    {
                        strCmdSQL = "";
                    }

                }
                else
                {
                    //沒有資料仍要提示訊息
                    strStatic[intIndex] = "Available[" + int_Net_Available + "]:[" + NetworkStream_Reader.CanRead.ToString() + "]@無資料:" + strSclass;

                    //沒有資料的時候丟個資料過去不要讓他斷線

                    //byte[] byte_Command_wlslcs = new byte[5];

                    //byte_Command_wlslcs[0] = 48;
                    //byte_Command_wlslcs[1] = 56;
                    //byte_Command_wlslcs[2] = 48;
                    //byte_Command_wlslcs[3] = 10;
                    //byte_Command_wlslcs[4] = 13;

                    //NetworkStream_Reader.Write(byte_Command_wlslcs, 0, 5);

                    //INSERT 一筆
                    if (booleanInsert == true)
                    {

                        // strCmdSQL = "INSERT INTO [dbo].[tb_recordslogTEST] ([DID]           ,[DIP]           ,[SID]           ,[DVALUE]           ,[SYSTIME])     VALUES ('" + strTID + "','" + strDIP + "','" + strSID + "','RUN-161103756-00',GETDATE()) ";

                    }

                    booleanInsert = false;

                }

            }
            catch (Exception EX)
            {

                if (EX.Source != null)
                {

                    strStatic[intIndex] = "Ex:" + EX.Source;

                    Console.WriteLine("M0375:Exception source: {0}", EX.Source + EX.Message);

                    //161208 超出時間後再連一次。
                    byte_function_03_loop_index += 1;

                    //當10次沒有連結的時候，重新連線一次。
                    if (byte_function_03_loop_index > 5)
                    {
                        //重新建立連結
                        SocketClientConnect();

                        byte_function_03_loop_index = 0;

                    }

                }
            }
        }
        #endregion

        #endregion
    }
}
