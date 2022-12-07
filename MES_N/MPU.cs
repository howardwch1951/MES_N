using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace MES_N
{

    //共用的函數群
    public static class MPU
    {

        public static Boolean TOSrun = false;

        public static double MPU_int_numericUpDown1 = 0.045;

        public static Boolean wme1 = false;

        public static Boolean wme2 = false;

        public static Boolean wme3 = false;

        public static Boolean isStatusChange = false;

        public static bool canInsertDayno, canInsertToDB;

        public static BindingSource bs_MainTable = new BindingSource();

        public static BindingSource bs_CurrentLog = new BindingSource();

        public static BindingSource bs_HistoryLog = new BindingSource();

        public static DataTable dt_MainTable = new DataTable();

        public static DataTable dt_CurrentLog = new DataTable();

        public static DataTable dt_HistoryLog = new DataTable();

        public static ConcurrentDictionary<int, string> dic_ReceiveMessage = new ConcurrentDictionary<int, string>();

        public static Dictionary<string, DateTime> dicSQL = new Dictionary<string, DateTime>();

        public static String[] str_ErrorMessage = new string[] { "Ex", "網路連線失敗", "設備查無資料", "連線中...", "Sclass設定錯誤", "無資料", "重新連線中" };

        public static String str_Barcode = "";

        public static int intSQLCount = 0;

        public static List<String> list_HistoryLog = new List<String>();

        public static List<String> list_CurrentLog = new List<String>();

        public static List<String> listSQL = new List<String>();

        public static Boolean Ethernet = false;

        public static String conStr = "server=192.168.1.58;Initial Catalog=dbMES_new;Persist Security Info=True;User ID=sa;Password=aaa222!!!;Max Pool Size=200";
        public static String conStr_dbMEStemp = "server=192.168.1.58;Initial Catalog=dbMES_temp;Persist Security Info=True;User ID=sa;Password=aaa222!!!;Max Pool Size=200";
        public static String conStr_old = "server=192.168.1.58;Initial Catalog=dbMES;Persist Security Info=True;User ID=sa;Password=aaa222!!!;Max Pool Size=200";

        //public static System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection("server=192.168.1.58;Initial Catalog=dbMES;Persist Security Info=True;User ID=sa;Password=aaa222!!!");
        public static System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(conStr);
        public static System.Data.SqlClient.SqlConnection conn_old = new System.Data.SqlClient.SqlConnection(conStr_old);

        private static object Lock_SQL = new object();

        #region 讀取SQL-MES暫存資料庫(dbMES_temp)
        /// <summary>
        /// 讀取SQL(請確認SQL指令是否正確)
        /// </summary>
        /// <param name="pSQL">SQL指令</param>
        public static void ReadSQL_dbMEStemp(string pSQL)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(conStr_dbMEStemp))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(pSQL, conn);
                    cmd.CommandTimeout = 3;
                    cmd.ExecuteNonQuery();
                    cmd.Cancel();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                WriteSQLErrorCode("[ReadSQL_dbMEStemp] " + ex.Message, pSQL);

            }
        }
        #endregion

        #region 讀取SQL回傳DataTable-MES暫存資料庫(dbMES_temp)
        /// <summary>
        /// 讀取SQL回傳DataTable(請確認SQL指令是否正確)
        /// </summary>
        /// <param name="pSQL">SQL指令</param>
        public static DataTable ReadSQLToDT_dbMEStemp(string pSQL, int timeout = 3)
        {
            DataTable dtSource = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(conStr_dbMEStemp))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(pSQL, conn);
                    cmd.CommandTimeout = timeout;
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dtSource);
                    cmd.Cancel();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                WriteSQLErrorCode("[ReadSQLToDT_dbMEStemp] " + ex.Message, pSQL);

            }
            return dtSource;
        }
        #endregion

        #region 讀取SQL
        /// <summary>
        /// 讀取SQL(請確認SQL指令是否正確)
        /// </summary>
        /// <param name="pSQL">SQL指令</param>
        public static void ReadSQL(string pSQL)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(conStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(pSQL, conn);
                    cmd.CommandTimeout = 10;
                    cmd.ExecuteNonQuery();
                    cmd.Cancel();
                    conn.Close();
                    MPU.Ethernet = true;
                }
            }
            catch (Exception ex)
            {
                //若dbMES資料庫寫入異常時，改為寫入dbMEStemp臨時資料庫
                if (!pSQL.Contains("tb_connectlog") && !ex.Message.Contains("字串或二進位資料會被截斷。"))
                {
                    //ReadSQL_dbMEStemp(pSQL);
                    //SQLInsertError("資料寫入異常");

                    //寫入失敗就先儲存SQL語法，再重新寫入一次
                    dicSQL.Add($"{pSQL}--{intSQLCount}", DateTime.Now);

                    if (intSQLCount <= 10000)
                        intSQLCount++;
                    else
                        intSQLCount = 0;
                }

                WriteSQLErrorCode("[ReadSQL] " + ex.Message, pSQL);
            }
        }

        public static void ReReadSQL(string pSQL)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(conStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(pSQL, conn);
                    cmd.CommandTimeout = 10;
                    cmd.ExecuteNonQuery();
                    cmd.Cancel();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                //若dbMES資料庫寫入異常時，改為寫入dbMEStemp臨時資料庫
                if (!pSQL.Contains("tb_connectlog") && !ex.Message.Contains("字串或二進位資料會被截斷。"))
                {
                    ReadSQL_dbMEStemp(pSQL);
                    //SQLInsertError("資料寫入異常");
                }

                WriteSQLErrorCode("[ReReadSQL] " + ex.Message, pSQL);
            }
        }
        #endregion

        #region 讀取SQL回傳DataTable
        /// <summary>
        /// 讀取SQL回傳DataTable(請確認SQL指令是否正確)
        /// </summary>
        /// <param name="pSQL">SQL指令</param>
        public static DataTable ReadSQLToDT(string pSQL, int timeout = 3)
        {
            DataTable dtSource = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(conStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(pSQL, conn);
                    cmd.CommandTimeout = timeout;
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dtSource);
                    cmd.Cancel();
                    conn.Close();
                    MPU.Ethernet = true;
                }
            }
            catch (Exception ex)
            {
                Check_Connection.CheckConnaction();
                WriteSQLErrorCode("[ReadSQLToDT] " + ex.Message, pSQL);

            }
            return dtSource;
        }
        #endregion

        #region 讀取SQL_old
        /// <summary>
        /// 讀取SQL(請確認SQL指令是否正確)
        /// </summary>
        /// <param name="pSQL">SQL指令</param>
        public static void ReadSQL_old(string pSQL)
        {
            try
            {
                if (Check_Connection.CheckConnaction())
                {
                    using (SqlConnection conn = new SqlConnection(conStr_old))
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(pSQL, conn);
                        cmd.CommandTimeout = 3;
                        cmd.ExecuteNonQuery();
                        cmd.Cancel();
                        conn.Close();
                        MPU.Ethernet = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MPU.Ethernet = false;
                throw ex;
            }
        }
        #endregion

        #region 讀取SQL回傳DataTable_old
        /// <summary>
        /// 讀取SQL回傳DataTable(請確認SQL指令是否正確)
        /// </summary>
        /// <param name="pSQL">SQL指令</param>
        public static DataTable ReadSQLToDT_old(string pSQL)
        {
            DataTable dtSource = new DataTable();
            try
            {
                if (Check_Connection.CheckConnaction())
                {
                    using (SqlConnection conn = new SqlConnection(conStr_old))
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(pSQL, conn);
                        cmd.CommandTimeout = 3;
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(dtSource);
                        cmd.Cancel();
                        conn.Close();
                        MPU.Ethernet = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MPU.Ethernet = false;
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
        public static void WriteData(string path, string name, string content)
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

        #region 讀檔案
        /// <summary>
        ///資料讀取
        /// </summary>
        /// <param name="path">路徑</param>
        /// <param name="name">檔名</param>
        /// <returns>回傳檔名全部內容,長度為0時則內容為空或路徑錯誤</returns>
        /// <summary>
        ///資料讀取
        /// </summary>
        /// <param name="path">路徑</param>
        /// <param name="name">檔名</param>
        /// <returns>回傳檔名全部內容,回傳空值時則內容為空,回傳null為路徑錯誤</returns>
        public static string[] GetFileData(string path, string name)
        {
            string[] content = new string[] { "" };
            try
            {
                string read = "", line;
                path = path + "/" + name;
                using (FileStream logFileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader logFileReader = new StreamReader(logFileStream, Encoding.UTF8))
                    {
                        while (!logFileReader.EndOfStream)
                        {
                            line = logFileReader.ReadLine();
                            read += line + "#@#";
                        }
                        logFileReader.Close();
                        logFileStream.Close();
                        if (read != "")
                        {
                            read = read.Substring(0, read.Length - 3);
                            content = Regex.Split(read, "#@#");
                        }
                    }
                }
                return content;
            }
            catch (Exception ex)
            {
                // log with exception here
                Console.WriteLine("[GetFileData]" + ex.Message);
                return content;
            }
        }
        #endregion

        /// <summary>
        /// 設定系統自建資料欄位
        /// </summary>
        /// <param name="Arry_Str_Set"></param>
        /// <param name="StrDataColumnsName"></param>
        public static void SetDataColumn(ref String[] Arry_Str_Set, String StrDataColumnsName)
        {

            Arry_Str_Set = StrDataColumnsName.Split(',');

        }

        #region SQL ERROR LOG
        public static void WriteSQLErrorCode(String strErrorMessage, string SQL)
        {
            object D_Lock = new object();
            string strErrorMessageLOG = "";
            lock (D_Lock)
            {
                string write_path = System.Environment.CurrentDirectory + @"\SQL_Error\" + DateTime.Now.ToString("yyyyMMdd") + ".txt";

                if (!File.Exists(System.Environment.CurrentDirectory + @"\SQL_Error\" + DateTime.Now.ToString("yyyyMMdd") + ".txt"))
                {
                    if (!Directory.Exists(System.Environment.CurrentDirectory + @"\SQL_Error\"))
                    {
                        Directory.CreateDirectory(System.Environment.CurrentDirectory + @"\SQL_Error\");
                    }

                    using (System.IO.FileStream fs = System.IO.File.Create(System.Environment.CurrentDirectory + @"\SQL_Error\" + DateTime.Now.ToString("yyyyMMdd") + ".txt"))
                    {
                    }
                }

                //重覆的錯誤，就不要再記錄了。
                if (strErrorMessage != strErrorMessageLOG)
                {

                    //如果檔案太大就清空文字檔。
                    FileInfo f = new FileInfo(System.Environment.CurrentDirectory + @"\SQL_Error\" + DateTime.Now.ToString("yyyyMMdd") + ".txt");

                    using (System.IO.StreamWriter wf = new System.IO.StreamWriter(write_path, true, System.Text.Encoding.GetEncoding("big5")))
                    {
                        wf.WriteLine(DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + ":" + strErrorMessage);
                        wf.WriteLine(SQL);
                    }

                    strErrorMessageLOG = strErrorMessage;

                }
            }
        }
        #endregion

        #region SQL INSERT ERROR LOG
        public static void SQLInsertError(String strErrorMessage)
        {
            object D_Lock = new object();
            lock (D_Lock)
            {
                string write_path = System.Environment.CurrentDirectory + @"\SQL_Error\" + DateTime.Now.ToString("yyyyMMdd") + ".txt";

                if (!File.Exists(System.Environment.CurrentDirectory + @"\SQL_Error\" + DateTime.Now.ToString("yyyyMMdd") + ".txt"))
                {
                    if (!Directory.Exists(System.Environment.CurrentDirectory + @"\SQL_Error\"))
                    {
                        Directory.CreateDirectory(System.Environment.CurrentDirectory + @"\SQL_Error\");
                    }

                    using (System.IO.FileStream fs = System.IO.File.Create(System.Environment.CurrentDirectory + @"\SQL_Error\" + DateTime.Now.ToString("yyyyMMdd") + ".txt"))
                    {
                    }
                }

                //如果檔案太大就清空文字檔。
                FileInfo f = new FileInfo(System.Environment.CurrentDirectory + @"\SQL_Error\" + DateTime.Now.ToString("yyyyMMdd") + ".txt");

                using (System.IO.StreamWriter wf = new System.IO.StreamWriter(write_path, true, System.Text.Encoding.GetEncoding("big5")))
                {
                    wf.WriteLine(DateTime.Now.ToString("[HH:mm:ss] ") + ":" + strErrorMessage);
                }
            }
        }
        #endregion

        public static void WriteErrorCode(String strSourceID, String strErrorMessage)
        {
            object D_Lock = new object();
            string strErrorMessageLOG = "";
            lock (D_Lock)
            {
                string write_path = System.Environment.CurrentDirectory + @"\Log\error.txt";

                if (!File.Exists(System.Environment.CurrentDirectory + @"\Log\error.txt"))
                {
                    if (!Directory.Exists(System.Environment.CurrentDirectory + @"\Log\"))
                    {
                        Directory.CreateDirectory(System.Environment.CurrentDirectory + @"\Log\");
                    }

                    using (System.IO.FileStream fs = System.IO.File.Create(System.Environment.CurrentDirectory + @"\Log\error.txt"))
                    {
                    }
                }

                //重覆的錯誤，就不要再記錄了。
                if (strErrorMessage != strErrorMessageLOG)
                {

                    //如果檔案太大就清空文字檔。
                    FileInfo f = new FileInfo(System.Environment.CurrentDirectory + @"\Log\error.txt");

                    using (System.IO.StreamWriter wf = new System.IO.StreamWriter(write_path, true, System.Text.Encoding.GetEncoding("big5")))
                    {
                        wf.WriteLine(DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + ":" + strErrorMessage);
                    }

                    strErrorMessageLOG = strErrorMessage;

                }
            }
        }


        public static void sleep_Loop(int sleepLoop, int sleepTime)
        {
            for (int int_t = 0; int_t<sleepLoop; int_t++)
            {
                System.Windows.Forms.Application.DoEvents();
                System.Threading.Thread.Sleep(sleepTime);
            }
        }
           
    /// <summary>
    /// 設定系統自建資料表
    /// </summary>
    /// <param name="StrDataTableName"></param>
    /// <param name="DataTable_Set"></param>
    /// <param name="StrDataColumnsName"></param>
    public static void SetDataTable(String StrDataTableName, ref System.Data.DataTable DataTable_Set, String[] StrDataColumnsName)
        { 

            try
            {
                //'先清空資料表
                DataTable_Set = new System.Data.DataTable ();

                //'設定資料表名稱
                DataTable_Set.TableName = StrDataTableName;

                //'設定欄位名稱

                for (int i = 0; i <= StrDataColumnsName.Length  - 1; i++)
                {

                    DataTable_Set.Columns.Add(StrDataColumnsName[i], Type.GetType("System.String")); 
                
                } 
                 
            }
                   
            catch (Exception ex)
            {
                if (ex.Source != null)
                    Console.WriteLine("MPU0060:Exception source: {0}", ex.Source);
            }
             
        }

        //記錄系統資訊
        public static void systemlog(String logmessage)
        {
            try
            {
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("INSERT  INTO [dbo].[tb_errorcode] ([err_str],[err_type],[err_source],[systime]) VALUES('" + logmessage + "','',0,'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "') ", conn);
 
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception EX)
            {
               // EX.Source

                if (EX.Source != null)
                    Console.WriteLine("MPU0082:Exception source: {0}", EX.Source);

            }
        }
    }
}
