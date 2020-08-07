using STUN.Client;
using System;
using System.Diagnostics;
using System.Net;
using STUN.StunResult;

namespace STUN.Utils
{
    public static class NetUtils
    {
        public const string DefaultLocalEnd = @"0.0.0.0:0";

        public static IPEndPoint ParseEndpoint(string str)
        {
            //TODO:IPv6
            var ipPort = str.Trim().Split(':');
            if (ipPort.Length == 2)
            {
                if (IPAddress.TryParse(ipPort[0], out var ip))
                {
                    if (ushort.TryParse(ipPort[1], out var port))
                    {
                        return new IPEndPoint(ip, port);
                    }
                }
            }

            return null;
        }

        public static (string, string, string) NatTypeTestCore(string local, string server, ushort port)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(server))
                {
                    Debug.WriteLine(@"[ERROR]: Please specify STUN server !");
                    return (string.Empty, DefaultLocalEnd, string.Empty);
                }

                using var client = new StunClient3489(server, port, ParseEndpoint(local));

                var result = (ClassicStunResult)client.Query();

                return (
                        result.NatType.ToString(),
                        client.LocalEndPoint.ToString(),
                        $@"{result.PublicEndPoint}"
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($@"[ERROR]: {ex}");
                return (string.Empty, DefaultLocalEnd, string.Empty);
            }
        }
    }
}
