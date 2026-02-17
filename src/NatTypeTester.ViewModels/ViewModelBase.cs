namespace NatTypeTester.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IDisposable
{
	public required ITransientCachedServiceProvider TransientCachedServiceProvider { get; [UsedImplicitly] init; }

	protected IServiceProvider ServiceProvider => TransientCachedServiceProvider.GetRequiredService<IServiceProvider>();

	protected IStringLocalizer L => TransientCachedServiceProvider.GetRequiredService<IStringLocalizer<NatTypeTesterResource>>();

	protected CompositeDisposable Disposables { get; } = new();

	public virtual void Dispose()
	{
		Disposables.Dispose();
		GC.SuppressFinalize(this);
	}
}
