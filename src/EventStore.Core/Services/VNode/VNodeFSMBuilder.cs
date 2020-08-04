using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.Messaging;

namespace EventStore.Core.Services.VNode
{
    /// <summary>
    /// Builder syntax for constructing <see cref="VNodeFSM"/> in the code
    /// </summary>
    public class VNodeFSMBuilder
    {
        private readonly Func<VNodeState> _getState;
        private readonly Dictionary<Type, Action<VNodeState, Message>>[] _handlers;
        private readonly Action<VNodeState, Message>[] _defaultHandlers;

        public VNodeFSMBuilder(Func<VNodeState> getState)
        {
            _getState = getState;

            var maxState = Enum.GetValues(typeof (VNodeState)).Cast<int>().Max();
            _handlers = new Dictionary<Type, Action<VNodeState, Message>>[maxState + 1];
            _defaultHandlers = new Action<VNodeState, Message>[maxState + 1];
        }

        internal void AddHandler<TActualMessage>(VNodeState state, Action<VNodeState, Message> handler)
            where TActualMessage: Message
        {
            var stateNum = (int) state;
            
            Dictionary<Type, Action<VNodeState, Message>> stateHandlers = _handlers[stateNum];
            if (stateHandlers is null)
                stateHandlers = _handlers[stateNum] = new Dictionary<Type, Action<VNodeState, Message>>();

            //var existingHandler = stateHandlers[typeof (TActualMessage)];
            //stateHandlers[typeof (TActualMessage)] = existingHandler is null
            //                                            ? handler
            //                                            : (s, m) => { existingHandler(s, m); handler(s, m); };

            if (stateHandlers.ContainsKey(typeof(TActualMessage)))
                ThrowHelper.ThrowInvalidOperationException_HandlerAlreadyDefinedForStateAndMessage<TActualMessage>(state);
            stateHandlers[typeof(TActualMessage)] = handler;
        }

        internal void AddDefaultHandler(VNodeState state, Action<VNodeState, Message> handler)
        {
            var stateNum = (int)state;
            //var existingHandler = _defaultHandlers[stateNum];
            //_defaultHandlers[stateNum] = existingHandler is null
            //                                ? handler
            //                                : (s, m) => { existingHandler(s, m); handler(s, m); };
            if (_defaultHandlers[stateNum] is object)
                ThrowHelper.ThrowInvalidOperationException_DefaultHandlerAlreadyDefinedForState(state);
            _defaultHandlers[stateNum] = handler;
        }

        public VNodeFSMStatesDefinition InAnyState()
        {
            var allStates = Enum.GetValues(typeof (VNodeState)).Cast<VNodeState>().ToArray();
            return new VNodeFSMStatesDefinition(this, allStates);
        }

        public VNodeFSMStatesDefinition InState(VNodeState state)
        {
            return new VNodeFSMStatesDefinition(this, state);
        }

        public VNodeFSMStatesDefinition InStates(params VNodeState[] states)
        {
            return new VNodeFSMStatesDefinition(this, states);
        }

        public VNodeFSMStatesDefinition InAllStatesExcept(VNodeState[] states)
        {
            if (0u >= (uint)states.Length) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.states_Length); }

            var s = Enum.GetValues(typeof (VNodeState)).Cast<VNodeState>().Except(states).ToArray();
            return new VNodeFSMStatesDefinition(this, s);
        }

        public VNodeFSM Build()
        {
            return new VNodeFSM(_getState, _handlers, _defaultHandlers);
        }
    }
}