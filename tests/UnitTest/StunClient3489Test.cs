using Dns.Net.Clients;
using STUN;
using STUN.Client;
using STUN.Enums;
using System.Net;

namespace UnitTest;

public class StunClient3489Test
{
	private readonly DefaultAClient _dnsClient = new();

	private static readonly IPEndPoint Any = new(IPAddress.Any, 0);

	[Test]
	public async Task QueryAsync(CancellationToken cancellationToken)
	{
		Skip.When(TestEnvironment.IsCI, "Skipped on CI");

		IPAddress ip = await _dnsClient.QueryAsync(TestEnvironment.StunServerHost, cancellationToken);
		await using StunClient3489 client = new(new IPEndPoint(ip, StunServer.DefaultPort), Any);

		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.NatType).IsNotEqualTo(NatType.Unknown);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
	}
}
