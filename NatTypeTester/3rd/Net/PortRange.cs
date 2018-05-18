using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net
{
    /// <summary>
    /// This class holds UDP or TCP port range.
    /// </summary>
    public class PortRange
    {
        private int m_Start = 1000;
        private int m_End   = 1100;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="start">Start port.</param>
        /// <param name="end">End port.</param>
        /// <exception cref="ArgumentOutOfRangeException">Is raised when any of the aruments value is out of range.</exception>
        public PortRange(int start,int end)
        {
            if(start < 1 || start > 0xFFFF){
                throw new ArgumentOutOfRangeException("Argument 'start' value must be > 0 and << 65 535.");
            }
            if(end < 1 || end > 0xFFFF){
                throw new ArgumentOutOfRangeException("Argument 'end' value must be > 0 and << 65 535.");
            }
            if(start > end){
                throw new ArgumentOutOfRangeException("Argumnet 'start' value must be >= argument 'end' value.");
            }

            m_Start = start;
            m_End   = end;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets start port.
        /// </summary>
        public int Start
        {
            get{ return m_Start; }
        }

        /// <summary>
        /// Gets end port.
        /// </summary>
        public int End
        {
            get{ return m_End; }
        }

        #endregion

    }
}
