namespace EventStore.ClientAPI.AutoSubscribing
{
  public enum SubscriptionType
  {
    /// <summary>This kind of subscription calls a given function for events written after the subscription is established.</summary>
    Volatile,

    /// <summary>This kind of subscription specifies a starting point, in the form of an event number or transaction file position.</summary>
    CatchUp,

    /// <summary>This kind of subscriptions supports the “competing consumers” messaging pattern.</summary>
    Persistent
  }
}
