using ReactiveUI;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.ViewModels
{
	public abstract class ViewModelBase : ReactiveObject, ISingletonDependency
	{
	}
}
