﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using STUN.Utils;

namespace STUN.Proxy
{
    class Socks5UdpProxy : IUdpProxy
    {
        TcpClient assoc = new TcpClient();
        IPEndPoint socksTcpEndPoint;

        IPEndPoint assocEndPoint;

        public TimeSpan Timeout
        {
            get => TimeSpan.FromMilliseconds(UdpClient.Client.ReceiveTimeout);
            set => UdpClient.Client.ReceiveTimeout = Convert.ToInt32(value.TotalMilliseconds);
        }

        public IPEndPoint LocalEndPoint { get => (IPEndPoint)UdpClient.Client.LocalEndPoint; }

        UdpClient UdpClient;

        string user;
        string password;
        public Socks5UdpProxy(IPEndPoint local, IPEndPoint proxy)
        {
            UdpClient = local == null ? new UdpClient() : new UdpClient(local);
            socksTcpEndPoint = proxy;
        }
        public Socks5UdpProxy(IPEndPoint local, IPEndPoint proxy, string user, string password) : this(local, proxy)
        {
            this.user = user;
            this.password = password;
        }
        public async Task ConnectAsync()
        {
            byte[] buf = new byte[1024];

            await assoc.ConnectAsync(socksTcpEndPoint.Address, socksTcpEndPoint.Port);
            try
            {
                var s = assoc.GetStream();
                bool requestPasswordAuth = !string.IsNullOrEmpty(user);

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
                if (s.Read(buf, 0, 2) != 2) throw new ProtocolViolationException();
                if (buf[0] != 5) throw new ProtocolViolationException();
                #endregion

                #region Auth
                var auth = buf[1];
                switch (auth)
                {
                    case 0:
                        break;
                    case 2:
                        byte[] ubyte = Encoding.UTF8.GetBytes(user);
                        byte[] pbyte = Encoding.UTF8.GetBytes(password);
                        buf[0] = 1;
                        buf[1] = (byte)ubyte.Length;
                        Array.Copy(ubyte, 0, buf, 2, ubyte.Length);
                        buf[ubyte.Length + 3] = (byte)pbyte.Length;
                        Array.Copy(pbyte, 0, buf, ubyte.Length + 4, pbyte.Length);
                        // 1 userlen user passlen pass
                        s.Write(buf, 0, ubyte.Length + pbyte.Length + 4);
                        // 1 state(0=ok)
                        if (s.Read(buf, 0, 2) != 2) throw new ProtocolViolationException();
                        if (buf[0] != 1) throw new ProtocolViolationException();
                        if (buf[1] != 0) throw new UnauthorizedAccessException();
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
                byte[] abyte = GetEndPointByte(new IPEndPoint(IPAddress.Any, 0));
                addrLen = abyte.Length;
                Array.Copy(abyte, 0, buf, 3, addrLen);
                // 5 cmd(3=udpassoc) 0 atyp(1=v4 3=dns 4=v5) addr port
                s.Write(buf, 0, addrLen + 3);
                #endregion

                #region UDP Assoc Response
                if (s.Read(buf, 0, 4) != 4) throw new ProtocolViolationException();
                if (buf[0] != 5 || buf[2] != 0) throw new ProtocolViolationException();
                if (buf[1] != 0) throw new UnauthorizedAccessException();

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

                byte[] addr = new byte[addrLen];
                if (s.Read(addr, 0, addrLen) != addrLen) throw new ProtocolViolationException();
                IPAddress assocIP = new IPAddress(addr);
                if (s.Read(buf, 0, 2) != 2) throw new ProtocolViolationException();
                int assocPort = buf[0] * 256 + buf[1];
                #endregion

                assocEndPoint = new IPEndPoint(assocIP, assocPort);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                assoc.Close();
            }
        }

        public async Task<(byte[], IPEndPoint, IPAddress)> RecieveAsync(byte[] bytes, IPEndPoint remote, EndPoint receive)
        {
            TcpState state = assoc.GetState();
            if (state != TcpState.Established)
                throw new InvalidOperationException("No UDP association, maybe already disconnected or not connected");

            byte[] remoteBytes = GetEndPointByte(remote);
            byte[] proxyBytes = new byte[bytes.Length + remoteBytes.Length + 3];
            Array.Copy(remoteBytes, 0, proxyBytes, 3, remoteBytes.Length);
            Array.Copy(bytes, 0, proxyBytes, remoteBytes.Length + 3, bytes.Length);

            await UdpClient.SendAsync(proxyBytes, proxyBytes.Length, assocEndPoint);
            var res = new byte[ushort.MaxValue];
            var flag = SocketFlags.None;
            EndPoint ep = new IPEndPoint(0, 0);
            var length = UdpClient.Client.ReceiveMessageFrom(res, 0, res.Length, ref flag, ref ep, out var ipPacketInformation);

            if (res[0] != 0 || res[1] != 0 || res[2] != 0)
            {
                throw new Exception();
            }

            int addrLen;
            switch (res[3])
            {
                case 1:
                    addrLen = 4;
                    break;
                case 4:
                    addrLen = 16;
                    break;
                default:
                    throw new Exception();
            }

            byte[] ipbyte = new byte[addrLen];
            Array.Copy(res, 4, ipbyte, 0, addrLen);

            IPAddress ip = new IPAddress(ipbyte);
            int port = res[addrLen + 4] * 256 + res[addrLen + 5];
            byte[] ret = new byte[length - addrLen - 6];
            Array.Copy(res, addrLen + 6, ret, 0, length - addrLen - 6);
            return (
                ret,
                new IPEndPoint(ip, port),
                ipPacketInformation.Address);
        }

        public Task DisconnectAsync()
        {
            try
            {
                assoc.Close();
            }
            catch { }
            return Task.CompletedTask;
        }

        byte[] GetEndPointByte(IPEndPoint ep)
        {
            byte[] ipbyte = ep.Address.GetAddressBytes();
            byte[] ret = new byte[ipbyte.Length + 3];
            ret[0] = (byte)(ipbyte.Length == 4 ? 1 : 4);
            Array.Copy(ipbyte, 0, ret, 1, ipbyte.Length);
            ret[ipbyte.Length + 1] = (byte)(ep.Port / 256);
            ret[ipbyte.Length + 2] = (byte)(ep.Port % 256);
            return ret;
        }

        public void Dispose()
        {
            UdpClient?.Dispose();
            assoc?.Dispose();
        }
    }
}