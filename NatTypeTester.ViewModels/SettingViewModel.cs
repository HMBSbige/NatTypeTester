using JetBrains.Annotations;
using NatTypeTester.Models;
using ReactiveUI;

namespace NatTypeTester.ViewModels
{
	[UsedImplicitly]
	public class SettingViewModel : ViewModelBase, IRoutableViewModel
	{
		public string UrlPathSegment => @"Settings";
		public IScreen HostScreen => LazyServiceProvider.LazyGetRequiredService<IScreen>();

		public Config Config => LazyServiceProvider.LazyGetRequiredService<Config>();
	}
}
