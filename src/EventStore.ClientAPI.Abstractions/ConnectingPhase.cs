namespace EventStore.ClientAPI
{
    /// <summary>TBD</summary>
    public enum ConnectingPhase
    {
        /// <summary>TBD</summary>
        Invalid = 0,
        /// <summary>TBD</summary>
        Reconnecting = 1,
        /// <summary>TBD</summary>
        EndPointDiscovery = 2,
        /// <summary>TBD</summary>
        ConnectionEstablishing = 3,
        /// <summary>TBD</summary>
        Authentication = 4,
        /// <summary>TBD</summary>
        Identification = 5,
        /// <summary>TBD</summary>
        Connected = 6,
    }
}
