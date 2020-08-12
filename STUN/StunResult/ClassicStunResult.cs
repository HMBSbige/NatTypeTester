using System.Net;
using ReactiveUI;
using STUN.Enums;

namespace STUN.StunResult
{
    public class ClassicStunResult : ReactiveObject
    {
        private NatType _natType = NatType.Unknown;
        private IPEndPoint _publicEndPoint;

        public NatType NatType
        {
            get => _natType;
            set => this.RaiseAndSetIfChanged(ref _natType, value);
        }

        public IPEndPoint PublicEndPoint
        {
            get => _publicEndPoint;
            set => this.RaiseAndSetIfChanged(ref _publicEndPoint, value);
        }
    }
}
