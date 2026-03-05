namespace NatTypeTester.Views.Infrastructure;

internal static class TopLevelHelper
{
	public static TopLevel? GetTopLevel()
	{
		return Avalonia.Application.Current?.ApplicationLifetime switch
		{
			IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
			ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView),
			_ => null
		};
	}
}
