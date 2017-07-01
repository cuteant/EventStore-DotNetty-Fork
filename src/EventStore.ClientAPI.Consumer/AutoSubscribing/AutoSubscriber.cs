//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;
//using CuteAnt.Pool;
//using CuteAnt.Reflection;
//using Microsoft.Extensions.DependencyInjection;

//namespace EventStore.ClientAPI.AutoSubscribing
//{
//  /// <summary>Lets you scan assemblies for implementations of <see cref="IAutoSubscriberConsume{T}"/> so that
//  /// these will get registrered as subscribers in the bus.</summary>
//  public class AutoSubscriber
//  {
//    private static readonly ISet<string> s_emptySubscribingTopics;
//    static AutoSubscriber()
//    {
//      s_emptySubscribingTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//    }

//    protected const string ConsumeMethodName = "Consume";
//    protected const string DispatchMethodName = "Dispatch";
//    protected const string DispatchAsyncMethodName = "DispatchAsync";
//    protected readonly IEventStoreConnectionBase2 Connection;

//    // 如果 SubscribingAllTopics 值 false，OnlySubscribingTopics 也没有设置任何 topics，也不做订阅，防止重复订阅
//    private ISet<string> _onlySubscribingTopics;
//    protected ISet<string> OnlySubscribingTopics => _onlySubscribingTopics;

//    /// <summary>Used when generating the unique SubscriptionId checksum.</summary>
//    public string SubscriptionIdPrefix { get; }

//    /// <summary>SubscribingAllTopics，default: true</summary>
//    public bool SubscribingAllTopics { get; }

//    public IServiceProvider Services { get; set; }

//    /// <summary>Responsible for generating SubscriptionIds, when you use
//    /// <see cref="IAutoSubscriberConsume{T}"/>, since it does not let you specify specific SubscriptionIds.
//    /// Message type and SubscriptionId is the key; which if two
//    /// equal keys exists, you will get round robin consumption of messages.</summary>
//    public Func<AutoSubscriberConsumerInfo, string> GenerateSubscriptionId { protected get; set; }

//    /// <summary>Responsible for setting subscription configuration for all auto subscribed consumers.</summary>
//    public Action<ISubscriptionConfiguration> ConfigureSubscriptionConfiguration { protected get; set; }

//    /// <summary>Responsible for setting subscription configuration for all auto subscribed consumers.</summary>
//    public AutoSubscriber(IEventStoreConnectionBase2 connection, string subscriptionIdPrefix, bool subscribingAllTopics = true)
//    {
//      if (string.IsNullOrWhiteSpace(subscriptionIdPrefix)) { throw new ArgumentNullException("You need to specify a SubscriptionId prefix, which will be used as part of the checksum of all generated subscription ids."); }

//      this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
//      SubscriptionIdPrefix = subscriptionIdPrefix;
//      SubscribingAllTopics = subscribingAllTopics;
//      _onlySubscribingTopics = s_emptySubscribingTopics;

//      AutoSubscriberMessageDispatcher = new DefaultAutoSubscriberMessageDispatcher();
//      GenerateSubscriptionId = DefaultSubscriptionIdGenerator;
//      ConfigureSubscriptionConfiguration = subscriptionConfiguration => { };
//    }

//    /// <summary>SetOnlySubscribingTopics</summary>
//    /// <param name="topics"></param>
//    public void SetOnlySubscribingTopics(params string[] topics)
//    {
//      if (topics == null || !topics.Any()) { return; }
//      _onlySubscribingTopics = new HashSet<string>(topics, StringComparer.OrdinalIgnoreCase);
//    }

//    protected virtual string DefaultSubscriptionIdGenerator(AutoSubscriberConsumerInfo c)
//    {
//      var r = StringBuilderManager.Allocate();
//      var unique = string.Concat(SubscriptionIdPrefix, ":", c.ConcreteType.FullName, ":", c.MessageType.FullName);

//      using (var md5 = MD5.Create())
//      {
//        var buff = md5.ComputeHash(Encoding.UTF8.GetBytes(unique));
//        foreach (var b in buff)
//        {
//          r.Append(b.ToString("x2"));
//        }
//      }

