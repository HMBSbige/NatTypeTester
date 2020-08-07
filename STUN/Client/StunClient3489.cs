using STUN.Client.Enums;
using STUN.Client.Interfaces;
using STUN.Message;
using STUN.Message.Enums;
using STUN.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace STUN.Client
{
    /// <summary>
    /// https://tools.ietf.org/html/rfc3489#section-10.1
    /// https://upload.wikimedia.org/wikipedia/commons/6/63/STUN_Algorithm3.svg
    /// </summary>
    public class StunClient3489 : IStunClient
    {
        private readonly UdpClient _udpClient;
        public IPEndPoint LocalEndPoint => (IPEndPoint)_udpClient.Client.LocalEndPoint;

        private readonly string _server;
        private readonly ushort _port;

        public StunClient3489(string server, ushort port = 3478, IPEndPoint local = null)
        {
            if (string.IsNullOrEmpty(server))
            {
                throw new ArgumentException(@"Please specify STUN server !");
            }

            if (port < 1)
            {
                throw new ArgumentException(@"Port value must be >= 1 !");
            }

            _server = server;
            _port = port;

            _udpClient = local == null ? new UdpClient() : new UdpClient(local);

            _udpClient.Client.ReceiveTimeout = TimeSpan.FromSeconds(1.6).Milliseconds;
        }

        public IStunResult Query()
        {
            // test I
            var test1 = new StunMessage5389 { StunMessageType = StunMessageType.BindingRequest, MagicCookie = 0 };

            var (response1, remote1) = Test(test1);
            if (response1 == null)
            {
                return new ClassicStunResult(NatType.UdpBlocked, null);
            }
            var mappedAddress1 = AttributeExtensions.GetMappedAddressAttribute(response1);
            var changedAddress1 = AttributeExtensions.GetChangedAddressAttribute(response1);
            if (mappedAddress1 == null || changedAddress1 == null)
            {
                return new ClassicStunResult(NatType.UnsupportedServer, null);
            }

            var test2 = new StunMessage5389
            {
                StunMessageType = StunMessageType.BindingRequest,
                MagicCookie = 0,
                Attributes = new[] { AttributeExtensions.BuildChangeRequest(true, true) }
            };

            // test II
            var (response2, remote2) = Test(test2);
            var mappedAddress2 = AttributeExtensions.GetMappedAddressAttribute(response2);

            if (Equals(mappedAddress1, LocalEndPoint))
            {
                // No NAT
                if (response2 == null)
                {
                    return new ClassicStunResult(NatType.SymmetricUdpFirewall, mappedAddress1);
                }
                return new ClassicStunResult(NatType.OpenInternet, mappedAddress2);
            }

            // NAT
            if (response2 != null)
            {
                // 有些单 IP 服务器并不能测 NAT 类型，比如 Google 的
                var type = Equals(remote1.Address, remote2.Address) || Equals(remote1.Port, remote2.Port) ? NatType.UnsupportedServer : NatType.FullCone;
                return new ClassicStunResult(type, mappedAddress2);
            }

            // Test I(#2)
            var test12 = new StunMessage5389 { StunMessageType = StunMessageType.BindingRequest, MagicCookie = 0 };
            var (response12, _) = Test(test12, changedAddress1);
            var mappedAddress12 = AttributeExtensions.GetMappedAddressAttribute(response12);

            if (mappedAddress12 == null) return new ClassicStunResult(NatType.Unknown, null);

            if (!Equals(mappedAddress12, mappedAddress1))
            {
                return new ClassicStunResult(NatType.Symmetric, mappedAddress12);
            }

            // Test III
            var test3 = new StunMessage5389
            {
                StunMessageType = StunMessageType.BindingRequest,
                MagicCookie = 0,
                Attributes = new[] { AttributeExtensions.BuildChangeRequest(false, true) }
            };
            var (response3, _) = Test(test3, changedAddress1);
            var mappedAddress3 = AttributeExtensions.GetMappedAddressAttribute(response3);
            if (mappedAddress3 != null)
            {
                return new ClassicStunResult(NatType.RestrictedCone, mappedAddress3);
            }
            return new ClassicStunResult(NatType.PortRestrictedCone, mappedAddress12);
        }

        public IStunResult QueryAsync()
        {
            throw new NotImplementedException();
        }

        private (StunMessage5389, IPEndPoint) Test(StunMessage5389 sendMessage, IPEndPoint remote = null)
        {
            try
            {
                var b1 = sendMessage.Bytes.ToArray();
                var t = DateTime.Now;

                // Simple retransmissions
                //https://tools.ietf.org/html/rfc3489#section-9.3
                while (t + TimeSpan.FromSeconds(3) > DateTime.Now)
                {
                    try
                    {
                        if (remote == null)
                        {
                            Debug.WriteLine($@"{LocalEndPoint} => {_server}:{_port} {b1.Length} 字节");
                            _udpClient.Send(b1, b1.Length, _server, _port);
                        }
                        else
                        {
                            Debug.WriteLine($@"{LocalEndPoint} => {remote} {b1.Length} 字节");
                            _udpClient.Send(b1, b1.Length, remote);
                        }

                        IPEndPoint ipe = null;

                        var receive1 = _udpClient.Receive(ref ipe);

                        var message = new StunMessage5389();
                        if (message.TryParse(receive1) &&
                            message.ClassicTransactionId.IsEqual(sendMessage.ClassicTransactionId))
                        {
                            Debug.WriteLine($@"收到 {ipe} {receive1.Length} 字节");
                            return (message, ipe);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return (null, null);
        }

        public void Dispose()
        {
            _udpClient?.Dispose();
        }
    }
}
