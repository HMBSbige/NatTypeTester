using STUN.Interfaces;
using STUN.Utils;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace STUN.Proxy
{
    public class Socks5UdpProxy : IUdpProxy
    {
        private readonly TcpClient _assoc = new TcpClient();
        private readonly IPEndPoint _socksTcpEndPoint;

        private IPEndPoint _assocEndPoint;

        public TimeSpan Timeout
        {
            get => TimeSpan.FromMilliseconds(_udpClient.Client.ReceiveTimeout);
            set => _udpClient.Client.ReceiveTimeout = Convert.ToInt32(value.TotalMilliseconds);
        }

        public IPEndPoint LocalEndPoint => (IPEndPoint)_udpClient.Client.LocalEndPoint;

        private readonly UdpClient _udpClient;

        private readonly string _user;
        private readonly string _password;

        public Socks5UdpProxy(IPEndPoint local, IPEndPoint proxy)
        {
            _udpClient = local == null ? new UdpClient() : new UdpClient(local);
            _socksTcpEndPoint = proxy;
        }

        public Socks5UdpProxy(IPEndPoint local, IPEndPoint proxy, string user, string password) : this(local, proxy)
        {
            _user = user;
            _password = password;
        }

        public async Task ConnectAsync()
        {
            var buf = new byte[1024];

            await _assoc.ConnectAsync(_socksTcpEndPoint.Address, _socksTcpEndPoint.Port);
            try
            {
                var s = _assoc.GetStream();
                var requestPasswordAuth = !string.IsNullOrEmpty(_user);

                #region Handshake
                // we have no gssapi support
                if (requestPasswordAuth)
                {
                    // 5 authlen auth[](0=none, 2=userpasswd)
                    s.Write(new byte[] { 5, 2, 0, 2 }, 0, 4);
                }
                else
                {
                    s.Write(new byte[] { 5, 1, 0 }, 0, 3);
                }
                // 5 auth(ff=deny)
                if (s.Read(buf, 0, 2) != 2)
                    throw new ProtocolViolationException();
                if (buf[0] != 5)
                    throw new ProtocolViolationException();
                #endregion

                #region Auth
                var auth = buf[1];
                switch (auth)
                {
                    case 0:
                        break;
                    case 2:
                        var ubyte = Encoding.UTF8.GetBytes(_user);
                        var pbyte = Encoding.UTF8.GetBytes(_password);
                        buf[0] = 1;
                        buf[1] = (byte)ubyte.Length;
                        Array.Copy(ubyte, 0, buf, 2, ubyte.Length);
                        buf[ubyte.Length + 3] = (byte)pbyte.Length;
                        Array.Copy(pbyte, 0, buf, ubyte.Length + 4, pbyte.Length);
                        // 1 userlen user passlen pass
                        s.Write(buf, 0, ubyte.Length + pbyte.Length + 4);
                        // 1 state(0=ok)
                        if (s.Read(buf, 0, 2) != 2)
                            throw new ProtocolViolationException();
                        if (buf[0] != 1)
                            throw new ProtocolViolationException();
                        if (buf[1] != 0)
                            throw new UnauthorizedAccessException();
                        break;
                    case 0xff:
                        throw new UnauthorizedAccessException();
                    default:
                        throw new ProtocolViolationException();
                }
                #endregion

                #region UDP Assoc Send
                buf[0] = 5;
                buf[1] = 3;
                buf[2] = 0;

                int addrLen;
                var abyte = GetEndPointByte(new IPEndPoint(IPAddress.Any, 0));
                addrLen = abyte.Length;
                Array.Copy(abyte, 0, buf, 3, addrLen);
                // 5 cmd(3=udpassoc) 0 atyp(1=v4 3=dns 4=v5) addr port
                s.Write(buf, 0, addrLen + 3);
                #endregion

                #region UDP Assoc Response
                if (s.Read(buf, 0, 4) != 4)
                    throw new ProtocolViolationException();
                if (buf[0] != 5 || buf[2] != 0)
                    throw new ProtocolViolationException();
                if (buf[1] != 0)
                    throw new UnauthorizedAccessException();

                switch (buf[3])
                {
                    case 1:
                        addrLen = 4;
                        break;
                    case 4:
                        addrLen = 16;
                        break;
                    default:
                        throw new NotSupportedException();
                }

                var addr = new byte[addrLen];
                if (s.Read(addr, 0, addrLen) != addrLen)
                    throw new ProtocolViolationException();
                var assocIP = new IPAddress(addr);
                if (s.Read(buf, 0, 2) != 2)
                    throw new ProtocolViolationException();
                var assocPort = buf[0] * 256 + buf[1];
                #endregion

                _assocEndPoint = new IPEndPoint(assocIP, assocPort);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _assoc.Close();
            }
        }

        public async Task<(byte[], IPEndPoint, IPAddress)> ReceiveAsync(byte[] bytes, IPEndPoint remote, EndPoint receive)
        {
            var state = _assoc.GetState();
            if (state != TcpState.Established)
                throw new InvalidOperationException("No UDP association, maybe already disconnected or not connected");

            var remoteBytes = GetEndPointByte(remote);
            var proxyBytes = new byte[bytes.Length + remoteBytes.Length + 3];
            Array.Copy(remoteBytes, 0, proxyBytes, 3, remoteBytes.Length);
            Array.Copy(bytes, 0, proxyBytes, remoteBytes.Length + 3, bytes.Length);

            await _udpClient.SendAsync(proxyBytes, proxyBytes.Length, _assocEndPoint);
            var res = new byte[ushort.MaxValue];
            var flag = SocketFlags.None;
            EndPoint ep = new IPEndPoint(0, 0);
            var length = _udpClient.Client.ReceiveMessageFrom(res, 0, res.Length, ref flag, ref ep, out var ipPacketInformation);

            if (res[0] != 0 || res[1] != 0 || res[2] != 0)
            {
                throw new Exception();
            }

            var addressLen = res[3] switch
            {
                1 => 4,
                4 => 16,
                _ => throw new Exception()
            };

            var ipByte = new byte[addressLen];
            Array.Copy(res, 4, ipByte, 0, addressLen);

            var ip = new IPAddress(ipByte);
            var port = res[addressLen + 4] * 256 + res[addressLen + 5];
            var ret = new byte[length - addressLen - 6];
            Array.Copy(res, addressLen + 6, ret, 0, length - addressLen - 6);
            return (
                ret,
                new IPEndPoint(ip, port),
                ipPacketInformation.Address);
        }

        public Task DisconnectAsync()
        {
            try
            {
                _assoc.Close();
            }
            catch
            {
                // ignored
            }

            return Task.CompletedTask;
        }

        private static byte[] GetEndPointByte(IPEndPoint ep)
        {
            var ipByte = ep.Address.GetAddressBytes();
            var ret = new byte[ipByte.Length + 3];
            ret[0] = (byte)(ipByte.Length == 4 ? 1 : 4);
            Array.Copy(ipByte, 0, ret, 1, ipByte.Length);
            ret[ipByte.Length + 1] = (byte)(ep.Port / 256);
            ret[ipByte.Length + 2] = (byte)(ep.Port % 256);
            return ret;
        }

        public void Dispose()
        {
            _udpClient?.Dispose();
            _assoc?.Dispose();
        }
    }
}
