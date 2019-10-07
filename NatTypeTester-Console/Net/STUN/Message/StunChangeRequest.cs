namespace NatTypeTester_Console.Net.STUN.Message
{
	/// <summary>
	/// This class implements STUN CHANGE-REQUEST attribute. Defined in RFC 3489 11.2.4.
	/// </summary>
	public class StunChangeRequest
	{
		private bool _mChangeIp = true;
		private bool _mChangePort = true;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public StunChangeRequest()
		{
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="changeIp">Specifies if STUN server must send response to different IP than request was received.</param>
		/// <param name="changePort">Specifies if STUN server must send response to different port than request was received.</param>
		public StunChangeRequest(bool changeIp, bool changePort)
		{
			_mChangeIp = changeIp;
			_mChangePort = changePort;
		}


		#region Properties Implementation

		/// <summary>
		/// Gets or sets if STUN server must send response to different IP than request was received.
		/// </summary>
		public bool ChangeIp
		{
			get => _mChangeIp;

			set => _mChangeIp = value;
		}

		/// <summary>
		/// Gets or sets if STUN server must send response to different port than request was received.
		/// </summary>
		public bool ChangePort
		{
			get => _mChangePort;

			set => _mChangePort = value;
		}

		#endregion

	}
}
