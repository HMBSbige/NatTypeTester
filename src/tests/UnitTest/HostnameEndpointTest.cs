using Shouldly;
using STUN;

namespace UnitTest;

public class HostnameEndpointTest
{
	[Theory]
	[InlineData(@"www.google.com", ushort.MinValue)]
	[InlineData(@"1.1.1.1", (ushort)1)]
	[InlineData(@"[2001:db8:1234:5678:11:2233:4455:6677]", (ushort)1919)]
	public void IsTrue(string host, ushort port)
	{
		string str = $@"{host}:{port}";
		StunServer.TryParse(str, out StunServer? server).ShouldBeTrue();
		server.ShouldNotBeNull();
		server.Hostname.ShouldBe(host);
		server.Port.ShouldBe(port);
		server.ToString().ShouldBe(str);
	}

	[Theory]
	[InlineData(@"")]
	[InlineData(@"www.google.com:114514")]
	[InlineData(@"/dw.[/[:114")]
	[InlineData(@"2001:db8:1234:5678:11:2233:4455:6677:65535")]
	public void IsFalse(string str)
	{
		StunServer.TryParse(str, out StunServer? server).ShouldBeFalse();
		server.ShouldBeNull();
	}

	[Theory]
	[InlineData(@"www.google.com")]
	[InlineData(@"1.1.1.1")]
	[InlineData(@"2001:db8:1234:5678:11:2233:4455:6677")]
	[InlineData(@"[2001:db8:1234:5678:11:2233:4455:6677]")]
	[InlineData(@"2001:db8:1234:5678:11:2233:4455:db8")]
	public void TestDefaultPort(string str)
	{
		StunServer.TryParse(str, out StunServer? server).ShouldBeTrue();
		server.ShouldNotBeNull();
		server.Hostname.ShouldBe(str);
		server.Port.ShouldBe((ushort)3478);
	}

	[Theory]
	[InlineData(@"stun.syncthing.net:114", @"stun.syncthing.net:114")]
	[InlineData(@"stun.syncthing.net:3478", @"stun.syncthing.net")]
	[InlineData(@"[2001:db8:1234:5678:11:2233:4455:6677]", @"[2001:db8:1234:5678:11:2233:4455:6677]")]
	[InlineData(@"[2001:db8:1234:5678:11:2233:4455:6677]:3478", @"[2001:db8:1234:5678:11:2233:4455:6677]")]
	[InlineData(@"1.1.1.1:3478", @"1.1.1.1")]
	[InlineData(@"1.1.1.1:1919", @"1.1.1.1:1919")]
	public void TestToString(string str, string expected)
	{
		StunServer.TryParse(str, out StunServer? server).ShouldBeTrue();
		server.ShouldNotBeNull();
		server.ToString().ShouldBe(expected);
	}

	[Fact]
	public void DefaultServer()
	{
		StunServer server = new();
		server.Hostname.ShouldBe("stun.syncthing.net");
		server.Port.ShouldBe((ushort)3478);
	}

	[Theory]
	[InlineData(@"stun.syncthing.net:114", @"stun.syncthing.net:114")]
	[InlineData(@"stun.syncthing.net:3478", @"stun.syncthing.net:3478")]
	[InlineData(@"[2001:db8:1234:5678:11:2233:4455:6677]", @"[2001:db8:1234:5678:11:2233:4455:6677]:0")]
	[InlineData(@"[2001:db8:1234:5678:11:2233:4455:6677]:3478", @"[2001:db8:1234:5678:11:2233:4455:6677]:3478")]
	[InlineData(@"1.1.1.1:3478", @"1.1.1.1:3478")]
	[InlineData(@"1.1.1.1:1919", @"1.1.1.1:1919")]
	public void HostnameEndpointToString(string str, string expected)
	{
		HostnameEndpoint.TryParse(str, out HostnameEndpoint? server).ShouldBeTrue();
		server.ShouldNotBeNull();
		server.ToString().ShouldBe(expected);
	}
}
