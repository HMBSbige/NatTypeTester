using NatTypeTester.Models;
using ReactiveUI;

namespace NatTypeTester.ViewModels
{
	public class SettingViewModel : ReactiveObject, IRoutableViewModel
	{
		public string UrlPathSegment { get; } = @"Settings";
		public IScreen HostScreen { get; }

		public Config Config { get; }

		public SettingViewModel(IScreen hostScreen, Config config)
		{
			HostScreen = hostScreen;
			Config = config;
		}
	}
}
