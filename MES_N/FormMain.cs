using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MES_N
{

    public partial class FormMain : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        public FormMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //防止Form繪製時閃爍問題
            this.SetStyle(
              ControlStyles.AllPaintingInWmPaint |
              ControlStyles.UserPaint |
              ControlStyles.DoubleBuffer, true);

            //設定Form大小
            this.Size = new Size(1290, 682);

            //config.txt中第一個布林變數為true代表會回寫Dayno資料表
            if (MPU.GetFileData("./Config", "config.txt")[0].Split(',')[0] == "true")
                MPU.canInsertDayno = true;
            else
                MPU.canInsertDayno = false;

            //config.txt中第二個布林變數為true代表會將感測器數據回寫資料庫
            //false則代表不會回寫資料庫，測試模式
            if (MPU.GetFileData("./Config", "config.txt")[0].Split(',')[1] == "true")
                MPU.canInsertToDB = true;
            else
                MPU.canInsertToDB = false;

            //獲取當前路徑最後一級資料夾名稱
            DirectoryInfo info = new DirectoryInfo(System.Environment.CurrentDirectory);
            string strFolderName = info.Name;
            
            //更改Form名稱
            this.Text += $" - {strFolderName}";

            GetDeviceList();

            SetDatatable();
        }

        System.Data.DataTable Datatable_showtxt;

        public void GetDeviceList()
        {

            Console.WriteLine("[GetDevicweList]");

            try
            {
                //純文字檔擷取資料版
                dynamic StreamReader_Txt = new System.IO.StreamReader(Application.StartupPath + "\\\\server.txt", System.Text.Encoding.GetEncoding("BIG5"));

                //儲存資料檔案的字串陣列
                string[] Barcode_P_DB = new string[1];

                string strLine = null;

                Datatable_showtxt = new System.Data.DataTable();

                String[] Arry_Str_Set = { "" };

                String StrDataColumnsName = "DID,DIP,S_KIND,Port,Address,Sclass,Portid,SID,text,note,Dline";

                MPU.SetDataColumn(ref Arry_Str_Set, StrDataColumnsName);

                MPU.SetDataTable("", ref Datatable_showtxt, Arry_Str_Set);

                do
                {
                    strLine = StreamReader_Txt.ReadLine();


                    if (!(strLine == null) && Datatable_showtxt.Columns.Count <= strLine.Split(',').Length)
                    {

                        Datatable_showtxt.Rows.Add(strLine.Split(','));

                    }

                } while (!(strLine == null));

                Datatable_showtxt.DefaultView.Sort = "DID asc";

                Datatable_showtxt = Datatable_showtxt.DefaultView.ToTable(true);

                StreamReader_Txt.Close();

                //從資料庫抓取資料版

            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "[GetDevicweList]" + ex.Message);
                if (ex.Source != null)

                    Console.WriteLine("F0475:Exception source: {0}", ex.Source);

            }

        }

        List<int> intDiveceIndex = new List<int>();
        Dictionary<String, Dictionary<String, List<String>>> dicDeviceList = new Dictionary<String, Dictionary<String, List<String>>>();
        private void SetDatatable()
        {
            Console.WriteLine("[SetDatatable]");
            try
            {
                String[] Arry_Str_Set = { "" };

                String StrDataColumnsName = "TID,Dline,DIP,Port,Address,Portid,Sclass,SID,NOTE,Static";

                MPU.SetDataColumn(ref Arry_Str_Set, StrDataColumnsName);

                MPU.SetDataTable("", ref MPU.dt_MainTable, Arry_Str_Set);

                //設定主頁DataTable內容
                for (int i = 0; i < Datatable_showtxt.Rows.Count; i++)
                {
                    String[] String_RowData = { Datatable_showtxt.Rows[i]["DID"].ToString(),
                    Datatable_showtxt.Rows[i]["Dline"].ToString(),
                    Datatable_showtxt.Rows[i]["DIP"].ToString(),
                    Datatable_showtxt.Rows[i]["Port"].ToString(),
                    Datatable_showtxt.Rows[i]["Address"].ToString(),
                    Datatable_showtxt.Rows[i]["Portid"].ToString() ,
                    Datatable_showtxt.Rows[i]["Sclass"].ToString() ,
                    Datatable_showtxt.Rows[i]["SID"].ToString() ,
                    Datatable_showtxt.Rows[i]["NOTE"].ToString() +  "_" +
                    Datatable_showtxt.Rows[i]["text"].ToString(), "連線中",
                    };

                    MPU.dt_MainTable.Rows.Add(String_RowData);
                }

                //設定主頁dgv的DataSourse
                dgvMainView.DataSource = MPU.dt_MainTable;

                //設定dgv的DataSourse
                if (MPU.Ethernet == true)
                {
                    dgvCurrentLog.DataSource = MPU.dt_CurrentLog;
                    dgvHistoryLog.DataSource = MPU.dt_HistoryLog;
                }

                //計算不同IP不同Port的數量並將IP和Port存入字典
                int count = 0;
                foreach (DataRow row in Datatable_showtxt.Rows)
                {
                    string ip = row["DIP"].ToString();
                    string port = row["Port"].ToString();
                    string sclass = row["Sclass"].ToString();

                    // IP不存在時，加入新的字典
                    if (!dicDeviceList.ContainsKey(ip))
                        dicDeviceList.Add(ip, new Dictionary<String, List<String>>());

                    // Port不存在時，加入新的List
                    if (!dicDeviceList[ip].ContainsKey(port))
                    {
                        dicDeviceList[ip].Add(port, new List<String>());
                        intDiveceIndex.Add(count);
                    }

                    count++;
                    dicDeviceList[ip][port].Add(sclass);
                }
                
                //設定執行緒參數並啟動執行緒
                SetThread();

            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "[SetDatatable] : " + ex.Message);

                if (ex.Source != null)

                    Console.WriteLine("F0573:Exception source: {0}", ex.Source);
            }
        }

        //實做n個MES傳輸類別
        MesNetSite[] MesNetSite_Sys = new MesNetSite[] { };
        Thread[] Thread_MesNetSite = new Thread[] { };
        int intThreadIndex = 0;
        List<bool> isThreadAlive = new List<bool>();
        List<bool> isTcpConnect = new List<bool>();

        private void SetThread()
        {
            try
            {
                //設定MesNetSite類別陣列的大小
                Array.Resize(ref MesNetSite_Sys, dicDeviceList.Values.SelectMany(t => t).Count());

                //設定執行緒陣列的大小
                Array.Resize(ref Thread_MesNetSite, dicDeviceList.Values.SelectMany(t => t).Count());

                //設定執行緒參數並啟動執行緒
                for (int i = 0; i < Datatable_showtxt.Rows.Count; i++)
                {
                    if (!MPU.dic_ReceiveMessage.ContainsKey(i))
                        MPU.dic_ReceiveMessage.GetOrAdd(i, MPU.dt_MainTable.Rows[i]["Static"].ToString());

                    if (intDiveceIndex.Contains(i))
                    {
                        int n = intDiveceIndex.IndexOf(i);
                        MesNetSite_Sys[n] = new MesNetSite();

                        MesNetSite_Sys[n].strTID = MPU.dt_MainTable.Rows[i]["TID"].ToString();

                        MesNetSite_Sys[n].strDline = MPU.dt_MainTable.Rows[i]["Dline"].ToString();

                        MesNetSite_Sys[n].strDIP = MPU.dt_MainTable.Rows[i]["DIP"].ToString();

                        MesNetSite_Sys[n].strSID = MPU.dt_MainTable.Rows[i]["SID"].ToString();

                        MesNetSite_Sys[n].strPort = MPU.dt_MainTable.Rows[i]["Port"].ToString();

                        MesNetSite_Sys[n].strAddress = MPU.dt_MainTable.Rows[i]["Address"].ToString();

                        MesNetSite_Sys[n].strPortId = MPU.dt_MainTable.Rows[i]["Portid"].ToString();

                        MesNetSite_Sys[n].strSclass = MPU.dt_MainTable.Rows[i]["Sclass"].ToString();

                        MesNetSite_Sys[n].strNote = MPU.dt_MainTable.Rows[i]["NOTE"].ToString();

                        MesNetSite_Sys[n].dicDeviceList = dicDeviceList[MPU.dt_MainTable.Rows[i]["DIP"].ToString()];

                        MesNetSite_Sys[n].int_ThreadNum = intDiveceIndex[n];


                        MesNetSite_Sys[n].int_ReaderSleep = 50000;


                        MesNetSite_Sys[n].bool_AutoRun = true;

                        MesNetSite_Sys[n].bool_isThreadSet = true;

                        //宣告一個執行緒來處理
                        Thread_MesNetSite[n] = new System.Threading.Thread(MesNetSite_Sys[n].MesNetSiteRunning);
                        Thread_MesNetSite[n].IsBackground = true;
                        Thread_MesNetSite[n].Start();

                        isThreadAlive.Add(true);
                        isTcpConnect.Add(true);
                        intThreadIndex++;
                    }
                }

                //設定dgv格式
                SetDatagridview();

                //啟動timer更新dgv
                bool_run = true;
                tmrUpdateDT.Enabled = true;
                tmrUpdateDT.Start();

                Application.DoEvents();
            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "[SetThread] " + ex.StackTrace);
                Console.WriteLine("[SetThread] " + ex.StackTrace);
            }
        }


        //設定dgv格式
        private void SetDatagridview()
        {
            try
            {
                dgvMainView.Columns[0].Width = 70;
                dgvMainView.Columns[1].Width = 60;
                dgvMainView.Columns[2].Width = 90;
                dgvMainView.Columns[3].Width = 60;
                dgvMainView.Columns[4].Width = 60;
                dgvMainView.Columns[5].Width = 100;
                dgvMainView.Columns[6].Width = 60;
                dgvMainView.Columns[7].Width = 100;
                dgvMainView.Columns[8].Width = 452;
                dgvMainView.Columns[9].Width = 190;                

                //限制使用者無法對DGV排序
                foreach (DataGridViewColumn column in dgvMainView.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
                foreach (DataGridViewColumn column in dataGridView_Result.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
                foreach (DataGridViewColumn column in dgvCurrentLog.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
                foreach (DataGridViewColumn column in dgvHistoryLog.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "[SetDatagridview] " + ex.StackTrace);
                Console.WriteLine("[SetDatagridview] " + ex.StackTrace);
            }
        }

        //新增Combobox內容
        private void SetComboboxValue()
        {
            try
            {
                //取得Dline不重複值，並新增到combobox
                List<string> Dline_list = new List<string>();
                Dline_cmb.Items.Add("");
                foreach (DataRow row in MPU.dt_MainTable.Rows)
                {
                    if (!Dline_list.Contains(row["Dline"].ToString())) // 判斷有沒有重覆。
                    {
                        Dline_list.Add(row["Dline"].ToString());
                        Dline_cmb.Items.Add(row["Dline"].ToString());
                    }
                }

                //取得Sclass不重複值，並新增到combobox
                List<string> Sclass_list = new List<string>();
                Sclass_cmb.Items.Add("");
                foreach (DataRow row in MPU.dt_MainTable.Rows)
                {
                    if (!Sclass_list.Contains(row["Sclass"].ToString())) // 判斷有沒有重覆。
                    {
                        Sclass_list.Add(row["Sclass"].ToString());
                        Sclass_cmb.Items.Add(row["Sclass"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "[SetComboboxValue] " + ex.StackTrace);
                Console.WriteLine("[SetComboboxValue] " + ex.StackTrace);
            }
        }

        private void SetHistoryLog()
        {
            try
            {
                if (MPU.Ethernet == true)
                {
                    //設定DateTimePicker格式
                    dtpStart.CustomFormat = "yyyy 年 MM 月 dd 日　HH:mm:ss";
                    dtpStart.Format = DateTimePickerFormat.Custom;
                    dtpEnd.CustomFormat = "yyyy 年 MM 月 dd 日　HH:mm:ss";
                    dtpEnd.Format = DateTimePickerFormat.Custom;

                    StringBuilder sbSQL = new StringBuilder();

                    // 1.建立暫存表
                    sbSQL.AppendFormat(@"create table #DIPtable
                                        (
                                            DIP char(100)
                                        )");
                    sbSQL.AppendLine();

                    // 2.將DID加入暫存表
                    sbSQL.AppendLine("insert into #DIPtable (DIP) values ");
                    for (int j = 0; j < MPU.dt_MainTable.Rows.Count; j++)
                    {
                        if (j == (MPU.dt_MainTable.Rows.Count - 1))
                            sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["DIP"]}');");
                        else
                            sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["DIP"]}'),");
                    }
                    sbSQL.AppendLine();

                    // 3.將tb_connectlog與暫存表做inner join，篩選出需要的資料
                    sbSQL.AppendFormat(@"SELECT TOP(100)
                                            tb_connectlog.DID, 
                                            tb_connectlog.DIP IP, 
                                            DVALUE Note, 
                                            FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss') as 重新連線時間, 
                                            FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間, 
                                            '' as 狀態,
											'' as Sort
                                        FROM tb_connectlog
                                        inner join #DIPtable on tb_connectlog.DIP = #DIPtable.DIP
                                        order by Sort desc");
                    sbSQL.AppendLine();

                    // 4.移除剛才建立的暫存表
                    sbSQL.AppendLine("drop table #DIPtable;");

                    DataTable DataTable_HistLog = new DataTable();
                    DataTable_HistLog = null;
                    DataTable_HistLog = MPU.ReadSQLToDT(sbSQL.ToString(), 10);

                    if (MPU.Ethernet == true)
                    {
                        //設定歷史警報的dgv內容
                        MPU.dt_HistoryLog = DataTable_HistLog.Clone();
                        for (int i = 0; i < DataTable_HistLog.Rows.Count; i++)
                        {
                            if (string.IsNullOrEmpty(DataTable_HistLog.Rows[i]["重新連線時間"].ToString()) || 
                                string.IsNullOrEmpty(DataTable_HistLog.Rows[i]["斷線時間"].ToString()))
                            {
                                string[] String_RowData =
                                {
                                        DataTable_HistLog.Rows[i]["DID"].ToString(),
                                        DataTable_HistLog.Rows[i]["IP"].ToString(),
                                        DataTable_HistLog.Rows[i]["Note"].ToString(),
                                        DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),
                                        DataTable_HistLog.Rows[i]["斷線時間"].ToString(), "斷線時間 " +
                                        DataTable_HistLog.Rows[i]["斷線時間"].ToString(),
                                        DataTable_HistLog.Rows[i]["斷線時間"].ToString()
                                    };
                                MPU.dt_HistoryLog.Rows.Add(String_RowData);
                            }
                            else
                            {
                                string[] String_RowData1 =
                                {
                                        DataTable_HistLog.Rows[i]["DID"].ToString(),
                                        DataTable_HistLog.Rows[i]["IP"].ToString(),
                                        DataTable_HistLog.Rows[i]["Note"].ToString(),
                                        DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),"","重新連線時間 " +
                                        DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),
                                        DataTable_HistLog.Rows[i]["重新連線時間"].ToString()
                                    };
                                MPU.dt_HistoryLog.Rows.Add(String_RowData1);

                                string[] String_RowData2 =
                                {
                                        DataTable_HistLog.Rows[i]["DID"].ToString(),
                                        DataTable_HistLog.Rows[i]["IP"].ToString(),
                                        DataTable_HistLog.Rows[i]["Note"].ToString(),"",
                                        DataTable_HistLog.Rows[i]["斷線時間"].ToString(),"斷線時間 " +
                                        DataTable_HistLog.Rows[i]["斷線時間"].ToString(),
                                        DataTable_HistLog.Rows[i]["斷線時間"].ToString()
                                    };
                                MPU.dt_HistoryLog.Rows.Add(String_RowData2);
                            }
                        }

                        DataView dv = MPU.dt_HistoryLog.DefaultView;
                        dv.Sort = "Sort desc";
                        MPU.dt_HistoryLog = dv.ToTable();
                        dgvHistoryLog.DataSource = dv.ToTable();


                        //設定歷史警報的dgv格式
                        dgvHistoryLog.Columns[0].Width = 100;
                        dgvHistoryLog.Columns[1].Width = 100;
                        dgvHistoryLog.Columns[2].Width = 827;
                        dgvHistoryLog.Columns[5].Width = 223;
                        dgvHistoryLog.Columns[3].Visible = false;
                        dgvHistoryLog.Columns[4].Visible = false;
                        dgvHistoryLog.Columns[6].Visible = false;
                    }
                    for (int i = 0; i < MPU.dt_HistoryLog.Rows.Count; i++)
                    {
                        if (MPU.dt_HistoryLog.Rows[i]["狀態"].ToString().Contains("斷線時間"))
                        {
                            dgvHistoryLog.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                        }
                        else
                        {
                            dgvHistoryLog.Rows[i].DefaultCellStyle.BackColor = Color.DarkSeaGreen;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "[SetHistoryLog] " + ex.Message);
                Console.WriteLine("[SetHistoryLog] " + ex.StackTrace);
            }
        }

        private void SetCurrentLog(object sender, EventArgs e)
        {
            try
            {
                //若連線成功才執行SQL
                if (MPU.Ethernet == true)
                { 
                    StringBuilder sbSQL = new StringBuilder();

                    // 1.建立暫存表
                    sbSQL.AppendFormat(@"create table #DIPtable
					                                (
						                                DIP char(100)
					                                )");
                    sbSQL.AppendLine();

                    // 2.將DID加入暫存表
                    sbSQL.AppendLine("insert into #DIPtable (DIP) values ");
                    for (int j = 0; j < MPU.dt_MainTable.Rows.Count; j++)
                    {
                        if (j == (MPU.dt_MainTable.Rows.Count - 1))
                            sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["DIP"]}');");
                        else
                            sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["DIP"]}'),");
                    }
                    sbSQL.AppendLine();

                    // 3.將tb_connectlog與暫存表做inner join，篩選出需要的資料
                    sbSQL.AppendFormat(@"select 
                                            tb_connectlog.DID, 
                                            tb_connectlog.DIP IP, 
                                            DVALUE Note, 
                                            format ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間 
                                        from tb_connectlog 
                                        inner join #DIPtable on tb_connectlog.DIP = #DIPtable.DIP
                                        where CONTIME is null 
                                        order by DISTIME desc");
                    sbSQL.AppendLine();

                    // 4.移除剛才建立的暫存表
                    sbSQL.AppendLine("drop table #DIPtable;");

                    MPU.dt_CurrentLog = MPU.ReadSQLToDT(sbSQL.ToString(), 10);
                }
            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "[SetCurrentLog] " + ex.StackTrace);
                Console.WriteLine("[SetCurrentLog] " + ex.StackTrace);
            }
        }

        #region 更新dgv，回寫Dayno資料表
        bool bool_run;
        string strSQLRumTime = "";
        private void tmrUpdateDT_Tick(object sender, EventArgs e)
        {
            Check_Connection.CheckConnaction();

            if (bool_run)
            {
                bool_run = false;
                try
                {
                    // 更新dgvMainView畫面
                    foreach (KeyValuePair<int, string> item in MPU.dic_ReceiveMessage)
                    {
                        try
                        {
                            if (MPU.dt_MainTable.Rows.Count == dgvMainView.Rows.Count)
                            {
                                if (MPU.dt_MainTable.Rows[item.Key]["Static"] != null && dgvMainView.Rows[item.Key] != null)
                                {
                                    //MPU.WriteData(@".\view_Log", "view_Log.txt", DateTime.Now.ToString("HH:mm:ss") + " intDiveceIndex：" + item.Key.ToString());
                                    MPU.dt_MainTable.Rows[item.Key]["Static"] = DateTime.Now.ToString("HH:mm:ss") + " ... " + item.Value;
                                    if (item.Value.Contains(MPU.str_ErrorMessage[0]) ||
                                        item.Value.Contains(MPU.str_ErrorMessage[1]) ||
                                        item.Value.Contains(MPU.str_ErrorMessage[2]) ||
                                        item.Value.Contains(MPU.str_ErrorMessage[6]))
                                    {
                                        dgvMainView.Rows[item.Key].DefaultCellStyle.BackColor = System.Drawing.Color.MistyRose;
                                    }
                                    //else if (string.IsNullOrEmpty(MPU.dt_MainTable.Rows[item.Key]["Static"].ToString()))
                                    //    MPU.dt_MainTable.Rows[item.Key]["Static"] = DateTime.Now.ToString("HH:mm:ss") + " ... " + MPU.str_ErrorMessage[1];
                                    else
                                        dgvMainView.Rows[item.Key].DefaultCellStyle.BackColor = System.Drawing.Color.White;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            bool_run = true;
                            MPU.WriteErrorCode("", "[tmrUpdateDT] " + ex.StackTrace);
                            Console.WriteLine($"[tmrUpdateDT setDataRow - {item.Key.ToString()}] " + ex.StackTrace);    
                            throw ex;
                        }
                    }

                    #region 暫時註解
                    //if (MPU.canInsertToDB && DateTime.Now.ToString("yyyy-MM-dd HH:mm") != strSQLRumTime)
                    //{
                    //    strSQLRumTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    //    for (int i = 0; i < MesNetSite_Sys.Length; i++)
                    //    {
                    //        StringBuilder sbSQL = new StringBuilder();
                    //        if (isAlarmExist == false && MesNetSite_Sys[i].clientSocket != null
                    //            && !((!(MesNetSite_Sys[i].clientSocket.Poll(1, SelectMode.SelectRead) && MesNetSite_Sys[i].clientSocket.Available == 0))
                    //            && MesNetSite_Sys[i].clientSocket.Connected == true))
                    //        {
                    //            isAlarmExist = true;
                    //            // IF判斷如果不存在斷線紀錄
                    //            sbSQL.AppendFormat(@"BEGIN TRAN
                    //                                        IF NOT EXISTS (SELECT * FROM tb_connectlog 
                    //                                        WHERE DIP = '{0}' and CONTIME IS NULL)",
                    //                                    MesNetSite_Sys[i].strDIP);
                    //            sbSQL.AppendLine();

                    //            sbSQL.Append("  INSERT INTO ");
                    //            sbSQL.AppendFormat(@"tb_connectlog (DID, DIP, DVALUE, DISTIME, SYSTIME) 
                    //                                        VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')
                    //                                    IF(@@ERROR<>0)
                    //                                     ROLLBACK TRAN;
                    //                                    ELSE
                    //                                     COMMIT TRAN;",
                    //                                MesNetSite_Sys[i].strTID, MesNetSite_Sys[i].strDIP, MesNetSite_Sys[i].strNote.Split('_')[0], DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    //            sbSQL.AppendLine();
                    //        }
                    //        else
                    //        {
                    //            isAlarmExist = false;
                    //            // IF判斷如果存在斷線紀錄，且沒有重新連線紀錄
                    //            sbSQL.AppendFormat(@"BEGIN TRAN
                    //                                        IF EXISTS (SELECT * FROM tb_connectlog 
                    //                                        WHERE DIP = '{0}' and CONTIME IS NULL)",
                    //                                        MesNetSite_Sys[i].strDIP);
                    //            sbSQL.AppendLine();

                    //            sbSQL.Append("  UPDATE ");
                    //            sbSQL.AppendFormat(@"tb_connectlog SET CONTIME = '{0}' 
                    //                                            WHERE  
                    //                                            DIP = '{1}' and  
                    //                                            CONTIME IS NULL
                    //                                    IF(@@ERROR<>0)
                    //                                     ROLLBACK TRAN;
                    //                                    ELSE
                    //                                     COMMIT TRAN;",
                    //                                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), MesNetSite_Sys[i].strDIP);
                    //            sbSQL.AppendLine();
                    //        }
                    //        if (!string.IsNullOrWhiteSpace(sbSQL.ToString()))
                    //            MPU.ReadSQL(sbSQL.ToString());
                    //    }
                    //}
                    #endregion

                    // 若config.txt參數為true時，才將DNO寫入tb_dayno資料表，若為false則不寫入
                    if (MPU.canInsertDayno)
                    {
                        // 整點時紀錄最新DNO
                        if (DateTime.Now.ToString("mm") == "00" && DateTime.Now.ToString("yyyy-MM-dd HH:mm") != strSQLRumTime)
                        {
                            strSQLRumTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                            MPU.ReadSQL(@"
                            INSERT INTO tb_dayno(DNO,SYSTIME,TYPE) VALUES( (SELECT MAX(DNO) FROM tb_recordslog),GETDATE(),'RE');
                            INSERT INTO tb_dayno(DNO,SYSTIME,TYPE) VALUES( (SELECT MAX(DNO) FROM tb_P2recordslog),GETDATE(),'P2');
                            INSERT INTO tb_dayno(DNO,SYSTIME,TYPE) VALUES( (SELECT MAX(DNO) FROM tb_P3recordslog),GETDATE(),'P3');
                            INSERT INTO tb_dayno(DNO,SYSTIME,TYPE) VALUES( (SELECT MAX(DNO) FROM tb_CSPrecordslog),GETDATE(),'CSP');");
                        }
                    }

                    #region 針對寫入敗的SQL語法等待10秒後再重送一次，還是失敗才寫入temp
                    //判斷是否有寫入失敗的SQL語法
                    if (MPU.dicSQL.Count > 0)
                    {
                        //一次只處理10個SQL語法
                        for (int i = 0; i < 10; i++)
                        {
                            //確保還有SQL尚未處理，並且距離上次寫入失敗已經超過10秒
                            if (MPU.dicSQL.Count > 0 && (DateTime.Now - MPU.dicSQL.First().Value).TotalSeconds >= 10)
                            {
                                MPU.ReReadSQL(MPU.dicSQL.First().Key);
                                MPU.dicSQL.Remove(MPU.dicSQL.First().Key);
                            }
                            else
                                break;
                        }
                    }
                    #endregion

                    bool_run = true;
                }
                catch (Exception ex)
                {
                    bool_run = true;
                    MPU.WriteErrorCode("", "[tmrUpdateDT] " + ex.Message.ToString());
                    Console.WriteLine("[tmrUpdateDT] " + ex.Message.ToString());
                }
            }
        }
        #endregion

        #region 即時警報搜尋按鈕功能
        string conn_str;
        string[] txt_str = new string[7];
        bool isEmpty;
        DataRow[] ResultRow;
        DataTable ResultTable = new DataTable();
        private void CurrLogSearch_btn_Click(object sender, EventArgs e)
        {
            try
            {
                string TID_conn = "TID = " + TID_txt.Text;
                string Dline_conn = "Dline = " + Dline_cmb.Text;
                string DIP_conn = "DIP = " + DIP_txt.Text;
                string Sclass_conn = "Sclass = " + Sclass_cmb.Text;
                string Note_conn = "Note = " + Note_txt.Text;
                string SID_conn = "SID = " + SID_txt.Text;
                string Static_conn = "Static = " + Static_txt.Text;

                conn_str = "";
                isEmpty = true;

                txt_str[0] = TID_txt.Text;
                txt_str[1] = Dline_cmb.Text;
                txt_str[2] = DIP_txt.Text;
                txt_str[3] = Sclass_cmb.Text;
                txt_str[4] = Note_txt.Text;
                txt_str[5] = SID_txt.Text;
                txt_str[6] = Static_txt.Text;


                for (int i = 0; i < 7; i++)
                {
                    isEmpty = isEmpty && string.IsNullOrEmpty(txt_str[i]);
                    if (isEmpty)
                    {
                        dataGridView_Result.DataSource = null;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(txt_str[i]))
                        {
                            switch (i)
                            {
                                case 0:
                                    conn_str += " and TID = '" + txt_str[i] + "'";
                                    break;
                                case 1:
                                    conn_str += " and Dline = '" + txt_str[i] + "'";
                                    break;
                                case 2:
                                    conn_str += " and DIP = '" + txt_str[i] + "'";
                                    break;
                                case 3:
                                    conn_str += " and Sclass = '" + txt_str[i] + "'";
                                    break;
                                case 4:
                                    conn_str += " and Note = '" + txt_str[i] + "'";
                                    break;
                                case 5:
                                    conn_str += " and SID = '" + txt_str[i] + "'";
                                    break;
                                case 6:
                                    conn_str += " and Static = '" + txt_str[i] + "'";
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                if (isEmpty)
                {
                    dataGridView_Result.DataSource = null;
                }
                else
                {
                    conn_str = conn_str.TrimStart(" and ".ToArray());

                    ResultRow = MPU.dt_MainTable.Select(conn_str);
                    ResultTable = MPU.dt_MainTable.Clone();
                    foreach (var Rows in ResultRow)
                    {
                        ResultTable.ImportRow(Rows);
                    }
                    dataGridView_Result.DataSource = ResultTable;

                    for (int i = 0; i < ResultTable.Rows.Count; i++)
                    {
                        if (ResultTable.Rows[i]["Static"].ToString().Contains(MPU.str_ErrorMessage[0]) || ResultTable.Rows[i]["Static"].ToString().Contains(MPU.str_ErrorMessage[1]))
                        {
                            dataGridView_Result.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                        }
                        else
                        {
                            dataGridView_Result.Rows[i].DefaultCellStyle.BackColor = Color.White;
                        }
                    }
                    dataGridView_Result.Columns[0].Width = 70;
                    dataGridView_Result.Columns[1].Width = 60;
                    dataGridView_Result.Columns[2].Width = 90;
                    dataGridView_Result.Columns[3].Width = 60;
                    dataGridView_Result.Columns[4].Width = 60;
                    dataGridView_Result.Columns[5].Width = 60;
                    dataGridView_Result.Columns[6].Width = 60;
                    dataGridView_Result.Columns[7].Width = 100;
                    dataGridView_Result.Columns[8].Width = 193;
                    dataGridView_Result.Columns[9].Width = 170;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            
        }
        #endregion

        #region 歷史警報搜尋按鈕功能
        DateTime start = new DateTime();
        DateTime end = new DateTime();
        private void HistLogSearch_btn_Click(object sender, EventArgs e)
        {
            try
            {
                start = dtpStart.Value;
                end = dtpEnd.Value;
                //end = end.AddDays(1);

                StringBuilder sbSQL = new StringBuilder();

                // 1.建立暫存表
                sbSQL.AppendFormat(@"create table #DIPtable
					                    (
						                    DIP char(100)
					                    )");
                sbSQL.AppendLine();

                // 2.將SID加入暫存表
                sbSQL.AppendLine("insert into #DIPtable (DIP) values ");
                for (int j = 0; j < MPU.dt_MainTable.Rows.Count; j++)
                {
                    if (j == (MPU.dt_MainTable.Rows.Count - 1))
                        sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["DIP"]}');");
                    else
                        sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["DIP"]}'),");
                }
                sbSQL.AppendLine();

                // 3.將tb_connectlog與暫存表做inner join，篩選出需要的資料
                sbSQL.AppendFormat(@"SELECT TOP(1000) 
                                            tb_connectlog.DID, 
                                            tb_connectlog.DIP IP, 
                                            DVALUE Note, 
                                            FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss') as 重新連線時間, 
                                            FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間, 
                                            '' as 狀態,
											'' as Sort
                                        FROM tb_connectlog 
                                        inner join #DIPtable on tb_connectlog.DIP = #DIPtable.DIP
                                        WHERE 
                                            ((CONTIME BETWEEN '{0}' AND '{1}') OR (DISTIME BETWEEN '{2}' AND 
                                            '{3}')) 
                                        ORDER BY Sort DESC",
                                    start.ToString("yyyy-MM-dd HH:mm:ss"), end.ToString("yyyy-MM-dd HH:mm:ss"), start.ToString("yyyy-MM-dd HH:mm:ss"), end.ToString("yyyy-MM-dd HH:mm:ss"));
                sbSQL.AppendLine();

                // 4.移除剛才建立的暫存表
                sbSQL.AppendLine("drop table #DIPtable;");

                DataTable DataTable_HistLog = new DataTable();
                DataTable_HistLog = null;
                DataTable_HistLog = MPU.ReadSQLToDT(sbSQL.ToString(), 10);

                if (MPU.Ethernet == true)
                {
                    MPU.dt_HistoryLog = DataTable_HistLog.Clone();
                    for (int i = 0; i < DataTable_HistLog.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(DataTable_HistLog.Rows[i]["重新連線時間"].ToString()) ||
                            string.IsNullOrEmpty(DataTable_HistLog.Rows[i]["斷線時間"].ToString()))
                        {
                            string[] String_RowData =
                            {
                                    DataTable_HistLog.Rows[i]["DID"].ToString(),
                                    DataTable_HistLog.Rows[i]["IP"].ToString(),
                                    DataTable_HistLog.Rows[i]["Note"].ToString(),
                                    DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),
                                    DataTable_HistLog.Rows[i]["斷線時間"].ToString(),"斷線時間 " +
                                    DataTable_HistLog.Rows[i]["斷線時間"].ToString(),
                                    DataTable_HistLog.Rows[i]["斷線時間"].ToString()
                                };
                            MPU.dt_HistoryLog.Rows.Add(String_RowData);
                        }
                        else
                        {
                            string[] String_RowData1 =
                            {
                                    DataTable_HistLog.Rows[i]["DID"].ToString(),
                                    DataTable_HistLog.Rows[i]["IP"].ToString(),
                                    DataTable_HistLog.Rows[i]["Note"].ToString(),
                                    DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),"","重新連線時間 " +
                                    DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),
                                    DataTable_HistLog.Rows[i]["重新連線時間"].ToString()
                                };
                            MPU.dt_HistoryLog.Rows.Add(String_RowData1);

                            string[] String_RowData2 =
                            {
                                    DataTable_HistLog.Rows[i]["DID"].ToString(),
                                    DataTable_HistLog.Rows[i]["IP"].ToString(),
                                    DataTable_HistLog.Rows[i]["Note"].ToString(),"",
                                    DataTable_HistLog.Rows[i]["斷線時間"].ToString(),"斷線時間 " +
                                    DataTable_HistLog.Rows[i]["斷線時間"].ToString(),
                                    DataTable_HistLog.Rows[i]["斷線時間"].ToString()
                                };
                            MPU.dt_HistoryLog.Rows.Add(String_RowData2);
                        }
                    }

                    DataView dv = MPU.dt_HistoryLog.DefaultView;
                    dv.Sort = "Sort desc";
                    MPU.dt_HistoryLog = dv.ToTable();
                    dgvHistoryLog.DataSource = dv.ToTable();

                    for (int i = 0; i < MPU.dt_HistoryLog.Rows.Count; i++)
                    {
                        if (string.IsNullOrEmpty(MPU.dt_HistoryLog.Rows[i]["重新連線時間"].ToString()))
                        {
                            dgvHistoryLog.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                        }
                        else
                        {
                            dgvHistoryLog.Rows[i].DefaultCellStyle.BackColor = Color.DarkSeaGreen;
                        }
                    }

                    dgvHistoryLog.Columns[0].Width = 100;
                    dgvHistoryLog.Columns[1].Width = 100;
                    dgvHistoryLog.Columns[2].Width = 820;
                    //dgvLog[1].Columns[3].Width = 150;
                    //dgvLog[1].Columns[4].Width = 150;
                    dgvHistoryLog.Columns[5].Width = 224;
                    dgvHistoryLog.Columns[3].Visible = false;
                    dgvHistoryLog.Columns[4].Visible = false;
                    dgvHistoryLog.Columns[6].Visible = false;
                }
                for (int i = 0; i < MPU.dt_HistoryLog.Rows.Count; i++)
                {
                    if (MPU.dt_HistoryLog.Rows[i]["狀態"].ToString().Contains("斷線時間"))
                    {
                        dgvHistoryLog.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                    }
                    else
                    {
                        dgvHistoryLog.Rows[i].DefaultCellStyle.BackColor = Color.DarkSeaGreen;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        #endregion

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Check_Connection.CheckConnaction())
            {
                if (tabControl1.SelectedIndex == 1) //Tools
                {
                    SetComboboxValue();
                    tmrCurrentLog.Stop();
                    tmrCurrentLog.Enabled = false;
                }
                else if (tabControl1.SelectedIndex == 2) //即時警報
                {

                    tmrCurrentLog.Interval = 1000;
                    dgvCurrentLog.DataSource = MPU.dt_CurrentLog;
                    dgvCurrentLog.Columns[0].Width = 100;
                    dgvCurrentLog.Columns[1].Width = 70;
                    dgvCurrentLog.Columns[2].Width = 857;
                    dgvCurrentLog.Columns[3].Width = 223;

                    tmrCurrentLog.Enabled = true;
                    tmrCurrentLog.Start();
                }
                else if (tabControl1.SelectedIndex == 3) //歷史警報
                {
                    tmrCurrentLog.Stop();
                    tmrCurrentLog.Enabled = false;
                    SetHistoryLog();
                }
                else
                {
                    tmrCurrentLog.Stop();
                    tmrCurrentLog.Enabled = false;
                }
            }
            else
                MessageBox.Show("資料庫連線失敗！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (MessageBox.Show("程式即將關閉，是否繼續？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    tmrUpdateDT.Stop();
                    tmrUpdateDT.Enabled = false;
                    tmrCurrentLog.Stop();
                    tmrCurrentLog.Enabled = false;
                    bool_run = false;

                    Thread.Sleep(500);
                    int count = 0;

                    //關閉設備連線，直到全部設備都關閉才往下繼續做
                    while (isTcpConnect.IndexOf(true) > 0 || count < 5)
                    {
                        for (int i = 0; i < isTcpConnect.Count; i++)
                        {
                            MesNetSite_Sys[i].bool_AutoRun = false;
                            if (MesNetSite_Sys[i].clientSocket != null)
                            {
                                try
                                {
                                    MesNetSite_Sys[i].clientSocket.Shutdown(SocketShutdown.Both);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("[SocketClientConnect] Socket斷線失敗"+ex.Message);
                                }
                                MesNetSite_Sys[i].clientSocket.Close();
                                MesNetSite_Sys[i].clientSocket = null;
                                isTcpConnect[i] = false;
                            }
                            else
                            {
                                isTcpConnect[i] = false;
                            }
                        }
                        count++;
                        Thread.Sleep(100);
                    }

                    Thread.Sleep(100);
                    count = 0;

                    //關閉執行緒，直到執行緒都關閉才關閉程式
                    while (isThreadAlive.IndexOf(true) > -1 || count < 5)
                    {
                        for (int i = 0; i < isThreadAlive.Count; i++)
                        {
                            if (Thread_MesNetSite[i] != null)
                            {
                                if (Thread_MesNetSite[i].IsAlive)
                                {
                                    Thread_MesNetSite[i].Abort();
                                    //只要有任何一個執行緒還在運作，就把isThreadAlive改為true
                                    isThreadAlive[i] = true;
                                }
                                else
                                {
                                    isThreadAlive[i] = false;
                                }
                            }
                            else
                            {
                                isThreadAlive[i] = false;
                            }
                        }
                        count++;
                        Thread.Sleep(100);
                    }
                    Environment.Exit(0);
                }
                else
                    e.Cancel = true;    //取消關閉
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void dataGridView_Threads_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            //if ((e.Exception) is ConstraintException)
            //{
            //    //MPU.WriteErrorCode("", "[dataGridView DataError] " + ex.Message);
            //}

            //if (e.Context == DataGridViewDataErrorContexts.Commit)
            //{

            //}

            //if (e.Context == DataGridViewDataErrorContexts.CurrentCellChange)
            //{

            //}

            //if (e.Context == DataGridViewDataErrorContexts.Parsing)
            //{

            //}

            //if (e.Context == DataGridViewDataErrorContexts.LeaveControl)
            //{

            //}
        }        
    }
}
