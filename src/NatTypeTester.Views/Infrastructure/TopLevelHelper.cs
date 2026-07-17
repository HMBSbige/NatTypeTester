namespace NatTypeTester.Views.Infrastructure;

internal static class TopLevelHelper
{
	private static WeakReference<Control>? _activityMainViewReference;

	public static Control RegisterActivityMainView(Control mainView)
	{
		_activityMainViewReference = new WeakReference<Control>(mainView);
		return mainView;
	}

	public static TopLevel? GetTopLevel()
	{
		return Avalonia.Application.Current?.ApplicationLifetime switch
		{
			IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
			IActivityApplicationLifetime => TopLevel.GetTopLevel(GetActivityMainView()),
			ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView),
			_ => null
		};
	}

	private static Control? GetActivityMainView()
	{
		return _activityMainViewReference is { } reference && reference.TryGetTarget(out Control? mainView) ? mainView : null;
	}
}
