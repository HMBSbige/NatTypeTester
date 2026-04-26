using Autofac.Extensions.DependencyInjection;
using ReactiveUI.Avalonia.Splat;

namespace NatTypeTester.Views.Infrastructure;

public static class AppBuilderExtensions
{
	public static AppBuilder UseNatTypeTesterApp(this AppBuilder builder)
	{
		return builder.UseReactiveUIWithAutofac
		(
			containerBuilder =>
			{
				ServiceCollection services = new();

				AbpApplicationFactory.Create<NatTypeTesterViewsModule>(services);

				containerBuilder.Populate(services);
			},
			withResolver: resolver =>
			{
				IServiceProvider serviceProvider = resolver.GetRequiredService<IServiceProvider>();
				resolver.GetRequiredService<IAbpApplicationWithExternalServiceProvider>().Initialize(serviceProvider);
			},
			withReactiveUIBuilder: rxBuilder =>
			{
				rxBuilder.WithExceptionHandler(NotificationExceptionHandler.ExceptionSubject);
			}
		);
	}
}
