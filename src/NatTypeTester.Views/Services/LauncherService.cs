namespace NatTypeTester.Views.Services;

internal sealed class LauncherService : ILauncherService
{
	public async ValueTask LaunchUriAsync(Uri uri)
	{
		TopLevel? topLevel = TopLevelHelper.GetTopLevel();

		if (topLevel is null)
		{
			return;
		}

		await topLevel.Launcher.LaunchUriAsync(uri);
	}
}
