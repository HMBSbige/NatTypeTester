namespace NatTypeTester.Views.ValueConverters;

public static class ProxyTypeConverters
{
	public static readonly IValueConverter PlainConverter = new ProxyTypeToCheckedConverter(ProxyType.Plain);
	public static readonly IValueConverter Socks5Converter = new ProxyTypeToCheckedConverter(ProxyType.Socks5);
	public static readonly IValueConverter IsSocks5Converter = new ProxyTypeToVisibilityConverter();

	private class ProxyTypeToCheckedConverter(ProxyType targetType) : IValueConverter
	{
		public object? Convert(object? value, Type targetType1, object? parameter, CultureInfo culture)
		{
			if (value is ProxyType proxyType)
			{
				return proxyType == targetType;
			}
			return false;
		}

		public object? ConvertBack(object? value, Type targetType1, object? parameter, CultureInfo culture)
		{
			if (value is true)
			{
				return targetType;
			}
			return BindingOperations.DoNothing;
		}
	}

	private class ProxyTypeToVisibilityConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is ProxyType proxyType)
			{
				return proxyType != ProxyType.Plain;
			}
			return false;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
