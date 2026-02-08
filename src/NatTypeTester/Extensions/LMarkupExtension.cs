using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace NatTypeTester.Extensions;

public class L : MarkupExtension
{
	public L(string key)
	{
		Key = key;
	}

	public string Key { get; }

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return new ObservableStringLocalizer(Key).ToBinding();
	}
}
