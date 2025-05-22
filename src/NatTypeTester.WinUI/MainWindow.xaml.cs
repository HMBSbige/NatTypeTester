namespace NatTypeTester;

public sealed partial class MainWindow : ISingletonDependency
{
	public MainWindow()
	{
		InitializeComponent();

		Title = nameof(NatTypeTester);
		ExtendsContentIntoTitleBar = true;

		AppWindow.Resize(new SizeInt32(500, 560));
		AppWindow.SetIcon(@"Assets\icon.ico");

		// CenterScreen
		{
			DisplayArea displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
			int x = (displayArea.WorkArea.Width - AppWindow.Size.Width) / 2;
			int y = (displayArea.WorkArea.Height - AppWindow.Size.Height) / 2;
			AppWindow.Move(new PointInt32(x, y));
		}

		MainFrame.Navigate(typeof(MainPage));
	}
}
