namespace NatTypeTester.Views.DesignViewModels;

public class DesignRFC3489ViewModel : RFC3489ViewModel
{
	public DesignRFC3489ViewModel()
	{
		if (!Design.IsDesignMode)
		{
			throw new InvalidOperationException();
		}

		TransientCachedServiceProvider = AppLocator.Current.GetService<ITransientCachedServiceProvider>()!;
	}
}
