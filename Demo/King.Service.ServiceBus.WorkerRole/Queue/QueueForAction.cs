﻿namespace King.Service.WorkerRole.Queue
{
    using King.Service.ServiceBus;
    using King.Service.ServiceBus.Queue;
    using Microsoft.ServiceBus.Messaging;
    using System;
    using System.Diagnostics;

    public class QueueForAction : RecurringTask
    {
        #region Members
        /// <summary>
        /// Queue Client
        /// </summary>
        private readonly IBusQueue client;

        /// <summary>
        /// Action
        /// </summary>
        private readonly string action = null;
        #endregion

        #region Constructors
        public QueueForAction(IBusQueue client, string action)
        {
            this.client = client;
            this.action = action;
        }
        #endregion

        public override void Run()
        {
            var model = new ExampleModel()
            {
                Identifier = Guid.NewGuid(),
                Action = action,
            };

            Trace.TraceInformation("Sending to {0}: '{1}'", model.Action, model.Identifier);

            client.Save(new BrokeredMessage(model));
        }
    }
}