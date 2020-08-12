using STUN.Enums;
using System.Net;
using ReactiveUI;

namespace STUN.StunResult
{
    public class StunResult5389 : ReactiveObject
    {
        public IPEndPoint PublicEndPoint { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }
        public IPEndPoint OtherEndPoint { get; set; }
        public BindingTestResult BindingTestResult { get; set; } = BindingTestResult.Unknown;
        public MappingBehavior MappingBehavior { get; set; } = MappingBehavior.Unknown;
        public FilteringBehavior FilteringBehavior { get; set; } = FilteringBehavior.Unknown;
    }
}
