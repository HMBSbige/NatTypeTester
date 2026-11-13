namespace UnitTest;

public static class TestEnvironment
{
	public const string StunServerHost = "stun.fitauto.ru";

	public static bool IsCI => bool.TryParse(Environment.GetEnvironmentVariable("CI"), out bool isCi) && isCi;
}
