using Avalonia.Data.Converters;
using STUN.Enums;
using System.Globalization;

namespace NatTypeTester.ValueConverters;

public static class TransportTypeConverters
{
	public static readonly IValueConverter UdpConverter = new TransportTypeToCheckedConverter(TransportType.Udp);
	public static readonly IValueConverter TcpConverter = new TransportTypeToCheckedConverter(TransportType.Tcp);
	public static readonly IValueConverter TlsConverter = new TransportTypeToCheckedConverter(TransportType.Tls);

	private class TransportTypeToCheckedConverter(TransportType targetType) : IValueConverter
	{
		public object? Convert(object? value, Type targetType1, object? parameter, CultureInfo culture)
		{
			if (value is TransportType transportType)
			{
				return transportType == targetType;
			}
			return false;
		}

		public object? ConvertBack(object? value, Type targetType1, object? parameter, CultureInfo culture)
		{
			if (value is true)
			{
				return targetType;
			}
			return Avalonia.Data.BindingOperations.DoNothing;
		}
	}
}
