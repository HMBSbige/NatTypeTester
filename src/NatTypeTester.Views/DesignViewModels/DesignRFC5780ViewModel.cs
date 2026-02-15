namespace NatTypeTester.Views.DesignViewModels;

public class DesignRFC5780ViewModel : RFC5780ViewModel
{
	public DesignRFC5780ViewModel()
	{
		if (!Design.IsDesignMode)
		{
			throw new InvalidOperationException();
		}

		TransientCachedServiceProvider = Locator.Current.GetService<ITransientCachedServiceProvider>()!;
	}
}
