using STUN.Client.Enums;
using STUN.Client.Interfaces;
using System.Net;

namespace STUN.Client
{
    public class ClassicStunResult : IStunResult
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="natType">Specifies UDP network type.</param>
        /// <param name="publicEndPoint">Public IP end point.</param>
        public ClassicStunResult(NatType natType, IPEndPoint publicEndPoint)
        {
            NatType = natType;
            PublicEndPoint = publicEndPoint;
        }

        #region Properties Implementation

        /// <summary>
        /// Gets UDP network type.
        /// </summary>
        public NatType NatType { get; }

        /// <summary>
        /// Gets public IP end point. This value is null if failed to get network type.
        /// </summary>
        public IPEndPoint PublicEndPoint { get; }

        #endregion

    }
}
