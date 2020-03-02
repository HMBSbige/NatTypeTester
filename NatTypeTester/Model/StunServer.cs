namespace NatTypeTester.Model
{
	public class StunServer
	{
		public string Hostname;
		public ushort Port;

		public StunServer()
		{
			Hostname = @"stun.qq.com";
			Port = 3478;
		}

		public bool Parse(string str)
		{
			var ipPort = str.Trim().Split(':', '：');
			if (ipPort.Length == 2)
			{
				if (!string.IsNullOrWhiteSpace(ipPort[0]) && ushort.TryParse(ipPort[1], out var port))
				{
					Hostname = ipPort[0];
					Port = port;
					return true;
				}
			}
			if (ipPort.Length == 1)
			{
				Hostname = ipPort[0];
				Port = 3478;
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			if (Port == 3478)
			{
				return Hostname;
			}
			return $@"{Hostname}:{Port}";
		}
	}
}
