using DTLS.Common;
using DTLS.Dtls;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.StunResult;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using TUnit.Core.Enums;

namespace UnitTest;

public class DtlsProxyTest
{
	private const int MappedPort = 40000;

	/// <summary>
	/// Shorter than the DTLS library's default 15-second handshake timeout,
	/// so a test times out unless <see cref="IUdpStunClient.ReceiveTimeout"/> also bounds the handshake.
	/// </summary>
	private const int TestTimeout = 10_000;

	private static readonly IPEndPoint Local = new(IPAddress.Loopback, 0);

	private static readonly TimeSpan ShortReceiveTimeout = TimeSpan.FromMilliseconds(500);

	/// <summary>
	/// A second loopback address for OTHER-ADDRESS. macOS configures only 127.0.0.1 by default,
	/// so the address cannot be bound there and the tests that use it are excluded on macOS.
	/// </summary>
	private static readonly IPAddress OtherIp = IPAddress.Parse(@"127.0.0.2");

	private static IPEndPoint Mapped(int port)
	{
		return new IPEndPoint(IPAddress.Parse(@"203.0.113.5"), port);
	}

	private static DtlsProxy CreateProxy(bool skipCertificateValidation = true)
	{
		return new DtlsProxy(new NoneUdpProxy(Local), @"localhost", skipCertificateValidation);
	}

	/// <summary>
	/// Mapping test II goes to the IP of OTHER-ADDRESS and the port of the primary server, so the primary server and the test II target need the same port.
	/// </summary>
	private static (Socket Primary, Socket Test2) BindPrimaryAndTest2Sockets()
	{
		for (int attempt = 1; ; ++attempt)
		{
			Socket primary = UdpSockets.Bind(IPAddress.Loopback, 0);

			try
			{
				return (primary, UdpSockets.Bind(OtherIp, UdpSockets.GetLocalEndPoint(primary).Port));
			}
			catch (SocketException) when (attempt < 10)
			{
				primary.Dispose();
			}
			catch
			{
				primary.Dispose();
				throw;
			}
		}
	}

	[Test]
	[ExcludeOn(OS.MacOs)]
	[Arguments(DtlsVersion.Dtls12, MappedPort, MappedPort + 3, MappingBehavior.EndpointIndependent)]
	[Arguments(DtlsVersion.Dtls12, MappedPort + 2, MappedPort + 2, MappingBehavior.AddressDependent)]
	[Arguments(DtlsVersion.Dtls12, MappedPort + 2, MappedPort + 3, MappingBehavior.AddressAndPortDependent)]
	[Arguments(DtlsVersion.Dtls13, MappedPort, MappedPort + 3, MappingBehavior.EndpointIndependent)]
	[Arguments(DtlsVersion.Dtls13, MappedPort + 2, MappedPort + 2, MappingBehavior.AddressDependent)]
	[Arguments(DtlsVersion.Dtls13, MappedPort + 2, MappedPort + 3, MappingBehavior.AddressAndPortDependent)]
	public async Task MappingBehaviorTestSendsEachRequestToItsTargetAsync(DtlsVersion version, int test2MappedPort, int test3MappedPort, MappingBehavior expected, CancellationToken cancellationToken)
	{
		using X509Certificate2 certificate = DtlsStunServer.CreateCertificate();
		await using DtlsStunServer test3 = new(UdpSockets.Bind(OtherIp, 0), certificate, Mapped(test3MappedPort), maxVersion: version);
		(Socket primarySocket, Socket test2Socket) = BindPrimaryAndTest2Sockets();
		await using DtlsStunServer test2 = new(test2Socket, certificate, Mapped(test2MappedPort), maxVersion: version);
		await using DtlsStunServer primary = new(primarySocket, certificate, Mapped(MappedPort), test3.EndPoint, version);

		await using StunClient5389UDP client = new(primary.EndPoint, Local, CreateProxy());
		await client.ConnectProxyAsync(cancellationToken);

		try
		{
			await client.MappingBehaviorTestAsync(cancellationToken);
		}
		finally
		{
			await client.CloseProxyAsync(cancellationToken);
		}

		using (Assert.Multiple())
		{
			await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
			await Assert.That(client.State.PublicEndPoint).IsEqualTo(Mapped(MappedPort));
			await Assert.That(client.State.OtherEndPoint).IsEqualTo(test3.EndPoint);
			await Assert.That(client.State.MappingBehavior).IsEqualTo(expected);
			await Assert.That(primary.RequestCount).IsEqualTo(1);
			await Assert.That(test2.RequestCount).IsEqualTo(1);
			await Assert.That(test3.RequestCount).IsEqualTo(expected is MappingBehavior.EndpointIndependent ? 0 : 1);
		}
	}

	[Test]
	[Timeout(TestTimeout)]
	public async Task BindingTestFailsWithinReceiveTimeoutWhenServerIsSilentAsync(CancellationToken cancellationToken)
	{
		using Socket silentServer = UdpSockets.Bind(IPAddress.Loopback, 0);

		await using StunClient5389UDP client = new(UdpSockets.GetLocalEndPoint(silentServer), Local, CreateProxy()) { ReceiveTimeout = ShortReceiveTimeout };
		await client.ConnectProxyAsync(cancellationToken);

		StunResult5389 result = await client.BindingTestAsync(cancellationToken);

		using (Assert.Multiple())
		{
			await Assert.That(result.BindingTestResult).IsEqualTo(BindingTestResult.Fail);
			await Assert.That(silentServer.Available).IsGreaterThan(0);
			await Assert.That(async () => await client.CloseProxyAsync(cancellationToken)).ThrowsNothing();
		}
	}

	[Test]
	public async Task CertificateValidationFailureIsNotMaskedByCloseProxyAsync(CancellationToken cancellationToken)
	{
		using X509Certificate2 certificate = DtlsStunServer.CreateCertificate();
		await using DtlsStunServer server = new(UdpSockets.Bind(IPAddress.Loopback, 0), certificate, Mapped(MappedPort));

		// Validates the server certificate, which a self-signed certificate fails
		await using StunClient5389UDP client = new(server.EndPoint, Local, CreateProxy(skipCertificateValidation: false));
		await client.ConnectProxyAsync(cancellationToken);

		// Same order as Rfc5780AppService: run the test, then close the proxy in finally
		await Assert.That
		(
			async () =>
			{
				try
				{
					await client.BindingTestAsync(cancellationToken);
				}
				finally
				{
					await client.CloseProxyAsync(cancellationToken);
				}
			}
		).Throws<CertificateException>();
	}
}
