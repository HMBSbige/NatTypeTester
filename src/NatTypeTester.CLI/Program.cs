using NatTypeTester.Application;
using NatTypeTester.Application.Contracts;
using STUN.Enums;
using STUN.StunResult;
using System.CommandLine;
using System.CommandLine.Help;
using System.Text;

namespace NatTypeTester.CLI;

internal class Program
{
	static void Main(string[] args)
	{
		RootCommand cmd = new("NAT type tester");

		Argument<string> serverArg = new("server");
		Option<int> portOption = new("--port", "-p")
		{
			Description = "which port to use",
			DefaultValueFactory = (_) => 3478,
		};
		Option<bool> oldModeOption = new("--3489", "-3")
		{
			Description = "use RFC 3489 client",
			DefaultValueFactory = (_) => false,
		};
		Option<bool> ip6Mode = new("--ipv6", "-6")
		{
			Description = "prefer IPv6",
			DefaultValueFactory = (_) => false,
		};
		Option<string> proxyOption = new("--proxy", "-x")
		{
			Description = "proxy address",
		};
		Option<string> proxyUserOption = new("--proxy-user", "-U")
		{
			Description = "proxy user",
		};
		Option<bool> tcpOption = new("--tcp", "-t")
		{
			Description = "use TCP",
		};
		Option<bool> tlsOption = new("--tls", "-s")
		{
			Description = "use TLS",
		};
		Option<string> localAddrOption = new("--local", "-l")
		{
			Description = "local address",
		};

		cmd.Add(serverArg);
		cmd.Add(portOption);
		cmd.Add(oldModeOption);
		cmd.Add(proxyOption);
		cmd.Add(proxyUserOption);
		cmd.Add(tcpOption);
		cmd.Add(tlsOption);
		cmd.Add(ip6Mode);
		cmd.Add(localAddrOption);

		cmd.SetAction(async result =>
		{

			string server = result.GetValue(serverArg) ?? "";
			if (string.IsNullOrEmpty(server))
			{
				new HelpAction().Invoke(result);
				Environment.Exit(1);
			}

			string? proxy = result.GetValue(proxyOption);
			ProxyType proxyType = ProxyType.Plain;
			string? proxyUser = result.GetValue(proxyUserOption);
			string proxyPassword = "";
			if (!proxy.IsNullOrWhiteSpace())
			{
				proxyType = ProxyType.Socks5;
				if (!string.IsNullOrEmpty(proxyUser))
				{
					string[] sp = proxyUser.Split(':', 2);
					proxyUser = sp[0];
					if (sp.Length > 1)
					{
						proxyPassword = sp[1];
					}
					else
					{
						Console.Write("Password:");
						proxyPassword = ReadPassword();
					}
				}
			}

			bool preferV6 = result.GetValue(ip6Mode);
			string? localAddr = result.GetValue(localAddrOption);
			if (localAddr.IsNullOrEmpty())
			{
				localAddr = preferV6 ? "[::1]:0" : "0.0.0.0:0";
			}

			StunTestInput input = new()
			{
				StunServer = server,
				ProxyType = proxyType,
				ProxyServer = proxy,
				ProxyUser = proxyUser,
				ProxyPassword = proxyPassword,
				LocalEndPoint = localAddr,
			};

			bool tcp = result.GetValue(tcpOption);
			bool tls = result.GetValue(tlsOption);
			TransportType transport = TransportType.Udp;
			if (tcp)
			{
				transport = tls ? TransportType.Tls : TransportType.Tcp;
			}
			else
			{
				transport = tls ? TransportType.Dtls : TransportType.Udp;
			}

			// TODO: setup DI
			if (result.GetValue(oldModeOption))
			{
				Rfc3489AppService service3489 = new();
				ClassicStunResult result3489 = await service3489.TestAsync(input);
				Console.WriteLine(result3489);
			}

			Rfc5780AppService service5780 = new();
			StunResult5389 result5780 = await service5780.TestAsync(input, transport);
			Console.WriteLine(result5780);
		});

		ParseResult result = cmd.Parse(args);
		result.Invoke();
	}

	static string ReadPassword()
	{
		StringBuilder sb = new();
		ConsoleKeyInfo key;
		while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
		{
			if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
			{
				sb.Remove(sb.Length - 1, 1);
			}
			else if (!char.IsControl(key.KeyChar))
			{
				sb.Append(key.KeyChar);
			}
		}
		Console.Write(Environment.NewLine);
		return sb.ToString();
	}
}
