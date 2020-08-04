using System;
using EventStore.Core.Bus;
using EventStore.Core.Data;
using EventStore.Core.Messaging;

namespace EventStore.Core.Services.VNode
{
    public class VNodeFSMHandling<TMessage> where TMessage: Message
    {
        private readonly VNodeFSMStatesDefinition _stateDef;
        private readonly bool _defaultHandler;

        public VNodeFSMHandling(VNodeFSMStatesDefinition stateDef, bool defaultHandler = false)
        {
            _stateDef = stateDef;
            _defaultHandler = defaultHandler;
        }

        public VNodeFSMStatesDefinition Do(Action<VNodeState, TMessage> handler)
        {
            var states = _stateDef.States;
            for (int idx = 0; idx < states.Length; idx++)
            {
                VNodeState state = states[idx];
                if (_defaultHandler)
                    _stateDef.FSM.AddDefaultHandler(state, (s, m) => handler(s, (TMessage)m));
                else
                    _stateDef.FSM.AddHandler<TMessage>(state, (s, m) => handler(s, (TMessage)m));
            }
            return _stateDef;
        }

        public VNodeFSMStatesDefinition Do(Action<TMessage> handler)
        {
            var states = _stateDef.States;
            for (int idx = 0; idx < states.Length; idx++)
            {
                VNodeState state = states[idx];
                if (_defaultHandler)
                    _stateDef.FSM.AddDefaultHandler(state, (s, m) => handler((TMessage)m));
                else
                    _stateDef.FSM.AddHandler<TMessage>(state, (s, m) => handler((TMessage)m));
            }
            return _stateDef;
        }

        public VNodeFSMStatesDefinition Ignore()
        {
            var states = _stateDef.States;
            for (int idx = 0; idx < states.Length; idx++)
            {
                VNodeState state = states[idx];
                if (_defaultHandler)
                    _stateDef.FSM.AddDefaultHandler(state, (s, m) => { });
                else
                    _stateDef.FSM.AddHandler<TMessage>(state, (s, m) => { });    
            }
            return _stateDef;
        }

        public VNodeFSMStatesDefinition Throw()
        {
            var states = _stateDef.States;
            for (int idx = 0; idx < states.Length; idx++)
            {
                VNodeState state = states[idx];
                if (_defaultHandler)
                    _stateDef.FSM.AddDefaultHandler(state, (s, m) => { throw new NotSupportedException(); });
                else
                    _stateDef.FSM.AddHandler<TMessage>(state, (s, m) => { throw new NotSupportedException(); });
            }
            return _stateDef;
        }

        public VNodeFSMStatesDefinition ForwardTo(IPublisher publisher)
        {
            if (publisher is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.publisher); }
            return Do(publisher.Publish);
        }
    }
}