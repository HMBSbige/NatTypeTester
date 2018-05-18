using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net
{
    /// <summary>
    /// This exception is thrown when parse errors are encountered.
    /// </summary>
    public class ParseException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="message"></param>
        public ParseException(string message) : base(message)
        {
        }
    }
}
