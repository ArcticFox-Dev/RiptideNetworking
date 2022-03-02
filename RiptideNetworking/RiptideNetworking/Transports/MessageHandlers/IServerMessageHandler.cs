namespace RiptideNetworking.Transports.MessageHandlers
{
    /// <summary>
    /// An interface defining the API for the messages received by a server. 
    /// </summary>
    public interface IServerMessageHandler
    {
        /// <summary>
        /// A default endpoint for handling any network messages sent to the Server.
        /// </summary>
        /// <param name="senderId"> The Id of the sender of the message.</param>
        /// <param name="messageId"> The Id of the message to be handled.</param>
        /// <param name="message"> The body of the message that arrived through he network.</param>
        void HandleMessage(ushort senderId, ushort messageId, Message message);
    }
}
