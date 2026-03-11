namespace NatTypeTester.Views.ValueConverters;

public static class TransportTypeConverters
{
	public static readonly IValueConverter IsChecked = new TransportTypeToCheckedConverter();
	public static readonly FuncValueConverter<TransportType, bool> IsFilteringVisible = new(t => t is TransportType.Udp);

	private class TransportTypeToCheckedConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value is TransportType v && parameter is TransportType p && v == p;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value is true && parameter is TransportType p ? p : BindingOperations.DoNothing;
		}
	}
}
