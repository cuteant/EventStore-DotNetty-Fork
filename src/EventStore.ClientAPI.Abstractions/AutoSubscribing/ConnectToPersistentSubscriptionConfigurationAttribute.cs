using System;

namespace EventStore.ClientAPI.AutoSubscribing
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
    public class ConnectToPersistentSubscriptionConfigurationAttribute : Attribute
    {
        /// <summary>Gets or sets the maximum number of messages that may be processed per task.</summary>
        public string MaxMessagesPerTask { get; set; }

        ///// <summary>Whether or not to resolve link events.</summary>
        //public string ResolveLinkTos { get; set; }

        /// <summary>Enables verbose logging on the subscription.</summary>
        public string VerboseLogging { get; set; }

        /// <summary>The buffer size to use for the persistent subscription.</summary>
        public string BufferSize { get; set; }

        /// <summary>Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</summary>
        public string AutoAck { get; set; }
    }
}
