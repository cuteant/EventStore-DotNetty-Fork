using System;
using System.Linq.Expressions;
using System.Reflection;

#if CLIENTAPI
namespace EventStore.ClientAPI.Common.Utils
#else
namespace EventStore.Common.Utils
#endif
{
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TItem">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    public sealed class ActionMatchBuilder<TItem, TArg1> : SimpleMatchBuilderBase<Action<TItem, TArg1>, TItem, object>
    {
        private ParameterExpression _parameter1;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter1 => _parameter1 ?? (_parameter1 = Expression.Parameter(typeof(TArg1), "arg1"));

        private ParameterExpression[] _parameters;
        protected override ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] 
            { 
                Parameter, Parameter1
            });
        private ParameterExpression[] _bindedParameters;
        protected override ParameterExpression[] BindedParameters => _bindedParameters ?? (_bindedParameters = new[] 
            { 
                Parameter1
            });

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1> handler) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1) => handler(_, arg1));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1>> processor) //where TCtx : TItem
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1) => handler(_, arg1), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TItem
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Action<TItem, TArg1> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1) => handler(_, arg1));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem, TArg1> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1) => handler(_, arg1));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public void Match(TItem value, TArg1 arg1) => MatcherFunc(value, arg1);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem, TArg1>(ActionMatchBuilder<TItem, TArg1> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TItem">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    public sealed class ActionMatchBuilder<TItem, TArg1, TArg2> : SimpleMatchBuilderBase<Action<TItem, TArg1, TArg2>, TItem, object>
    {
        private ParameterExpression _parameter1;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter1 => _parameter1 ?? (_parameter1 = Expression.Parameter(typeof(TArg1), "arg1"));
        private ParameterExpression _parameter2;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter2 => _parameter2 ?? (_parameter2 = Expression.Parameter(typeof(TArg2), "arg2"));

        private ParameterExpression[] _parameters;
        protected override ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] 
            { 
                Parameter, Parameter1, Parameter2
            });
        private ParameterExpression[] _bindedParameters;
        protected override ParameterExpression[] BindedParameters => _bindedParameters ?? (_bindedParameters = new[] 
            { 
                Parameter1, Parameter2
            });

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2> handler) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2) => handler(_, arg1, arg2));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2>> processor) //where TCtx : TItem
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2) => handler(_, arg1, arg2), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TItem
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Action<TItem, TArg1, TArg2> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2) => handler(_, arg1, arg2));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem, TArg1, TArg2> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2) => handler(_, arg1, arg2));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public void Match(TItem value, TArg1 arg1, TArg2 arg2) => MatcherFunc(value, arg1, arg2);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem, TArg1, TArg2>(ActionMatchBuilder<TItem, TArg1, TArg2> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TItem">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    public sealed class ActionMatchBuilder<TItem, TArg1, TArg2, TArg3> : SimpleMatchBuilderBase<Action<TItem, TArg1, TArg2, TArg3>, TItem, object>
    {
        private ParameterExpression _parameter1;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter1 => _parameter1 ?? (_parameter1 = Expression.Parameter(typeof(TArg1), "arg1"));
        private ParameterExpression _parameter2;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter2 => _parameter2 ?? (_parameter2 = Expression.Parameter(typeof(TArg2), "arg2"));
        private ParameterExpression _parameter3;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter3 => _parameter3 ?? (_parameter3 = Expression.Parameter(typeof(TArg3), "arg3"));

        private ParameterExpression[] _parameters;
        protected override ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] 
            { 
                Parameter, Parameter1, Parameter2, Parameter3
            });
        private ParameterExpression[] _bindedParameters;
        protected override ParameterExpression[] BindedParameters => _bindedParameters ?? (_bindedParameters = new[] 
            { 
                Parameter1, Parameter2, Parameter3
            });

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3> handler) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3) => handler(_, arg1, arg2, arg3));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3>> processor) //where TCtx : TItem
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3) => handler(_, arg1, arg2, arg3), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TItem
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Action<TItem, TArg1, TArg2, TArg3> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3) => handler(_, arg1, arg2, arg3));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem, TArg1, TArg2, TArg3> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3) => handler(_, arg1, arg2, arg3));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public void Match(TItem value, TArg1 arg1, TArg2 arg2, TArg3 arg3) => MatcherFunc(value, arg1, arg2, arg3);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem, TArg1, TArg2, TArg3>(ActionMatchBuilder<TItem, TArg1, TArg2, TArg3> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TItem">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    public sealed class ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4> : SimpleMatchBuilderBase<Action<TItem, TArg1, TArg2, TArg3, TArg4>, TItem, object>
    {
        private ParameterExpression _parameter1;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter1 => _parameter1 ?? (_parameter1 = Expression.Parameter(typeof(TArg1), "arg1"));
        private ParameterExpression _parameter2;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter2 => _parameter2 ?? (_parameter2 = Expression.Parameter(typeof(TArg2), "arg2"));
        private ParameterExpression _parameter3;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter3 => _parameter3 ?? (_parameter3 = Expression.Parameter(typeof(TArg3), "arg3"));
        private ParameterExpression _parameter4;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter4 => _parameter4 ?? (_parameter4 = Expression.Parameter(typeof(TArg4), "arg4"));

        private ParameterExpression[] _parameters;
        protected override ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] 
            { 
                Parameter, Parameter1, Parameter2, Parameter3, Parameter4
            });
        private ParameterExpression[] _bindedParameters;
        protected override ParameterExpression[] BindedParameters => _bindedParameters ?? (_bindedParameters = new[] 
            { 
                Parameter1, Parameter2, Parameter3, Parameter4
            });

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4> handler) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4) => handler(_, arg1, arg2, arg3, arg4));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4>> processor) //where TCtx : TItem
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4) => handler(_, arg1, arg2, arg3, arg4), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TItem
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4) => handler(_, arg1, arg2, arg3, arg4));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4) => handler(_, arg1, arg2, arg3, arg4));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public void Match(TItem value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) => MatcherFunc(value, arg1, arg2, arg3, arg4);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem, TArg1, TArg2, TArg3, TArg4>(ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TItem">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TArg5">Argument type</typeparam>
    public sealed class ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5> : SimpleMatchBuilderBase<Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5>, TItem, object>
    {
        private ParameterExpression _parameter1;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter1 => _parameter1 ?? (_parameter1 = Expression.Parameter(typeof(TArg1), "arg1"));
        private ParameterExpression _parameter2;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter2 => _parameter2 ?? (_parameter2 = Expression.Parameter(typeof(TArg2), "arg2"));
        private ParameterExpression _parameter3;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter3 => _parameter3 ?? (_parameter3 = Expression.Parameter(typeof(TArg3), "arg3"));
        private ParameterExpression _parameter4;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter4 => _parameter4 ?? (_parameter4 = Expression.Parameter(typeof(TArg4), "arg4"));
        private ParameterExpression _parameter5;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter5 => _parameter5 ?? (_parameter5 = Expression.Parameter(typeof(TArg5), "arg5"));

        private ParameterExpression[] _parameters;
        protected override ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] 
            { 
                Parameter, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5
            });
        private ParameterExpression[] _bindedParameters;
        protected override ParameterExpression[] BindedParameters => _bindedParameters ?? (_bindedParameters = new[] 
            { 
                Parameter1, Parameter2, Parameter3, Parameter4, Parameter5
            });

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5> handler) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5) => handler(_, arg1, arg2, arg3, arg4, arg5));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5>> processor) //where TCtx : TItem
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5) => handler(_, arg1, arg2, arg3, arg4, arg5), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TItem
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5) => handler(_, arg1, arg2, arg3, arg4, arg5));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5) => handler(_, arg1, arg2, arg3, arg4, arg5));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public void Match(TItem value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5>(ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TItem">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TArg5">Argument type</typeparam>
    /// <typeparam name="TArg6">Argument type</typeparam>
    public sealed class ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> : SimpleMatchBuilderBase<Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>, TItem, object>
    {
        private ParameterExpression _parameter1;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter1 => _parameter1 ?? (_parameter1 = Expression.Parameter(typeof(TArg1), "arg1"));
        private ParameterExpression _parameter2;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter2 => _parameter2 ?? (_parameter2 = Expression.Parameter(typeof(TArg2), "arg2"));
        private ParameterExpression _parameter3;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter3 => _parameter3 ?? (_parameter3 = Expression.Parameter(typeof(TArg3), "arg3"));
        private ParameterExpression _parameter4;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter4 => _parameter4 ?? (_parameter4 = Expression.Parameter(typeof(TArg4), "arg4"));
        private ParameterExpression _parameter5;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter5 => _parameter5 ?? (_parameter5 = Expression.Parameter(typeof(TArg5), "arg5"));
        private ParameterExpression _parameter6;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter6 => _parameter6 ?? (_parameter6 = Expression.Parameter(typeof(TArg6), "arg6"));

        private ParameterExpression[] _parameters;
        protected override ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] 
            { 
                Parameter, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6
            });
        private ParameterExpression[] _bindedParameters;
        protected override ParameterExpression[] BindedParameters => _bindedParameters ?? (_bindedParameters = new[] 
            { 
                Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6
            });

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> handler) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5, arg6) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>> processor) //where TCtx : TItem
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5, arg6) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TItem
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public void Match(TItem value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5, arg6);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TItem">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TArg5">Argument type</typeparam>
    /// <typeparam name="TArg6">Argument type</typeparam>
    /// <typeparam name="TArg7">Argument type</typeparam>
    public sealed class ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> : SimpleMatchBuilderBase<Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>, TItem, object>
    {
        private ParameterExpression _parameter1;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter1 => _parameter1 ?? (_parameter1 = Expression.Parameter(typeof(TArg1), "arg1"));
        private ParameterExpression _parameter2;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter2 => _parameter2 ?? (_parameter2 = Expression.Parameter(typeof(TArg2), "arg2"));
        private ParameterExpression _parameter3;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter3 => _parameter3 ?? (_parameter3 = Expression.Parameter(typeof(TArg3), "arg3"));
        private ParameterExpression _parameter4;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter4 => _parameter4 ?? (_parameter4 = Expression.Parameter(typeof(TArg4), "arg4"));
        private ParameterExpression _parameter5;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter5 => _parameter5 ?? (_parameter5 = Expression.Parameter(typeof(TArg5), "arg5"));
        private ParameterExpression _parameter6;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter6 => _parameter6 ?? (_parameter6 = Expression.Parameter(typeof(TArg6), "arg6"));
        private ParameterExpression _parameter7;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter7 => _parameter7 ?? (_parameter7 = Expression.Parameter(typeof(TArg7), "arg7"));

        private ParameterExpression[] _parameters;
        protected override ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] 
            { 
                Parameter, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7
            });
        private ParameterExpression[] _bindedParameters;
        protected override ParameterExpression[] BindedParameters => _bindedParameters ?? (_bindedParameters = new[] 
            { 
                Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7
            });

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> handler) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>> processor) //where TCtx : TItem
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TItem
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public void Match(TItem value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TItem">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TArg5">Argument type</typeparam>
    /// <typeparam name="TArg6">Argument type</typeparam>
    /// <typeparam name="TArg7">Argument type</typeparam>
    /// <typeparam name="TArg8">Argument type</typeparam>
    public sealed class ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> : SimpleMatchBuilderBase<Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>, TItem, object>
    {
        private ParameterExpression _parameter1;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter1 => _parameter1 ?? (_parameter1 = Expression.Parameter(typeof(TArg1), "arg1"));
        private ParameterExpression _parameter2;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter2 => _parameter2 ?? (_parameter2 = Expression.Parameter(typeof(TArg2), "arg2"));
        private ParameterExpression _parameter3;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter3 => _parameter3 ?? (_parameter3 = Expression.Parameter(typeof(TArg3), "arg3"));
        private ParameterExpression _parameter4;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter4 => _parameter4 ?? (_parameter4 = Expression.Parameter(typeof(TArg4), "arg4"));
        private ParameterExpression _parameter5;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter5 => _parameter5 ?? (_parameter5 = Expression.Parameter(typeof(TArg5), "arg5"));
        private ParameterExpression _parameter6;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter6 => _parameter6 ?? (_parameter6 = Expression.Parameter(typeof(TArg6), "arg6"));
        private ParameterExpression _parameter7;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter7 => _parameter7 ?? (_parameter7 = Expression.Parameter(typeof(TArg7), "arg7"));
        private ParameterExpression _parameter8;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter8 => _parameter8 ?? (_parameter8 = Expression.Parameter(typeof(TArg8), "arg8"));

        private ParameterExpression[] _parameters;
        protected override ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] 
            { 
                Parameter, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7, Parameter8
            });
        private ParameterExpression[] _bindedParameters;
        protected override ParameterExpression[] BindedParameters => _bindedParameters ?? (_bindedParameters = new[] 
            { 
                Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7, Parameter8
            });

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> handler) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>> processor) //where TCtx : TItem
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TItem
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public void Match(TItem value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TItem">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TArg5">Argument type</typeparam>
    /// <typeparam name="TArg6">Argument type</typeparam>
    /// <typeparam name="TArg7">Argument type</typeparam>
    /// <typeparam name="TArg8">Argument type</typeparam>
    /// <typeparam name="TArg9">Argument type</typeparam>
    public sealed class ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> : SimpleMatchBuilderBase<Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>, TItem, object>
    {
        private ParameterExpression _parameter1;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter1 => _parameter1 ?? (_parameter1 = Expression.Parameter(typeof(TArg1), "arg1"));
        private ParameterExpression _parameter2;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter2 => _parameter2 ?? (_parameter2 = Expression.Parameter(typeof(TArg2), "arg2"));
        private ParameterExpression _parameter3;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter3 => _parameter3 ?? (_parameter3 = Expression.Parameter(typeof(TArg3), "arg3"));
        private ParameterExpression _parameter4;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter4 => _parameter4 ?? (_parameter4 = Expression.Parameter(typeof(TArg4), "arg4"));
        private ParameterExpression _parameter5;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter5 => _parameter5 ?? (_parameter5 = Expression.Parameter(typeof(TArg5), "arg5"));
        private ParameterExpression _parameter6;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter6 => _parameter6 ?? (_parameter6 = Expression.Parameter(typeof(TArg6), "arg6"));
        private ParameterExpression _parameter7;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter7 => _parameter7 ?? (_parameter7 = Expression.Parameter(typeof(TArg7), "arg7"));
        private ParameterExpression _parameter8;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter8 => _parameter8 ?? (_parameter8 = Expression.Parameter(typeof(TArg8), "arg8"));
        private ParameterExpression _parameter9;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter9 => _parameter9 ?? (_parameter9 = Expression.Parameter(typeof(TArg9), "arg9"));

        private ParameterExpression[] _parameters;
        protected override ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] 
            { 
                Parameter, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7, Parameter8, Parameter9
            });
        private ParameterExpression[] _bindedParameters;
        protected override ParameterExpression[] BindedParameters => _bindedParameters ?? (_bindedParameters = new[] 
            { 
                Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7, Parameter8, Parameter9
            });

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> handler) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>> processor) //where TCtx : TItem
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TItem
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public void Match(TItem value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TItem">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TArg5">Argument type</typeparam>
    /// <typeparam name="TArg6">Argument type</typeparam>
    /// <typeparam name="TArg7">Argument type</typeparam>
    /// <typeparam name="TArg8">Argument type</typeparam>
    /// <typeparam name="TArg9">Argument type</typeparam>
    /// <typeparam name="TArg10">Argument type</typeparam>
    public sealed class ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> : SimpleMatchBuilderBase<Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>, TItem, object>
    {
        private ParameterExpression _parameter1;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter1 => _parameter1 ?? (_parameter1 = Expression.Parameter(typeof(TArg1), "arg1"));
        private ParameterExpression _parameter2;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter2 => _parameter2 ?? (_parameter2 = Expression.Parameter(typeof(TArg2), "arg2"));
        private ParameterExpression _parameter3;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter3 => _parameter3 ?? (_parameter3 = Expression.Parameter(typeof(TArg3), "arg3"));
        private ParameterExpression _parameter4;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter4 => _parameter4 ?? (_parameter4 = Expression.Parameter(typeof(TArg4), "arg4"));
        private ParameterExpression _parameter5;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter5 => _parameter5 ?? (_parameter5 = Expression.Parameter(typeof(TArg5), "arg5"));
        private ParameterExpression _parameter6;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter6 => _parameter6 ?? (_parameter6 = Expression.Parameter(typeof(TArg6), "arg6"));
        private ParameterExpression _parameter7;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter7 => _parameter7 ?? (_parameter7 = Expression.Parameter(typeof(TArg7), "arg7"));
        private ParameterExpression _parameter8;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter8 => _parameter8 ?? (_parameter8 = Expression.Parameter(typeof(TArg8), "arg8"));
        private ParameterExpression _parameter9;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter9 => _parameter9 ?? (_parameter9 = Expression.Parameter(typeof(TArg9), "arg9"));
        private ParameterExpression _parameter10;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter10 => _parameter10 ?? (_parameter10 = Expression.Parameter(typeof(TArg10), "arg10"));

        private ParameterExpression[] _parameters;
        protected override ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] 
            { 
                Parameter, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7, Parameter8, Parameter9, Parameter10
            });
        private ParameterExpression[] _bindedParameters;
        protected override ParameterExpression[] BindedParameters => _bindedParameters ?? (_bindedParameters = new[] 
            { 
                Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7, Parameter8, Parameter9, Parameter10
            });

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> handler) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>> processor) //where TCtx : TItem
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            AddHandler<TCtx>((_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TItem
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public void Match(TItem value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(ActionMatchBuilder<TItem, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> matcher) => matcher.MatcherFunc;
    }
}