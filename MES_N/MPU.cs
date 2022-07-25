using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace MES_N
{

    //共用的函數群
    public static  class MPU
    {

        public static Boolean TOSrun = false;

        public static double MPU_int_numericUpDown1 = 0.045;

        public static Boolean wme1 = false;

        public static Boolean wme2 = false;

        public static Boolean wme3 = false;

        public static Boolean isStatusChange = false;

        public static DataTable DataTable_Threads = new DataTable();

        public static DataTable DataTable_CurrLog = new DataTable();

        public static DataTable DataTable_HistLog = new DataTable();

        public static String[] static_msg = new string[] { "Ex", "I/O網路連線失敗", "I/O設備查無資料" ,"連線中..."};

        public static String str_Barcode = "";

        public static List<String> histLog = new List<String>();

        public static List<String> currLog = new List<String>();

        public static Boolean Ethernet = false;

        public static String conStr = "server=192.168.0.180;Initial Catalog=dbMES;Persist Security Info=True;User ID=sa;Password=28921148";

        //public static System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection("server=192.168.1.58;Initial Catalog=dbMES;Persist Security Info=True;User ID=sa;Password=aaa222!!!");
        public static System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(conStr);

        #region 讀取SQL資料庫
        /// <summary>
        /// 讀取SQL資料庫(請確認SQL指令是否正確)
        /// </summary>
        /// <param name="pSQL">SQL指令</param>
        public static DataTable ReadSQLToDT(string pSQL)
        {
            DataTable dtSource = new DataTable();
            try
            {
                if (Check_Connection.CheckConnaction())
                {
                    using (SqlConnection conn = new SqlConnection(conStr))
                    {
                        SqlCommand cmd = new SqlCommand(pSQL, conn);
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(dtSource);
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

        /// <summary>
        /// 設定系統自建資料欄位
        /// </summary>
        /// <param name="Arry_Str_Set"></param>
        /// <param name="StrDataColumnsName"></param>
        public static  void SetDataColumn(ref String[] Arry_Str_Set, String StrDataColumnsName)
        {

            Arry_Str_Set = StrDataColumnsName.Split(',');           

        }

        static string strErrorMessageLOG = "";
        public static void WriteErrorCode(String strSourceID, String strErrorMessage)
        {
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
                Boolean bool_OverWriter = true;

                if (f.Length > 50000)
                {
                    bool_OverWriter = false;
                }

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(System.Environment.CurrentDirectory + @"\Log\error.txt", bool_OverWriter))
                {
                    file.WriteLine(DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + ":" + strErrorMessage);
                }

                strErrorMessageLOG = strErrorMessage;

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
