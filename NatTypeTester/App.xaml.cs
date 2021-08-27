using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Splat.Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using Volo.Abp;

#pragma warning disable VSTHRD100 // 避免使用 Async Void 方法
namespace NatTypeTester
{
	public partial class App
	{
		private readonly IHost _host;
		private readonly IAbpApplicationWithExternalServiceProvider _application;

		public App()
		{
			_host = CreateHostBuilder();
			_application = _host.Services.GetRequiredService<IAbpApplicationWithExternalServiceProvider>();
		}

		protected override async void OnStartup(StartupEventArgs e)
		{
			try
			{
				await _host.StartAsync();
				Initialize(_host.Services);
				_host.Services.GetRequiredService<MainWindow>().Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, nameof(NatTypeTester), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		protected override async void OnExit(ExitEventArgs e)
		{
			_application.Shutdown();
			await _host.StopAsync();
			_host.Dispose();
		}

		private void Initialize(IServiceProvider serviceProvider)
		{
			_application.Initialize(serviceProvider);
			serviceProvider.UseMicrosoftDependencyResolver();
		}

		private static IHost CreateHostBuilder()
		{
			return Host.CreateDefaultBuilder()
					.UseAutofac()
					.ConfigureServices((_, services) => services.AddApplication<NatTypeTesterModule>())
					.Build();
		}
	}
}
