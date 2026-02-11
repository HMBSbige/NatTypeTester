namespace NatTypeTester.DesignViewModels;

public class DesignSettingsViewModel : SettingsViewModel
{
	public DesignSettingsViewModel()
	{
		if (!Design.IsDesignMode)
		{
			throw new InvalidOperationException();
		}

		TransientCachedServiceProvider = Locator.Current.GetService<ITransientCachedServiceProvider>()!;
	}
}
