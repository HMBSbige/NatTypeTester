using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net
{
    /// <summary>
    /// This class provides data for error events and methods.
    /// </summary>
    public class ExceptionEventArgs : EventArgs
    {
        private Exception m_pException = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>exception</b> is null reference value.</exception>
        public ExceptionEventArgs(Exception exception)
        {
            if(exception == null){
                throw new ArgumentNullException("exception");
            }

            m_pException = exception;
        }


        #region Properties implementation

        /// <summary>
        /// Gets exception.
        /// </summary>
        public Exception Exception
        {
            get{ return m_pException; }
        }

        #endregion

    }
}
