namespace NatTypeTester.Views.Services;

[UsedImplicitly]
internal class LauncherService : ILauncherService, ISingletonDependency
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
