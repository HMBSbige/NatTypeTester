using System;
using System.Diagnostics;

namespace LumiSoft.Net
{
	/// <summary>
	/// Provides data for the SysError event for servers.
	/// </summary>
	public class Error_EventArgs
	{
		private Exception  m_pException  = null;
		private StackTrace m_pStackTrace = null;
		private string     m_Text        = "";

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="stackTrace"></param>
		public Error_EventArgs(Exception x,StackTrace stackTrace)
		{
			m_pException  = x;
			m_pStackTrace = stackTrace;
		}


		#region Properties Implementaion

		/// <summary>
		/// Occured error's exception.
		/// </summary>
		public Exception Exception
		{
			get{ return m_pException; }
		}

		/// <summary>
		/// Occured error's stacktrace.
		/// </summary>
		public StackTrace StackTrace
		{
			get{ return m_pStackTrace; }
		}

		/// <summary>
		/// Gets comment text.
		/// </summary>
		public string Text
		{
			get{ return m_Text; }
		}

		#endregion

	}
}
