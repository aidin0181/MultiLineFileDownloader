using HttpClient.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace HttpClient
{
    internal class Internet
    {
        public static List<NIC> Connected_NICS = new List<NIC>();
        public static List<NIC> NICS = new List<NIC>();
        private static List<string> Hostnames = new List<string>();
        private static List<IPAddress> IpAddresses = new List<IPAddress>();

        public static void Init()
        {
            //google
            Hostnames.Add("8.8.8.8");
            //google 2
            Hostnames.Add("8.8.4.4");
            //opendns
            Hostnames.Add("208.67.222.222");
            //cloudflare
            Hostnames.Add("1.1.1.1");
            //comodo
            Hostnames.Add("8.26.56.26");
            //quad9
            Hostnames.Add("9.9.9.9");
            //opennic
            Hostnames.Add("64.6.65.6");
            //yandex
            Hostnames.Add("77.88.8.7");
            //adguard
            Hostnames.Add("176.103.130.130");

            foreach(var host in Hostnames)
                IpAddresses.Add(IPAddress.Parse(host));

            Connected_NICS = new List<NIC>(Get_Internet_Connected_NICS_IpAdress());

            //for testing
            //Connected_NICS.Add(new NIC("127.0.0.1", 200000));
            //Connected_NICS.Add(new NIC("0.0.0.0", 300000));
            //Connected_NICS.Add(new NIC("192.168.1.1", 300000));
            //for testing

            NICS = Get_All_NICS_IpAddress();
            int i = 0;
            Console.WriteLine("---------------------");
            foreach (var nic in Connected_NICS)
                Console.WriteLine($"{i++}- {nic.IPAddress} speed = {nic.LinkSpeed / (1000 * 1000)}MB");
            Console.WriteLine("---------------------");
        }


        private static bool Ping(string LocalIpAddress)
        {
            try
            {
                var client = new TcpClient();
                //Console.WriteLine(LocalIpAddress);
                var address = IPAddress.Parse(LocalIpAddress);
                IPEndPoint endPoint = new IPEndPoint(address, 0);

                client.Client.Bind(endPoint);

                client.Connect(IpAddresses.ToArray(), 53);
                if (client.Connected)
                {
                    //Console.WriteLine("connected 53");
                    //Console.WriteLine($"{client.Client.RemoteEndPoint.ToString()}");
                    client.Close();
                    client.Dispose();
                    return true;
                }

                return false;
            }
            catch(Exception ex)
            {
                //Console.WriteLine(ex);
                return false;
            }
        }

        private static List<NIC> Get_Internet_Connected_NICS_IpAdress()
        {
            List<NIC> connected_nics = new List<NIC>();
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach(var nic in nics)
            {
                foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if(Ping(ip.Address.ToString()))
                            connected_nics.Add(new NIC(ip.Address.ToString() , nic.Speed));
                    }
                }
            }
            return connected_nics;
        }
        
        private static List<NIC> Get_All_NICS_IpAddress()
        {
            var localIPs = new List<NIC>();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            localIPs.Add(new NIC(ip.Address.ToString(), ni.Speed));
                        }
                    }
                }
            }
            return localIPs;
        }


        internal static bool ValidateServerCertificate(object sender, X509Certificate certificate,
        X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

    }
}
