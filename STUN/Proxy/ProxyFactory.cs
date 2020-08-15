using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using STUN.Enums;

namespace STUN.Proxy
{
    public static class ProxyFactory
    {
        public static IUdpProxy CreateProxy(ProxyType type, IPEndPoint local, IPEndPoint proxy, string user, string password)
        {
            switch (type)
            {
                case ProxyType.Plain:
                    return new NoneUdpProxy(local, null);
                case ProxyType.Socks5:
                    return new Socks5UdpProxy(local, proxy);
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }
    }
}
