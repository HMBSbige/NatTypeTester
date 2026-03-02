namespace NatTypeTester.Views.DesignViewModels;

public class DesignMainWindowViewModel : MainWindowViewModel
{
	public DesignMainWindowViewModel()
	{
		if (!Design.IsDesignMode)
		{
			throw new InvalidOperationException();
		}

		TransientCachedServiceProvider = AppLocator.Current.GetService<ITransientCachedServiceProvider>()!;
	}
}
