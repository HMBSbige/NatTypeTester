namespace NatTypeTester.Net.STUN.Message
{
	/// <summary>
	/// This class implements STUN CHANGE-REQUEST attribute. Defined in RFC 3489 11.2.4.
	/// </summary>
	public class StunChangeRequest
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="changeIp">Specifies if STUN server must send response to different IP than request was received.</param>
		/// <param name="changePort">Specifies if STUN server must send response to different port than request was received.</param>
		public StunChangeRequest(bool changeIp, bool changePort)
		{
			ChangeIp = changeIp;
			ChangePort = changePort;
		}

		#region Properties Implementation

		/// <summary>
		/// Gets or sets if STUN server must send response to different IP than request was received.
		/// </summary>
		public bool ChangeIp { get; set; }

		/// <summary>
		/// Gets or sets if STUN server must send response to different port than request was received.
		/// </summary>
		public bool ChangePort { get; set; }

		#endregion

	}
}
