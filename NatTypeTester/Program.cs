using System;
using System.Reflection;
using System.Windows.Forms;

namespace NatTypeTester
{
	static class Program
	{
		/// <summary>
		/// 应用程序的主入口点。
		/// </summary>
		[STAThread]
		static void Main()
		{
			AppDomain.CurrentDomain.AssemblyResolve += (sender, arg) =>
			{
				if (arg.Name.StartsWith(@"LumiSoft.Net"))
				{
					return Assembly.Load(Properties.Resources.LumiSoft_Net);
				}

				return null;
			};
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}
