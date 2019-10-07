using System;

namespace NatTypeTester_Console
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			var res = Utils.NatTypeTestCore(Utils.DefaultLocalEnd, @"stun.miwifi.com", 3478);
			Console.WriteLine(res.Item1);
			Console.WriteLine(res.Item2);
			Console.WriteLine(res.Item3);
		}
	}
}
