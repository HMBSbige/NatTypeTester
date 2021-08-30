using Dns.Net.Clients;
using STUN;
using STUN.Client;
using System;
using System.Net;
using System.Threading;

//stun.qq.com:3478 0.0.0.0:0
var server = @"stun.syncthing.net";
ushort port = 3478;
var local = new IPEndPoint(IPAddress.Any, 0);

if (args.Length > 0 && StunServer.TryParse(args[0], out var stun))
{
	server = stun.Hostname;
	port = stun.Port;
}

if (args.Length > 1)
{
	if (IPEndPoint.TryParse(args[2], out var ipEndPoint))
	{
		local = ipEndPoint;
	}
}

var dnsClient = new DefaultDnsClient();
var ip = await dnsClient.QueryAsync(server);
using var client = new StunClient5389UDP(new IPEndPoint(ip, port), local);

using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(5));
await client.QueryAsync(cts.Token);

var res = client.State;

Console.WriteLine($@"Other address is {res.OtherEndPoint}");
Console.WriteLine($@"Binding test: {res.BindingTestResult}");
Console.WriteLine($@"Local address: {res.LocalEndPoint}");
Console.WriteLine($@"Mapped address: {res.PublicEndPoint}");
Console.WriteLine($@"Nat mapping behavior: {res.MappingBehavior}");
Console.WriteLine($@"Nat filtering behavior: {res.FilteringBehavior}");
