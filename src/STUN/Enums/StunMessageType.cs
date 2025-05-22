namespace STUN.Enums;

/// <summary>
/// This enum specifies STUN message type.
/// </summary>
/// <returns>
/// https://tools.ietf.org/html/rfc5389#section-6
/// </returns>
public enum StunMessageType : ushort
{
	/// <summary>
	/// STUN message is binding request.
	/// </summary>
	BindingRequest = Class.Request | Method.Binding,

	/// <summary>
	/// STUN message is binding request success response.
	/// </summary>
	BindingResponse = Class.SuccessResponse | Method.Binding,

	/// <summary>
	/// STUN message is binding request error response.
	/// </summary>
	BindingErrorResponse = Class.ErrorResponse | Method.Binding,

	/// <summary>
	/// STUN message is "shared secret" request.
	/// </summary>
	SharedSecretRequest = Class.Request | Method.SharedSecret,

	/// <summary>
	/// STUN message is "shared secret" request success response.
	/// </summary>
	SharedSecretResponse = Class.SuccessResponse | Method.SharedSecret,

	/// <summary>
	/// STUN message is "shared secret" request error response.
	/// </summary>
	SharedSecretErrorResponse = Class.ErrorResponse | Method.SharedSecret,
}
