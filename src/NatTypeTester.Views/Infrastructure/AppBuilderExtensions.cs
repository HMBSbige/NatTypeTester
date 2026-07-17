namespace NatTypeTester.Views.Infrastructure;

public static class AppBuilderExtensions
{
	public static AppBuilder UseNatTypeTesterApp(this AppBuilder builder)
	{
		return builder.UseReactiveUIWithMicrosoftDependencyResolver
		(
			static services => services.AddNatTypeTesterViews(),
			withResolver: static provider =>
			{
				ArgumentNullException.ThrowIfNull(provider);
				provider.GetRequiredService<NotificationExceptionHandler>().Install();
			},
			withReactiveUIBuilder: static rxBuilder =>
			{
				rxBuilder.WithExceptionHandler(NotificationExceptionHandler.ExceptionSubject);
			}
		);
	}
}
