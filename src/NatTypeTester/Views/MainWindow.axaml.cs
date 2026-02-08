namespace NatTypeTester.Views;

[UsedImplicitly]
public partial class MainWindow : ReactiveWindow<MainWindowViewModel>, ISingletonDependency
{
	public MainWindow()
	{
		InitializeComponent();
	}
}
