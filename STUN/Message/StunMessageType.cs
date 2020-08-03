namespace STUN.Message
{
	/// <summary>
	/// This enum specifies STUN message type.
	/// </summary>
	public enum StunMessageType
	{
		/// <summary>
		/// STUN message is binding request.
		/// </summary>
		BindingRequest = 0x0001,

		/// <summary>
		/// STUN message is binding request response.
		/// </summary>
		BindingResponse = 0x0101,

		/// <summary>
		/// STUN message is binding request error response.
		/// </summary>
		BindingErrorResponse = 0x0111,

		/// <summary>
		/// STUN message is "shared secret" request.
		/// </summary>
		SharedSecretRequest = 0x0002,

		/// <summary>
		/// STUN message is "shared secret" request response.
		/// </summary>
		SharedSecretResponse = 0x0102,

		/// <summary>
		/// STUN message is "shared secret" request error response.
		/// </summary>
		SharedSecretErrorResponse = 0x0112
	}
}
