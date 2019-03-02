using System;

namespace EventStore.ClientAPI.AutoSubscribing
{
    public class CatchUpSubscriptionConfigurationAttribute : Attribute
    {
        public readonly long LastCheckpoint;

        public CatchUpSubscriptionConfigurationAttribute(long lastCheckpoint) => LastCheckpoint = lastCheckpoint;
    }
}