//      return string.Concat(SubscriptionIdPrefix, ":", StringBuilderManager.ReturnAndFree(r));
//    }

//    /// <summary>Registers all consumers in passed assembly. The actual Subscriber instances is
//    /// created using <seealso cref="AutoSubscriberMessageDispatcher"/>. The SubscriptionId per consumer
//    /// method is determined by <seealso cref="GenerateSubscriptionId"/> or if the method
//    /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.</summary>
//    /// <param name="assemblies">The assemblies to scan for consumers.</param>
//    public virtual void Subscribe(params Assembly[] assemblies)
//    {
//      if (assemblies == null || !assemblies.Any()) { throw new ArgumentException("No assemblies specified.", nameof(assemblies)); }

//      Subscribe(assemblies.SelectMany(a => a.GetTypes()).ToArray());
//    }

//    /// <summary>Registers all types as consumers. The actual Subscriber instances is
//    /// created using <seealso cref="AutoSubscriberMessageDispatcher"/>. The SubscriptionId per consumer
//    /// method is determined by <seealso cref="GenerateSubscriptionId"/> or if the method
//    /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.</summary>
//    /// <param name="consumerTypes">the types to register as consumers.</param>
//    public virtual void Subscribe(params Type[] consumerTypes)
//    {
//      if (consumerTypes == null) throw new ArgumentNullException(nameof(consumerTypes));

//      var genericBusSubscribeMethod = GetSubscribeMethodOfBus(nameof(IBus.Subscribe), typeof(Action<>));
//      var subscriptionInfos = GetGenericSubscriptionInfos(consumerTypes, typeof(IConsume<>));

//      //InvokeMethods(subscriptionInfos, DispatchMethodName, genericBusSubscribeMethod, messageType => typeof(Action<>).MakeGenericType(messageType));
//      InvokeMethods(subscriptionInfos, DispatchMethodName, genericBusSubscribeMethod, messageType => typeof(Action<>).GetCachedGenericType(messageType));
//    }

//    /// <summary>Registers all async consumers in passed assembly. The actual Subscriber instances is
//    /// created using <seealso cref="AutoSubscriberMessageDispatcher"/>. The SubscriptionId per consumer
//    /// method is determined by <seealso cref="GenerateSubscriptionId"/> or if the method
//    /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.</summary>
//    /// <param name="assemblies">The assemblies to scan for consumers.</param>
//    public virtual void SubscribeAsync(params Assembly[] assemblies)
//    {
//      if (assemblies == null || !assemblies.Any()) { throw new ArgumentException("No assemblies specified.", nameof(assemblies)); }

//      SubscribeAsync(assemblies.SelectMany(a => a.GetTypes()).ToArray());
//    }

//    /// <summary>Registers all async consumers in passed assembly. The actual Subscriber instances is
//    /// created using <seealso cref="AutoSubscriberMessageDispatcher"/>. The SubscriptionId per consumer
//    /// method is determined by <seealso cref="GenerateSubscriptionId"/> or if the method
//    /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.</summary>
//    /// <param name="consumerTypes">the types to register as consumers.</param>
//    public virtual void SubscribeAsync(params Type[] consumerTypes)
//    {
//      var genericBusSubscribeMethod = GetSubscribeMethodOfBus(nameof(IBus.SubscribeAsync), typeof(Func<,>));
//      var subscriptionInfos = GetGenericSubscriptionInfos(consumerTypes, typeof(IConsumeAsync<>));
//      //Func<Type, Type> subscriberTypeFromMessageTypeDelegate = messageType => typeof(Func<,>).MakeGenericType(messageType, typeof(Task));
//      Func<Type, Type> subscriberTypeFromMessageTypeDelegate = messageType => typeof(Func<,>).GetCachedGenericType(messageType, typeof(Task));
//      InvokeMethods(subscriptionInfos, DispatchAsyncMethodName, genericBusSubscribeMethod, subscriberTypeFromMessageTypeDelegate);
//    }

