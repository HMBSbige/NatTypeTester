namespace STUN.Enums;

/// <summary>
/// Represents the result of a STUN binding test.
/// </summary>
public enum BindingTestResult
{
	/// <summary>
	/// The test result is unknown or has not been determined.
	/// </summary>
	Unknown,

	/// <summary>
	/// The STUN server does not support the required features for the test.
	/// </summary>
	UnsupportedServer,

	/// <summary>
	/// The binding test completed successfully.
	/// </summary>
	Success,

	/// <summary>
	/// The binding test failed.
	/// </summary>
	Fail
}
