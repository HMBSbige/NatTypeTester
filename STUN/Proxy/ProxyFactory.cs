using STUN.Enums;
using STUN.Interfaces;
using System;
using System.Net;

namespace STUN.Proxy
{
    public static class ProxyFactory
    {
        public static IUdpProxy CreateProxy(ProxyType type, IPEndPoint local, IPEndPoint proxy, string user = null, string password = null)
        {
            return type switch
            {
                ProxyType.Plain => new NoneUdpProxy(local),
                ProxyType.Socks5 => new Socks5UdpProxy(local, proxy, user, password),
                _ => throw new NotSupportedException(type.ToString())
            };
        }
    }
}
