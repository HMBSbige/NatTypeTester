using System;
using System.Net;
using System.Net.Sockets;

namespace NatTypeTester.Model
{
    public class StunServer
    {
        public string Hostname { get; set; }
        public ushort Port { get; set; }

        public StunServer()
        {
            Hostname = @"stun.syncthing.net";
            Port = 3478;
        }

        public bool Parse(string str)
        {
            var ipPort = str.Trim().Split(':', '：');
            var host = ipPort[0].Trim();
            if (Uri.CheckHostName(host) != UriHostNameType.Dns && !IPAddress.TryParse(host, out _))
            {
                return false;
            }
            switch (ipPort.Length)
            {
                case 2:
                {
                    if (ushort.TryParse(ipPort[1], out var port))
                    {
                        Hostname = host;
                        Port = port;
                        return true;
                    }
                    break;
                }
                case 1:
                {
                    Hostname = host;
                    Port = 3478;
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Hostname))
            {
                return string.Empty;
            }
            if (Port == 3478)
            {
                return Hostname;
            }
            if (IPAddress.TryParse(Hostname, out var ip) && ip.AddressFamily != AddressFamily.InterNetwork)
            {
                return $@"[{Hostname}]:{Port}";
            }
            return $@"{Hostname}:{Port}";
        }
    }
}
