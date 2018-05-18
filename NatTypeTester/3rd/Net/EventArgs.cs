using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net
{
    /// <summary>
    /// This class universal event arguments for transporting single value.
    /// </summary>
    /// <typeparam name="T">Event data.</typeparam>
    public class EventArgs<T> : EventArgs
    {
        private T m_pValue;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">Event data.</param>
        public EventArgs(T value)
        {
            m_pValue = value;
        }


        #region Properties implementation

        /// <summary>
        /// Gets event data.
        /// </summary>
        public T Value
        {
            get{ return m_pValue; }
        }

        #endregion

    }
}
