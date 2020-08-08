using STUN.Enums;
using STUN.Interfaces;
using System.Net;

namespace STUN.StunResult
{
    public class StunResult5389 : IStunResult
    {
        public IPEndPoint PublicEndPoint { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }
        public BindingTestResult BindingTestResult { get; set; } = BindingTestResult.Unknown;

    }
}
