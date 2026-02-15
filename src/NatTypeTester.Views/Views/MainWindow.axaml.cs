namespace NatTypeTester.Views.Views;

[UsedImplicitly]
public partial class MainWindow : ReactiveWindow<MainWindowViewModel>, ISingletonDependency
{
	public MainWindow()
	{
		InitializeComponent();
	}
}
