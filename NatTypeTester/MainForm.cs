using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using LumiSoft.Net.STUN.Client;

namespace NatTypeTester
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		private delegate void VoidMethodDelegate();

		private static string[] Core(string server, int port)
		{
			var socketv4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			try
			{
				if (string.IsNullOrWhiteSpace(server))
				{
					MessageBox.Show(@"Please specify STUN server !", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return null;
				}

				socketv4.Bind(new IPEndPoint(IPAddress.Any, 0));

				var result = STUN_Client.Query(server, port, socketv4);

				return new[]
				{
					result.NetType.ToString(),
					socketv4.LocalEndPoint.ToString(),
					result.NetType != STUN_NetType.UdpBlocked ? result.PublicEndPoint.ToString() : string.Empty
				};
			}
			catch (Exception ex)
			{
				MessageBox.Show($@"Error: {ex}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return null;
			}
			finally
			{
				socketv4.Close();
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			try
			{
				button1.Enabled = false;
				var t = new Task(() =>
				{
					BeginInvoke(new VoidMethodDelegate(() =>
					{
						var res = Core(comboBox1.Text, Convert.ToInt32(numericUpDown1.Value));
						if (res != null)
						{
							textBox2.Text = res[0];
							textBox3.Text = res[1];
							textBox4.Text = res[2];
						}
					}));
				});
				t.Start();
				t.ContinueWith(task =>
				{
					BeginInvoke(new VoidMethodDelegate(() =>
					{
						button1.Enabled = true;
					}));
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show($@"Error: {ex}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				button1.Enabled = true;
			}
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			comboBox1.SelectedIndex = 0;
		}
	}
}
