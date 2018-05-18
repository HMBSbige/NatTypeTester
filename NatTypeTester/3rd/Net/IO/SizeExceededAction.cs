using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IO
{
    /// <summary>
    /// Specifies action what is done if requested action exceeds maximum allowed size.
    /// </summary>
    public enum SizeExceededAction
    {
        /// <summary>
        /// Throws exception at once when maximum size exceeded.
        /// </summary>
        ThrowException = 1,

        /// <summary>
        /// Junks all data what exceeds maximum allowed size and after requested operation completes,
        /// throws exception.
        /// </summary>
        JunkAndThrowException = 2,
    }
}
