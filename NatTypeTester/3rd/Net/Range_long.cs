using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net
{
    /// <summary>
    /// This class represent 2-point <b>long</b> value range.
    /// </summary>
    public class Range_long
    {
        private long m_Start = 0;
        private long m_End   = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">Start/End value.</param>
        public Range_long(long value)
        {
            m_Start = value;
            m_End   = value;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="start">Range start value.</param>
        /// <param name="end">Range end value.</param>
        public Range_long(long start,long end)
        {
            m_Start = start;
            m_End   = end;
        }


        #region method Contains

        /// <summary>
        /// Gets if the specified value is within range.
        /// </summary>
        /// <param name="value">Value to check.</param>
        /// <returns>Returns true if specified value is within range, otherwise false.</returns>
        public bool Contains(long value)
        {
            if(value >= m_Start && value <= m_End){
                return true;
            }

            return false;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets range start.
        /// </summary>
        public long Start
        {
            get{ return m_Start; }
        }

        /// <summary>
        /// Gets range end.
        /// </summary>
        public long End
        {
            get{ return m_End; }
        }

        #endregion
    }
}
