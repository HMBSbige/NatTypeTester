namespace NatTypeTester.Extensions;

public class ViewModelLocator : MarkupExtension
{
	public required Type Type { get; set; }

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return Locator.Current.GetService(Type)!;
	}
}
