using STUN.Message;
using STUN.Message.Enums;
using STUN.Utils;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace STUN.Client
{
    /// <summary>
    /// This class implements STUN client. Defined in RFC 3489.
    /// </summary>
    /// <example>
    /// <code>
    /// // Create new socket for STUN client.
    /// Socket socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
    /// socket.Bind(new IPEndPoint(IPAddress.Any,0));
    /// 
    /// // Query STUN server
    /// STUN_Result result = STUN_Client.Query("stun.ekiga.net",3478,socket);
    /// if(result.NetType != STUN_NetType.UdpBlocked){
    ///     // UDP blocked or !!!! bad STUN server
    /// }
    /// else{
    ///     IPEndPoint publicEP = result.PublicEndPoint;
    ///     // Do your stuff
    /// }
    /// </code>
    /// </example>
    public static class StunClient
    {
        #region static method Query

        /// <summary>
        /// Gets NAT info from STUN server.
        /// </summary>
        /// <param name="host">STUN server name or IP.</param>
        /// <param name="port">STUN server port. Default port is 3478.</param>
        /// <param name="localEP">Local IP end point.</param>
        /// <returns>Returns UDP network info.</returns>
        /// <exception cref="Exception">Is raised when <b>host</b> or <b>localEP</b> is null reference.</exception>
        /// <exception cref="ArgumentNullException">Throws exception if unexpected error happens.</exception>
        public static StunResult Query(string host, int port, IPEndPoint localEP)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (localEP == null)
            {
                throw new ArgumentNullException(nameof(localEP));
            }

            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(localEP);
            return Query(host, port, s);
        }

        /// <summary>
        /// Gets NAT info from STUN server.
        /// </summary>
        /// <param name="host">STUN server name or IP.</param>
        /// <param name="port">STUN server port. Default port is 3478.</param>
        /// <param name="socket">UDP socket to use.</param>
        /// <returns>Returns UDP network info.</returns>
        /// <exception cref="Exception">Throws exception if unexpected error happens.</exception>
        public static StunResult Query(string host, int port, Socket socket)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }
            if (port < 1)
            {
                throw new ArgumentException(@"Port value must be >= 1 !");
            }
            if (socket.ProtocolType != ProtocolType.Udp)
            {
                throw new ArgumentException(@"Socket must be UDP socket !");
            }

            var remoteEndPoint = new IPEndPoint(Dns.GetHostAddresses(host)[0], port);

            /*
                In test I, the client sends a STUN Binding Request to a server, without any flags set in the
                CHANGE-REQUEST attribute, and without the RESPONSE-ADDRESS attribute. This causes the server 
                to send the response back to the address and port that the request came from.
            
                In test II, the client sends a Binding Request with both the "change IP" and "change port" flags
                from the CHANGE-REQUEST attribute set.  
              
                In test III, the client sends a Binding Request with only the "change port" flag set.
                          
                                    +--------+
                                    |  Test  |
                                    |   I    |
                                    +--------+
                                         |
                                         |
                                         V
                                        /\              /\
                                     N /  \ Y          /  \ Y             +--------+
                      UDP     <-------/Resp\--------->/ IP \------------->|  Test  |
                      Blocked         \ ?  /          \Same/              |   II   |
                                       \  /            \? /               +--------+
                                        \/              \/                    |
                                                         | N                  |
                                                         |                    V
                                                         V                    /\
                                                     +--------+  Sym.      N /  \
                                                     |  Test  |  UDP    <---/Resp\
                                                     |   II   |  Firewall   \ ?  /
                                                     +--------+              \  /
                                                         |                    \/
                                                         V                     |Y
                              /\                         /\                    |
               Symmetric  N  /  \       +--------+   N  /  \                   V
                  NAT  <--- / IP \<-----|  Test  |<--- /Resp\               Open
                            \Same/      |   I    |     \ ?  /               Internet
                             \? /       +--------+      \  /
                              \/                         \/
                              |                           |Y
                              |                           |
                              |                           V
                              |                           Full
                              |                           Cone
                              V              /\
                          +--------+        /  \ Y
                          |  Test  |------>/Resp\---->Restricted
                          |   III  |       \ ?  /
                          +--------+        \  /
                                             \/
                                              |N
                                              |       Port
                                              +------>Restricted

            */

            try
            {
                // Test I
                var test1 = new StunMessage { Type = StunMessageType.BindingRequest };
                var test1Response = DoTransaction(test1, socket, remoteEndPoint, 1600);

                // UDP blocked.
                if (test1Response == null)
                {
                    return new StunResult(NatType.UdpBlocked, null);
                }
                else
                {
                    // Test II
                    var test2 = new StunMessage
                    {
                        Type = StunMessageType.BindingRequest,
                        ChangeRequest = new StunChangeRequest(true, true)
                    };

                    // No NAT.
                    if (socket.LocalEndPoint.Equals(test1Response.MappedAddress))
                    {
                        var test2Response = DoTransaction(test2, socket, remoteEndPoint, 1600);
                        // Open Internet.
                        if (test2Response != null)
                        {
                            return new StunResult(NatType.OpenInternet, test1Response.MappedAddress);
                        }
                        // Symmetric UDP firewall.
                        else
                        {
                            return new StunResult(NatType.SymmetricUdpFirewall, test1Response.MappedAddress);
                        }
                    }
                    // NAT
                    else
                    {
                        var test2Response = DoTransaction(test2, socket, remoteEndPoint, 1600);

                        // Full cone NAT.
                        if (test2Response != null)
                        {
                            return new StunResult(NatType.FullCone, test1Response.MappedAddress);
                        }
                        else
                        {
                            /*
                                If no response is received, it performs test I again, but this time, does so to 
                                the address and port from the CHANGED-ADDRESS attribute from the response to test I.
                            */

                            // Test I(II)
                            var test12 = new StunMessage { Type = StunMessageType.BindingRequest };

                            var test12Response = DoTransaction(test12, socket, test1Response.ChangedAddress, 1600);
                            if (test12Response == null)
                            {
                                throw new Exception(@"STUN Test I(II) didn't get response !");
                            }
                            else
                            {
                                // Symmetric NAT
                                if (!test12Response.MappedAddress.Equals(test1Response.MappedAddress))
                                {
                                    return new StunResult(NatType.Symmetric, test1Response.MappedAddress);
                                }
                                else
                                {
                                    // Test III
                                    var test3 = new StunMessage
                                    {
                                        Type = StunMessageType.BindingRequest,
                                        ChangeRequest = new StunChangeRequest(false, true)
                                    };

                                    var test3Response = DoTransaction(test3, socket, test1Response.ChangedAddress, 1600);
                                    // Restricted
                                    if (test3Response != null)
                                    {
                                        return new StunResult(NatType.RestrictedCone, test1Response.MappedAddress);
                                    }
                                    // Port restricted
                                    else
                                    {
                                        return new StunResult(NatType.PortRestrictedCone, test1Response.MappedAddress);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                // Junk all late responses.
                var startTime = DateTime.Now;
                while (startTime.AddMilliseconds(200) > DateTime.Now)
                {
                    // We got response.
                    if (socket.Poll(1, SelectMode.SelectRead))
                    {
                        var receiveBuffer = new byte[512];
                        socket.Receive(receiveBuffer);
                    }
                }
            }
        }

        #endregion

        #region method GetPublicIP

        /// <summary>
        /// Resolves local IP to public IP using STUN.
        /// </summary>
        /// <param name="stunServer">STUN server.</param>
        /// <param name="port">STUN server port. Default port is 3478.</param>
        /// <param name="localIP">Local IP address.</param>
        /// <returns>Returns public IP address.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>stunServer</b> or <b>localIP</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IOException">Is raised when no connection to STUN server.</exception>
        public static IPAddress GetPublicIP(string stunServer, int port, IPAddress localIP)
        {
            if (stunServer == null)
            {
                throw new ArgumentNullException(nameof(stunServer));
            }
            if (stunServer == string.Empty)
            {
                throw new ArgumentException(@"Argument 'stunServer' value must be specified.");
            }
            if (port < 1)
            {
                throw new ArgumentException(@"Invalid argument 'port' value.");
            }
            if (localIP == null)
            {
                throw new ArgumentNullException(nameof(localIP));
            }

            if (!NetUtils.IsPrivateIP(localIP))
            {
                return localIP;
            }

            var result = Query(stunServer, port, NetUtils.CreateSocket(new IPEndPoint(localIP, 0), ProtocolType.Udp));
            if (result.PublicEndPoint != null)
            {
                return result.PublicEndPoint.Address;
            }

            throw new IOException(@"Failed to STUN public IP address. STUN server name is invalid or firewall blocks STUN.");
        }

        #endregion

        #region method GetPublicEP

        /// <summary>
        /// Resolves socket local end point to public end point.
        /// </summary>
        /// <param name="stunServer">STUN server.</param>
        /// <param name="port">STUN server port. Default port is 3478.</param>
        /// <param name="socket">UDP socket to use.</param>
        /// <returns>Returns public IP end point.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>stunServer</b> or <b>socket</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IOException">Is raised when no connection to STUN server.</exception>
        public static IPEndPoint GetPublicEP(string stunServer, int port, Socket socket)
        {
            if (stunServer == null)
            {
                throw new ArgumentNullException(nameof(stunServer));
            }
            if (stunServer == string.Empty)
            {
                throw new ArgumentException(@"Argument 'stunServer' value must be specified.");
            }
            if (port < 1)
            {
                throw new ArgumentException(@"Invalid argument 'port' value.");
            }
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }
            if (socket.ProtocolType != ProtocolType.Udp)
            {
                throw new ArgumentException(@"Socket must be UDP socket !");
            }

            var remoteEndPoint = new IPEndPoint(Dns.GetHostAddresses(stunServer)[0], port);

            try
            {
                // Test I
                var test1 = new StunMessage { Type = StunMessageType.BindingRequest };
                var test1response = DoTransaction(test1, socket, remoteEndPoint, 1000);

                // UDP blocked.
                if (test1response == null)
                {
                    throw new IOException(@"Failed to STUN public IP address. STUN server name is invalid or firewall blocks STUN.");
                }

                return test1response.SourceAddress;
            }
            catch
            {
                throw new IOException(@"Failed to STUN public IP address. STUN server name is invalid or firewall blocks STUN.");
            }
            finally
            {
                // Junk all late responses.
                var startTime = DateTime.Now;
                while (startTime.AddMilliseconds(200) > DateTime.Now)
                {
                    // We got response.
                    if (socket.Poll(1, SelectMode.SelectRead))
                    {
                        var receiveBuffer = new byte[512];
                        socket.Receive(receiveBuffer);
                    }
                }
            }
        }

        #endregion

        #region method DoTransaction

        /// <summary>
        /// Does STUN transaction. Returns transaction response or null if transaction failed.
        /// </summary>
        /// <param name="request">STUN message.</param>
        /// <param name="socket">Socket to use for send/receive.</param>
        /// <param name="remoteEndPoint">Remote end point.</param>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <returns>Returns transaction response or null if transaction failed.</returns>
        private static StunMessage DoTransaction(StunMessage request, Socket socket, IPEndPoint remoteEndPoint, int timeout)
        {
            var requestBytes = request.ToByteData();
            var startTime = DateTime.Now;
            // Retransmit with 500 ms.
            while (startTime.AddMilliseconds(timeout) > DateTime.Now)
            {
                try
                {
                    socket.SendTo(requestBytes, remoteEndPoint);

                    // We got response.
                    if (socket.Poll(500 * 1000, SelectMode.SelectRead))
                    {
                        var receiveBuffer = new byte[512];
                        socket.Receive(receiveBuffer);

                        // Parse message
                        var response = new StunMessage();
                        response.Parse(receiveBuffer);

                        // Check that transaction ID matches or not response what we want.
                        if (NetUtils.CompareArray(request.TransactionId, response.TransactionId))
                        {
                            return response;
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }

        #endregion

        // TODO: Update to RFC 5389

    }
}
