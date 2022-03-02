namespace RiptideNetworking.Transports.RudpTransport
{
    /// <summary>
    /// A convenience factory class providing an easy way of creating Rudp Clients and Servers
    /// </summary>
    public static class RudpTransport
    {
        /// <summary>An RudpServer factory method.</summary>
        /// <param name="clientTimeoutTime">The time (in milliseconds) after which to disconnect a client without a heartbeat.</param>
        /// <param name="clientHeartbeatInterval">The interval (in milliseconds) at which heartbeats are to be expected from clients.</param>
        /// <param name="logName">The name to use when logging messages via <see cref="RiptideNetworking.Utils.RiptideLogger"/>.</param>
        public static RudpServer BuildServer(ushort clientTimeoutTime = 5000, ushort clientHeartbeatInterval = 1000,
            string logName = "SERVER")
        {
            return new RudpServer(clientTimeoutTime, clientHeartbeatInterval, logName);
        }

        /// <summary>An RudpClient factory method.</summary>
        /// <param name="timeoutTime">The time (in milliseconds) after which to disconnect if there's no heartbeat from the server.</param>
        /// <param name="heartbeatInterval">The interval (in milliseconds) at which heartbeats should be sent to the server.</param>
        /// <param name="maxConnectionAttempts">How many connection attempts to make before giving up.</param>
        /// <param name="logName">The name to use when logging messages via <see cref="RiptideNetworking.Utils.RiptideLogger"/>.</param>
        public static RudpClient BuildClient(ushort timeoutTime = 5000, ushort heartbeatInterval = 1000, byte maxConnectionAttempts = 5, string logName = "CLIENT")
        {
            return new RudpClient(timeoutTime, heartbeatInterval, maxConnectionAttempts, logName);
        }
    }
}
