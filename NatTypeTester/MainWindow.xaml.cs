using NatTypeTester.Model;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using STUN.Utils;

namespace NatTypeTester
{
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();
			LoadStunServer();
		}

		public static HashSet<string> StunServers { get; set; } = new HashSet<string>
		{
				@"stun.qq.com",
				@"stun.miwifi.com",
				@"stun.bige0.com",
				@"stun.syncthing.net",
				@"stun.stunprotocol.org"
		};

		private async void TestButton_OnClick(object sender, RoutedEventArgs e)
		{
			var stun = new StunServer();
			if (stun.Parse(ServersComboBox.Text))
			{
				var server = stun.Hostname;
				var port = stun.Port;
				var local = LocalEndTextBox.Text;
				TestButton.IsEnabled = false;
				await Task.Run(() =>
				{
					var (natType, localEnd, publicEnd) = NetUtils.NatTypeTestCore(local, server, port);

					Dispatcher?.InvokeAsync(() =>
					{
						NatTypeTextBox.Text = natType;
						LocalEndTextBox.Text = localEnd;
						PublicEndTextBox.Text = publicEnd;
						TestButton.IsEnabled = true;
					});
				});
			}
			else
			{
				MessageBox.Show(@"Wrong Stun server!", @"NatTypeTester", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				TestButton_OnClick(this, new RoutedEventArgs());
			}
		}

		private async void LoadStunServer()
		{
			const string path = @"stun.txt";
			if (File.Exists(path))
			{
				using var sw = new StreamReader(path);
				string line;
				var stun = new StunServer();
				while ((line = await sw.ReadLineAsync()) != null)
				{
					if (!string.IsNullOrWhiteSpace(line) && stun.Parse(line))
					{
						StunServers.Add(stun.ToString());
					}
				}
			}
		}
	}
}
