using STUN.Enums;
using STUN.Interfaces;
using System.Net;

namespace STUN.StunResult
{
    public class StunResult5389 : IStunResult
    {
        public IPEndPoint PublicEndPoint { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }
        public IPEndPoint OtherEndPoint { get; set; }
        public BindingTestResult BindingTestResult { get; set; } = BindingTestResult.Unknown;
        public MappingBehavior MappingBehavior { get; set; } = MappingBehavior.Unknown;
        public FilteringBehavior FilteringBehavior { get; set; } = FilteringBehavior.Unknown;
    }
}
