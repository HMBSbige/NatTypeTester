namespace NatTypeTester.Application.Contracts;

public sealed class StunTestInput : EntityDto, IValidatableObject
{
	public required string StunServer { get; init; }

	public ProxyType ProxyType { get; init; }

	public string ProxyServer { get; init; } = string.Empty;

	public string? ProxyUser { get; init; }

	public string? ProxyPassword { get; init; }

	public string? LocalEndPoint { get; init; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		IStringLocalizer<NatTypeTesterResource> l = validationContext.GetRequiredService<IStringLocalizer<NatTypeTesterResource>>();

		if (!STUN.StunServer.TryParse(StunServer, out _))
		{
			yield return new ValidationResult(l["WrongStunServer"], [nameof(StunServer)]);
		}

		if (!HostnameEndpoint.TryParse(ProxyServer, out _))
		{
			yield return new ValidationResult(l["UnknownProxyAddress"], [nameof(ProxyServer)]);
		}

		if (LocalEndPoint is not null && !IPEndPoint.TryParse(LocalEndPoint, out _))
		{
			yield return new ValidationResult(l["InvalidLocalEndPoint"], [nameof(LocalEndPoint)]);
		}
	}
}
