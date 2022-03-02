
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
    /// <summary>A server that can accept connections from <see cref="Client"/>s.</summary>
    public class Server : ICommon
    {
        /// <inheritdoc cref="IServer.ClientConnected"/>
        public event EventHandler<ServerClientConnectedEventArgs> ClientConnected;
        /// <inheritdoc cref="IServer.MessageReceived"/>
        public event EventHandler<ServerMessageReceivedEventArgs> MessageReceived;
        /// <inheritdoc cref="IServer.ClientDisconnected"/>
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

        /// <summary>Whether or not the server is currently running.</summary>
        public bool IsRunning { get; private set; }
        /// <inheritdoc cref="IServer.Port"/>
        public ushort Port => _server.Port;
        /// <inheritdoc cref="IServer.Clients"/>
        public IConnectionInfo[] Clients => _server.Clients;
        /// <inheritdoc cref="IServer.MaxClientCount"/>
        public ushort MaxClientCount => _server.MaxClientCount;
        /// <inheritdoc cref="IServer.ClientCount"/>
        public int ClientCount => _server.ClientCount;
        /// <inheritdoc cref="IServer.AllowAutoMessageRelay"/>
        public bool AllowAutoMessageRelay
        {
            get => _server.AllowAutoMessageRelay;
            set => _server.AllowAutoMessageRelay = value;
        }
        /// <summary>Encapsulates a method that handles a message from a certain client.</summary>
        /// <param name="fromClientId">The numeric ID of the client from whom the message was received.</param>
        /// <param name="message">The message that was received.</param>
        public delegate void ServerMessageHandler(ushort fromClientId, Message message);

        /// <summary>
        /// A message handler to which handling of all messages will be delegated to.
        /// </summary>
        private readonly IServerMessageHandler _messageHandler;
        /// <summary>Methods used to handle messages, accessible by their corresponding message IDs.</summary>
        /// <summary>The underlying server that is used for managing connections and sending and receiving data.</summary>
        private IServer _server;

        /// <summary>
        /// Handles initial setup.
        /// Initializes the class with a default ServerReflectionMessageHandler.
        /// </summary>
        /// <param name="server">The underlying server that is used for managing connections and sending and receiving data.</param>
        /// /// <param name="messageHandlerGroupId">The ID of the group of message handler methods to use when building <see cref="_messageHandler"/>.</param>
        public Server(IServer server, byte messageHandlerGroupId = 0)
        {
            _server = server;
            _messageHandler = new ServerReflectionMessageHandler(Assembly.GetCallingAssembly(), messageHandlerGroupId);
        }

        /// <summary>
        /// Handles initial setup.
        /// Provides a parameter to provide a customised message handler.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="messageHandler"></param>
        public Server(IServer server, IServerMessageHandler messageHandler)
        {
            _server = server;
            _messageHandler = messageHandler;
        }

        /// <summary>
        /// Handles initial setup using the built-in RUDP transport.
        /// Initializes the class with a default ServerReflectionMessageHandler.
        /// </summary>
        /// <param name="clientTimeoutTime">The time (in milliseconds) after which to disconnect a client without a heartbeat.</param>
        /// <param name="clientHeartbeatInterval">The interval (in milliseconds) at which heartbeats are to be expected from clients.</param>
        /// <param name="logName">The name to use when logging messages via <see cref="RiptideLogger"/>.</param>
        public Server(ushort clientTimeoutTime = 5000, ushort clientHeartbeatInterval = 1000, string logName = "SERVER")
        {
            _server = new Transports.RudpTransport.RudpServer(clientTimeoutTime, clientHeartbeatInterval, logName);
            _messageHandler = new ServerReflectionMessageHandler(Assembly.GetCallingAssembly());
        }

        /// <summary>Stops the server if it's running and swaps out the transport it's using.</summary>
        /// <param name="server">The underlying server that is used for managing connections and sending and receiving data.</param>
        /// <remarks>This method does not automatically restart the server. To continue accepting connections, <see cref="Start(ushort, ushort)"/> will need to be called again.</remarks>
        public void ChangeTransport(IServer server)
        {
            Stop();
            this._server = server;
        }

        /// <summary>Starts the server.</summary>
        /// <param name="port">The local port on which to start the server.</param>
        /// <param name="maxClientCount">The maximum number of concurrent connections to allow.</param>
        public void Start(ushort port, ushort maxClientCount)
        {
            Stop();

            _server.ClientConnected += OnClientConnected;
            _server.MessageReceived += OnMessageReceived;
            _server.ClientDisconnected += OnClientDisconnected;
            _server.Start(port, maxClientCount);

            IsRunning = true;
        }
        
        /// <inheritdoc/>
        public void Tick() => _server.Tick();

        /// <inheritdoc cref="IServer.Send(Message, ushort, bool)"/>
        public void Send(Message message, ushort toClientId, bool shouldRelease = true) => _server.Send(message, toClientId, shouldRelease);

        /// <inheritdoc cref="IServer.SendToAll(Message, bool)"/>
        public void SendToAll(Message message, bool shouldRelease = true) => _server.SendToAll(message, shouldRelease);

        /// <inheritdoc cref="IServer.SendToAll(Message, ushort, bool)"/>
        public void SendToAll(Message message, ushort exceptToClientId, bool shouldRelease = true) => _server.SendToAll(message, exceptToClientId, shouldRelease);

        /// <inheritdoc cref="IServer.DisconnectClient(ushort)"/>
        public void DisconnectClient(ushort clientId) => _server.DisconnectClient(clientId);

        /// <summary>Stops the server.</summary>
        public void Stop()
        {
            if (!IsRunning)
                return;

            _server.Shutdown();
            _server.ClientConnected -= OnClientConnected;
            _server.MessageReceived -= OnMessageReceived;
            _server.ClientDisconnected -= OnClientDisconnected;

            IsRunning = false;
        }

        /// <summary>Invokes the <see cref="ClientConnected"/> event.</summary>
        private void OnClientConnected(object s, ServerClientConnectedEventArgs e) => ClientConnected?.Invoke(this, e);

        /// <summary>Invokes the <see cref="MessageReceived"/> event and initiates handling of the received message.</summary>
        private void OnMessageReceived(object s, ServerMessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
            _messageHandler.HandleMessage(e.FromClientId,e.MessageId,e.Message);
        }

        /// <summary>Invokes the <see cref="ClientDisconnected"/> event.</summary>
        private void OnClientDisconnected(object s, ClientDisconnectedEventArgs e) => ClientDisconnected?.Invoke(this, e);
    }
}
