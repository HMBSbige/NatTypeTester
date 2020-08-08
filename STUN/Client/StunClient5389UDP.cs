using STUN.Enums;
using STUN.Interfaces;
using STUN.Message;
using STUN.StunResult;
using STUN.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace STUN.Client
{
    public class StunClient5389UDP : StunClient3489
    {
        public StunClient5389UDP(string server, ushort port = 3478, IPEndPoint local = null, IDnsQuery dnsQuery = null)
        : base(server, port, local, dnsQuery)
        {
            Timeout = TimeSpan.FromSeconds(3);
        }

        public override IStunResult Query()
        {
            throw new NotImplementedException();
        }

        public override async Task<IStunResult> QueryAsync()
        {
            return await BindingTestAsync();
        }

        public async Task<StunResult5389> BindingTestAsync()
        {
            BindingTestResult res;

            var test = new StunMessage5389 { StunMessageType = StunMessageType.BindingRequest };
            var (response1, _, local1) = await TestAsync(test, RemoteEndPoint, RemoteEndPoint);
            var mappedAddress1 = AttributeExtensions.GetXorMappedAddressAttribute(response1);

            if (response1 == null)
            {
                res = BindingTestResult.Fail;
            }
            else if (mappedAddress1 == null)
            {
                res = BindingTestResult.UnsupportedServer;
            }
            else
            {
                res = BindingTestResult.Success;
            }

            return new StunResult5389
            {
                BindingTestResult = res,
                LocalEndPoint = local1 == null ? null : new IPEndPoint(local1, LocalEndPoint.Port),
                PublicEndPoint = mappedAddress1
            };
        }

        private async Task<(StunMessage5389, IPEndPoint, IPAddress)> TestAsync(StunMessage5389 sendMessage, IPEndPoint remote, IPEndPoint receive)
        {
            try
            {
                var b1 = sendMessage.Bytes.ToArray();
                //var t = DateTime.Now;

                // Simple retransmissions
                //https://tools.ietf.org/html/rfc5389#section-7.2.1
                //while (t + TimeSpan.FromSeconds(6) > DateTime.Now)
                {
                    try
                    {

                        var (receive1, ipe, local) = await UdpClient.UdpReceiveAsync(b1, remote, receive);

                        var message = new StunMessage5389();
                        if (message.TryParse(receive1) &&
                            message.ClassicTransactionId.IsEqual(sendMessage.ClassicTransactionId))
                        {
                            return (message, ipe, local);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return (null, null, null);
        }
    }
}
