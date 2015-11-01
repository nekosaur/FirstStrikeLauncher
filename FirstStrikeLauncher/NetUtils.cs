using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FirstStrikeLauncher
{
    public static class NetUtils
    {
        
        public static bool Ping(string ip, int timeout)
        {
            Ping ping = new Ping();

            try
            {
                PingReply reply = ping.Send(ip, timeout);

                if (reply.Status == IPStatus.Success)
                    return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            return false;
        }

        public static bool CheckStatus(string address)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://" + address + "/status.txt");
            request.Timeout = 2000;
            request.KeepAlive = false;
            request.ReadWriteTimeout = 2000;
            request.Method = "GET";

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream stream = response.GetResponseStream())
                        {
                            if (stream != null)
                            {
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    string status = reader.ReadLine();

                                    if (status != "OK")
                                        return false;
                                }
                            }
                        }

                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Timed out");
                return false;
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        private delegate IPHostEntry GetHostEntryHandler(string ip);

        public static bool CheckPort(string server, int port)
        {
            try
            {
                TcpClient tcp = new TcpClient();
                tcp.Connect(server, Convert.ToInt16(port));
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public static string CheckInternetConnection(string host, int timeout)
        {
            Regex url = new Regex("(?:http://)?([0-9a-z-.]*)(?:[/a-z0-9_-]*)",
                                  RegexOptions.IgnoreCase | RegexOptions.Compiled);

            Match match = url.Match(host);

            string server = match.Groups[1].Captures[0].Value.ToLower();

            if (ResolveDNS(server, timeout) == null)
                return "Unable to resolve DNS of server " + server + ". Make sure you have an internet connection.";

            if (!CheckPort(server, 80))
                return "Unable to connect to port 80. Make sure the launcher is not blocked by any firewalls.";

            return null;
        }

        private static string ResolveDNS(string server, int timeout)
        {

            try
            {
                GetHostEntryHandler callback = new GetHostEntryHandler(Dns.GetHostEntry);
                IAsyncResult result = callback.BeginInvoke(server, null, null);
                if (result.AsyncWaitHandle.WaitOne(timeout, false))
                {
                    return callback.EndInvoke(result).AddressList[0].ToString();
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }
    }
}