//    protected virtual void InvokeMethods(IEnumerable<KeyValuePair<Type, AutoSubscriberConsumerInfo[]>> subscriptionInfos, string dispatchName, MethodInfo genericBusSubscribeMethod, Func<Type, Type> subscriberTypeFromMessageTypeDelegate)
//    {
//      foreach (var kv in subscriptionInfos)
//      {
//        foreach (var subscriptionInfo in kv.Value)
//        {
//          var dispatchMethod =
//                  AutoSubscriberMessageDispatcher.GetType()
//                                                 .GetMethod(dispatchName, BindingFlags.Instance | BindingFlags.Public)
//                                                 .MakeGenericMethod(subscriptionInfo.MessageType, subscriptionInfo.ConcreteType);
//#if NETSTANDARD
//          var dispatchDelegate = dispatchMethod.CreateDelegate(
//              subscriberTypeFromMessageTypeDelegate(subscriptionInfo.MessageType),
//              AutoSubscriberMessageDispatcher);
//#else
//          var dispatchDelegate = Delegate.CreateDelegate(subscriberTypeFromMessageTypeDelegate(subscriptionInfo.MessageType), AutoSubscriberMessageDispatcher, dispatchMethod);
//#endif
//          var subscriptionAttribute = GetSubscriptionAttribute(subscriptionInfo);
//          //var subscriptionId = subscriptionAttribute != null ? subscriptionAttribute.SubscriptionId : GenerateSubscriptionId(subscriptionInfo);
//          var subscriptionId = subscriptionAttribute?.SubscriptionId ?? GenerateSubscriptionId(subscriptionInfo);
//          #region ## 苦竹 修改 ##
//          //var busSubscribeMethod = genericBusSubscribeMethod.MakeGenericMethod(subscriptionInfo.MessageType);
//          //Action<ISubscriptionConfiguration> configAction = GenerateConfigurationAction(subscriptionInfo);
//          //busSubscribeMethod.Invoke(Bus, new object[] { subscriptionId, dispatchDelegate, configAction });
//          var topics = GetTopAttributeValues(subscriptionInfo);
//          var busSubscribeMethod = genericBusSubscribeMethod.MakeGenericMethod(subscriptionInfo.MessageType);
//          if (topics.Count <= 0)
//          {
//            Action<ISubscriptionConfiguration> configAction = GenerateConfigurationAction(subscriptionInfo, topics.FirstOrDefault());
//            busSubscribeMethod.Invoke(Connection, new object[] { subscriptionId, dispatchDelegate, configAction });
//          }
//          else
//          {
//            foreach (var topic in topics)
//            {
//              // 如果 SubscribingAllTopics 值 false，OnlySubscribingTopics 也没有设置任何 topics，也不做订阅，防止重复订阅
//              if (!SubscribingAllTopics && !_onlySubscribingTopics.Contains(topic)) { continue; }
//              Action<ISubscriptionConfiguration> configAction = GenerateConfigurationAction(subscriptionInfo, topic);
//              busSubscribeMethod.Invoke(Connection, new object[] { $"{subscriptionId}_{topic}", dispatchDelegate, configAction });
//            }
//          }
//          #endregion
//        }
//      }
//    }

//    private Action<ISubscriptionConfiguration> GenerateConfigurationAction(AutoSubscriberConsumerInfo subscriptionInfo, string topic)
//    {
//      return sc =>
//      {
//        ConfigureSubscriptionConfiguration(sc);
//        TopicInfo(topic)(sc);
//        AutoSubscriberConsumerInfo(subscriptionInfo)(sc);
//      };
//    }

//    private Action<ISubscriptionConfiguration> TopicInfo(string topic)
//    {
//      if (string.IsNullOrWhiteSpace(topic))
//      {
//        return configuration => configuration.WithTopic("#");
//      }
//      else
//      {
//        return configuration => configuration.WithTopic(topic);
//      }
//    }

