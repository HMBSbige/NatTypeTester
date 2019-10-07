namespace NatTypeTester_Console.Net.STUN.Message
{
	/// <summary>
	/// This class implements STUN ERROR-CODE. Defined in RFC 3489 11.2.9.
	/// </summary>
	public class StunErrorCode
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="code">Error code.</param>
		/// <param name="reasonText">Reason text.</param>
		public StunErrorCode(int code, string reasonText)
		{
			Code = code;
			ReasonText = reasonText;
		}


		#region Properties Implementation

		/// <summary>
		/// Gets or sets error code.
		/// </summary>
		public int Code { get; set; }

		/// <summary>
		/// Gets reason text.
		/// </summary>
		public string ReasonText { get; set; }

		#endregion

	}
}
