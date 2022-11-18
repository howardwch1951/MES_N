using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic; 
using System.Collections; 
using System.Data;
using System.Diagnostics;

namespace MES_N
{
    
    //處理連結socket超時問題，當連結超出時間時，會以例外方式中斷此IP連結。
    public class TimeOutSocket
    {

        private static bool IsConnectionSuccessful = false;

		private static Exception socketexception;

	    private static System.Threading.ManualResetEvent TimeoutObject = new System.Threading.ManualResetEvent(false);
      
        public static System.Net.Sockets.TcpClient Connect(System.Net.IPEndPoint remoteEndPoint, int timeoutMSec)
	    {
           
		    TimeoutObject.Reset();

			socketexception = null;

		    string serverip = Convert.ToString(remoteEndPoint.Address);
		    int serverport = remoteEndPoint.Port;
            System.Net.Sockets.TcpClient tcpclient = new System.Net.Sockets.TcpClient();

		    tcpclient.BeginConnect(serverip, serverport, new AsyncCallback(CallBackMethod), tcpclient);

		    if (TimeoutObject.WaitOne(timeoutMSec, false)) {
			    if (IsConnectionSuccessful) {
				    return tcpclient;
			    } 
				else
				{
					tcpclient.Close();
					throw new Exception();
			    }
		    } else {
			    tcpclient.Close();
			    throw new TimeoutException("TimeOut Exception (TimeOutSocket-0040)");
		    }
	    }

	    private static void CallBackMethod(IAsyncResult asyncresult)
	    {
		    try {
			    IsConnectionSuccessful = false;
                System.Net.Sockets.TcpClient tcpclient = asyncresult.AsyncState as System.Net.Sockets.TcpClient;

			    if (tcpclient.Client != null) {
				    tcpclient.EndConnect(asyncresult);
				    IsConnectionSuccessful = true;
			    }
		    } catch (Exception ex) {
			    IsConnectionSuccessful = false;
			    socketexception = ex;
		    } finally {
			    TimeoutObject.Set();
		    }
	    }
    } 
}
