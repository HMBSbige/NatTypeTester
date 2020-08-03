using System.Net;

namespace STUN.Client
{
	/// <summary>
	/// This class holds STUN_Client.Query method return data.
	/// </summary>
	public class StunResult
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="natType">Specifies UDP network type.</param>
		/// <param name="publicEndPoint">Public IP end point.</param>
		public StunResult(NatType natType, IPEndPoint publicEndPoint)
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
