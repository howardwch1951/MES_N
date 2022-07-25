using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MES_N
{
    public partial class Form1 : Form
    {
        public static Form1 form1;
        public Form1()
        {
            InitializeComponent();
            form1 = this;
        }

        public List<DataGridView> Datagridview_Log = new List<DataGridView>();
        private delegate void InvokeDelegate();
        private void Form1_Load(object sender, EventArgs e)
        {   
            dateTimePicker_Start.CustomFormat = "yyyy 年 MM 月 dd 日　HH:mm:ss";
            dateTimePicker_Start.Format = DateTimePickerFormat.Custom;
            dateTimePicker_End.CustomFormat = "yyyy 年 MM 月 dd 日　HH:mm:ss";
            dateTimePicker_End.Format = DateTimePickerFormat.Custom;

            Datagridview_Log.Add(this.Controls.Find("dataGridView_Threads", true)[0] as DataGridView);
            Datagridview_Log.Add(this.Controls.Find("dataGridView_CurrLog", true)[0] as DataGridView);
            Datagridview_Log.Add(this.Controls.Find("dataGridView_HistLog", true)[0] as DataGridView);

            bool_isAutoRun = false;

            SetDatatable();

            SetThreads();

            //new Thread(SetThreads)
            //{
            //    IsBackground = true
            //}
            //.Start();
        }

        System.Data.DataTable Datatable_showtxt;

        public void SetDatatable()
        {

            Console.WriteLine("[SetDatatable]");

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

                Datatable_showtxt.DefaultView.Sort = "DIP asc";
                Datatable_showtxt = Datatable_showtxt.DefaultView.ToTable(true);

                StreamReader_Txt.Close();

                //從資料庫抓取資料版

            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "SetDatatable" + ex.Message);
                if (ex.Source != null)

                    Console.WriteLine("F0475:Exception source: {0}", ex.Source);

            }

        }               

        MesNetSite[] MesNetSite_Sys;  
        System.Threading.Thread[] Thread_MesNetSite;
        List<string> txt = new List<string>();
        List<string> address_index = new List<string>();
        int[,] index;
        private void SetThreads()
        {
            Console.WriteLine("[SetThreads]");
            try
            {
                String[] Arry_Str_Set = { "" };

                //String StrDataColumnsName = "DID,DIP,SID,port,portid,text,note";
                String StrDataColumnsName = "TID,Dline,DIP,Port,Address,Portid,Sclass,SID,NOTE,Static";

                MPU.SetDataColumn(ref Arry_Str_Set, StrDataColumnsName);

                MPU.SetDataTable("", ref MPU.DataTable_Threads, Arry_Str_Set);

                
                for (int i = 0; i <= Datatable_showtxt.Rows.Count - 1; i++)
                {
                    String[] String_RowData = { Datatable_showtxt.Rows[i]["DID"].ToString(),
                    Datatable_showtxt.Rows[i]["Dline"].ToString(),
                    Datatable_showtxt.Rows[i]["DIP"].ToString(),
                    Datatable_showtxt.Rows[i]["Port"].ToString(),
                    Datatable_showtxt.Rows[i]["Address"].ToString(),
                    Datatable_showtxt.Rows[i]["Portid"].ToString() ,
                    Datatable_showtxt.Rows[i]["Sclass"].ToString() ,
                    Datatable_showtxt.Rows[i]["SID"].ToString().Replace('.',' ') ,
                    Datatable_showtxt.Rows[i]["NOTE"].ToString() +  "_" +
                    Datatable_showtxt.Rows[i]["text"].ToString() , "連線中...",
                    };

                    MPU.DataTable_Threads.Rows.Add(String_RowData);
                }
                dataGridView_Threads.DataSource = MPU.DataTable_Threads;

                string concat_str = "";
                for (int j = 0; j < MPU.DataTable_Threads.Rows.Count; j++)
                {
                    concat_str += "'" + MPU.DataTable_Threads.Rows[j]["DIP"].ToString() + "," + MPU.DataTable_Threads.Rows[j]["ADDRESS"].ToString() + "," + MPU.DataTable_Threads.Rows[j]["NOTE"].ToString() + "'" + ",";
                }
                concat_str = concat_str.Trim(',');
                MPU.DataTable_CurrLog = MPU.ReadSQLToDT(string.Format("SELECT DIP IP, ADDRESS 站號, DVALUE Note, FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間 FROM tb_connectlog WHERE CONTIME IS NULL AND CONCAT(TRIM(DIP),',',ADDRESS,',',TRIM(DVALUE)) IN ({0}) ORDER BY DISTIME DESC", concat_str));
                if (MPU.Ethernet == true)
                    dataGridView_CurrLog.DataSource = MPU.DataTable_CurrLog;

                concat_str = "";
                for (int i = 0; i < MPU.DataTable_Threads.Rows.Count; i++)
                {
                    concat_str += "'" + MPU.DataTable_Threads.Rows[i]["DIP"].ToString() + "," + MPU.DataTable_Threads.Rows[i]["ADDRESS"].ToString() + "," + MPU.DataTable_Threads.Rows[i]["NOTE"].ToString() + "'" + ",";
                }

                concat_str = concat_str.Trim(',');
                MPU.DataTable_HistLog = MPU.ReadSQLToDT(string.Format("SELECT TOP(50) DIP IP, ADDRESS 站號, DVALUE Note, FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss') as 重新連線時間, FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間, CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'),FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as 狀態, CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'),FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as Sort FROM tb_connectlog WHERE CONCAT(TRIM(DIP),',',ADDRESS,',',TRIM(DVALUE)) IN ({0}) ORDER BY Sort DESC", concat_str));
                if (MPU.Ethernet == true)
                    dataGridView_HistLog.DataSource = MPU.DataTable_HistLog;

                SetDatagridview();

                foreach (DataRow row in Datatable_showtxt.Rows)
                {
                    if (!txt.Contains(row["DIP"].ToString() + row["Port"].ToString())) // 判斷有沒有重覆。
                    {
                        txt.Add(row["DIP"].ToString() + "," + row["Port"].ToString());
                    }
                }
                int count = 1;
                int row_index = 0;
                index = new int[txt.Count, 1];
                for (int i = 0; i < Datatable_showtxt.Rows.Count; i++)
                {
                    if (i != Datatable_showtxt.Rows.Count - 1)
                    {
                        if (Datatable_showtxt.Rows[i]["DIP"].ToString() + Datatable_showtxt.Rows[i]["Port"].ToString() == Datatable_showtxt.Rows[i + 1]["DIP"].ToString() + Datatable_showtxt.Rows[i + 1]["Port"].ToString())
                        {
                            count++;
                            index[row_index, 0] = count;
                        }
                        else
                        {
                            if (i != 0)
                            {
                                count = 1;
                                row_index++;
                                index[row_index, 0] = count;
                            }
                            else
                            {
                                count = 1;
                                index[row_index, 0] = count;
                            }
                        }
                    }
                    else
                    {
                        if (Datatable_showtxt.Rows.Count == 1)
                        {
                            count = 1;
                            index[row_index, 0] = count;
                        }
                        else
                        {
                            count = 1;
                            row_index++;
                            index[row_index, 0] = count;
                        }
                    }
                }
                for (int i = 0; i < txt.Count; i++)
                {
                    for (int j = 0; j < index[i, 0]; j++)
                    {
                        address_index.Add((j + 1).ToString() + "," + index[i, 0].ToString());
                    }                    
                }
                new Thread(SetDatagridviewValue).Start();

               


            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "SetThreads : " + ex.Message);
                if (ex.Source != null)

                    Console.WriteLine("F0573:Exception source: {0}", ex.Source);
            }
        }

        int thread_count = 0;
        private void SetDatagridviewValue()
        {
            try
            {
                //實做n個MES傳輸類別
                MesNetSite_Sys = new MesNetSite[Datatable_showtxt.Rows.Count];

                Thread_MesNetSite = new System.Threading.Thread[Datatable_showtxt.Rows.Count];

                for (int i = 0; i < Datatable_showtxt.Rows.Count; i++)
                {
                    MesNetSite_Sys[i] = new MesNetSite();

                    MesNetSite_Sys[i].String_TID = MPU.DataTable_Threads.Rows[i]["TID"].ToString();

                    MesNetSite_Sys[i].String_Dline = MPU.DataTable_Threads.Rows[i]["Dline"].ToString();

                    MesNetSite_Sys[i].String_DIP = MPU.DataTable_Threads.Rows[i]["DIP"].ToString();

                    MesNetSite_Sys[i].String_SID = MPU.DataTable_Threads.Rows[i]["SID"].ToString();

                    MesNetSite_Sys[i].String_Port = MPU.DataTable_Threads.Rows[i]["Port"].ToString();

                    MesNetSite_Sys[i].String_Address = MPU.DataTable_Threads.Rows[i]["Address"].ToString();

                    MesNetSite_Sys[i].Str_Portid = MPU.DataTable_Threads.Rows[i]["Portid"].ToString();

                    MesNetSite_Sys[i].String_Sclass = MPU.DataTable_Threads.Rows[i]["Sclass"].ToString();

                    MesNetSite_Sys[i].String_NOTE = MPU.DataTable_Threads.Rows[i]["NOTE"].ToString();

                    MesNetSite_Sys[i].address_index = address_index[i];

                    Array.Resize(ref MesNetSite_Sys[i].String_ReData, 1);
                    MesNetSite_Sys[i].String_ReData[0] = MPU.static_msg[3];

                    MesNetSite_Sys[i].int_ThreadNum = i;

                    MesNetSite_Sys[i].int_timeOutMsec = 0;

                    MesNetSite_Sys[i].int_ReaderSleep = 200;

                    MesNetSite_Sys[i].int_ReaderSleepSET = 200;

                    MesNetSite_Sys[i].bool_AutoRun = true;

                    MesNetSite_Sys[i].TcpClientConnect();


                    //'宣告一個執行緒來處理 reader讀取 電子標籤的動作。
                    Thread_MesNetSite[i] = new System.Threading.Thread(MesNetSite_Sys[i].MesNetSiteRunning);
                    Thread_MesNetSite[i].IsBackground = true;
                    Thread_MesNetSite[i].Start();
                    //ThreadPool.QueueUserWorkItem(new WaitCallback(MesNetSite_Sys[i].MesNetSiteRunning));
                    MesNetSite_Sys[i].bool_isThreadSet = true;
                    thread_count++;
                }
                Application.DoEvents();

                bool_isAutoRun = true;

                if(Check_Connection.CheckConnaction())
                    MPU.conn.Open();

                SetComboboxValue();

                SetHistoryLog();

                SetDatagridview();

                //ThreadPool.QueueUserWorkItem(new WaitCallback(CurrLog_Timer));

                new Thread(CurrLog_Timer)
                {
                    IsBackground = true
                }
                .Start();

                new Thread(UpdateValue)
                {
                    IsBackground = true
                }
                .Start();
            }
            catch (Exception ex)
            {
                MPU.WriteErrorCode("", "SetDatagridviewValue" + ex.Message);
                throw ex;
            }
        }

        private void SetDatagridview()
        {
            try
            {
                dataGridView_Threads.Columns[0].Width = 70;
                dataGridView_Threads.Columns[1].Width = 60;
                dataGridView_Threads.Columns[2].Width = 90;
                dataGridView_Threads.Columns[3].Width = 60;
                dataGridView_Threads.Columns[4].Width = 60;
                dataGridView_Threads.Columns[5].Width = 100;
                dataGridView_Threads.Columns[6].Width = 60;
                dataGridView_Threads.Columns[7].Width = 100;
                dataGridView_Threads.Columns[8].Width = 452;
                dataGridView_Threads.Columns[9].Width = 190;

                if (MPU.Ethernet == true)
                {
                    dataGridView_CurrLog.Columns[0].Width = 100;
                    dataGridView_CurrLog.Columns[1].Width = 60;
                    dataGridView_CurrLog.Columns[2].Width = 859;
                    dataGridView_CurrLog.Columns[3].Width = 223;

                    dataGridView_HistLog.Columns[0].Width = 100;
                    dataGridView_HistLog.Columns[1].Width = 60;
                    dataGridView_HistLog.Columns[2].Width = 860;
                    dataGridView_HistLog.Columns[5].Width = 223;
                    dataGridView_HistLog.Columns[3].Visible = false;
                    dataGridView_HistLog.Columns[4].Visible = false;
                    dataGridView_HistLog.Columns[6].Visible = false;
                }


                foreach (DataGridViewColumn column in dataGridView_Threads.Columns)
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

                throw ex;
            }


        }

        private void SetComboboxValue()
        {
            try
            {
                List<string> Dline_list = new List<string>();
                Dline_cmb.Items.Add("");
                foreach (DataRow row in MPU.DataTable_Threads.Rows)
                {
                    if (!Dline_list.Contains(row["Dline"].ToString())) // 判斷有沒有重覆。
                    {
                        Dline_list.Add(row["Dline"].ToString());
                        Dline_cmb.Items.Add(row["Dline"].ToString());
                    }
                }

                List<string> Sclass_list = new List<string>();
                Sclass_cmb.Items.Add("");
                foreach (DataRow row in MPU.DataTable_Threads.Rows)
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

                throw ex;
            }
        }

        private void SetHistoryLog()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(SetHistoryLog), new object[] { });
            }
            else
            {
                try
                {
                    if (MPU.Ethernet == true)
                    {
                        string concat_str = "";
                        for (int i = 0; i < MPU.DataTable_Threads.Rows.Count; i++)
                        {
                            concat_str += "'" + MPU.DataTable_Threads.Rows[i]["DIP"].ToString() + "," + MPU.DataTable_Threads.Rows[i]["ADDRESS"].ToString() + "," + MPU.DataTable_Threads.Rows[i]["NOTE"].ToString() + "'" + ",";
                        }

                        concat_str = concat_str.Trim(',');
                        DataTable DataTable_HistLog = new DataTable();
                        DataTable_HistLog = null;
                        DataTable_HistLog = MPU.ReadSQLToDT(string.Format("SELECT TOP(50) DIP IP, ADDRESS 站號, DVALUE Note, FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss') as 重新連線時間, FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間, CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'),FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as 狀態, CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'),FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as Sort FROM tb_connectlog WHERE CONCAT(TRIM(DIP),',',ADDRESS,',',TRIM(DVALUE)) IN ({0}) ORDER BY Sort DESC", concat_str));
                        if (MPU.Ethernet == true)
                        {

                            MPU.DataTable_HistLog = DataTable_HistLog.Clone();
                            for (int i = 0; i < DataTable_HistLog.Rows.Count; i++)
                            {
                                if (string.IsNullOrEmpty(DataTable_HistLog.Rows[i]["重新連線時間"].ToString()) || string.IsNullOrEmpty(DataTable_HistLog.Rows[i]["斷線時間"].ToString()))
                                {
                                    string[] String_RowData =
                                    {
                                DataTable_HistLog.Rows[i]["IP"].ToString(),
                                DataTable_HistLog.Rows[i]["站號"].ToString(),
                                DataTable_HistLog.Rows[i]["Note"].ToString(),
                                DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString(), "斷線時間 " +
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString(),
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString()
                            };
                                    MPU.DataTable_HistLog.Rows.Add(String_RowData);
                                }
                                else
                                {
                                    string[] String_RowData1 =
                                    {
                                DataTable_HistLog.Rows[i]["IP"].ToString(),
                                DataTable_HistLog.Rows[i]["站號"].ToString(),
                                DataTable_HistLog.Rows[i]["Note"].ToString(),
                                DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),"","重新連線時間 " +
                                DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),
                                DataTable_HistLog.Rows[i]["重新連線時間"].ToString()
                            };
                                    MPU.DataTable_HistLog.Rows.Add(String_RowData1);

                                    string[] String_RowData2 =
                                    {
                                DataTable_HistLog.Rows[i]["IP"].ToString(),
                                DataTable_HistLog.Rows[i]["站號"].ToString(),
                                DataTable_HistLog.Rows[i]["Note"].ToString(),"",
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString(),"斷線時間 " +
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString(),
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString()
                            };
                                    MPU.DataTable_HistLog.Rows.Add(String_RowData2);
                                }
                            }

                            DataView dv = MPU.DataTable_HistLog.DefaultView;
                            dv.Sort = "Sort desc";
                            MPU.DataTable_HistLog = dv.ToTable();
                            Datagridview_Log[1].DataSource = dv.ToTable();



                            //Datagridview_Log[1].Columns[0].Width = 100;
                            //Datagridview_Log[1].Columns[1].Width = 60;
                            //Datagridview_Log[1].Columns[2].Width = 860;
                            ////Datagridview_Log[1].Columns[3].Width = 150;
                            ////Datagridview_Log[1].Columns[4].Width = 150;
                            //Datagridview_Log[1].Columns[5].Width = 224;
                            //Datagridview_Log[1].Columns[3].Visible = false;
                            //Datagridview_Log[1].Columns[4].Visible = false;
                            //Datagridview_Log[1].Columns[6].Visible = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        Boolean canUpdateCurrLog = true;
        private static readonly object D_Lock = new object();

        private void CurrLog_Timer()
        {

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new InvokeDelegate(CurrLog_Timer));
            }
            else
            {
                Counter.Enabled = true;                
                while (bool_isAutoRun)
                {
                    lock (D_Lock)
                    {
                        if (canUpdateCurrLog)
                        {
                            canUpdateCurrLog = false;
                            Counter.Enabled = false;
                            string concat_str = "";
                            for (int j = 0; j < MPU.DataTable_Threads.Rows.Count; j++)
                            {
                                concat_str += "'" + MPU.DataTable_Threads.Rows[j]["DIP"].ToString() + "," + MPU.DataTable_Threads.Rows[j]["ADDRESS"].ToString() + "," + MPU.DataTable_Threads.Rows[j]["NOTE"].ToString() + "'" + ",";
                            }
                            concat_str = concat_str.Trim(',');
                            //MPU.DataTable_CurrLog = null;

                            if (MPU.Ethernet == true)
                            {
                                MPU.DataTable_CurrLog = MPU.ReadSQLToDT(string.Format("SELECT DIP IP, ADDRESS 站號, DVALUE Note, FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間 FROM tb_connectlog WHERE CONTIME IS NULL AND CONCAT(TRIM(DIP),',',ADDRESS,',',TRIM(DVALUE)) IN ({0}) ORDER BY DISTIME DESC", concat_str));

                                //Datagridview_Log[0].DataSource = MPU.DataTable_CurrLog;

                                //Datagridview_Log[0].Columns[0].Width = 100;
                                //Datagridview_Log[0].Columns[1].Width = 60;
                                //Datagridview_Log[0].Columns[2].Width = 859;
                                //Datagridview_Log[0].Columns[3].Width = 224;
                            }
                            Counter.Enabled = true;

                            //Thread.Sleep(30000);
                            //Application.DoEvents();
                        }
                        Thread.Sleep(500);
                        Application.DoEvents();
                    }
                }
            }            
        }

        int count = 0;
        Boolean FirstEnter = true;
        private void Counter_Tick(object sender, EventArgs e)
        {
            if (FirstEnter)
            {
                canUpdateCurrLog = true;
                FirstEnter = false;
            }
            else
            {
                if (count >= 30)
                {
                    count = 0;
                    canUpdateCurrLog = true;
                    Console.WriteLine("Update Time : " + DateTime.Now);
                }
                else
                {
                    count++;
                    canUpdateCurrLog = false;
                }
            }
        }

        bool bool_isAutoRun;
        string string_dno = "";
        string[] disconnected_list = new string[] { };
        private void UpdateValue()
        {
            try
            {

                while (bool_isAutoRun)
                {
                    MPU.MPU_int_numericUpDown1 = (double)numericUpDown1.Value;

                    Console.WriteLine("[timer_Server_Tick]" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    for (int i = 0; i < MPU.DataTable_HistLog.Rows.Count; i++)
                    {
                        if (MPU.DataTable_HistLog.Rows[i]["狀態"].ToString().Contains("斷線時間"))
                        {
                            dataGridView_HistLog.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                        }
                        else
                        {
                            dataGridView_HistLog.Rows[i].DefaultCellStyle.BackColor = Color.DarkSeaGreen;
                        }
                    }

                    //Console.WriteLine(MesNetSite_Sys[0].String_ReData);
                    for (int i = 0; i <= MPU.DataTable_Threads.Rows.Count - 1; i++)
                    {
                        if (MesNetSite_Sys[i].str_Barcode != "")
                        {
                            //MesNetSite_Sys[i + 1].str_BarcodeLight = MesNetSite_Sys[i].str_Barcode;

                            //MesNetSite_Sys[i].str_Barcode = "";
                            //DataTable_Threads.Select ("DIP =" + DataTable_Threads.Rows[i]["DIP"] + "AND port = 102")[0]
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
                    Thread.Sleep(1000);
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {

                MPU.WriteErrorCode("", "UpdateValue" + ex.Message);
            }
        }

        public void ChangeColor(int i)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int>(ChangeColor), new object[] { i });
            }
            else
            {
                try
                {
                    if (MesNetSite_Sys[i].bool_isThreadSet)
                    {
                        if (MPU.DataTable_Threads.Rows[i]["Static"].ToString().Contains(MPU.static_msg[0]) || MPU.DataTable_Threads.Rows[i]["Static"].ToString().Contains(MPU.static_msg[1]))
                        {
                            dataGridView_Threads.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                        }
                        else
                        {
                            dataGridView_Threads.Rows[i].DefaultCellStyle.BackColor = Color.White;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MPU.WriteErrorCode("", "ChangeColor " + ex.Message);
                    throw ex;
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //CurrentLog_txt.ScrollBars = ScrollBars.Vertical;
            //CurrentLog_txt.SelectionStart = CurrentLog_txt.Text.Length;
            //CurrentLog_txt.ScrollToCaret();
            //if (CurrentLog_txt.Lines.Length > 500)
            //{
            //    CurrentLog_txt.Clear();
            //}
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            textBox2.ScrollBars = ScrollBars.Vertical;
            textBox2.SelectionStart = textBox2.Text.Length;
            textBox2.ScrollToCaret();
            if (textBox2.Lines.Length > 500)
            {
                textBox2.Clear();
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
        }

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

                    ResultRow = MPU.DataTable_Threads.Select(conn_str);
                    ResultTable = MPU.DataTable_Threads.Clone();
                    foreach (var Rows in ResultRow)
                    {
                        ResultTable.ImportRow(Rows);
                    }
                    dataGridView_Result.DataSource = ResultTable;

                    for (int i = 0; i < ResultTable.Rows.Count; i++)
                    {
                        if (ResultTable.Rows[i]["Static"].ToString().Contains(MPU.static_msg[0]) || ResultTable.Rows[i]["Static"].ToString().Contains(MPU.static_msg[1]))
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

        DateTime start = new DateTime();
        DateTime end = new DateTime();
        private void HistLogSearch_btn_Click(object sender, EventArgs e)
        {
            try
            {
                if (MPU.Ethernet == true)
                {
                    start = dateTimePicker_Start.Value;
                    end = dateTimePicker_End.Value;
                    //end = end.AddDays(1);

                    string concat_str = "";
                    for (int i = 0; i < MPU.DataTable_Threads.Rows.Count; i++)
                    {
                        concat_str += "'" + MPU.DataTable_Threads.Rows[i]["DIP"].ToString() + "   " + MPU.DataTable_Threads.Rows[i]["ADDRESS"].ToString() + MPU.DataTable_Threads.Rows[i]["NOTE"].ToString() + "  '" + ",";
                    }

                    concat_str = concat_str.Trim(',');
                    DataTable DataTable_HistLog = new DataTable();
                    DataTable_HistLog = null;
                    DataTable_HistLog = MPU.ReadSQLToDT(string.Format("SELECT DIP IP, ADDRESS 站號, DVALUE Note, FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss') as 重新連線時間, FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss') as 斷線時間, CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'),FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as 狀態, CONCAT(FORMAT ([CONTIME], 'yyyy-MM-dd　HH:mm:ss'),FORMAT ([DISTIME], 'yyyy-MM-dd　HH:mm:ss')) as Sort FROM tb_connectlog WHERE ((CONTIME BETWEEN '{1}' AND '{2}') OR (DISTIME BETWEEN '{3}' AND '{4}')) AND CONCAT(TRIM(DIP),',',ADDRESS,',',TRIM(DVALUE)) IN ({0}) ORDER BY Sort DESC", concat_str, start.ToString("yyyy-MM-dd HH:mm:ss"), end.ToString("yyyy-MM-dd HH:mm:ss"), start.ToString("yyyy-MM-dd HH:mm:ss"), end.ToString("yyyy-MM-dd HH:mm:ss")));
                    if (MPU.Ethernet == true)
                    {
                        MPU.DataTable_HistLog = DataTable_HistLog.Clone();
                        for (int i = 0; i < DataTable_HistLog.Rows.Count; i++)
                        {
                            if (string.IsNullOrEmpty(DataTable_HistLog.Rows[i]["重新連線時間"].ToString()) || string.IsNullOrEmpty(DataTable_HistLog.Rows[i]["斷線時間"].ToString()))
                            {
                                string[] String_RowData =
                                {
                                DataTable_HistLog.Rows[i]["IP"].ToString(),
                                DataTable_HistLog.Rows[i]["站號"].ToString(),
                                DataTable_HistLog.Rows[i]["Note"].ToString(),
                                DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString(),"斷線時間 " +
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString(),
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString()
                            };
                                MPU.DataTable_HistLog.Rows.Add(String_RowData);
                            }
                            else
                            {
                                string[] String_RowData1 =
                                {
                                DataTable_HistLog.Rows[i]["IP"].ToString(),
                                DataTable_HistLog.Rows[i]["站號"].ToString(),
                                DataTable_HistLog.Rows[i]["Note"].ToString(),
                                DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),"","重新連線時間 " +
                                DataTable_HistLog.Rows[i]["重新連線時間"].ToString(),
                                DataTable_HistLog.Rows[i]["重新連線時間"].ToString()
                            };
                                MPU.DataTable_HistLog.Rows.Add(String_RowData1);

                                string[] String_RowData2 =
                                {
                                DataTable_HistLog.Rows[i]["IP"].ToString(),
                                DataTable_HistLog.Rows[i]["站號"].ToString(),
                                DataTable_HistLog.Rows[i]["Note"].ToString(),"",
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString(),"斷線時間 " +
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString(),
                                DataTable_HistLog.Rows[i]["斷線時間"].ToString()
                            };
                                MPU.DataTable_HistLog.Rows.Add(String_RowData2);
                            }
                        }

                        DataView dv = MPU.DataTable_HistLog.DefaultView;
                        dv.Sort = "Sort desc";
                        MPU.DataTable_HistLog = dv.ToTable();
                        Datagridview_Log[1].DataSource = dv.ToTable();

                        for (int i = 0; i < MPU.DataTable_HistLog.Rows.Count; i++)
                        {
                            if (string.IsNullOrEmpty(MPU.DataTable_HistLog.Rows[i]["重新連線時間"].ToString()))
                            {
                                dataGridView_HistLog.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                            }
                            else
                            {
                                dataGridView_HistLog.Rows[i].DefaultCellStyle.BackColor = Color.DarkSeaGreen;
                            }
                        }

                        Datagridview_Log[1].Columns[0].Width = 100;
                        Datagridview_Log[1].Columns[1].Width = 60;
                        Datagridview_Log[1].Columns[2].Width = 860;
                        //Datagridview_Log[1].Columns[3].Width = 150;
                        //Datagridview_Log[1].Columns[4].Width = 150;
                        Datagridview_Log[1].Columns[5].Width = 224;
                        Datagridview_Log[1].Columns[3].Visible = false;
                        Datagridview_Log[1].Columns[4].Visible = false;
                        Datagridview_Log[1].Columns[6].Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 3)
            {
                SetHistoryLog();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < thread_count; i++)
            {
                if (Thread_MesNetSite[i].IsAlive)
                {
                    Thread_MesNetSite[i].Abort();
                }
            }

            e.Cancel = true;

            bool_isAutoRun = false;
            MPU.conn.Close();

            this.Close();
        }
    }
}
