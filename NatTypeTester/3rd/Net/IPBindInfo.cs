using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace LumiSoft.Net
{
    /// <summary>
    /// Holds IP bind info.
    /// </summary>
    public class IPBindInfo
    {
        private string           m_HostName     = "";
        private BindInfoProtocol m_Protocol     = BindInfoProtocol.TCP;  
        private IPEndPoint       m_pEndPoint    = null;
        private SslMode          m_SslMode      = SslMode.None;
        private X509Certificate2 m_pCertificate = null;
        private object           m_Tag          = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="hostName">Host name.</param>
        /// <param name="protocol">Bind protocol.</param>
        /// <param name="ip">IP address to listen.</param>
        /// <param name="port">Port to listen.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null.</exception>
        public IPBindInfo(string hostName,BindInfoProtocol protocol,IPAddress ip,int port)
        {
            if(ip == null){
                throw new ArgumentNullException("ip");
            }

            m_HostName  = hostName;
            m_Protocol  = protocol;
            m_pEndPoint = new IPEndPoint(ip,port);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="hostName">Host name.</param>
        /// <param name="ip">IP address to listen.</param>
        /// <param name="port">Port to listen.</param>
        /// <param name="sslMode">Specifies SSL mode.</param>
        /// <param name="sslCertificate">Certificate to use for SSL connections.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null.</exception>
        public IPBindInfo(string hostName,IPAddress ip,int port,SslMode sslMode,X509Certificate2 sslCertificate) : this(hostName,BindInfoProtocol.TCP,ip,port,sslMode,sslCertificate)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="hostName">Host name.</param>
        /// <param name="protocol">Bind protocol.</param>
        /// <param name="ip">IP address to listen.</param>
        /// <param name="port">Port to listen.</param>
        /// <param name="sslMode">Specifies SSL mode.</param>
        /// <param name="sslCertificate">Certificate to use for SSL connections.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IPBindInfo(string hostName,BindInfoProtocol protocol,IPAddress ip,int port,SslMode sslMode,X509Certificate2 sslCertificate)
        {
            if(ip == null){
                throw new ArgumentNullException("ip");
            }
            
            m_HostName     = hostName;
            m_Protocol     = protocol;
            m_pEndPoint    = new IPEndPoint(ip,port);
            m_SslMode      = sslMode;
            m_pCertificate = sslCertificate;
            if((sslMode == SslMode.SSL || sslMode == SslMode.TLS) && sslCertificate == null){
                throw new ArgumentException("SSL requested, but argument 'sslCertificate' is not provided.");
            }
        }


        #region override method Equals

        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>Returns true if two objects are equal.</returns>
        public override bool Equals(object obj)
        {
            if(obj == null){
                return false;
            }
            if(!(obj is IPBindInfo)){
                return false;
            }

            IPBindInfo bInfo = (IPBindInfo)obj;
            if(bInfo.HostName != m_HostName){
                return false;
            }
            if(bInfo.Protocol != m_Protocol){
                return false;
            }
            if(!bInfo.EndPoint.Equals(m_pEndPoint)){
                return false;
            }
            if(bInfo.SslMode != m_SslMode){
                return false;
            }
            if(!X509Certificate.Equals(bInfo.Certificate,m_pCertificate)){
                return false;
            }

            return true;
        }

        #endregion

        #region override method GetHashCode

        /// <summary>
        /// Returns the hash code.
        /// </summary>
        /// <returns>Returns the hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets host name.
        /// </summary>
        public string HostName
        {
            get{ return m_HostName; }
        }

        /// <summary>
        /// Gets protocol.
        /// </summary>
        public BindInfoProtocol Protocol
        {
            get{ return m_Protocol; }
        }

        /// <summary>
        /// Gets IP end point.
        /// </summary>
        public IPEndPoint EndPoint
        {
            get{ return m_pEndPoint; }
        }

        /// <summary>
        /// Gets IP address.
        /// </summary>
        public IPAddress IP
        {
            get{ return m_pEndPoint.Address; }
        }

        /// <summary>
        /// Gets port.
        /// </summary>
        public int Port
        {
            get{ return m_pEndPoint.Port; }
        }

        /// <summary>
        /// Gets SSL mode.
        /// </summary>
        public SslMode SslMode
        {
            get{ return m_SslMode; }
        }

        /// <summary>
        /// Gets SSL certificate.
        /// </summary>
        [Obsolete("Use property Certificate instead.")]
        public X509Certificate2 SSL_Certificate
        {
            get{ return m_pCertificate; }
        }

        /// <summary>
        /// Gets SSL certificate.
        /// </summary>
        public X509Certificate2 Certificate
        {
            get{ return m_pCertificate; }
        }


        /// <summary>
        /// Gets or sets user data. This is used internally don't use it !!!.
        /// </summary>
        public object Tag
        {
            get{ return m_Tag; }

            set{ m_Tag = value; }
        }

        #endregion

    }
}
