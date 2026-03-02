using STUN;

namespace UnitTest;

public class HostnameEndpointTest
{
	[Test]
	[Arguments(@"www.google.com", ushort.MinValue)]
	[Arguments(@"1.1.1.1", (ushort)1)]
	[Arguments(@"[2001:db8:1234:5678:11:2233:4455:6677]", (ushort)1919)]
	public async Task IsTrue(string host, ushort port)
	{
		string str = $@"{host}:{port}";
		await Assert.That(StunServer.TryParse(str, out StunServer? server)).IsTrue();
		await Assert.That(server).IsNotNull();
		await Assert.That(server.Hostname).IsEqualTo(host);
		await Assert.That(server.Port).IsEqualTo(port);
		await Assert.That(server.ToString()).IsEqualTo(str);
	}

	[Test]
	[Arguments(@"")]
	[Arguments(@"www.google.com:114514")]
	[Arguments(@"/dw.[/[:114")]
	[Arguments(@"2001:db8:1234:5678:11:2233:4455:6677:65535")]
	public async Task IsFalse(string str)
	{
		await Assert.That(StunServer.TryParse(str, out StunServer? server)).IsFalse();
		await Assert.That(server).IsNull();
	}

	[Test]
	[Arguments(@"www.google.com")]
	[Arguments(@"1.1.1.1")]
	[Arguments(@"2001:db8:1234:5678:11:2233:4455:6677")]
	[Arguments(@"[2001:db8:1234:5678:11:2233:4455:6677]")]
	[Arguments(@"2001:db8:1234:5678:11:2233:4455:db8")]
	public async Task TestDefaultPort(string str)
	{
		await Assert.That(StunServer.TryParse(str, out StunServer? server)).IsTrue();
		await Assert.That(server).IsNotNull();
		await Assert.That(server.Hostname).IsEqualTo(str);
		await Assert.That(server.Port).IsEqualTo((ushort)3478);
	}

	[Test]
	[Arguments(@"stun.syncthing.net:114", @"stun.syncthing.net:114")]
	[Arguments(@"stun.syncthing.net:3478", @"stun.syncthing.net")]
	[Arguments(@"[2001:db8:1234:5678:11:2233:4455:6677]", @"[2001:db8:1234:5678:11:2233:4455:6677]")]
	[Arguments(@"[2001:db8:1234:5678:11:2233:4455:6677]:3478", @"[2001:db8:1234:5678:11:2233:4455:6677]")]
	[Arguments(@"1.1.1.1:3478", @"1.1.1.1")]
	[Arguments(@"1.1.1.1:1919", @"1.1.1.1:1919")]
	public async Task TestToString(string str, string expected)
	{
		await Assert.That(StunServer.TryParse(str, out StunServer? server)).IsTrue();
		await Assert.That(server).IsNotNull();
		await Assert.That(server.ToString()).IsEqualTo(expected);
	}

	[Test]
	public async Task DefaultServer()
	{
		StunServer server = new();
		await Assert.That(server.Hostname).IsEqualTo("stun.syncthing.net");
		await Assert.That(server.Port).IsEqualTo((ushort)3478);
	}

	[Test]
	[Arguments(@"stun.syncthing.net:114", @"stun.syncthing.net:114")]
	[Arguments(@"stun.syncthing.net:3478", @"stun.syncthing.net:3478")]
	[Arguments(@"[2001:db8:1234:5678:11:2233:4455:6677]", @"[2001:db8:1234:5678:11:2233:4455:6677]:0")]
	[Arguments(@"[2001:db8:1234:5678:11:2233:4455:6677]:3478", @"[2001:db8:1234:5678:11:2233:4455:6677]:3478")]
	[Arguments(@"1.1.1.1:3478", @"1.1.1.1:3478")]
	[Arguments(@"1.1.1.1:1919", @"1.1.1.1:1919")]
	public async Task HostnameEndpointToString(string str, string expected)
	{
		await Assert.That(HostnameEndpoint.TryParse(str, out HostnameEndpoint? server)).IsTrue();
		await Assert.That(server).IsNotNull();
		await Assert.That(server.ToString()).IsEqualTo(expected);
	}
}
