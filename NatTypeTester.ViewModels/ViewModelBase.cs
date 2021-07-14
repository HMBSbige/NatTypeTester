using ReactiveUI;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.ViewModels
{
	public abstract class ViewModelBase : ReactiveObject, ISingletonDependency
	{
		public IAbpLazyServiceProvider LazyServiceProvider { get; set; } = null!;
	}
}
