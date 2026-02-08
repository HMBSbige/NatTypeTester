namespace NatTypeTester.ViewModels;

public abstract class ViewModelBase : ReactiveObject
{
	public required ITransientCachedServiceProvider TransientCachedServiceProvider { get; [UsedImplicitly] init; }

	protected IServiceProvider ServiceProvider => TransientCachedServiceProvider.GetRequiredService<IServiceProvider>();

	protected IStringLocalizer L => TransientCachedServiceProvider.GetRequiredService<IStringLocalizer<NatTypeTesterResource>>();
}
