using STUN.Interfaces;
using STUN.Utils;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace STUN.Proxy
{
    public class Socks5UdpProxy : IUdpProxy
    {
        private readonly TcpClient _assoc;
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
            _assoc = new TcpClient(proxy.AddressFamily);
            _socksTcpEndPoint = proxy;
        }

        public Socks5UdpProxy(IPEndPoint local, IPEndPoint proxy, string user, string password) : this(local, proxy)
        {
            _user = user;
            _password = password;
        }

        public async Task ConnectAsync(CancellationToken token = default)
        {
            try
            {
                var buf = new byte[1024];
                await _assoc.ConnectAsync(_socksTcpEndPoint.Address, _socksTcpEndPoint.Port);
                var s = _assoc.GetStream();
                var requestPasswordAuth = !string.IsNullOrEmpty(_user);

                #region Handshake
                // we have no GSS-API support
                var handShake = requestPasswordAuth ? new byte[] { 5, 2, 0, 2 } : new byte[] { 5, 1, 0 };
                await s.WriteAsync(handShake, 0, handShake.Length, token);

                // 5 auth(ff=deny)
                if (await s.ReadAsync(buf, 0, 2, token) != 2)
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
                        var uByte = Encoding.UTF8.GetBytes(_user);
                        var pByte = Encoding.UTF8.GetBytes(_password);
                        buf[0] = 1;
                        buf[1] = (byte)uByte.Length;
                        Array.Copy(uByte, 0, buf, 2, uByte.Length);
                        buf[uByte.Length + 2] = (byte)pByte.Length;
                        Array.Copy(pByte, 0, buf, uByte.Length + 3, pByte.Length);
                        // 1 userLen user passLen pass
                        await s.WriteAsync(buf, 0, uByte.Length + pByte.Length + 3, token);
                        // 1 state(0=ok)
                        if (await s.ReadAsync(buf, 0, 2, token) != 2)
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

                var abyte = GetEndPointByte(new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort));
                var addrLen = abyte.Length;
                Array.Copy(abyte, 0, buf, 3, addrLen);
                // 5 cmd(3=udpassoc) 0 atyp(1=v4 3=dns 4=v5) addr port
                await s.WriteAsync(buf, 0, addrLen + 3, token);
                #endregion

                #region UDP Assoc Response
                if (await s.ReadAsync(buf, 0, 4, token) != 4)
                    throw new ProtocolViolationException();
                if (buf[0] != 5 || buf[2] != 0)
                    throw new ProtocolViolationException();
                if (buf[1] != 0)
                    throw new UnauthorizedAccessException();

                addrLen = GetAddressLength(buf[3]);

                var addr = new byte[addrLen];
                if (await s.ReadAsync(addr, 0, addrLen, token) != addrLen)
                    throw new ProtocolViolationException();
                var assocIP = new IPAddress(addr);
                if (await s.ReadAsync(buf, 0, 2, token) != 2)
                    throw new ProtocolViolationException();
                var assocPort = buf[0] * 256 + buf[1];
                #endregion

                _assocEndPoint = new IPEndPoint(assocIP, assocPort);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await DisconnectAsync(token);
                throw;
            }
        }

        public async Task<(byte[], IPEndPoint, IPAddress)> ReceiveAsync(byte[] bytes, IPEndPoint remote, EndPoint receive, CancellationToken token = default)
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

            var addressLen = GetAddressLength(res[3]);

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

        public Task DisconnectAsync(CancellationToken token = default)
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

        private static int GetAddressLength(byte b)
        {
            return b switch
            {
                1 => 4,
                4 => 16,
                _ => throw new NotSupportedException()
            };
        }

        public void Dispose()
        {
            _udpClient?.Dispose();
            _assoc?.Dispose();
        }
    }
}
