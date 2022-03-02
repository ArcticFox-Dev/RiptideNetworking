
// This file is provided under The MIT License as part of RiptideNetworking.
// Copyright (c) 2021 Tom Weiland
// For additional information please see the included LICENSE.md file or view it on GitHub: https://github.com/tom-weiland/RiptideNetworking/blob/main/LICENSE.md

using RiptideNetworking.Transports;
using RiptideNetworking.Transports.MessageHandlers;
using RiptideNetworking.Utils;
using System;
using System.Reflection;

namespace RiptideNetworking
{
    /// <summary>A client that can connect to a <see cref="Server"/>.</summary>
    public class Client : ICommon
    {
        /// <inheritdoc cref="IClient.Connected"/>
        public event EventHandler Connected;
        /// <inheritdoc cref="IClient.ConnectionFailed"/>
        public event EventHandler ConnectionFailed;
        /// <inheritdoc cref="IClient.MessageReceived"/>
        public event EventHandler<ClientMessageReceivedEventArgs> MessageReceived;
        /// <inheritdoc cref="IClient.Disconnected"/>
        public event EventHandler Disconnected;
        /// <inheritdoc cref="IClient.ClientConnected"/>
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        /// <inheritdoc cref="IClient.ClientDisconnected"/>
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

        /// <inheritdoc cref="IConnectionInfo.Id"/>
        public ushort Id => _transportClient.Id;
        /// <inheritdoc cref="IConnectionInfo.RTT"/>
        public short RTT => _transportClient.RTT;
        /// <inheritdoc cref="IConnectionInfo.SmoothRTT"/>
        public short SmoothRTT => _transportClient.SmoothRTT;
        /// <inheritdoc cref="IConnectionInfo.IsNotConnected"/>
        public bool IsNotConnected => _transportClient.IsNotConnected;
        /// <inheritdoc cref="IConnectionInfo.IsConnecting"/>
        public bool IsConnecting => _transportClient.IsConnecting;
        /// <inheritdoc cref="IConnectionInfo.IsConnected"/>
        public bool IsConnected => _transportClient.IsConnected;
        /// <summary>Encapsulates a method that handles a message from the server.</summary>
        /// <param name="message">The message that was received.</param>
        public delegate void MessageHandler(Message message);

        /// <summary>Methods used to handle messages, accessible by their corresponding message IDs.</summary>
        private readonly IClientMessageHandler _messageHandler;
        /// <summary>The underlying client that is used for sending and receiving data.</summary>
        private IClient _transportClient;

        /// <summary>
        /// Handles initial setup.
        /// Initializes internal messageHandler to the reflection based message handler. 
        /// </summary>
        /// <param name="transportClient">The underlying transport client that is used for sending and receiving data.</param>
        /// /// <param name="messageHandlerGroupId">The ID of th group of message handler methods to use when building <see cref="_messageHandler"/>.</param>
        public Client(IClient transportClient, byte messageHandlerGroupId = 0)
        {
            _messageHandler = new ClientReflectionMessageHandler(Assembly.GetCallingAssembly(), messageHandlerGroupId);
            _transportClient = transportClient;
        }

        /// <summary>
        /// Handles initial setup.
        /// Initializes the internal messageHandler to the one provided in arguments.
        /// </summary>
        /// <param name="messageHandler">The message handler to be used by the client to process incoming messages.</param>
        /// <param name="transportClient">The underlying transport client that is used for sending and receiving data.</param>
        public Client(IClientMessageHandler messageHandler, IClient transportClient)
        {
            _messageHandler = messageHandler;
            _transportClient = transportClient;
        }

        /// <summary>
        /// Handles initial setup using the built-in RUDP transport.
        /// Initializes internal messageHandler to the reflection based message handler.
        /// </summary>
        /// <param name="timeoutTime">The time (in milliseconds) after which to disconnect if there's no heartbeat from the server.</param>
        /// <param name="heartbeatInterval">The interval (in milliseconds) at which heartbeats should be sent to the server.</param>
        /// <param name="maxConnectionAttempts">How many connection attempts to make before giving up.</param>
        /// <param name="logName">The name to use when logging messages via <see cref="RiptideLogger"/>.</param>
        public Client(ushort timeoutTime = 5000, ushort heartbeatInterval = 1000, byte maxConnectionAttempts = 5, string logName = "CLIENT")
        {
            _messageHandler = new ClientReflectionMessageHandler(Assembly.GetCallingAssembly());
            _transportClient =
                new Transports.RudpTransport.RudpClient(timeoutTime, heartbeatInterval, maxConnectionAttempts, logName);
        }