//    //private Action<ISubscriptionConfiguration> GenerateConfigurationFromTopics(IEnumerable<string> topics)
//    //{
//    //  return configuration =>
//    //  {
//    //    foreach (var topic in topics)
//    //    {
//    //      configuration.WithTopic(topic);
//    //    }
//    //  };
//    //}

//    private ISet<string> GetTopAttributeValues(AutoSubscriberConsumerInfo subscriptionInfo)
//    {
//      var consumeMethod = ConsumeMethod(subscriptionInfo);
//      // ## 苦竹 修改 ##
//      //object[] customAttributes = consumeMethod.GetCustomAttributes(typeof(ForTopicAttribute), true).Cast<object>().ToArray();
//      //return customAttributes
//      //                 .OfType<ForTopicAttribute>()
//      //                 .Select(a => a.Topic);
//      var topicAttrs = consumeMethod.GetAllAttributes<ForTopicAttribute>();
//      if (!topicAttrs.Any())
//      {
//        topicAttrs = consumeMethod.ReflectedType?.GetAllAttributes<ForTopicAttribute>() ?? Enumerable.Empty<ForTopicAttribute>();
//      }
//      var topics = new HashSet<string>(topicAttrs.Select(_ => _.Topic), StringComparer.OrdinalIgnoreCase);
//      var topicsAttr = consumeMethod.FirstAttribute<ForTopicsAttribute>();
//      if (topicsAttr == null)
//      {
//        topicsAttr = consumeMethod.ReflectedType?.FirstAttribute<ForTopicsAttribute>();
//      }
//      if (topicsAttr != null && topicsAttr.Topics != null && topicsAttr.Topics.Length > 0)
//      {
//        topics.UnionWith(topicsAttr.Topics);
//      }
//      return topics;
//    }

//    private Action<ISubscriptionConfiguration> AutoSubscriberConsumerInfo(AutoSubscriberConsumerInfo subscriptionInfo)
//    {
//      var configSettings = GetSubscriptionConfigurationAttributeValue(subscriptionInfo);
//      if (configSettings == null)
//      {
//        return subscriptionConfiguration => { };
//      }
//      return configuration =>
//      {
//        //prefetch count is set to a configurable default in RabbitAdvancedBus
//        //so don't touch it unless SubscriptionConfigurationAttribute value is other than 0.
//        if (configSettings.PrefetchCount > 0)
//          configuration.WithPrefetchCount(configSettings.PrefetchCount);

//        if (configSettings.Expires > 0)
//          configuration.WithExpires(configSettings.Expires);

//        configuration
//                  .WithAutoDelete(configSettings.AutoDelete)
//                  .WithCancelOnHaFailover(configSettings.CancelOnHaFailover)
//                  .WithPriority(configSettings.Priority);
//      };
//    }

//    private SubscriptionConfigurationAttribute GetSubscriptionConfigurationAttributeValue(AutoSubscriberConsumerInfo subscriptionInfo)
//    {
//      var consumeMethod = ConsumeMethod(subscriptionInfo);
//      // ## 苦竹 修改 ##
//      //object[] customAttributes = consumeMethod.GetCustomAttributes(typeof(SubscriptionConfigurationAttribute), true).Cast<object>().ToArray();
//      //return customAttributes
//      //                 .OfType<SubscriptionConfigurationAttribute>()
//      //                 .FirstOrDefault();
//      var subscriptionConfigurationAttr = consumeMethod.FirstAttribute<SubscriptionConfigurationAttribute>();
//      if (subscriptionConfigurationAttr == null)
//      {
//        subscriptionConfigurationAttr = consumeMethod.ReflectedType?.FirstAttribute<SubscriptionConfigurationAttribute>();
//      }
//      return subscriptionConfigurationAttr;
//    }

//    protected virtual bool IsValidMarkerType(Type markerType)
//    {
//      return markerType.IsInterface() && markerType.GetMethods().Any(m => m.Name == ConsumeMethodName);
//    }

