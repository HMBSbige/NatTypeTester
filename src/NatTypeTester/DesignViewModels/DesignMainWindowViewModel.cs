using Avalonia.Controls;

namespace NatTypeTester.DesignViewModels;

public class DesignMainWindowViewModel : MainWindowViewModel
{
	public DesignMainWindowViewModel()
	{
		if (!Design.IsDesignMode)
		{
			throw new InvalidOperationException();
		}

		TransientCachedServiceProvider = Locator.Current.GetService<ITransientCachedServiceProvider>()!;
	}
}
