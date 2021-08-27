using Microsoft.VisualStudio.TestTools.UnitTesting;
using STUN;

namespace UnitTest
{
	[TestClass]
	public class HostnameEndpointTest
	{
		[TestMethod]
		[DataRow(@"www.google.com", ushort.MinValue)]
		[DataRow(@"1.1.1.1", (ushort)1)]
		[DataRow(@"[2001:db8:1234:5678:11:2233:4455:6677]", (ushort)1919)]
		public void IsTrue(string host, ushort port)
		{
			var str = $@"{host}:{port}";
			Assert.IsTrue(StunServer.TryParse(str, out var server));
			Assert.IsNotNull(server);
			Assert.AreEqual(host, server.Hostname);
			Assert.AreEqual(port, server.Port);
			Assert.AreEqual(str, server.ToString());
		}

		[TestMethod]
		[DataRow(@"")]
		[DataRow(@"www.google.com:114514")]
		[DataRow(@"/dw.[/[:114")]
		[DataRow(@"2001:db8:1234:5678:11:2233:4455:6677:65535")]
		public void IsFalse(string str)
		{
			Assert.IsFalse(StunServer.TryParse(str, out var server));
			Assert.IsNull(server);
		}

		[TestMethod]
		[DataRow(@"www.google.com")]
		[DataRow(@"1.1.1.1")]
		[DataRow(@"2001:db8:1234:5678:11:2233:4455:6677")]
		[DataRow(@"[2001:db8:1234:5678:11:2233:4455:6677]")]
		[DataRow(@"2001:db8:1234:5678:11:2233:4455:db8")]
		public void TestDefaultPort(string str)
		{
			Assert.IsTrue(StunServer.TryParse(str, out var server));
			Assert.IsNotNull(server);
			Assert.AreEqual(str, server.Hostname);
			Assert.AreEqual(3478, server.Port);
		}

		[TestMethod]
		[DataRow(@"stun.syncthing.net:114", @"stun.syncthing.net:114")]
		[DataRow(@"stun.syncthing.net:3478", @"stun.syncthing.net")]
		[DataRow(@"[2001:db8:1234:5678:11:2233:4455:6677]", @"[2001:db8:1234:5678:11:2233:4455:6677]")]
		[DataRow(@"[2001:db8:1234:5678:11:2233:4455:6677]:3478", @"[2001:db8:1234:5678:11:2233:4455:6677]")]
		[DataRow(@"1.1.1.1:3478", @"1.1.1.1")]
		[DataRow(@"1.1.1.1:1919", @"1.1.1.1:1919")]
		public void ToString(string str, string expected)
		{
			Assert.IsTrue(StunServer.TryParse(str, out var server));
			Assert.IsNotNull(server);
			Assert.AreEqual(expected, server.ToString());
		}

		[TestMethod]
		public void DefaultServer()
		{
			var server = new StunServer();
			Assert.AreEqual(@"stun.syncthing.net", server.Hostname);
			Assert.AreEqual(3478, server.Port);
		}

		[TestMethod]
		[DataRow(@"stun.syncthing.net:114", @"stun.syncthing.net:114")]
		[DataRow(@"stun.syncthing.net:3478", @"stun.syncthing.net:3478")]
		[DataRow(@"[2001:db8:1234:5678:11:2233:4455:6677]", @"[2001:db8:1234:5678:11:2233:4455:6677]:0")]
		[DataRow(@"[2001:db8:1234:5678:11:2233:4455:6677]:3478", @"[2001:db8:1234:5678:11:2233:4455:6677]:3478")]
		[DataRow(@"1.1.1.1:3478", @"1.1.1.1:3478")]
		[DataRow(@"1.1.1.1:1919", @"1.1.1.1:1919")]
		public void HostnameEndpointToString(string str, string expected)
		{
			Assert.IsTrue(HostnameEndpoint.TryParse(str, out var server));
			Assert.IsNotNull(server);
			Assert.AreEqual(expected, server.ToString());
		}
	}
}
