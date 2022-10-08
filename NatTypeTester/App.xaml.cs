using Microsoft.Extensions.DependencyInjection;
using Splat.Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Volo.Abp;

#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
namespace NatTypeTester;

public partial class App
{
	private readonly IAbpApplicationWithInternalServiceProvider _application;

	public App()
	{
		_application = AbpApplicationFactory.Create<NatTypeTesterModule>(options =>
		{
			options.UseAutofac();
		});
	}

	protected override async void OnStartup(StartupEventArgs e)
	{
		try
		{
			await _application.InitializeAsync();
			_application.ServiceProvider.UseMicrosoftDependencyResolver();
			_application.Services.GetRequiredService<MainWindow>().Show();
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, nameof(NatTypeTester), MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	protected override async void OnExit(ExitEventArgs e)
	{
		await _application.ShutdownAsync();
	}
}
