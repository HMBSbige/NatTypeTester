namespace UnitTest;

public static class TestEnvironment
{
	public static bool IsCI => bool.TryParse(Environment.GetEnvironmentVariable("CI"), out bool isCi) && isCi;

	public static bool IsFullCone => !IsCI && false;
}
