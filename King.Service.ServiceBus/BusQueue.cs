﻿namespace King.Service.ServiceBus
{
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Bus Queue
    /// </summary>
    public class BusQueue : IBusQueue
    {
        #region Members
        /// <summary>
        /// Queue Client
        /// </summary>
        protected readonly QueueClient client = null;

        /// <summary>
        /// Name
        /// </summary>
        protected readonly string name = null;

        /// <summary>
        /// Namespace Manager
        /// </summary>
        protected readonly NamespaceManager manager = null;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Queue Name</param>
        /// <param name="connectionString">Connection String</param>
        public BusQueue(string name, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name");
            }
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("connectionString");
            }

            this.name = name;
            this.manager = NamespaceManager.CreateFromConnectionString(connectionString);
            this.client = QueueClient.CreateFromConnectionString(connectionString, name);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Create If Not Exists
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> CreateIfNotExists()
        {
            var created = false;
            if (!manager.QueueExists(name))
            {
                await manager.CreateQueueAsync(name);
                created = true;
            }

            return created;
        }

        /// <summary>
        /// Delete Queue
        /// </summary>
        /// <returns></returns>
        public virtual async Task Delete()
        {
            await manager.DeleteQueueAsync(this.name);
        }

        /// <summary>
        /// Get Cloud Queue Message
        /// </summary>
        /// <returns>Message</returns>
        public virtual async Task<BrokeredMessage> Get()
        {
            return await this.client.ReceiveAsync();
        }

        /// <summary>
        /// Approixmate Message Count
        /// </summary>
        /// <returns>Message Count</returns>
        public virtual async Task<long> ApproixmateMessageCount()
        {
            var queue = await this.manager.GetQueueAsync(this.name);
            return queue.MessageCount;
        }

        /// <summary>
        /// Get Many Cloud Queue Message
        /// </summary>
        /// <returns>Messages</returns>
        public virtual async Task<IEnumerable<BrokeredMessage>> GetMany(int messageCount = 5)
        {
            if (0 > messageCount)
            {
                throw new ArgumentException("Message count must be greater than 0.");
            }

            return await this.client.ReceiveBatchAsync(messageCount);
        }

        /// <summary>
        /// Save Message to Queue
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>Task</returns>
        public virtual async Task Save(BrokeredMessage message)
        {
            if (null == message)
            {
                throw new ArgumentNullException("message");
            }

            await this.client.SendAsync(message);
        }

        /// <summary>
        /// Save Object to queue, as json
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Task</returns>
        public virtual async Task Save(object obj)
        {
            if (null == obj)
            {
                throw new ArgumentNullException("obj");
            }

            if (obj is BrokeredMessage)
            {
                await this.Save(obj as BrokeredMessage);
            }
            else
            {
                var msg = new BrokeredMessage(JsonConvert.SerializeObject(obj))
                {
                    ContentType = obj.GetType().ToString(),
                };

                await this.Save(msg);
            }
        }

        /// <summary>
        /// On Error
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="ex">Exception</param>
        public virtual void RegisterForEvents(Func<BrokeredMessage, Task> callback, OnMessageOptions options)
        {
            this.client.OnMessageAsync(callback, options);
        }
        #endregion
    }
}