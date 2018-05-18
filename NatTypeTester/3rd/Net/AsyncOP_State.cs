using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net
{
    /// <summary>
    /// Specifies asynchronous operation state.
    /// </summary>
    public enum AsyncOP_State
    {
        /// <summary>
        /// Operation waits for start.
        /// </summary>
        WaitingForStart,

        /// <summary>
        /// Operation processing is in progress.
        /// </summary>
        Active,

        /// <summary>
        /// Operations is completed.
        /// </summary>
        Completed,

        /// <summary>
        /// Operation is disposed.
        /// </summary>
        Disposed,
    }
}
