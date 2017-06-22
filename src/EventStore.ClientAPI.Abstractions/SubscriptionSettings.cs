using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.ClientAPI
{
  /// <summary>Settings for <c>Volatile Subscriptions</c>.</summary>
  public class SubscriptionSettings
  {
    /// <summary>A constant used to specify an unlimited quantity for <see cref="SubscriptionSettings"/> members
    /// that provide an upper bound. This field is constant.</summary>
    public const Int32 Unbounded = -1;

    /// <summary>The default number of tasks that may be used concurrently to process messages.</summary>
    public const Int32 DefaultDegreeOfParallelism = 1;

    public const Int32 DefaultNumActionBlocks = 1;

    /// <summary>A default instance of <see cref="SubscriptionSettings"/>.</summary>
    /// <remarks>Do not change the values of this instance.  It is shared by all of our subscriptions when no options are provided by the user.</remarks>
    public static readonly SubscriptionSettings Default = new SubscriptionSettings();

    internal static readonly SubscriptionSettings ResolveLinkTosSettings = new SubscriptionSettings { ResolveLinkTos = true };

    /// <summary>Initializes the <see cref="SubscriptionSettings"/>.</summary>
    public SubscriptionSettings()
    {
    }

    ///// <summary>The scheduler to use for scheduling tasks to process messages.</summary>
    //private TaskScheduler _taskScheduler = TaskScheduler.Default;

    ///// <summary>Gets or sets the <see cref="System.Threading.Tasks.TaskScheduler"/> to use for scheduling tasks.</summary>
    //public TaskScheduler TaskScheduler
    //{
    //  get { return _taskScheduler; }
    //  set
    //  {
    //    Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
    //    _taskScheduler = value ?? throw new ArgumentNullException(nameof(value));
    //  }
    //}

    /// <summary>The cancellation token to monitor for cancellation requests.</summary>
    private CancellationToken _cancellationToken = CancellationToken.None;

    /// <summary>Gets or sets the <see cref="System.Threading.CancellationToken"/> to monitor for cancellation requests.</summary>
    public CancellationToken CancellationToken
    {
      get { return _cancellationToken; }
      set
      {
        Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
        _cancellationToken = value;
      }
    }

    /// <summary>The maximum number of messages that may be processed per task.</summary>
    private Int32 _maxMessagesPerTask = Unbounded;

    /// <summary>Gets or sets the maximum number of messages that may be processed per task.</summary>
    public Int32 MaxMessagesPerTask
    {
      get { return _maxMessagesPerTask; }
      set
      {
        Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
        if (value < 1 && value != Unbounded) { throw new ArgumentOutOfRangeException(nameof(value)); }
        _maxMessagesPerTask = value;
      }
    }

    /// <summary>The maximum number of messages that may be buffered by the block.</summary>
    private Int32 _boundedCapacityPerBlock = Unbounded;

    /// <summary>Gets or sets the maximum number of messages that may be buffered by the block.</summary>
    public virtual Int32 BoundedCapacityPerBlock
    {
      get { return _boundedCapacityPerBlock; }
      set
      {
        Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
        if (value < 1 && value != Unbounded) { throw new ArgumentOutOfRangeException(nameof(value)); }
        _boundedCapacityPerBlock = value;
      }
    }

    /// <summary>The maximum number of tasks that may be used concurrently to process messages.</summary>
    private Int32 _maxDegreeOfParallelismPerBlock = DefaultDegreeOfParallelism;

    /// <summary>Gets the maximum number of messages that may be processed by the block concurrently.</summary>
    public virtual Int32 MaxDegreeOfParallelismPerBlock
    {
      get { return _maxDegreeOfParallelismPerBlock; }
      set
      {
        Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
        if (value < 1 && value != Unbounded) { throw new ArgumentOutOfRangeException(nameof(value)); }
        _maxDegreeOfParallelismPerBlock = value;
      }
    }

    private Int32 _numActionBlocks = DefaultNumActionBlocks;

    public virtual Int32 NumActionBlocks
    {
      get { return _numActionBlocks; }
      set
      {
        Debug.Assert(this != Default, "Default instance is supposed to be immutable.");
        if (value < 1) { throw new ArgumentOutOfRangeException(nameof(value)); }
        _numActionBlocks = value;
      }
    }

    /// <summary>Whether or not to resolve link events.</summary>
    public virtual bool ResolveLinkTos { get; set; }

    /// <summary>Enables verbose logging on the subscription.</summary>
    public bool VerboseLogging { get; set; }
  }
}