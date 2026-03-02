namespace NatTypeTester.Views.DesignViewModels;

public class DesignSettingsViewModel : SettingsViewModel
{
	public DesignSettingsViewModel()
	{
		if (!Design.IsDesignMode)
		{
			throw new InvalidOperationException();
		}

		TransientCachedServiceProvider = AppLocator.Current.GetService<ITransientCachedServiceProvider>()!;
	}
}
