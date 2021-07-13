using JetBrains.Annotations;
using NatTypeTester.Models;
using ReactiveUI;

namespace NatTypeTester.ViewModels
{
	[UsedImplicitly]
	public class SettingViewModel : ViewModelBase, IRoutableViewModel
	{
		public string UrlPathSegment => @"Settings";
		public IScreen HostScreen { get; }

		public Config Config { get; }

		public SettingViewModel(IScreen hostScreen, Config config)
		{
			HostScreen = hostScreen;
			Config = config;
		}
	}
}
