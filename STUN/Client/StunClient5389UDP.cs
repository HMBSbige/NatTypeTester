using STUN.Enums;
using STUN.Interfaces;
using STUN.Message;
using STUN.StunResult;
using STUN.Utils;
using System;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace STUN.Client
{
    /// <summary>
    /// https://tools.ietf.org/html/rfc5389#section-7.2.1
    /// https://tools.ietf.org/html/rfc5780#section-4.2
    /// </summary>
    public class StunClient5389UDP : StunClient3489
    {
        #region Subject

        private readonly Subject<BindingTestResult> _bindingSubj = new Subject<BindingTestResult>();
        public IObservable<BindingTestResult> BindingTestResultChanged => _bindingSubj.AsObservable();

        private readonly Subject<MappingBehavior> _mappingBehaviorSubj = new Subject<MappingBehavior>();
        public IObservable<MappingBehavior> MappingBehaviorChanged => _mappingBehaviorSubj.AsObservable();

        private readonly Subject<FilteringBehavior> _filteringBehaviorSubj = new Subject<FilteringBehavior>();
        public IObservable<FilteringBehavior> FilteringBehaviorChanged => _filteringBehaviorSubj.AsObservable();

        #endregion

        public StunClient5389UDP(string server, ushort port = 3478, IPEndPoint local = null, IUdpProxy proxy = null, IDnsQuery dnsQuery = null)
        : base(server, port, local, proxy, dnsQuery)
        {
            Timeout = TimeSpan.FromSeconds(3);
        }

        public async Task<StunResult5389> QueryAsync()
        {
            var result = new StunResult5389();
            try
            {
                _bindingSubj.OnNext(result.BindingTestResult);
                _mappingBehaviorSubj.OnNext(result.MappingBehavior);
                _filteringBehaviorSubj.OnNext(result.FilteringBehavior);
                PubSubj.OnNext(result.PublicEndPoint);

                await Proxy.ConnectAsync();

                result = await FilteringBehaviorTestBaseAsync();
                if (result.BindingTestResult != BindingTestResult.Success
                || result.FilteringBehavior == FilteringBehavior.UnsupportedServer
                )
                {
                    return result;
                }

                if (Equals(result.PublicEndPoint, result.LocalEndPoint))
                {
                    result.MappingBehavior = MappingBehavior.Direct;
                    return result;
                }

                // MappingBehaviorTest test II
                var result2 = await BindingTestBaseAsync(new IPEndPoint(result.OtherEndPoint.Address, RemoteEndPoint.Port), false);
                if (result2.BindingTestResult != BindingTestResult.Success)
                {
                    result.MappingBehavior = MappingBehavior.Fail;
                    return result;
                }

                if (Equals(result2.PublicEndPoint, result.PublicEndPoint))
                {
                    result.MappingBehavior = MappingBehavior.EndpointIndependent;
                    return result;
                }

                // MappingBehaviorTest test III
                var result3 = await BindingTestBaseAsync(result.OtherEndPoint, false);
                if (result3.BindingTestResult != BindingTestResult.Success)
                {
                    result.MappingBehavior = MappingBehavior.Fail;
                    return result;
                }

                result.MappingBehavior = Equals(result3.PublicEndPoint, result2.PublicEndPoint) ? MappingBehavior.AddressDependent : MappingBehavior.AddressAndPortDependent;

                return result;
            }
            finally
            {
                _mappingBehaviorSubj.OnNext(result.MappingBehavior);
                await Proxy.DisconnectAsync();
            }
        }

        public async Task<StunResult5389> BindingTestAsync()
        {
            try
            {
                await Proxy.ConnectAsync();
                var result = await BindingTestBaseAsync(RemoteEndPoint, true);
                return result;
            }
            finally
            {
                await Proxy.DisconnectAsync();
            }
        }

        private async Task<StunResult5389> BindingTestBaseAsync(IPEndPoint remote, bool notifyChanged)
        {
            BindingTestResult res;
            var test = new StunMessage5389 { StunMessageType = StunMessageType.BindingRequest };
            var (response1, _, local1) = await TestAsync(test, remote, remote);
            var mappedAddress1 = AttributeExtensions.GetXorMappedAddressAttribute(response1);
            var otherAddress = AttributeExtensions.GetOtherAddressAttribute(response1);
            var local = local1 == null ? null : new IPEndPoint(local1, LocalEndPoint.Port);

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

            if (notifyChanged)
            {
                _bindingSubj.OnNext(res);
                PubSubj.OnNext(mappedAddress1);
            }
            LocalSubj.OnNext(LocalEndPoint);

            return new StunResult5389
            {
                BindingTestResult = res,
                LocalEndPoint = local,
                PublicEndPoint = mappedAddress1,
                OtherEndPoint = otherAddress
            };
        }

        public async Task<StunResult5389> MappingBehaviorTestAsync()
        {
            var result = new StunResult5389();
            try
            {
                await Proxy.ConnectAsync();
                // test I
                result = await BindingTestBaseAsync(RemoteEndPoint, true);
                if (result.BindingTestResult != BindingTestResult.Success)
                {
                    return result;
                }

                if (result.OtherEndPoint == null
                    || Equals(result.OtherEndPoint.Address, RemoteEndPoint.Address)
                    || result.OtherEndPoint.Port == RemoteEndPoint.Port)
                {
                    result.MappingBehavior = MappingBehavior.UnsupportedServer;
                    return result;
                }

                if (Equals(result.PublicEndPoint, result.LocalEndPoint))
                {
                    result.MappingBehavior = MappingBehavior.Direct;
                    return result;
                }

                // test II
                var result2 = await BindingTestBaseAsync(new IPEndPoint(result.OtherEndPoint.Address, RemoteEndPoint.Port), false);
                if (result2.BindingTestResult != BindingTestResult.Success)
                {
                    result.MappingBehavior = MappingBehavior.Fail;
                    return result;
                }

                if (Equals(result2.PublicEndPoint, result.PublicEndPoint))
                {
                    result.MappingBehavior = MappingBehavior.EndpointIndependent;
                    return result;
                }

                // test III
                var result3 = await BindingTestBaseAsync(result.OtherEndPoint, false);
                if (result3.BindingTestResult != BindingTestResult.Success)
                {
                    result.MappingBehavior = MappingBehavior.Fail;
                    return result;
                }

                result.MappingBehavior = Equals(result3.PublicEndPoint, result2.PublicEndPoint) ? MappingBehavior.AddressDependent : MappingBehavior.AddressAndPortDependent;

                return result;
            }
            finally
            {
                _mappingBehaviorSubj.OnNext(result.MappingBehavior);
                await Proxy.DisconnectAsync();
            }
        }

        private async Task<StunResult5389> FilteringBehaviorTestBaseAsync()
        {
            // test I
            var result1 = await BindingTestBaseAsync(RemoteEndPoint, true);
            try
            {
                if (result1.BindingTestResult != BindingTestResult.Success)
                {
                    return result1;
                }

                if (result1.OtherEndPoint == null
                    || Equals(result1.OtherEndPoint.Address, RemoteEndPoint.Address)
                    || result1.OtherEndPoint.Port == RemoteEndPoint.Port)
                {
                    result1.FilteringBehavior = FilteringBehavior.UnsupportedServer;
                    return result1;
                }

                // test II
                var test2 = new StunMessage5389
                {
                    StunMessageType = StunMessageType.BindingRequest,
                    Attributes = new[] { AttributeExtensions.BuildChangeRequest(true, true) }
                };
                var (response2, _, _) = await TestAsync(test2, RemoteEndPoint, result1.OtherEndPoint);

                if (response2 != null)
                {
                    result1.FilteringBehavior = FilteringBehavior.EndpointIndependent;
                    return result1;
                }

                // test III
                var test3 = new StunMessage5389
                {
                    StunMessageType = StunMessageType.BindingRequest,
                    Attributes = new[] { AttributeExtensions.BuildChangeRequest(false, true) }
                };
                var (response3, remote3, _) = await TestAsync(test3, RemoteEndPoint, RemoteEndPoint);

                if (response3 == null)
                {
                    result1.FilteringBehavior = FilteringBehavior.AddressAndPortDependent;
                    return result1;
                }

                if (Equals(remote3.Address, RemoteEndPoint.Address) && remote3.Port != RemoteEndPoint.Port)
                {
                    result1.FilteringBehavior = FilteringBehavior.AddressAndPortDependent;
                }
                else
                {
                    result1.FilteringBehavior = FilteringBehavior.UnsupportedServer;
                }
                return result1;
            }
            finally
            {
                _filteringBehaviorSubj.OnNext(result1.FilteringBehavior);
            }
        }

        public async Task<StunResult5389> FilteringBehaviorTestAsync()
        {
            try
            {
                await Proxy.ConnectAsync();
                var result = await FilteringBehaviorTestBaseAsync();
                return result;
            }
            finally
            {
                await Proxy.DisconnectAsync();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _bindingSubj.OnCompleted();
            _mappingBehaviorSubj.OnCompleted();
            _filteringBehaviorSubj.OnCompleted();
        }
    }
}
