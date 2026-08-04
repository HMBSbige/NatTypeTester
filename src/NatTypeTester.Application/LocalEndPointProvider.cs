using System.Net.NetworkInformation;

namespace NatTypeTester.Application;

internal sealed class LocalEndPointProvider : ILocalEndPointProvider
{
	public IReadOnlyList<string> GetLocalEndPoints()
	{
		HashSet<IPAddress> uniqueAddresses = [];
		return
		[
			ToEndPointString(IPAddress.Any),
			ToEndPointString(IPAddress.IPv6Any),
			.. GetLocalAddresses()
				.OrderBy(static address => address.AddressFamily is AddressFamily.InterNetwork ? 0 : 1)
				.ThenBy(static address => Convert.ToHexString(address.GetAddressBytes()))
				.Where(uniqueAddresses.Add)
				.Select(ToEndPointString)
		];

		static string ToEndPointString(IPAddress address)
		{
			return new IPEndPoint(address, IPEndPoint.MinPort).ToString();
		}

		static IReadOnlyList<IPAddress> GetLocalAddresses()
		{
			try
			{
				return
				[
					.. NetworkInterface.GetAllNetworkInterfaces()
						.Where(static networkInterface => networkInterface.OperationalStatus is OperationalStatus.Up)
						.SelectMany(static networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
						.Select(static address => address.Address)
						.Where(IsUsable)
				];
			}
			catch (Exception exception) when (exception is NetworkInformationException or PlatformNotSupportedException)
			{
				return [];
			}
		}

		static bool IsUsable(IPAddress address)
		{
			return address.AddressFamily switch
			{
				AddressFamily.InterNetwork => !address.Equals(IPAddress.Loopback) && address.GetAddressBytes() is not [169, 254, ..],
				AddressFamily.InterNetworkV6 => !address.Equals(IPAddress.IPv6Loopback) && !address.IsIPv6LinkLocal,
				_ => false
			};
		}
	}
}
