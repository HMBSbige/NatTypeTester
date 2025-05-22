namespace NatTypeTester;

public partial class App
{
	private readonly IAbpApplicationWithInternalServiceProvider _application;

	public App()
	{
		InitializeComponent();
		_application = AbpApplicationFactory.Create<NatTypeTesterModule>(options => options.UseAutofac());
		_application.Initialize();
		_application.ServiceProvider.UseMicrosoftDependencyResolver();
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		_application.ServiceProvider.GetRequiredService<MainWindow>().Activate();
	}
}
