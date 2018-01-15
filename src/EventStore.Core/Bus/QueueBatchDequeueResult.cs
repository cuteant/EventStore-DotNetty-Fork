namespace EventStore.Core.Bus
{
    /// <summary>
    /// A struct providing information for <see cref="ISingleConsumerMessageQueue.TryDequeue"/> result.
    /// </summary>
    public readonly struct QueueBatchDequeueResult
    {
        public readonly int DequeueCount;
        public readonly int EstimateCurrentQueueCount;

        public QueueBatchDequeueResult(int dequeueCount, int estimateCurrentQueueCount)
        {
            DequeueCount = dequeueCount;
            EstimateCurrentQueueCount = estimateCurrentQueueCount;
        }
    }
}