using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NatTypeTester
{
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			foreach (var stunServer in StunServers)
			{
				ServersComboBox.Items.Add(stunServer);
			}

			ServersComboBox.SelectedIndex = 0;
			LocalEndTextBox.Text = Utils.Utils.DefaultLocalEnd;
		}

		private static readonly string[] StunServers =
		{
				@"stun.miwifi.com",
				@"stun.bige0.com",
				@"stun.syncthing.net",
				@"stun.stunprotocol.org",
				@"iphone-stun.strato-iphone.de",
				@"stun.voipstunt.com",
				@"stun.xten.com",
				@"stun.schlund.de",
				@"numb.viagenie.ca",
				@"stun.ekiga.net",
				@"stun.sipgate.net",
		};

		private void TestButton_OnClick(object sender, RoutedEventArgs e)
		{
			TestButton.IsEnabled = false;
			var server = ServersComboBox.Text;
			var port = PortNumber.NumValue;
			var local = LocalEndTextBox.Text;
			Task.Run(() =>
			{
				var (natType, localEnd, publicEnd) = Utils.Utils.NatTypeTestCore(local, server, port);

				Dispatcher.BeginInvoke(new Action(() =>
				{
					NatTypeTextBox.Text = natType;
					LocalEndTextBox.Text = localEnd;
					PublicEndTextBox.Text = publicEnd;
					TestButton.IsEnabled = true;
				}));
			});
		}

		private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				TestButton_OnClick(this, new RoutedEventArgs());
			}
		}
	}
}
