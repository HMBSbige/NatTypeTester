namespace NatTypeTester.Extensions;

internal static class DIExtension
{
	public static T GetRequiredService<T>(this IReadonlyDependencyResolver resolver, string? contract = null) where T : notnull
	{
		Requires.NotNull(resolver);

		T? service = resolver.GetService<T>(contract);

		Verify.Operation(service is not null, $@"No service for type {typeof(T)} has been registered.");

		return service;
	}
}
