using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace LumiSoft.Net.STUN.Client
{
    /// <summary>
    /// This class holds STUN_Client.Query method return data.
    /// </summary>
    public class STUN_Result
    {
        private STUN_NetType m_NetType         = STUN_NetType.OpenInternet;
        private IPEndPoint   m_pPublicEndPoint = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="netType">Specifies UDP network type.</param>
        /// <param name="publicEndPoint">Public IP end point.</param>
        public STUN_Result(STUN_NetType netType,IPEndPoint publicEndPoint)
        {            
            m_NetType = netType;
            m_pPublicEndPoint = publicEndPoint;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets UDP network type.
        /// </summary>
        public STUN_NetType NetType
        {
            get{ return m_NetType; }
        }

        /// <summary>
        /// Gets public IP end point. This value is null if failed to get network type.
        /// </summary>
        public IPEndPoint PublicEndPoint
        {
            get{ return m_pPublicEndPoint; }
        }

        #endregion

    }
}
