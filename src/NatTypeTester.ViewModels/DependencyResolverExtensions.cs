namespace NatTypeTester.ViewModels;

public static class DependencyResolverExtensions
{
	extension(IReadonlyDependencyResolver resolver)
	{
		public T GetRequiredService<T>() where T : notnull
		{
			T? service = resolver.GetService<T>();
			return service ?? throw new InvalidOperationException($@"No service for type '{typeof(T)}' has been registered.");
		}

		internal StunTestInput CreateStunTestInput(string? localEndPoint)
		{
			StunServerSettingsViewModel stunServerSettings = resolver.GetRequiredService<StunServerSettingsViewModel>();
			ConnectionSettingsViewModel connectionSettings = resolver.GetRequiredService<ConnectionSettingsViewModel>();

			return new StunTestInput
			{
				StunServer = stunServerSettings.CurrentStunServer,
				Proxy = connectionSettings.CreateProxyOptions(),
				LocalEndPoint = localEndPoint,
				SkipCertificateValidation = connectionSettings.SkipCertificateValidation
			};
		}
	}
}
