using Dns.Net.Clients;
using STUN;
using STUN.Client;
using STUN.StunResult;
using System.Net;

//stun.qq.com:3478 0.0.0.0:0
string server = @"stun.syncthing.net";
ushort port = 3478;
IPEndPoint local = new(IPAddress.Any, 0);

if (args.Length > 0 && StunServer.TryParse(args[0], out StunServer? stun))
{
	server = stun.Hostname;
	port = stun.Port;
}

if (args.Length > 1)
{
	if (IPEndPoint.TryParse(args[2], out IPEndPoint? ipEndPoint))
	{
		local = ipEndPoint;
	}
}

DefaultDnsClient dnsClient = new();
IPAddress ip = await dnsClient.QueryAsync(server);
using StunClient5389UDP client = new(new IPEndPoint(ip, port), local);

using CancellationTokenSource cts = new();
cts.CancelAfter(TimeSpan.FromSeconds(5));
await client.QueryAsync(cts.Token);

StunResult5389 res = client.State;

Console.WriteLine($@"Other address is {res.OtherEndPoint}");
Console.WriteLine($@"Binding test: {res.BindingTestResult}");
Console.WriteLine($@"Local address: {res.LocalEndPoint}");
Console.WriteLine($@"Mapped address: {res.PublicEndPoint}");
Console.WriteLine($@"Nat mapping behavior: {res.MappingBehavior}");
Console.WriteLine($@"Nat filtering behavior: {res.FilteringBehavior}");
