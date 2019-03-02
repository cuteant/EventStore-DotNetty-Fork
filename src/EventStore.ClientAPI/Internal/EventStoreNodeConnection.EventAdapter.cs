using System.Collections.Generic;

namespace EventStore.ClientAPI.Internal
{
    partial class EventStoreNodeConnection
    {
        private readonly IEventAdapter _eventAdapter;

        /// <summary>TBD</summary>
        public IEventAdapter EventAdapter => _eventAdapter;

        /// <summary>TBD</summary>
        /// <param name="evt"></param>
        /// <param name="eventContext"></param>
        /// <returns></returns>
        public EventData ToEventData(object evt, Dictionary<string, object> eventContext)
        {
            return _eventAdapter.Adapt(evt, _eventAdapter.ToEventMetadata(eventContext));
        }

        /// <summary>TBD</summary>
        /// <param name="evt"></param>
        /// <param name="eventMeta"></param>
        /// <returns></returns>
        public EventData ToEventData(object evt, IEventMetadata eventMeta = null)
        {
            return _eventAdapter.Adapt(evt, eventMeta);
        }

        /// <summary>TBD</summary>
        /// <param name="events"></param>
        /// <param name="eventContexts"></param>
        /// <returns></returns>
        public EventData[] ToEventDatas(IList<object> events, IList<Dictionary<string, object>> eventContexts)
        {
            return EventManager.ToEventDatas(_eventAdapter, events, eventContexts);
        }

        /// <summary>TBD</summary>
        /// <param name="events"></param>
        /// <param name="eventMetas"></param>
        /// <returns></returns>
        public EventData[] ToEventDatas(IList<object> events, IList<IEventMetadata> eventMetas = null)
        {
            return EventManager.ToEventDatas(_eventAdapter, events, eventMetas);
        }

        /// <summary>TBD</summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="events"></param>
        /// <param name="eventContexts"></param>
        /// <returns></returns>
        public EventData[] ToEventDatas<TEvent>(IList<TEvent> events, IList<Dictionary<string, object>> eventContexts)
        {
            return EventManager.ToEventDatas(_eventAdapter, events, eventContexts);
        }

        /// <summary>TBD</summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="events"></param>
        /// <param name="eventMetas"></param>
        /// <returns></returns>
        public EventData[] ToEventDatas<TEvent>(IList<TEvent> events, IList<IEventMetadata> eventMetas = null)
        {
            return EventManager.ToEventDatas(_eventAdapter, events, eventMetas);
        }
    }
}
