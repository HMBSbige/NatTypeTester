using System;

namespace NatTypeTester.Model
{
    public class StunServer
    {
        public string Hostname;
        public ushort Port;

        public StunServer()
        {
            Hostname = @"stun.qq.com";
            Port = 3478;
        }

        public bool Parse(string str)
        {
            var ipPort = str.Trim().Split(':', '：');
            var host = ipPort[0].Trim();
            if (Uri.CheckHostName(host) != UriHostNameType.Dns)
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
            if (Port == 3478)
            {
                return Hostname;
            }
            return $@"{Hostname}:{Port}";
        }
    }
}
