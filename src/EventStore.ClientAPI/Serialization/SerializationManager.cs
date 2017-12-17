using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CuteAnt;
using CuteAnt.Extensions.Serialization;
using CuteAnt.Reflection;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Internal;
using Hyperion;
using Hyperion.SerializerFactories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventStore.ClientAPI.Serialization
{
  /// <summary>SerializationManager to oversee the EventStore serializer system.</summary>
  public static class SerializationManager
  {
    #region @@ Fields @@

    private static ILogger s_logger = TraceLogger.GetLogger(typeof(SerializationManager));
    private static IList<IExternalSerializer> _externalSerializers;
    private static readonly ConcurrentDictionary<Type, IExternalSerializer> _typeToExternalSerializerDictionary;

    private static readonly ConcurrentDictionary<Type, StreamAttribute> _typeToStreamProviderDictionary;

    private static readonly ConcurrentDictionary<Type, SerializationTokenAttribute> _typeToSerializationTokenDictionary;

    private static readonly JsonSerializerSettings _metadataSettings;
    private static readonly IJsonMessageFormatter _jsonFormatter;

    private static IExternalSerializer _jsonSerializer;
    private static IExternalSerializer _gzJsonSerializer;
    private static IExternalSerializer _lz4JsonSerializer;

    private static IExternalSerializer _protobufSerializer;
    private static IExternalSerializer _gzProtobufSerializer;
    private static IExternalSerializer _lz4ProtobufSerializer;

    private static IExternalSerializer _hyperionSerializer;
    private static IExternalSerializer _gzHyperionSerializer;
    private static IExternalSerializer _lz4HyperionSerializer;

    #endregion

    #region @@ Constructor @@

    static SerializationManager()
    {
      _externalSerializers = new List<IExternalSerializer>();
      _typeToExternalSerializerDictionary = new ConcurrentDictionary<Type, IExternalSerializer>();
      _typeToStreamProviderDictionary = new ConcurrentDictionary<Type, StreamAttribute>();
      _typeToSerializationTokenDictionary = new ConcurrentDictionary<Type, SerializationTokenAttribute>();

      _metadataSettings = JsonConvertX.CreateSerializerSettings(Formatting.Indented);
      _metadataSettings.Converters.Add(JsonConvertX.DefaultStringEnumCamelCaseConverter);
      _jsonFormatter = new JsonMessageFormatter()
      {
        DefaultSerializerSettings = _metadataSettings,
        DefaultDeserializerSettings = _metadataSettings
      };

      _jsonSerializer = new JsonEventSerializer();
      _gzJsonSerializer = new GzJsonEventSerializer();
      _lz4JsonSerializer = new LZ4JsonEventSerializer();

      _protobufSerializer = new ProtobufEventSerializer();
      _gzProtobufSerializer = new GzProtobufEventSerializer();
      _lz4ProtobufSerializer = new LZ4ProtobufEventSerializer();

      _hyperionSerializer = new HyperionEventSerializer();
      _gzHyperionSerializer = new GzHyperionEventSerializer();
      _lz4HyperionSerializer = new LZ4HyperionEventSerializer();
    }

    #endregion

    #region -- Initialize --

    public static void Initialize(JsonSerializerSettings serializerSettings, JsonSerializerSettings deserializerSettings)
    {
      if (null == serializerSettings) { throw new ArgumentNullException(nameof(serializerSettings)); }
      if (null == deserializerSettings) { throw new ArgumentNullException(nameof(deserializerSettings)); }

      _jsonSerializer = new JsonEventSerializer(serializerSettings, deserializerSettings);
      _gzJsonSerializer = new GzJsonEventSerializer(serializerSettings, deserializerSettings);
      _lz4JsonSerializer = new LZ4JsonEventSerializer(serializerSettings, deserializerSettings);
    }

    public static void Initialize(IEnumerable<Surrogate> surrogates,
      IEnumerable<ValueSerializerFactory> serializerFactories = null, IEnumerable<Type> knownTypes = null)
    {
      if (null == surrogates) { throw new ArgumentNullException(nameof(surrogates)); }

      _hyperionSerializer = new HyperionEventSerializer(surrogates, serializerFactories, knownTypes);
      _gzHyperionSerializer = new GzHyperionEventSerializer(surrogates, serializerFactories, knownTypes);
      _lz4HyperionSerializer = new LZ4HyperionEventSerializer(surrogates, serializerFactories, knownTypes);
    }

    public static void Initialize(JsonSerializerSettings serializerSettings, JsonSerializerSettings deserializerSettings,
      IEnumerable<Surrogate> surrogates, IEnumerable<ValueSerializerFactory> serializerFactories = null, IEnumerable<Type> knownTypes = null)
    {
      if (null == serializerSettings) { throw new ArgumentNullException(nameof(serializerSettings)); }
      if (null == deserializerSettings) { throw new ArgumentNullException(nameof(deserializerSettings)); }
      if (null == surrogates) { throw new ArgumentNullException(nameof(surrogates)); }

      _jsonSerializer = new JsonEventSerializer(serializerSettings, deserializerSettings);
      _gzJsonSerializer = new GzJsonEventSerializer(serializerSettings, deserializerSettings);
      _lz4JsonSerializer = new LZ4JsonEventSerializer(serializerSettings, deserializerSettings);

      _hyperionSerializer = new HyperionEventSerializer(surrogates, serializerFactories, knownTypes);
      _gzHyperionSerializer = new GzHyperionEventSerializer(surrogates, serializerFactories, knownTypes);
      _lz4HyperionSerializer = new LZ4HyperionEventSerializer(surrogates, serializerFactories, knownTypes);
    }

    #endregion

    #region -- RegisterSerializationProvider --

    public static void RegisterSerializationProvider(IExternalSerializer serializer, int? insertIndex = null)
    {
      if (null == serializer) { throw new ArgumentNullException(nameof(serializer)); }

      if (insertIndex.HasValue)
      {
        // 插入失败，也需要添加
        try
        {
          _externalSerializers.Insert(insertIndex.Value, serializer);
          return;
        }
        catch { }
      }
      _externalSerializers.Add(serializer);
    }

    /// <summary>Loads the external srializers and places them into a hash set</summary>
    /// <param name="providerTypes">The list of types that implement <see cref="IExternalSerializer"/></param>
    public static void RegisterSerializationProviders(List<TypeInfo> providerTypes)
    {
      if (providerTypes == null) { return; }

      _externalSerializers.Clear();
      _typeToExternalSerializerDictionary.Clear();
      providerTypes.ForEach(typeInfo =>
      {
        try
        {
          var serializer = ActivatorUtils.FastCreateInstance<IExternalSerializer>(typeInfo.AsType());
          _externalSerializers.Add(serializer);
        }
        catch (Exception exception)
        {
          s_logger.LogError(exception, $"Failed to create instance of type: {typeInfo.FullName}");
        }
      });
    }

    #endregion

    #region ** TryLookupExternalSerializer **

    private static bool TryLookupExternalSerializer(Type t, out IExternalSerializer serializer)
    {
      // essentially a no-op if there are no external serializers registered
      if (_externalSerializers.Count == 0)
      {
        serializer = null;
        return false;
      }

      // the associated serializer will be null if there are no external serializers that handle this type
      if (_typeToExternalSerializerDictionary.TryGetValue(t, out serializer))
      {
        return serializer != null;
      }

      serializer = _externalSerializers.FirstOrDefault(s => s.IsSupportedType(t));

      _typeToExternalSerializerDictionary.TryAdd(t, serializer);

      return serializer != null;
    }

    #endregion

    #region -- RegisterStreamProvider --

    public static void RegisterStreamProvider(Type expectedType, string stream, string eventType = null, string expectedVersion = null)
    {
      if (null == expectedType) { throw new ArgumentNullException(nameof(expectedType)); }
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }

      if (string.IsNullOrWhiteSpace(eventType)) { eventType = RuntimeTypeNameFormatter.Serialize(expectedType); }
      _typeToStreamProviderDictionary.TryAdd(expectedType, new StreamAttribute(stream, eventType, expectedVersion));
    }

    #endregion

    #region ** TryLookupStreamProvider **

    private static bool TryLookupStreamProvider(Type expectedType, out StreamAttribute streamAttr)
    {
      if (_typeToStreamProviderDictionary.TryGetValue(expectedType, out streamAttr))
      {
        return streamAttr != null;
      }

      streamAttr = expectedType.GetCustomAttributeX<StreamAttribute>();

      _typeToStreamProviderDictionary.TryAdd(expectedType, streamAttr);

      return streamAttr != null;
    }

    #endregion

    #region == GetStreamProvider ==

    internal static StreamAttribute GetStreamProvider(Type actualType, Type expectedType = null)
    {
      if (expectedType != null && TryLookupStreamProvider(expectedType, out StreamAttribute streamAttr))
      {
        return streamAttr;
      }
      else if (TryLookupStreamProvider(actualType, out streamAttr))
      {
        return streamAttr;
      }
      return null;
    }

    #endregion

    #region -- RegisterSerializationToken --

    public static void RegisterSerializationToken(Type expectedType, SerializationToken token)
    {
      if (null == expectedType) { throw new ArgumentNullException(nameof(expectedType)); }

      _typeToSerializationTokenDictionary.TryAdd(expectedType, new SerializationTokenAttribute(token));
    }

    #endregion

    #region ** TryLookupSerializationToken **

    private static bool TryLookupSerializationToken(Type expectedType, out SerializationTokenAttribute tokenAttr)
    {
      if (_typeToSerializationTokenDictionary.TryGetValue(expectedType, out tokenAttr))
      {
        return tokenAttr != null;
      }

      tokenAttr = expectedType.GetCustomAttributeX<SerializationTokenAttribute>();

      _typeToSerializationTokenDictionary.TryAdd(expectedType, tokenAttr);

      return tokenAttr != null;
    }

    #endregion

    #region ** GetSerializationToken **

    internal static SerializationToken GetSerializationToken(Type actualType, Type expectedType = null)
    {
      var token = SerializationToken.Json;
      if (expectedType != null && TryLookupSerializationToken(expectedType, out SerializationTokenAttribute tokenAttr))
      {
        token = tokenAttr.Token;
      }
      else if (TryLookupSerializationToken(actualType, out tokenAttr))
      {
        token = tokenAttr.Token;
      }
      else
      {
        var protoContract = actualType.GetCustomAttributeX<ProtoBuf.ProtoContractAttribute>();
        if (protoContract != null) { return SerializationToken.Protobuf; }
      }
      return token;
    }

    #endregion

    #region -- GetStreamId --

    internal static string GetStreamId(Type actualType, Type expectedType = null)
    {
      var streamAttr = GetStreamProvider(actualType, expectedType);
      return streamAttr != null ? streamAttr.StreamId : RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType);
    }

    #endregion

    #region -- SerializeEvent --

    public static EventData SerializeEvent(object @event, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      if (null == @event) { throw new ArgumentNullException(nameof(@event)); }

      return SerializeEvent(@event.GetType(), @event, eventContext, expectedType);
    }
    public static EventData SerializeEvent(Type actualType, object @event, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      if (null == @event) { throw new ArgumentNullException(nameof(@event)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }

      var streamAttr = GetStreamProvider(actualType, expectedType);
      return SerializeEvent(streamAttr?.EventType, actualType, @event, eventContext, expectedType);
    }


    internal static EventData SerializeEvent(StreamAttribute streamAttr, object @event, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      //if (null == streamAttr) { throw new ArgumentNullException(nameof(streamAttr)); }

      return SerializeEvent(streamAttr?.EventType, @event, eventContext, expectedType);
    }
    internal static EventData SerializeEvent(StreamAttribute streamAttr, Type actualType, object @event, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      //if (null == streamAttr) { throw new ArgumentNullException(nameof(streamAttr)); }

      return SerializeEvent(streamAttr?.EventType, actualType, @event, eventContext, expectedType);
    }


    public static EventData SerializeEvent(string eventType, object @event, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      if (null == @event) { throw new ArgumentNullException(nameof(@event)); }

      return SerializeEvent(eventType, @event.GetType(), @event, eventContext, expectedType);
    }
    public static EventData SerializeEvent(string eventType, Type actualType, object @event, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      if (null == @event) { throw new ArgumentNullException(nameof(@event)); }

      var token = GetSerializationToken(actualType, expectedType);
      return SerializeEvent(token, eventType, actualType, @event, eventContext, expectedType);
    }

    internal static EventData SerializeEvent(SerializationToken token, string eventType, Type actualType, object @event, Dictionary<string, object> eventContext, Type expectedType)
    {
      if (string.IsNullOrWhiteSpace(eventType)) { eventType = RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType); }
      byte[] data;
      switch (token)
      {
        case SerializationToken.GzJson:
          data = _gzJsonSerializer.Serialize(@event);
          break;
        case SerializationToken.Lz4Json:
          data = _lz4JsonSerializer.Serialize(@event);
          break;
        case SerializationToken.Hyperion:
          data = _hyperionSerializer.Serialize(@event);
          break;
        case SerializationToken.GzHyperion:
          data = _gzHyperionSerializer.Serialize(@event);
          break;
        case SerializationToken.Lz4Hyperion:
          data = _lz4HyperionSerializer.Serialize(@event);
          break;
        case SerializationToken.Protobuf:
          data = _protobufSerializer.Serialize(@event);
          break;
        case SerializationToken.GzProtobuf:
          data = _gzProtobufSerializer.Serialize(@event);
          break;
        case SerializationToken.Lz4Protobuf:
          data = _lz4ProtobufSerializer.Serialize(@event);
          break;
        case SerializationToken.External:
          // 此处不考虑 expectedType
          if (TryLookupExternalSerializer(actualType, out IExternalSerializer serializer))
          {
            data = serializer.Serialize(@event);
          }
          else
          {
            throw new InvalidOperationException($"Non-serializable exception of type {actualType.AssemblyQualifiedName}");
          }
          break;
        case SerializationToken.Json:
        default:
          data = _jsonSerializer.Serialize(@event);
          break;
      }
      return new EventData(
          Guid.NewGuid(), eventType, SerializationToken.Json == token, data,
          _jsonFormatter.SerializeToBytes(new EventMetadata
          {
            EventType = RuntimeTypeNameFormatter.Serialize(actualType),
            Token = token,
            Context = eventContext
          }));
    }

    #endregion

    #region -- SerializeEvents --

    public static EventData[] SerializeEvents<TEvent>(IEnumerable<TEvent> events, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        return events.Select(_ => SerializeEvent(_, eventContext, expectedType)).ToArray();
      }
      else
      {
        return SerializeEvents(actualType, events, eventContext, expectedType);
      }
    }
    public static EventData[] SerializeEvents<TEvent>(Type actualType, IEnumerable<TEvent> events, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }

      var streamAttr = GetStreamProvider(actualType, expectedType);
      return SerializeEvents(streamAttr?.EventType, actualType, events, eventContext, expectedType);
    }

    internal static EventData[] SerializeEvents<TEvent>(StreamAttribute streamAttr, IEnumerable<TEvent> events, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      //if (null == streamAttr) { throw new ArgumentNullException(nameof(streamAttr)); }
      return SerializeEvents(streamAttr?.EventType, events, eventContext, expectedType);
    }
    internal static EventData[] SerializeEvents<TEvent>(StreamAttribute streamAttr, Type actualType, IEnumerable<TEvent> events, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      //if (null == streamAttr) { throw new ArgumentNullException(nameof(streamAttr)); }
      return SerializeEvents(streamAttr?.EventType, actualType, events, eventContext, expectedType);
    }

    public static EventData[] SerializeEvents<TEvent>(string eventType, IEnumerable<TEvent> events, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        return events.Select(_ => SerializeEvent(eventType, _, eventContext, expectedType)).ToArray();
      }
      else
      {
        return SerializeEvents(eventType, actualType, events, eventContext, expectedType);
      }
    }

    public static EventData[] SerializeEvents<TEvent>(string eventType, Type actualType, IEnumerable<TEvent> events, Dictionary<string, object> eventContext = null, Type expectedType = null)
    {
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var token = GetSerializationToken(actualType, expectedType);
      return events.Select(_ => SerializeEvent(token, eventType, actualType, _, eventContext, expectedType)).ToArray();
    }




    public static EventData[] SerializeEvents<TEvent>(IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, Type expectedType = null)
    {
      if (null == events) { throw new ArgumentNullException(nameof(events)); }
      if (null == eventContexts) { throw new ArgumentNullException(nameof(eventContexts)); }
      if (events.Count != eventContexts.Count) { throw new ArgumentOutOfRangeException(nameof(eventContexts)); }

      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        var list = new EventData[events.Count];
        for (var idx = 0; idx < events.Count; idx++)
        {
          list[idx] = SerializeEvent(events[idx], eventContexts[idx], expectedType);
        }
        return list;
      }
      else
      {
        return SerializeEvents(actualType, events, eventContexts, expectedType);
      }
    }
    public static EventData[] SerializeEvents<TEvent>(Type actualType, IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, Type expectedType = null)
    {
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }

      var streamAttr = GetStreamProvider(actualType, expectedType);
      return SerializeEvents(streamAttr?.EventType, actualType, events, eventContexts, expectedType);
    }

    internal static EventData[] SerializeEvents<TEvent>(StreamAttribute streamAttr, IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, Type expectedType = null)
    {
      if (null == streamAttr) { throw new ArgumentNullException(nameof(streamAttr)); }
      return SerializeEvents(streamAttr.EventType, events, eventContexts, expectedType);
    }
    internal static EventData[] SerializeEvents<TEvent>(StreamAttribute streamAttr, Type actualType, IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, Type expectedType = null)
    {
      if (null == streamAttr) { throw new ArgumentNullException(nameof(streamAttr)); }
      return SerializeEvents(streamAttr.EventType, actualType, events, eventContexts, expectedType);
    }

    public static EventData[] SerializeEvents<TEvent>(string eventType, IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, Type expectedType = null)
    {
      if (null == events) { throw new ArgumentNullException(nameof(events)); }
      if (null == eventContexts) { throw new ArgumentNullException(nameof(eventContexts)); }
      if (events.Count != eventContexts.Count) { throw new ArgumentOutOfRangeException(nameof(eventContexts)); }

      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        var list = new EventData[events.Count];
        for (var idx = 0; idx < events.Count; idx++)
        {
          list[idx] = SerializeEvent(eventType, events[idx], eventContexts[idx], expectedType);
        }
        return list;
      }
      else
      {
        return SerializeEvents(eventType, actualType, events, eventContexts, expectedType);
      }
    }

    public static EventData[] SerializeEvents<TEvent>(string eventType, Type actualType, IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, Type expectedType = null)
    {
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      if (null == events) { throw new ArgumentNullException(nameof(events)); }
      if (null == eventContexts) { throw new ArgumentNullException(nameof(eventContexts)); }
      if (events.Count != eventContexts.Count) { throw new ArgumentOutOfRangeException(nameof(eventContexts)); }

      var token = GetSerializationToken(actualType, expectedType);
      var list = new EventData[events.Count];
      for (var idx = 0; idx < events.Count; idx++)
      {
        list[idx] = SerializeEvent(token, eventType, actualType, events[idx], eventContexts[idx], expectedType);
      }

      return list;
    }

    #endregion

    #region -- DeserializeEvent --

    public static EventMetadata DeserializeMetadata(byte[] metadata)
    {
      const string _metadataEmpty = "The meta-data of EventRecord is not available.";
      if (null == metadata || metadata.Length == 0)
      {
        throw new EventMetadataDeserializationException(_metadataEmpty);
      }
      var meta = _jsonFormatter.DeserializeFromBytes<EventMetadata>(metadata);
      if (null == meta)
      {
        throw new EventMetadataDeserializationException(_metadataEmpty);
      }
      return meta;
    }

    public static IFullEvent DeserializeEvent(EventData eventData)
    {
      return DeserializeEvent(eventData.Metadata, eventData.Data);
    }
    public static IFullEvent<T> DeserializeEvent<T>(EventData eventData) where T : class
    {
      return DeserializeEvent<T>(eventData.Metadata, eventData.Data);
    }

    public static IFullEvent DeserializeEvent(byte[] metadata, byte[] data)
    {
      var meta = DeserializeMetadata(metadata);
      DeserializeEvent(meta, null, data, out IEventDescriptor eventDescriptor, out object obj);
      return new DefaultFullEvent { Descriptor = eventDescriptor, Value = obj };
    }

    public static IFullEvent<T> DeserializeEvent<T>(byte[] metadata, byte[] data) where T : class
    {
      var meta = DeserializeMetadata(metadata);
      DeserializeEvent(meta, null, data, out IEventDescriptor eventDescriptor, out object obj);
      return new DefaultFullEvent<T> { Descriptor = eventDescriptor, Value = obj as T };
    }


    public static IFullEvent DeserializeEvent(EventMetadata metadata, byte[] data)
    {
      return DeserializeEvent(metadata, null, data);
    }
    public static IFullEvent DeserializeEvent(EventMetadata metadata, Type eventType, byte[] data)
    {
      if (null == metadata) { throw new ArgumentNullException(nameof(metadata)); }

      DeserializeEvent(metadata, eventType, data, out IEventDescriptor eventDescriptor, out object obj);
      return new DefaultFullEvent { Descriptor = eventDescriptor, Value = obj };
    }

    public static IFullEvent<T> DeserializeEvent<T>(EventMetadata metadata, byte[] data) where T : class
    {
      return DeserializeEvent<T>(metadata, null, data);
    }
    public static IFullEvent<T> DeserializeEvent<T>(EventMetadata metadata, Type eventType, byte[] data) where T : class
    {
      if (null == metadata) { throw new ArgumentNullException(nameof(metadata)); }

      DeserializeEvent(metadata, eventType, data, out IEventDescriptor eventDescriptor, out object obj);
      return new DefaultFullEvent<T> { Descriptor = eventDescriptor, Value = obj as T };
    }

    private static void DeserializeEvent(EventMetadata meta, Type eventType, byte[] data, out IEventDescriptor eventDescriptor, out object obj)
    {
      eventDescriptor = meta.Context != null ? new DefaultEventDescriptor(meta.Context) : NullEventDescriptor.Instance;

      if (null == data || data.Length == 0)
      {
        obj = null;
        return;
      }
      try
      {
        var type = eventType ?? TypeUtils.ResolveType(meta.EventType);
        switch (meta.Token)
        {
          case SerializationToken.GzJson:
            obj = _gzJsonSerializer.Deserialize(type, data);
            break;
          case SerializationToken.Lz4Json:
            obj = _lz4JsonSerializer.Deserialize(type, data);
            break;
          case SerializationToken.Hyperion:
            obj = _hyperionSerializer.Deserialize(type, data);
            break;
          case SerializationToken.GzHyperion:
            obj = _gzHyperionSerializer.Deserialize(type, data);
            break;
          case SerializationToken.Lz4Hyperion:
            obj = _lz4HyperionSerializer.Deserialize(type, data);
            break;
          case SerializationToken.Protobuf:
            obj = _protobufSerializer.Deserialize(type, data);
            break;
          case SerializationToken.GzProtobuf:
            obj = _gzProtobufSerializer.Deserialize(type, data);
            break;
          case SerializationToken.Lz4Protobuf:
            obj = _lz4ProtobufSerializer.Deserialize(type, data);
            break;
          case SerializationToken.External:
            if (TryLookupExternalSerializer(type, out IExternalSerializer serializer))
            {
              obj = serializer.Deserialize(type, data);
            }
            else
            {
              throw new Exception($"Non-serializable exception of type {type.AssemblyQualifiedName}");
            }
            break;
          case SerializationToken.Json:
          default:
            obj = _jsonSerializer.Deserialize(type, data);
            break;
        }
      }
      catch (Exception exc)
      {
        throw new EventDataDeserializationException(exc.Message, exc);
      }
    }

    #endregion
  }
}
