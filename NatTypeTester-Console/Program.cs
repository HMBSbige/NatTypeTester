using NatTypeTester.Net;
using System;

namespace NatTypeTester_Console
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			var server = @"stun.qq.com";
			ushort port = 3478;
			if (args.Length > 0)
			{
				server = args[0];
			}
			if (args.Length > 1)
			{
				ushort.TryParse(args[1], out port);
			}
			var res = NetUtils.NatTypeTestCore(NetUtils.DefaultLocalEnd, server, port);
			var natType = res.Item1;
			Console.WriteLine(string.IsNullOrWhiteSpace(natType) ? @"Error" : natType);
		}
	}
}