        /// <summary>Disconnects the client if it's connected and swaps out the transport it's using.</summary>
        /// <param name="client">The underlying client that is used for managing the connection to the server.</param>
        /// <remarks>This method does not automatically reconnect to the server. To continue communicating with the server, <see cref="Connect(string, Message)"/> will need to be called again.</remarks>
        public void ChangeTransport(IClient client)
        {
            Disconnect();
            this._transportClient = client;
        }

        /// <summary>Attempts to connect to the given host address.</summary>
        /// <param name="hostAddress">The host address to connect to.</param>
        /// <param name="message">A message containing data that should be sent to the server with the connection attempt. Use <see cref="Message.Create()"/> to get an empty message instance.</param>
        /// <remarks>
        ///   Riptide's default transport expects the host address to consist of an IP and port, separated by a colon. For example: <c>127.0.0.1:7777</c>.<br/>
        ///   If you are using a different transport, check the relevant documentation for what information it requires in the host address.
        /// </remarks>
        public void Connect(string hostAddress, Message message = null)
        {
            Disconnect();

            _transportClient.Connected += OnConnected;
            _transportClient.ConnectionFailed += OnConnectionFailed;
            _transportClient.MessageReceived += OnMessageReceived;
            _transportClient.Disconnected += OnDisconnected;
            _transportClient.ClientConnected += OnClientConnected;
            _transportClient.ClientDisconnected += OnClientDisconnected;
            _transportClient.Connect(hostAddress, message);
        }

        /// <inheritdoc/>
        public void Tick() => _transportClient.Tick();

        /// <inheritdoc cref="IClient.Send(Message, bool)"/>
        public void Send(Message message, bool shouldRelease = true) => _transportClient.Send(message, shouldRelease);

        /// <summary>Disconnects from the server.</summary>
        public void Disconnect()
        {
            if (IsNotConnected)
                return;

            _transportClient.Disconnect();
            LocalDisconnect();
        }

        private void LocalDisconnect()
        {
            _transportClient.Connected -= OnConnected;
            _transportClient.ConnectionFailed -= OnConnectionFailed;
            _transportClient.MessageReceived -= OnMessageReceived;
            _transportClient.Disconnected -= OnDisconnected;
            _transportClient.ClientConnected -= OnClientConnected;
            _transportClient.ClientDisconnected -= OnClientDisconnected;
        }

        /// <summary>Invokes the <see cref="Connected"/> event.</summary>
        private void OnConnected(object s, EventArgs e) => Connected?.Invoke(this, e);

        /// <summary>Invokes the <see cref="ConnectionFailed"/> event.</summary>
        private void OnConnectionFailed(object s, EventArgs e)
        {
            LocalDisconnect();
            ConnectionFailed?.Invoke(this, e);
        }

        /// <summary>Invokes the <see cref="MessageReceived"/> event and initiates handling of the received message.</summary>
        private void OnMessageReceived(object s, ClientMessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
            _messageHandler.HandleMessage(e.MessageId,e.Message);
        }

        /// <summary>Invokes the <see cref="Disconnected"/> event.</summary>
        private void OnDisconnected(object s, EventArgs e)
        {
            LocalDisconnect();
            Disconnected?.Invoke(this, e);
        }

        /// <summary>Invokes the <see cref="ClientConnected"/> event.</summary>
        private void OnClientConnected(object s, ClientConnectedEventArgs e) => ClientConnected?.Invoke(this, e);

        /// <summary>Invokes the <see cref="ClientDisconnected"/> event.</summary>
        private void OnClientDisconnected(object s, ClientDisconnectedEventArgs e) => ClientDisconnected?.Invoke(this, e);
    }
}
