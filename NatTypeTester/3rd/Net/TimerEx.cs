using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace LumiSoft.Net
{
    /// <summary>
    /// Simple timer implementation.
    /// </summary>
    public class TimerEx : Timer
    {
        /// <summary>
        /// Default contructor.
        /// </summary>
        public TimerEx() : base()
        {
        }

        /// <summary>
        /// Default contructor.
        /// </summary>
        /// <param name="interval">The time in milliseconds between events.</param>
        public TimerEx(double interval) : base(interval)
        {
        }

        /// <summary>
        /// Default contructor.
        /// </summary>
        /// <param name="interval">The time in milliseconds between events.</param>
        /// <param name="autoReset">Specifies if timer is auto reseted.</param>
        public TimerEx(double interval,bool autoReset) : base(interval)
        {
            this.AutoReset = autoReset;
        }

        // TODO: We need to do this class .NET CF compatible.

    }
}
