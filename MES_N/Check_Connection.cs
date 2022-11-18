using System.Data.SqlClient;
using System.Net.NetworkInformation;

namespace MES_N
{
    class Check_Connection
    {
        private static bool IsSQLOK(params SqlParameter[] parameters)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(MPU.conStr))
                {
                    string sql = "select 1";

                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandText = sql;
                        cmd.Parameters.AddRange(parameters);
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        cmd.CommandTimeout = 3;
                        cmd.Cancel();
                        conn.Close();
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool checkPing()
        {
            Ping ping = new Ping();
            try
            {
                PingReply reply = ping.Send("192.168.1.58", 3000);
                if (reply.Status == IPStatus.Success)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool CheckConnaction()
        {
            try
            {
                if (checkPing() && IsSQLOK())
                {
                    using (SqlConnection conn = new SqlConnection(MPU.conStr))
                    {
                        conn.Open();
                        conn.Close();
                        MPU.Ethernet = true;
                        return true;
                    }
                }
                else
                    MPU.Ethernet = false;
                return false;
            }
            catch
            {
                MPU.Ethernet = false;
                return false;
            }
        }
    }
}
