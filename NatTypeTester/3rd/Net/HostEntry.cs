using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace LumiSoft.Net
{
    /// <summary>
    /// This class represent DNS host entry.
    /// </summary>
    public class HostEntry
    {
        private string      m_HostName   = null;
        private IPAddress[] m_pAddresses = null;
        private string[]    m_pAliases   = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="hostName">DNS host name.</param>
        /// <param name="ipAddresses">Host IP addresses.</param>
        /// <param name="aliases">Host aliases(CNAME).</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>hostName</b> or <b>ipAddresses</b> is null reference.</exception>
        public HostEntry(string hostName,IPAddress[] ipAddresses,string[] aliases)
        {
            if(hostName == null){
                throw new ArgumentNullException("hostName");
            }
            if(hostName == string.Empty){
                throw new ArgumentException("Argument 'hostName' value must be specified.","hostName");
            }
            if(ipAddresses == null){
                throw new ArgumentNullException("ipAddresses");
            }

            m_HostName   = hostName;
            m_pAddresses = ipAddresses;
            m_pAliases   = (aliases == null ? new string[0] : aliases);
        }


        #region Properties implementation

        /// <summary>
        /// Gets DNS host name.
        /// </summary>
        public string HostName
        {
            get{ return m_HostName; }
        }

        /// <summary>
        /// Gets list of IP addresses that are associated with a host.
        /// </summary>
        public IPAddress[] Addresses
        {
            get{ return m_pAddresses; }
        }

        /// <summary>
        /// Gets list of aliases(CNAME) that are associated with a host.
        /// </summary>
        public string[] Aliases
        {
            get{ return m_pAliases; }
        }

        #endregion
    }
}
