using System;
using System.Linq;
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
            switch (ipPort.Length)
            {
                case 0: return false;
                case 1:
                {
                    var host = ipPort[0].Trim();
                    if (Uri.CheckHostName(host) != UriHostNameType.Dns && !IPAddress.TryParse(host, out _))
                    {
                        return false;
                    }
                    Hostname = host;
                    Port = 3478;
                    return true;
                }
                case 2:
                {
                    var host = ipPort[0].Trim();
                    if (Uri.CheckHostName(host) != UriHostNameType.Dns && !IPAddress.TryParse(host, out _))
                    {
                        return false;
                    }
                    if (ushort.TryParse(ipPort[1], out var port))
                    {
                        Hostname = host;
                        Port = port;
                        return true;
                    }
                    break;
                }
                default:
                {
                    if (IPAddress.TryParse(str.Trim(), out var ipv6))
                    {
                        Hostname = $@"{ipv6}";
                        Port = ushort.TryParse(ipPort.Last(), out var portV6) ? portV6 : (ushort)3478;
                        return true;
                    }

                    var ipStr = string.Join(@":", ipPort, 0, ipPort.Length - 1);
                    if (!ipStr.StartsWith(@"[") || !ipStr.EndsWith(@"]") || !IPAddress.TryParse(ipStr, out _))
                    {
                        return false;
                    }

                    if (ushort.TryParse(ipPort.Last(), out var port))
                    {
                        Port = port;
                        return true;
                    }

                    break;
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