//    protected virtual MethodInfo GetSubscribeMethodOfBus(string methodName, Type parmType)
//    {
//      return typeof(IBus).GetMethods()
//          .Where(m => m.Name == methodName)
//          .Select(m => new { Method = m, Params = m.GetParameters() })
//          .Single(m => m.Params.Length == 3
//              && m.Params[0].ParameterType == typeof(string)
//              && m.Params[1].ParameterType.GetGenericTypeDefinition() == parmType
//              && m.Params[2].ParameterType == typeof(Action<ISubscriptionConfiguration>)
//             ).Method;
//    }

//    protected virtual AutoSubscriberConsumerAttribute GetSubscriptionAttribute(AutoSubscriberConsumerInfo consumerInfo)
//    {
//      var consumeMethod = ConsumeMethod(consumerInfo);

//      // ## 苦竹 修改 ##
//      //return consumeMethod.GetCustomAttributes(typeof(AutoSubscriberConsumerAttribute), true).SingleOrDefault() as AutoSubscriberConsumerAttribute;
//      var subscriberAttr = consumeMethod.FirstAttribute<AutoSubscriberConsumerAttribute>();
//      if (subscriberAttr == null)
//      {
//        subscriberAttr = consumeMethod.ReflectedType?.FirstAttribute<AutoSubscriberConsumerAttribute>();
//      }
//      return subscriberAttr;
//    }

//    private MethodInfo ConsumeMethod(AutoSubscriberConsumerInfo consumerInfo)
//    {
//      return consumerInfo.ConcreteType.GetMethod(ConsumeMethodName, new[] { consumerInfo.MessageType })
//          ?? GetExplicitlyDeclaredInterfaceMethod(consumerInfo.MessageType);
//    }

//    private MethodInfo GetExplicitlyDeclaredInterfaceMethod(Type messageType)
//    {
//      var interfaceType = typeof(IAutoSubscriberConsume<>).GetCachedGenericType(messageType);
//      return interfaceType.GetMethod(ConsumeMethodName);
//    }

//    protected virtual IEnumerable<KeyValuePair<Type, AutoSubscriberConsumerInfo[]>> GetSubscriptionInfos(IEnumerable<Type> types, Type interfaceType)
//    {
//      foreach (var concreteType in types.Where(t => t.IsClass() && !t.IsAbstract()))
//      {
//        var subscriptionInfos = concreteType.GetInterfaces()
//            .Where(i => !i.IsGenericType() && i == interfaceType)
//            .Select(i => new AutoSubscriberConsumerInfo(concreteType, i, null))
//            .ToArray();

//        if (subscriptionInfos.Any())
//        {
//          yield return new KeyValuePair<Type, AutoSubscriberConsumerInfo[]>(concreteType, subscriptionInfos);
//        }
//      }
//    }

//    protected virtual IEnumerable<KeyValuePair<Type, AutoSubscriberConsumerInfo[]>> GetGenericSubscriptionInfos(IEnumerable<Type> types, Type interfaceType)
//    {
//      foreach (var concreteType in types.Where(t => t.IsClass() && !t.IsAbstract()))
//      {
//        var subscriptionInfos = concreteType.GetInterfaces()
//            .Where(i => i.IsGenericType() && i.GetGenericTypeDefinition() == interfaceType && !i.GetGenericArguments()[0].IsGenericParameter)
//            .Select(i => new AutoSubscriberConsumerInfo(concreteType, i, i.GetGenericArguments()[0]))
//            .ToArray();

//        if (subscriptionInfos.Any())
//        {
//          yield return new KeyValuePair<Type, AutoSubscriberConsumerInfo[]>(concreteType, subscriptionInfos);
//        }
//      }
//    }

//    private object CreateInstance(Type instanceType)
//    {
//      var services = Services;
//      return services != null
//          ? ActivatorUtilities.CreateInstance(services, instanceType)
//          : instanceType.CreateInstance();
//    }

//    private T CreateInstance<T>(Type instanceType)
//    {
//      var services = Services;
//      return services != null
//          ? (T)ActivatorUtilities.CreateInstance(services, instanceType)
//          : instanceType.CreateInstance<T>();
//    }
//  }
//}