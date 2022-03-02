namespace RiptideNetworking
{
    /// <summary>
    /// An interface defining the API for the messages received by a client. 
    /// </summary>
    public interface IClientMessageHandler
    {
        /// <summary>
        /// A default endpoint for handling any network messages from the Client.
        /// </summary>
        /// <param name="messageId"> The Id of the message to be handled.</param>
        /// <param name="message"> The body of the message that arrived through he network.</param>
        void HandleMessage(ushort messageId, Message message);
    }
}
