using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NatTypeTester.Models;
using ReactiveUI;

namespace NatTypeTester.ViewModels;

[UsedImplicitly]
public class SettingViewModel : ViewModelBase, IRoutableViewModel
{
	public string UrlPathSegment => @"Settings";

	public IScreen HostScreen => TransientCachedServiceProvider.GetRequiredService<IScreen>();

	public Config Config => TransientCachedServiceProvider.GetRequiredService<Config>();
}
