using HttpClient.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpClient
{
    internal class Downloader
    {
        public static TcpClient client;
        public static string ipAddress;
        public static int port = 0;
        public static NetworkStream netStream;
        public static bool Connected = false;
        public static string localIpAddress;
        public static SslStream sslStream;
        public static bool complete_download = false;
        public static long content_length = 0;
        public static bool Print_Header_Information = false;
        public static List<string> Http_Versions = new List<string>() { "HTTP/1.0", "HTTP/1.1", "HTTP/2" };
        public static bool Correct_Url = false;
        private static List<string> domains;
        private static string http_request = "";
        private static string header_file_request = "";
        private static List<Download_Range> ranges = new List<Download_Range>();
        public static string Filename = "";
        private static string HttpVersion = "";
        private static string HostName = "";
        private static int TryCount = 3;
        private static int CurrentTry = 1;
        internal static string CompleteURl = "";


        private static void Connect()
        {
            try
            {
                client = new TcpClient();

                //Console.WriteLine($"host name = {ipAddress}");
                var address = IPAddress.Parse(Internet.Connected_NICS[0].IPAddress);
                IPEndPoint endPoint = new IPEndPoint(address, 0);
                client.SendTimeout = 3000;
                client.ReceiveTimeout = 3000;
                client.Client.Bind(endPoint);
                client.Connect(ipAddress, port);


                if (client.Connected)
                {
                    Connected = true;
                    netStream = client.GetStream();
                    if (port == 443)
                    {
                        sslStream = new SslStream(netStream, true, new RemoteCertificateValidationCallback(Internet.ValidateServerCertificate), null);
                        sslStream.AuthenticateAsClient(ipAddress);
                    }
                    Task.Run(() => { handleIncomingMessage(); });
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Connect();
            }
        }

        internal static void Disconnect()
        {
            if (Connected)
            {
                client.Close();
                client.Dispose();
                Connected = false;
                Console.WriteLine($"you are disconnected");
            }
        }

        internal static void Start_Downloading(string url)
        {
            try
            {
                CompleteURl = url;
                DownloadManager.AbortProcess = false;

                var splited_url = url.Split('/');

                Filename = splited_url[splited_url.Length - 1];

                if (splited_url[0].ToLower() == "https:")
                    port = 443;
                else if (splited_url[0].ToLower() == "http:")
                    port = 80;
                else
                    Console.WriteLine("unsupported protocol from url");

                header_file_request = "/";

                for (int i = 3; i < splited_url.Length; i++)
                {
                    if (i + 1 == splited_url.Length)
                        header_file_request += $"{splited_url[i]}";
                    else
                        header_file_request += $"{splited_url[i]}/";
                }

                var domains_splited = splited_url[2].Split('.');

                domains = new List<string>();
                if (domains_splited.Length > 1)
                {
                    Console.WriteLine($"domains_splited count {domains_splited.Length}");
                    domains.Add($"{domains_splited[domains_splited.Length - 2]}.{domains_splited[domains_splited.Length - 1]}");
                    if (domains_splited.Length >= 3)
                    {
                        for (int i = 3; i <= domains_splited.Length; i++)
                        {
                            string domain = "";
                            for (int j = 0; j < i; j++)
                            {
                                domain = domains_splited[domains_splited.Length - j - 1] + domain;
                                if (j != i - 1)
                                    domain = "." + domain;
                            }
                            if (domain == "localhost")
                                domains.Add("0.0.0.0");
                            else
                                domains.Add(domain);
                        }
                    }

                    domains.Reverse();
                }
                else
                {
                    if (splited_url[2] == "localhost")
                    {
                        //IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                        //IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 80);
                        domains.Add("192.168.43.143");
                    }
                    else
                        domains.Add(splited_url[2]);
                }

                Correct_Url = false;
                Send_Http_Requests();
            }
            catch (Exception ex)
            {
                if (CurrentTry > TryCount)
                {
                    Console.WriteLine($"cant connect, exiting {ex}");
                    return;
                }
                else
                {
                    CurrentTry++;
                    Console.WriteLine($"cant connect, starting again {ex}");
                    Start_Downloading(url);
                }
            }
        }
        
        
        private static int current_httprequest_domain_counter = 0;
        private static int current_httprequest_httpversion_counter = 0;

        private static bool Send_Http_Requests()
        {
            if (current_httprequest_domain_counter == domains.Count & current_httprequest_httpversion_counter == Http_Versions.Count - 1)
            {
                return false;
            }

            if (current_httprequest_domain_counter == domains.Count)
            {
                current_httprequest_httpversion_counter++;
                current_httprequest_domain_counter = 0;
            }
            HttpVersion = Http_Versions[current_httprequest_httpversion_counter];
            HostName = domains[current_httprequest_domain_counter];
            http_request = $"GET {header_file_request} {HttpVersion}\r\nHost: {HostName}\r\n\r\n\r\n";

            //Console.WriteLine($"sending http request \r\n{http_request}");

            ipAddress = domains[current_httprequest_domain_counter];
            current_httprequest_domain_counter++;
            Connect();
            //Task.Delay(1000).Wait();
            SendMessage(http_request);
            return true;
        }

        private static void Try_Another_Request()
        {
            if (client.Connected)
            {
                client.Close();
                client.Dispose();
            }
            if (!Send_Http_Requests())
            {
                Console.WriteLine($"can't download the file");
                return;
            }
        }

        private static void SendMessage(string message)
        {
            try
            {
                var bytesToSend2 = Encoding.UTF8.GetBytes(message);
                if (port == 443)
                    sslStream.Write(bytesToSend2, 0, bytesToSend2.Length);
                else
                    netStream.Write(bytesToSend2, 0, bytesToSend2.Length);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message + " downloader line 221");
            }
        }

        internal static void handleIncomingMessage()
        {
            int bufferSize = 1024;
            int current_read = 0;
            byte[] buffer = new byte[bufferSize];
            List<byte> file_bytes = new List<byte>();

            Print_Header_Information = false;
            while (Connected)
            {
                try
                {
                    while (client.Available > 0)
                    {
                        //Console.WriteLine($"data available is {client.Available}");
                        //Connection.DisplayPendingByteCount(client.Client);
                        buffer = new byte[client.Available];

                        try
                        {
                            //Console.WriteLine($"trying to read");
                            if (port == 443)
                                sslStream.Read(buffer, current_read, buffer.Length);
                            else
                                netStream.Read(buffer, current_read, buffer.Length);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("disconnected , can't read from stream");
                            if (!complete_download)
                                Task.Run(() => { Try_Another_Request(); });
                            return;
                        }

                        var message = Encoding.UTF8.GetString(buffer);
                        var headers = message.Split(new[] { '\r', '\n' });

                        var start_body_index = message.IndexOf("\r\n\r\n");

                        var header = message.Substring(0, start_body_index + 4);

                        //Console.WriteLine($"header is :");
                        //Console.WriteLine(header);

                        if (!header.Contains("200 OK") && !header.Contains("206 Partial Content"))
                        {
                            if (!complete_download)
                                Task.Run(() => { Try_Another_Request(); });
                            return;
                        }
                        else
                        {
                            foreach (var head in headers)
                            {
                                if (head.Contains("Content-Length:"))
                                {
                                    long.TryParse(head.Remove(0, "Content-Length: ".Length), out content_length);
                                    break;
                                }
                            }
                            Console.WriteLine($"File length is {content_length} bytes");
                            Console.WriteLine($"total length is {Math.Round((double)(content_length / (1024 * 1024)), 2)}MB");
                            Console.WriteLine($"");
                            Console.WriteLine($"");


                            //we got correct http_request;
                            //here we should create connections and download simultanously
                            ranges = new List<Download_Range>(Tools.GetRangeForEachConnection(content_length));
                            //Console.WriteLine($"http version is {HttpVersion}");
                            Download_Infromation dlinfo = new Download_Infromation(CompleteURl, ipAddress, port, Filename , http_request, HttpVersion, header_file_request, HostName, Setting.Connections_Count, ranges);

                            DownloadManager.Current_DlInfo = dlinfo;
                            DownloadManager.Download_List.Add(dlinfo);


                            Console.WriteLine($"range in first element is {dlinfo.original_ranges[0].Min} - {dlinfo.original_ranges[0].Max}");


                            Task.Run(() => { DownloadManager.ShowMaxConnetionSpeed(); });
                            Task.Run(() => { DownloadManager.ShowTotalDownloaded(); });
                            foreach (var range in ranges)
                            {
                                //Task.Delay((3000 / Setting.Connections_Count) * range.Part_Number).Wait();
                                StartConnectionForEachRange(range);
                            }
                            DownloadManager.download_timer.Start();
                        }

                        client.Close();
                        client.Dispose();
                        return;
                    }
                
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    return;
                }
            }
        }


        private static void StartConnectionForEachRange(Download_Range range)
        {
            //var connection = DownloadManager.Connections.Find(x=> x.Part_Number == range.Part_Number);
            Connection cnc = new Connection();
            cnc.Part_Number = range.Part_Number;
            //Console.WriteLine($"start connection for part {range.Part_Number} - range {range.Min} - {range.Max}");
            Task.Run(() => { cnc.StartDownloding(ipAddress, port, range, HostName, HttpVersion, Filename, header_file_request); });
            DownloadManager.Connections.Add(cnc);
            DownloadManager.Downloading = true;
        }
        



    }
}
