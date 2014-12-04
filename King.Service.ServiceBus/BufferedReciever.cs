﻿namespace King.Service.ServiceBus
{
    using King.Service.ServiceBus.Models;
    using King.Service.ServiceBus.Timing;
    using Microsoft.ServiceBus.Messaging;
    using Newtonsoft.Json;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Buffered Reciever
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    public class BufferedReciever<T> : BusEvents<T>, IScalable
    {
        #region Members
        /// <summary>
        /// Sleep
        /// </summary>
        protected readonly ISleep sleep = null;

        /// <summary>
        /// Default Concurrent Calls
        /// </summary>
        public new const byte DefaultConcurrentCalls = 50;

        /// <summary>
        /// Scale
        /// </summary>
        private volatile bool scale = false;
        #endregion

        #region Constructors
        /// <summary>
        /// Service Bus Queue Events
        /// </summary>
        /// <param name="queue">Queue</param>
        /// <param name="eventHandler">Event Handler</param>
        /// <param name="concurrentCalls">Concurrent Calls</param>
        public BufferedReciever(IBusQueueReciever queue, IBusEventHandler<T> eventHandler, byte concurrentCalls = BufferedReciever<T>.DefaultConcurrentCalls)
            : this(queue, eventHandler, new Sleep(), concurrentCalls)
        {
        }

        /// <summary>
        /// Service Bus Queue Events
        /// </summary>
        /// <param name="queue">Queue</param>
        /// <param name="eventHandler">Event Handler</param>
        /// <param name="sleep"></param>
        /// <param name="concurrentCalls">Concurrent Calls</param>
        public BufferedReciever(IBusQueueReciever queue, IBusEventHandler<T> eventHandler, ISleep sleep, byte concurrentCalls = BufferedReciever<T>.DefaultConcurrentCalls)
            : base(queue, eventHandler, concurrentCalls)
        {
            if (null == sleep)
            {
                throw new ArgumentNullException("sleep");
            }

            this.sleep = sleep;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Scale Receiver
        /// </summary>
        public virtual bool Scale
        {
            get
            {
                return this.scale;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// This event will be called each time a message arrives.
        /// </summary>
        /// <param name="message">Brokered Message</param>
        /// <returns>Task</returns>
        public override async Task OnMessageArrived(BrokeredMessage message)
        {
            this.scale = true;
            await Task.Factory.StartNew(this.MessageArrived, message.GetBody<BufferedMessage>());
            this.scale = false;
        }

        /// <summary>
        /// Message Arrived, Background Thread
        /// </summary>
        /// <param name="body">Message Body</param>
        public override void MessageArrived(object body)
        {
            var buffered = (BufferedMessage)body;
            var obj = JsonConvert.DeserializeObject<T>(buffered.Data);

            Trace.TraceInformation("Message timing: {0} before scheduled release.", buffered.ReleaseAt.Subtract(DateTime.UtcNow));

            this.sleep.Until(buffered.ReleaseAt);

            Trace.TraceInformation("Message timing: {0} afer scheduled release.", DateTime.UtcNow.Subtract(buffered.ReleaseAt));

            var success = this.eventHandler.Process(obj).Result;
            if (success)
            {
                Trace.TraceInformation("Message processed successfully.");
            }
            else
            {
                throw new InvalidOperationException("Message not processed successfully.");
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
        #endregion
    }
}