using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.ViewModels;

public abstract class ViewModelBase : ReactiveObject, ISingletonDependency
{
	public required ITransientCachedServiceProvider TransientCachedServiceProvider { get; init; }

	protected IServiceProvider ServiceProvider => TransientCachedServiceProvider.GetRequiredService<IServiceProvider>();
}
