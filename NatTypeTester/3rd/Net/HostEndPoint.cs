using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace LumiSoft.Net
{
    /// <summary>
    /// Represents a network endpoint as an host(name or IP address) and a port number.
    /// </summary>
    public class HostEndPoint
    {
        private string m_Host = "";
        private int    m_Port = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="port">The port number associated with the host. Value -1 means port not specified.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>host</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public HostEndPoint(string host,int port)
        {
            if(host == null){
                throw new ArgumentNullException("host");
            }
            if(host == ""){
                throw new ArgumentException("Argument 'host' value must be specified.");
            }

            m_Host = host;
            m_Port = port;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="endPoint">Host IP end point.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>endPoint</b> is null reference.</exception>
        public HostEndPoint(IPEndPoint endPoint)
        {
            if(endPoint == null){
                throw new ArgumentNullException("endPoint");
            }

            m_Host = endPoint.Address.ToString();
            m_Port = endPoint.Port;
        }


        #region static method Parse

        /// <summary>
        /// Parses HostEndPoint from the specified string.
        /// </summary>
        /// <param name="value">HostEndPoint value.</param>
        /// <returns>Returns parsed HostEndPoint value.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public static HostEndPoint Parse(string value)
        {
            return Parse(value,-1);
        }

        /// <summary>
        /// Parses HostEndPoint from the specified string.
        /// </summary>
        /// <param name="value">HostEndPoint value.</param>
        /// <param name="defaultPort">If port isn't specified in value, specified port will be used.</param>
        /// <returns>Returns parsed HostEndPoint value.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public static HostEndPoint Parse(string value,int defaultPort)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }
            if(value == ""){
                throw new ArgumentException("Argument 'value' value must be specified.");
            }

            // We have host name with port.
            if(value.IndexOf(':') > -1){
                string[] host_port = value.Split(new char[]{':'},2);

                try{
                    return new HostEndPoint(host_port[0],Convert.ToInt32(host_port[1]));
                }
                catch{
                    throw new ArgumentException("Argument 'value' has invalid value.");
                }
            }
            // We have host name without port.
            else{
                return new HostEndPoint(value,defaultPort);
            }
        }

        #endregion


        #region method ToString

        /// <summary>
        /// Returns HostEndPoint as string.
        /// </summary>
        /// <returns>Returns HostEndPoint as string.</returns>
        public override string ToString()
        {
            if(m_Port == -1){
                return m_Host;
            }
            else{
                return m_Host + ":" + m_Port.ToString();
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets if <b>Host</b> is IP address.
        /// </summary>
        public bool IsIPAddress
        {
            get{ return Net_Utils.IsIPAddress(m_Host); }
        }

        /// <summary>
        /// Gets host name or IP address.
        /// </summary>
        public string Host
        {
            get{ return m_Host; }
        }

        /// <summary>
        /// Gets the port number of the endpoint. Value -1 means port not specified.
        /// </summary>
        public int Port
        {
            get{ return m_Port; }
        }

        #endregion

    }
}
