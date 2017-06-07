using System;

namespace EventStore.ClientAPI.AutoSubscribe
{
  /// <summary>ForTopicAttribute</summary>
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
  public class ForTopicAttribute : Attribute
  {
    /// <summary>Constructor</summary>
    /// <param name="topic"></param>
    public ForTopicAttribute(string topic)
    {
      Topic = topic;
    }

    /// <summary>Topic</summary>
    public string Topic { get; set; }
  }

  /// <summary>ForTopicsAttribute</summary>
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
  public class ForTopicsAttribute : Attribute
  {
    /// <summary>Constructor</summary>
    /// <param name="topics"></param>
    public ForTopicsAttribute(params string[] topics)
    {
      Topics = topics;
    }

    /// <summary>Topic</summary>
    public string[] Topics { get; set; }
  }
}