namespace UnitTest;

public static class TestEnvironment
{
	public static bool IsRunningOnGitHubActions => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") is "true";

	public static bool IsFullCone => !IsRunningOnGitHubActions && false;
}
