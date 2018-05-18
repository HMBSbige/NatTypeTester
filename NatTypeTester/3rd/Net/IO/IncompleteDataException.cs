using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IO
{
    /// <summary>
    /// The exception that is thrown when incomplete data received.
    /// For example for ReadPeriodTerminated() method reaches end of stream before getting period terminator.
    /// </summary>
    public class IncompleteDataException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public IncompleteDataException() : base()
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="message">Exception message text.</param>
        public IncompleteDataException(string message) : base(message)
        {
        }
    }
}
