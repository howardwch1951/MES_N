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
    public partial class Form1 : Form
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


        public static Form1 form1;
        public Form1()
        {
            InitializeComponent();
            form1 = this;
        }

        public List<DataGridView> dgvLog = new List<DataGridView>();
        private delegate void InvokeDelegate();
        bool canInsertDayno;
        private void Form1_Load(object sender, EventArgs e)
        {
            this.SetStyle(
              ControlStyles.AllPaintingInWmPaint |
              ControlStyles.UserPaint |
              ControlStyles.DoubleBuffer, true);

            this.Size = new Size(1290, 682);

            if (MPU.GetFileData("./Config", "config.txt")[0] == "true")
                canInsertDayno = true;
            else
                canInsertDayno = false;

            DirectoryInfo info = new DirectoryInfo(System.Environment.CurrentDirectory);
            string strFolderName = info.Name;//獲取當前路徑最後一級資料夾名稱
            
            this.Text += $" - {strFolderName}";

            dtpStart.CustomFormat = "yyyy 年 MM 月 dd 日　HH:mm:ss";
            dtpStart.Format = DateTimePickerFormat.Custom;
            dtpEnd.CustomFormat = "yyyy 年 MM 月 dd 日　HH:mm:ss";
            dtpEnd.Format = DateTimePickerFormat.Custom;

            dgvLog.Add(this.Controls.Find("dgvMainView", true)[0] as DataGridView);
            dgvLog.Add(this.Controls.Find("dataGridView_CurrLog", true)[0] as DataGridView);
            dgvLog.Add(this.Controls.Find("dataGridView_HistLog", true)[0] as DataGridView);

            bool_isAutoRun = false;

            GetDeviceList();

            SetDatatable();

            //new Thread(SetDatatable)
            //{
            //    IsBackground = true
            //}
            //.Start();
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

                //String StrDataColumnsName = "DID,DIP,SID,port,portid,text,note";
                String StrDataColumnsName = "TID,Dline,DIP,Port,Address,Portid,Sclass,SID,NOTE,Static";

                MPU.SetDataColumn(ref Arry_Str_Set, StrDataColumnsName);

                MPU.SetDataTable("", ref MPU.dt_MainTable, Arry_Str_Set);

                
                for (int i = 0; i <= Datatable_showtxt.Rows.Count - 1; i++)
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

                //MPU.bs_MainTable.DataSource = MPU.dt_MainTable;
                //dgvMainView.DataSource = MPU.bs_MainTable;

                dgvMainView.DataSource = MPU.dt_MainTable;

                string str_SQL = "";
                for (int j = 0; j < MPU.dt_MainTable.Rows.Count; j++)
                {
                    str_SQL += "'" 
                        + MPU.dt_MainTable.Rows[j]["DIP"].ToString() + "," 
                        + MPU.dt_MainTable.Rows[j]["SID"].ToString() + "," 
                        + MPU.dt_MainTable.Rows[j]["NOTE"].ToString() + "'" 
                        + ",";
                }
                str_SQL = str_SQL.Trim(',');

                //MPU.dt_CurrentLog = MPU.ReadSQLToDT(string.Format(@"
                //    SELECT DIP IP, 
                //        SID Sclass, 
                //        DVALUE Note, 
                //        FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間 
                //    FROM tb_connectlog 
                //    WHERE CONTIME IS NULL AND 
                //        CONCAT(LTRIM(RTRIM(DIP)),',',LTRIM(RTRIM(SID)),',',LTRIM(RTRIM(DVALUE))) IN ({0}) 
                //    ORDER BY DISTIME DESC", str_SQL));

                if (MPU.Ethernet == true)
                {
                    //MPU.bs_CurrentLog.DataSource = MPU.dt_CurrentLog;
                    //dataGridView_CurrLog.DataSource = MPU.bs_CurrentLog;

                    dataGridView_CurrLog.DataSource = MPU.dt_CurrentLog;
                }
                    

                str_SQL = "";
                for (int i = 0; i < MPU.dt_MainTable.Rows.Count; i++)
                {
                    str_SQL += "'" 
                        + MPU.dt_MainTable.Rows[i]["DIP"].ToString() + "," 
                        + MPU.dt_MainTable.Rows[i]["SID"].ToString() + "," 
                        + MPU.dt_MainTable.Rows[i]["NOTE"].ToString() + "'" 
                        + ",";
                }

                str_SQL = str_SQL.Trim(',');

                //MPU.dt_HistoryLog = MPU.ReadSQLToDT(string.Format(@"
                //    SELECT TOP(50) 
                //        DIP IP, 
                //        SID Sclass, 
                //        DVALUE Note, 
                //        FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss') as 重新連線時間, 
                //        FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間, 
                //        CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'), 
                //        FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as 狀態, 
                //        CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'), 
                //        FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as Sort 
                //    FROM tb_connectlog 
                //    WHERE CONCAT(LTRIM(RTRIM(DIP)),',',LTRIM(RTRIM(SID)),',',LTRIM(RTRIM(DVALUE))) IN ({0}) 
                //    ORDER BY Sort DESC", str_SQL));

                if (MPU.Ethernet == true)
                {
                    //MPU.bs_HistoryLog.DataSource = MPU.dt_HistoryLog;
                    //dataGridView_HistLog.DataSource = MPU.bs_HistoryLog;

                    dataGridView_HistLog.DataSource = MPU.dt_HistoryLog;
                }

                SetDatagridview();

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
                
                new Thread(SetThread).Start();

               


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
        System.Threading.Thread[] Thread_MesNetSite = new System.Threading.Thread[] { };
        int intThreadIndex = 0;
        List<bool> isThreadAlive = new List<bool>();
        List<bool> isTcpConnect = new List<bool>();
        private void SetThread()
        {
            try
            {
                SetComboboxValue();

                Array.Resize(ref MesNetSite_Sys, dicDeviceList.Values.SelectMany(t => t).Count());

                Array.Resize(ref Thread_MesNetSite, dicDeviceList.Values.SelectMany(t => t).Count());

                for (int i = 0; i < Datatable_showtxt.Rows.Count; i++)
                {
                    if (!MPU.dic_ReceiveMessage.ContainsKey(i))
                        MPU.dic_ReceiveMessage.GetOrAdd(i, MPU.dt_MainTable.Rows[i]["Static"].ToString());

                    if (intDiveceIndex.Contains(i))
                    {
                        int n = intDiveceIndex.IndexOf(i);
                        MesNetSite_Sys[n] = new MesNetSite();

                        MesNetSite_Sys[n].String_TID = MPU.dt_MainTable.Rows[i]["TID"].ToString();

                        MesNetSite_Sys[n].String_Dline = MPU.dt_MainTable.Rows[i]["Dline"].ToString();

                        MesNetSite_Sys[n].String_DIP = MPU.dt_MainTable.Rows[i]["DIP"].ToString();

                        MesNetSite_Sys[n].String_SID = MPU.dt_MainTable.Rows[i]["SID"].ToString();

                        MesNetSite_Sys[n].String_Port = MPU.dt_MainTable.Rows[i]["Port"].ToString();

                        MesNetSite_Sys[n].String_Address = MPU.dt_MainTable.Rows[i]["Address"].ToString();

                        MesNetSite_Sys[n].Str_Portid = MPU.dt_MainTable.Rows[i]["Portid"].ToString();

                        MesNetSite_Sys[n].String_Sclass = MPU.dt_MainTable.Rows[i]["Sclass"].ToString();

                        MesNetSite_Sys[n].String_NOTE = MPU.dt_MainTable.Rows[i]["NOTE"].ToString();

                        MesNetSite_Sys[n].dicDeviceList = dicDeviceList[MPU.dt_MainTable.Rows[i]["DIP"].ToString()];

                        //Array.Resize(ref MesNetSite_Sys[n].String_ReData, 1);
                        //MesNetSite_Sys[n].String_ReData[0] = MPU.str_ErrorMessage[3];

                        MesNetSite_Sys[n].int_ThreadNum = intDiveceIndex[n];

                        MesNetSite_Sys[n].int_timeOutMsec = 0;

                        MesNetSite_Sys[n].int_ReaderSleep = 1000;

                        MesNetSite_Sys[n].int_ReaderSleepSET = 1000;

                        MesNetSite_Sys[n].bool_AutoRun = true;

                        MesNetSite_Sys[n].bool_isThreadSet = true;

                        MesNetSite_Sys[n].TcpClientConnect();

                        //'宣告一個執行緒來處理 reader讀取 電子標籤的動作。
                        //Thread_MesNetSite[i] = new System.Threading.Thread(MesNetSite_Sys[i].MesNetSiteRunning);
                        //Thread_MesNetSite[i].IsBackground = true;
                        //Thread_MesNetSite[i].Start();

                        //ThreadPool.QueueUserWorkItem(new WaitCallback(MesNetSite_Sys[i].MesNetSiteRunning));

                        //(new TaskFactory()).StartNew(() =>
                        //{
                        //    MesNetSite_Sys[i].MesNetSiteRunning();
                        //});

                        //action[intThreadIndex] = ()=>MesNetSite_Sys[intThreadIndex].MesNetSiteRunning();
                        isThreadAlive.Add(true);
                        isTcpConnect.Add(true);
                        intThreadIndex++;
                    }
                }

                if (Check_Connection.CheckConnaction())
                {
                    //MPU.conn.Open();
                    //MPU.conn_old.Open();

                }

                bool_isAutoRun = true;

                //SetHistoryLog();

                SetDatagridview();

                bool_run = true;
                tmrUpdateDT.Enabled = true;
                tmrUpdateDT.Start();

                Parallel.For(0, dicDeviceList.Values.SelectMany(t => t).Count(), (i, state) =>
                {
                    MesNetSite_Sys[i].MesNetSiteRunning();
                    if (state.IsStopped)
                    {
                        return;
                    }
                });
                //ThreadPool.QueueUserWorkItem(new WaitCallback(SetCurrentLog));

                //Parallel.Invoke(action);
                Application.DoEvents();


                

                //new Thread(UpdateDTToSQL)
                //{
                //    IsBackground = true
                //}
                //.Start();
            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "[SetThread] " + ex.StackTrace);
                Console.WriteLine("[SetThread] " + ex.StackTrace);
            }
        }



        private void SetDatagridview()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(SetDatagridview), new object[] { });
            }
            else
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

                    //if (MPU.Ethernet == true)
                    //{
                    //    dataGridView_CurrLog.Columns[0].Width = 100;
                    //    dataGridView_CurrLog.Columns[1].Width = 70;
                    //    dataGridView_CurrLog.Columns[2].Width = 857;
                    //    dataGridView_CurrLog.Columns[3].Width = 223;

                    //    dataGridView_HistLog.Columns[0].Width = 100;
                    //    dataGridView_HistLog.Columns[1].Width = 70;
                    //    dataGridView_HistLog.Columns[2].Width = 857;
                    //    dataGridView_HistLog.Columns[5].Width = 223;
                    //    dataGridView_HistLog.Columns[3].Visible = false;
                    //    dataGridView_HistLog.Columns[4].Visible = false;
                    //    dataGridView_HistLog.Columns[6].Visible = false;
                    //}


                    foreach (DataGridViewColumn column in dgvMainView.Columns)
                    {
                        column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    }
                    foreach (DataGridViewColumn column in dataGridView_Result.Columns)
                    {
                        column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    }
                    foreach (DataGridViewColumn column in dataGridView_CurrLog.Columns)
                    {
                        column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    }
                    foreach (DataGridViewColumn column in dataGridView_HistLog.Columns)
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
        }

        private void SetComboboxValue()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(SetComboboxValue), new object[] { });
            }
            else
            {
                try
                {
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
        }

        private void SetHistoryLog()
        {
            try
            {
                if (MPU.Ethernet == true)
                {
                    StringBuilder sbSQL = new StringBuilder();

                    // 1.建立暫存表
                    sbSQL.AppendFormat(@"create table #SIDtable
                                        (
                                            SID char(100)
                                        )");
                    sbSQL.AppendLine();

                    // 2.將SID加入暫存表
                    sbSQL.AppendLine("insert into #SIDtable (SID) values ");
                    for (int j = 0; j < MPU.dt_MainTable.Rows.Count; j++)
                    {
                        if (j == (MPU.dt_MainTable.Rows.Count - 1))
                            sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["SID"]}');");
                        else
                            sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["SID"]}'),");
                    }
                    sbSQL.AppendLine();

                    // 3.將tb_connectlog與暫存表做inner join，篩選出需要的資料
                    sbSQL.AppendFormat(@"SELECT 
                                            DIP IP, 
                                            tb_connectlog.SID, 
                                            DVALUE Note, 
                                            FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss') as 重新連線時間, 
                                            FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間, 
                                            CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'), 
                                        FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as 狀態, 
                                            CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'), 
                                            FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as Sort 
                                        FROM tb_connectlog
                                        inner join #SIDtable on tb_connectlog.SID = #SIDtable.SID
                                        order by SID asc");
                    sbSQL.AppendLine();

                    // 4.移除剛才建立的暫存表
                    sbSQL.AppendLine("drop table #SIDtable;");

                    DataTable DataTable_HistLog = new DataTable();
                    DataTable_HistLog = null;
                    DataTable_HistLog = MPU.ReadSQLToDT(sbSQL.ToString());

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
                                        DataTable_HistLog.Rows[i]["IP"].ToString(),
                                        DataTable_HistLog.Rows[i]["SID"].ToString(),
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
                                        DataTable_HistLog.Rows[i]["IP"].ToString(),
                                        DataTable_HistLog.Rows[i]["SID"].ToString(),
                                        DataTable_HistLog.Rows[i]["Note"].ToString(),
                                        DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),"","重新連線時間 " +
                                        DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),
                                        DataTable_HistLog.Rows[i]["重新連線時間"].ToString()
                                    };
                                MPU.dt_HistoryLog.Rows.Add(String_RowData1);

                                string[] String_RowData2 =
                                {
                                        DataTable_HistLog.Rows[i]["IP"].ToString(),
                                        DataTable_HistLog.Rows[i]["SID"].ToString(),
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
                        dataGridView_HistLog.DataSource = dv.ToTable();





                        dataGridView_HistLog.Columns[0].Width = 100;
                        dataGridView_HistLog.Columns[1].Width = 70;
                        dataGridView_HistLog.Columns[2].Width = 857;
                        dataGridView_HistLog.Columns[5].Width = 223;
                        dataGridView_HistLog.Columns[3].Visible = false;
                        dataGridView_HistLog.Columns[4].Visible = false;
                        dataGridView_HistLog.Columns[6].Visible = false;
                    }
                    for (int i = 0; i < MPU.dt_HistoryLog.Rows.Count; i++)
                    {
                        if (MPU.dt_HistoryLog.Rows[i]["狀態"].ToString().Contains("斷線時間"))
                        {
                            dataGridView_HistLog.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                        }
                        else
                        {
                            dataGridView_HistLog.Rows[i].DefaultCellStyle.BackColor = Color.DarkSeaGreen;
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

        Boolean isFirstReConnect = true;
        DateTime DBdisTime = DateTime.Now;
        Boolean isDBreConnect = true;
        Boolean bool_currentlog = false;
        private static readonly object D_Lock = new object();
        private void SetCurrentLog()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(SetCurrentLog), new object[] { });
                }
                else
                {
                    while (bool_currentlog)
                    {
                        // 如果資料庫斷線，就每30秒重新連線一次
                        if (MPU.Ethernet == false)
                        {
                            if ((DateTime.Now - DBdisTime).Seconds >= 30)
                            {
                                isDBreConnect = true;
                            }

                            if (isDBreConnect == true)
                            {
                                DBdisTime = DateTime.Now;
                                isDBreConnect = false;
                                Check_Connection.CheckConnaction();
                            }
                        }
                        lock (D_Lock)
                        {
                            StringBuilder sbSQL = new StringBuilder();

                            // 1.建立暫存表
                            sbSQL.AppendFormat(@"create table #SIDtable
					                            (
						                            SID char(100)
					                            )");
                            sbSQL.AppendLine();

                            // 2.將SID加入暫存表
                            sbSQL.AppendLine("insert into #SIDtable (SID) values ");
                            for (int j = 0; j < MPU.dt_MainTable.Rows.Count; j++)
                            {
                                if (j == (MPU.dt_MainTable.Rows.Count - 1))
                                    sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["SID"]}');");
                                else
                                    sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["SID"]}'),");
                            }
                            sbSQL.AppendLine();

                            // 3.將tb_connectlog與暫存表做inner join，篩選出需要的資料
                            sbSQL.AppendFormat(@"select DIP IP, 
                                                    tb_connectlog.SID Sclass, 
                                                    DVALUE Note, 
                                                    format ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間 
                                                from tb_connectlog 
                                                inner join #SIDtable on tb_connectlog.SID = #SIDtable.SID
                                                where CONTIME is null 
                                                order by DISTIME desc");
                            sbSQL.AppendLine();

                            // 4.移除剛才建立的暫存表
                            sbSQL.AppendLine("drop table #SIDtable;");

                            if (MPU.Ethernet == true)
                            {
                                MPU.dt_CurrentLog = MPU.ReadSQLToDT(sbSQL.ToString());

                                if (isFirstReConnect == true)
                                {
                                    isFirstReConnect = false;
                                    dataGridView_CurrLog.DataSource = MPU.dt_CurrentLog;
                                    dataGridView_CurrLog.Columns[0].Width = 100;
                                    dataGridView_CurrLog.Columns[1].Width = 70;
                                    dataGridView_CurrLog.Columns[2].Width = 857;
                                    dataGridView_CurrLog.Columns[3].Width = 223;
                                }
                                //dgvLog[0].DataSource = MPU.dt_CurrentLog;

                                //dgvLog[0].Columns[0].Width = 100;
                                //dgvLog[0].Columns[1].Width = 60;
                                //dgvLog[0].Columns[2].Width = 859;
                                //dgvLog[0].Columns[3].Width = 224;
                            }
                            else
                            {
                                isFirstReConnect = true;
                            }

                            //Thread.Sleep(30000);
                            //Application.DoEvents();
                        }
                        Thread.Sleep(100);
                        Application.DoEvents();
                    }
                }
            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "[SetCurrentLog] " + ex.StackTrace);
                Console.WriteLine("[SetCurrentLog] " + ex.StackTrace);
            }
        }

        List<DataRow> list_dr = new List<DataRow>();
        bool bool_run;
        string string_dno = "";
        private void tmrUpdateDT_Tick(object sender, EventArgs e)
        {
            if (bool_run)
            {
                bool_run = false;
                try
                {
                    // 每天晚上12點，刪除超過3個月的connectlog紀錄
                    if (DateTime.Now.ToString("HH") == "00")
                    {
                        MPU.ReadSQL(string.Format(@"
                                delete tb_connectlog 
                                where DISTIME is not null and CONTIME is not null and CONTIME <= '{0}'",
                                    DateTime.Now.AddMonths(-3).ToString("yyyy-MM-dd HH:mm:ss")));
                    }

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
                                    if (MPU.dt_MainTable.Rows[item.Key]["Static"].ToString().Contains(MPU.str_ErrorMessage[0]) ||
                                        MPU.dt_MainTable.Rows[item.Key]["Static"].ToString().Contains(MPU.str_ErrorMessage[1]) ||
                                        MPU.dt_MainTable.Rows[item.Key]["Static"].ToString().Contains(MPU.str_ErrorMessage[2]))
                                    {
                                        dgvMainView.Rows[item.Key].DefaultCellStyle.BackColor = System.Drawing.Color.MistyRose;
                                    }
                                    else if (string.IsNullOrEmpty(MPU.dt_MainTable.Rows[item.Key]["Static"].ToString()))
                                        MPU.dt_MainTable.Rows[item.Key]["Static"] = DateTime.Now.ToString("HH:mm:ss") + " ... " + MPU.str_ErrorMessage[1];
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

                        // 若config.txt參數為true時，才將DNO寫入tb_dayno資料表，若為false則不寫入
                        if (canInsertDayno)
                        {
                            // 整點時紀錄最新DNO
                            if (DateTime.Now.ToString("mm") == "00" && DateTime.Now.ToString("yyyy-MM-dd HH:mm") != string_dno)
                            {
                                string_dno = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                                MPU.ReadSQL(@"
                                INSERT INTO tb_dayno(DNO,SYSTIME,TYPE) VALUES( (SELECT MAX(DNO) FROM tb_recordslog),GETDATE(),'RE');
                                INSERT INTO tb_dayno(DNO,SYSTIME,TYPE) VALUES( (SELECT MAX(DNO) FROM tb_P2recordslog),GETDATE(),'P2');
                                INSERT INTO tb_dayno(DNO,SYSTIME,TYPE) VALUES( (SELECT MAX(DNO) FROM tb_P3recordslog),GETDATE(),'P3');");
                            }
                        }
                    } 
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

        bool bool_isAutoRun;
        string[] disconnected_list = new string[] { };
        private void UpdateValue()
        {
            try
            {

                while (bool_isAutoRun)
                {
                    MPU.MPU_int_numericUpDown1 = (double)numericUpDown1.Value;

                    Console.WriteLine("[timer_Server_Tick]" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    

                    //Console.WriteLine(MesNetSite_Sys[0].String_ReData);
                    for (int i = 0; i <= MPU.dt_MainTable.Rows.Count - 1; i++)
                    {
                        if (MesNetSite_Sys[i].str_Barcode != "")
                        {
                            //MesNetSite_Sys[i + 1].str_BarcodeLight = MesNetSite_Sys[i].str_Barcode;

                            //MesNetSite_Sys[i].str_Barcode = "";
                            //dt_MainTable.Select ("DIP =" + dt_MainTable.Rows[i]["DIP"] + "AND port = 102")[0]
                        }




                        //textBoxRElog.Text += MesNetSite_Sys[i].String_ReData + "\r\n";
                        //針對SQL 指令 單獨做一個TRY/CATCH

                        try
                        {
                            if (MesNetSite_Sys[i].String_SQLcommand != "")
                            {
                                //MesNetSite_Sys[i].String_SQLcommand = "INSERT INTO [dbo].[tb_recordslog] ([DID]           ,[DIP]           ,[SID]           ,[DVALUE]           ,[SYSTIME])     VALUES ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_T + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "') ,  ('" + String_TID + "','" + String_DIP + "','" + String_SID + "','" + str_H + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
                                System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(MesNetSite_Sys[i].String_SQLcommand, MPU.conn);

                                cmd.ExecuteNonQuery();

                                MesNetSite_Sys[i].String_SQLcommand = "";

                            }

                            //執行每天抓DNO動作 
                            if (DateTime.Now.ToString("HH") == "00" && DateTime.Now.ToString("yyyy-MM-dd") != string_dno)
                            {

                                string_dno = DateTime.Now.ToString("yyyy-MM-dd");

                                System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("INSERT INTO tb_dayno(DNO,SYSTIME,TYPE) VALUES( (SELECT MAX(DNO) FROM tb_recordslog),GETDATE(),'RE');INSERT INTO tb_dayno(DNO,SYSTIME,TYPE) VALUES( (SELECT MAX(DNO) FROM tb_P2recordslog),GETDATE(),'P2');INSERT INTO tb_dayno(DNO,SYSTIME,TYPE) VALUES( (SELECT MAX(DNO) FROM tb_P3recordslog),GETDATE(),'P3');", MPU.conn);

                                cmd.ExecuteNonQuery();

                                MesNetSite_Sys[i].String_SQLcommand = "";
                            }

                        }
                        catch (Exception ex)
                        {

                            if (ex.Source != null)
                            {

                                Console.WriteLine("F0649:Exception source: {0}", "[" + ex.Message + "]" + MesNetSite_Sys[i].String_SQLcommand);

                                MesNetSite_Sys[i].String_SQLcommand = "";

                            }

                        }
                    }
                    dgvMainView.DataSource = MPU.dt_MainTable;
                    Thread.Sleep(1000);
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {

                MPU.WriteErrorCode("", "UpdateDTToSQL" + ex.Message);
            }
        }

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
                if (MPU.Ethernet == true)
                {
                    start = dtpStart.Value;
                    end = dtpEnd.Value;
                    //end = end.AddDays(1);

                    StringBuilder sbSQL = new StringBuilder();

                    // 1.建立暫存表
                    sbSQL.AppendFormat(@"create table #SIDtable
					                    (
						                    SID char(100)
					                    )");
                    sbSQL.AppendLine();

                    // 2.將SID加入暫存表
                    sbSQL.AppendLine("insert into #SIDtable (SID) values ");
                    for (int j = 0; j < MPU.dt_MainTable.Rows.Count; j++)
                    {
	                    if (j == (MPU.dt_MainTable.Rows.Count - 1))
		                    sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["SID"]}');");
	                    else
		                    sbSQL.AppendLine($"('{MPU.dt_MainTable.Rows[j]["SID"]}'),");
                    }
                    sbSQL.AppendLine();

                    // 3.將tb_connectlog與暫存表做inner join，篩選出需要的資料
                    sbSQL.AppendFormat(@"SELECT 
                                            DIP IP, 
                                            tb_connectlog.SID, 
                                            DVALUE Note, 
                                            FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss') as 重新連線時間, 
                                            FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間, 
                                            CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'),FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as 狀態, 
                                            CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'),FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as Sort 
                                        FROM tb_connectlog 
                                        inner join #SIDtable on tb_connectlog.SID = #SIDtable.SID
                                        WHERE 
                                            ((CONTIME BETWEEN '{0}' AND '{1}') OR (DISTIME BETWEEN '{2}' AND 
                                            '{3}')) 
                                        ORDER BY Sort DESC",
                                        start.ToString("yyyy-MM-dd HH:mm:ss"), end.ToString("yyyy-MM-dd HH:mm:ss"), start.ToString("yyyy-MM-dd HH:mm:ss"), end.ToString("yyyy-MM-dd HH:mm:ss"));
                    sbSQL.AppendLine();

                    // 4.移除剛才建立的暫存表
                    sbSQL.AppendLine("drop table #SIDtable;");

                    DataTable DataTable_HistLog = new DataTable();
                    DataTable_HistLog = null;
                    DataTable_HistLog = MPU.ReadSQLToDT(sbSQL.ToString());
                    
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
                                    DataTable_HistLog.Rows[i]["IP"].ToString(),
                                    DataTable_HistLog.Rows[i]["SID"].ToString(),
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
                                    DataTable_HistLog.Rows[i]["IP"].ToString(),
                                    DataTable_HistLog.Rows[i]["SID"].ToString(),
                                    DataTable_HistLog.Rows[i]["Note"].ToString(),
                                    DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),"","重新連線時間 " +
                                    DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),
                                    DataTable_HistLog.Rows[i]["重新連線時間"].ToString()
                                };
                                MPU.dt_HistoryLog.Rows.Add(String_RowData1);

                                string[] String_RowData2 =
                                {
                                    DataTable_HistLog.Rows[i]["IP"].ToString(),
                                    DataTable_HistLog.Rows[i]["SID"].ToString(),
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
                        dataGridView_HistLog.DataSource = dv.ToTable();

                        for (int i = 0; i < MPU.dt_HistoryLog.Rows.Count; i++)
                        {
                            if (string.IsNullOrEmpty(MPU.dt_HistoryLog.Rows[i]["重新連線時間"].ToString()))
                            {
                                dataGridView_HistLog.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                            }
                            else
                            {
                                dataGridView_HistLog.Rows[i].DefaultCellStyle.BackColor = Color.DarkSeaGreen;
                            }
                        }

                        dataGridView_HistLog.Columns[0].Width = 100;
                        dataGridView_HistLog.Columns[1].Width = 60;
                        dataGridView_HistLog.Columns[2].Width = 860;
                        //dgvLog[1].Columns[3].Width = 150;
                        //dgvLog[1].Columns[4].Width = 150;
                        dataGridView_HistLog.Columns[5].Width = 224;
                        dataGridView_HistLog.Columns[3].Visible = false;
                        dataGridView_HistLog.Columns[4].Visible = false;
                        dataGridView_HistLog.Columns[6].Visible = false;
                    }
                    for (int i = 0; i < MPU.dt_HistoryLog.Rows.Count; i++)
                    {
                        if (MPU.dt_HistoryLog.Rows[i]["狀態"].ToString().Contains("斷線時間"))
                        {
                            dataGridView_HistLog.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                        }
                        else
                        {
                            dataGridView_HistLog.Rows[i].DefaultCellStyle.BackColor = Color.DarkSeaGreen;
                        }
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
            Thread th = new Thread(SetCurrentLog);
            if (tabControl1.SelectedIndex == 2) //即時警報
            {
                bool_currentlog = true;
                th.Start();
            }
            else if (tabControl1.SelectedIndex == 3) //歷史警報
            {
                bool_currentlog = false;
                if (th != null)
                {
                    if (!th.IsAlive)
                    {
                        th.Abort();
                        th = null;
                    }
                }

                SetHistoryLog();
            }
            else
            {
                bool_currentlog = false;
                if (th != null)
                {
                    if (!th.IsAlive)
                    {
                        th.Abort();
                        th = null;
                    }
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (MessageBox.Show("程式即將關閉，是否繼續？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    tmrUpdateDT.Stop();
                    tmrUpdateDT.Enabled = false;
                    bool_isAutoRun = false;

                    while (isTcpConnect.IndexOf(true) != -1)
                    {
                        for (int i = 0; i < Datatable_showtxt.Rows.Count; i++)
                        {
                            if (intDiveceIndex.Contains(i))
                            {
                                int n = intDiveceIndex.IndexOf(i);
                                MesNetSite_Sys[n].bool_AutoRun = false;
                                MesNetSite_Sys[n].isReconnecting = true;
                                MesNetSite_Sys[n].isReconnecting = false;
                                if ((MesNetSite_Sys[n].TcpClient_Reader != null) && MesNetSite_Sys[n].TcpClient_Reader.Connected)
                                {
                                    MesNetSite_Sys[n].TcpClient_Reader.Close();
                                }
                                else if ((MesNetSite_Sys[n].TcpClient_Reader != null) && !MesNetSite_Sys[n].TcpClient_Reader.Connected)
                                {
                                    MesNetSite_Sys[n].TcpClient_Reader = null;
                                    isTcpConnect[n] = false;
                                }
                                else
                                {
                                    isTcpConnect[n] = false;
                                }
                            }
                        }
                        Thread.Sleep(100);
                    }

                    Thread.Sleep(100);

                    while (isThreadAlive.IndexOf(true) != -1)
                    {
                        for (int i = 0; i < intThreadIndex; i++)
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
                        Thread.Sleep(1000);
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
