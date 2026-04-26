namespace NatTypeTester.Views.Infrastructure;

internal static class TopLevelHelper
{
	private static Control? _activityMainView;

	public static Control RegisterActivityMainView(Control mainView)
	{
		_activityMainView = mainView;
		return mainView;
	}

	public static TopLevel? GetTopLevel()
	{
		return Avalonia.Application.Current?.ApplicationLifetime switch
		{
			IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
			IActivityApplicationLifetime => TopLevel.GetTopLevel(_activityMainView),
			ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView),
			_ => null
		};
	}
}
