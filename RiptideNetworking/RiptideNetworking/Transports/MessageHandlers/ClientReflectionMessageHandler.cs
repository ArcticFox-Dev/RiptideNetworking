using RiptideNetworking.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RiptideNetworking.Transports.MessageHandlers
{
    /// <summary>
    /// A message handler implementation that will scan the assembly for any message handlers.
    /// To be a client message handler a method must be static and must be decorated with the MessageHandler attribute.
    /// The method signature must also match the one below:
    /// void MethodName(Message message)
    /// </summary>
    public class ClientReflectionMessageHandler : IClientMessageHandler
    {
        private Dictionary<ushort, Client.MessageHandler> _messageHandlers;
        /// <summary>
        /// Sets up the Message Handler and runs the reflection to scan for any Client message handlers.
        /// </summary>
        /// <param name="assembly">The assembly which needs to be parsed for the static handler methods</param>
        /// <param name="messageHandlerGroupId">The ID of the group of message handler methods to use when building the message handlers dictionary.</param>
        public ClientReflectionMessageHandler(Assembly assembly, byte messageHandlerGroupId = 0)
        {
            CreateMessageHandlersDictionary(assembly, messageHandlerGroupId);
        }
        
        /// <summary>Searches the given assembly for methods with the <see cref="MessageHandlerAttribute"/> and adds them to the dictionary of handler methods.</summary>
        /// <param name="assembly">The assembly to search for methods with the <see cref="MessageHandlerAttribute"/>.</param>
        /// <param name="messageHandlerGroupId">The ID of the group of message handler methods to use when building the message handlers dictionary.</param>
        private void CreateMessageHandlersDictionary(Assembly assembly, byte messageHandlerGroupId)
        {
            MethodInfo[] methods = assembly.GetTypes()
                                           .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)) // Include instance methods in the search so we can show the developer an error instead of silently not adding instance methods to the dictionary
                                           .Where(m => m.GetCustomAttributes(typeof(MessageHandlerAttribute), false).Length > 0)
                                           .ToArray();

            _messageHandlers = new Dictionary<ushort, Client.MessageHandler>(methods.Length);
            for (int i = 0; i < methods.Length; i++)
            {
                MessageHandlerAttribute attribute = methods[i].GetCustomAttribute<MessageHandlerAttribute>();
                if (attribute.GroupId != messageHandlerGroupId)
                    continue;

                if (!methods[i].IsStatic)
                    throw new Exception($"Message handler methods should be static, but '{methods[i].DeclaringType}.{methods[i].Name}' is an instance method!");

                Delegate clientMessageHandler = Delegate.CreateDelegate(typeof(Client.MessageHandler), methods[i], false);
                if (clientMessageHandler == null) continue;
                
                // It's a message handler for Client instances
                if (_messageHandlers.ContainsKey(attribute.MessageId))
                {
                    MethodInfo otherMethodWithId = _messageHandlers[attribute.MessageId].GetMethodInfo();
                    throw new Exception($"Client-side message handler methods '{methods[i].DeclaringType}.{methods[i].Name}' and '{otherMethodWithId.DeclaringType}.{otherMethodWithId.Name}' are both set to handle messages with ID {attribute.MessageId}! Only one handler method is allowed per message ID!");
                }
                _messageHandlers.Add(attribute.MessageId, (Client.MessageHandler)clientMessageHandler);
            }
        }

        /// <inheritdoc/>
        public void HandleMessage(ushort messageId, Message message)
        {
            if (_messageHandlers.TryGetValue(messageId, out Client.MessageHandler messageHandler))
                messageHandler(message);
            else
                RiptideLogger.Log(LogType.warning, $"No client-side handler method found for message ID {messageId}!");
        }
    }
}
