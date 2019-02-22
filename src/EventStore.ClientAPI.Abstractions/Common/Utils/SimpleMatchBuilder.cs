using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using CuteAnt;

// TODO Expression.SwitchCase with C# 7 pattern matching, 
// None of the C# 7.0 feature have support in expression trees at this point. 
// https://stackoverflow.com/questions/46533824/expression-switchcase-with-c-sharp-7-pattern-matching

#if CLIENTAPI
namespace EventStore.ClientAPI.Common.Utils
#else
namespace EventStore.Common.Utils
#endif
{
    #region -- class SimpleMatchBuilder<TItem> --

    /// <summary>Pattern matcher with void return type</summary>
    /// <typeparam name="TItem">Matcher argument type</typeparam>
    public class SimpleMatchBuilder<TItem> : SimpleMatchBuilderBase<Action<TItem>, TItem, object>
    {
        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx> handler) where TCtx : TItem
        {
            AddHandler<TCtx>(_ => handler(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx>> processor) //where TCtx : TItem
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            AddHandler<TCtx>(_ => handler(_), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Action<TCtx>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TItem
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Action<TItem> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: _ => handler(_));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: _ => handler(_));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        internal void Match(TItem value) => MatcherFunc(value);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem>(SimpleMatchBuilder<TItem> matcher) => matcher.MatcherFunc;
    }

    #endregion

    #region -- class SimpleMatchBuilder<TIn, TOut> --

    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public class SimpleMatchBuilder<TIn, TOut> : SimpleMatchBuilderBase<Func<TIn, TOut>, TIn, TOut>
    {
        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TOut> handler) where TCtx : TIn
        {
            AddHandler<TCtx>(_ => handler(_));
        }

        private void AddHandler<TCtx>(Expression<Func<TCtx, TOut>> processor) //where TCtx : TIn
        {
            Add<TCtx>(processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TOut> handler, Predicate<TCtx> shouldHandle) where TCtx : TIn
        {
            AddHandler<TCtx>(_ => handler(_), _ => shouldHandle(_));
        }

        private void AddHandler<TCtx>(Expression<Func<TCtx, TOut>> processor, Expression<Predicate<TCtx>> condition) //where TCtx : TIn
        {
            Add<TCtx>(processor, condition);
        }

        public void MatchAny(Func<TIn, TOut> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: _ => handler(_));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Func<TIn, TOut> handler)
        {
            if (FinalExpr != null || _state != State.Adding) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: _ => handler(_));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public TOut Match(TIn value) => MatcherFunc(value);

        /// <summary>Converts matcher into Func&lt;T&gt; instance</summary>
        public static implicit operator Func<TIn, TOut>(SimpleMatchBuilder<TIn, TOut> matcher) => matcher.MatcherFunc;
    }

    #endregion

    #region -- class SimpleMatchBuilderBase<TDelegate, TIn, TOut> --

    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TDelegate">Delegate type</typeparam>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public abstract class SimpleMatchBuilderBase<TDelegate, TIn, TOut>
        where TDelegate : Delegate
    {
        /// <summary>TBD</summary>
        protected static readonly bool IsActionDelegate;
        static SimpleMatchBuilderBase()
        {
            IsActionDelegate = typeof(TDelegate).GetMethod("Invoke").ReturnType == typeof(void);
        }

        /// <summary>TBD</summary>
        protected static readonly Type ItemType = typeof(TIn);

        private ParameterExpression _parameter;
        /// <summary>Expression representing matching parameter</summary>
        protected ParameterExpression Parameter => _parameter ?? (_parameter = Expression.Parameter(typeof(TIn), "inputValue"));

        private ParameterExpression[] _parameters;
        /// <summary>TBD</summary>
        protected virtual ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] { Parameter });

        /// <summary>TBD</summary>
        protected virtual ParameterExpression[] BindedParameters => EmptyArray<ParameterExpression>.Instance;

        /// <summary>TBD</summary>
        protected BlockExpression FinalExpr;

        private LabelTarget _retPoint;
        /// <summary>Expression representing return point</summary>
        protected LabelTarget RetPoint
        {
            get
            {
                if (_retPoint != null) { return _retPoint; }

                if (IsActionDelegate)
                {
                    _retPoint = Expression.Label();
                }
                else
                {
                    _retPoint = Expression.Label(typeof(TOut));
                }
                return _retPoint;
            }
        }

        protected State _state;

        private IDictionary<object, object> _properties;
        /// <summary>sharing state</summary>
        public virtual IDictionary<object, object> Properties => _properties ?? (_properties = new Dictionary<object, object>());

        private readonly Dictionary<Type, PartialHandlerExpression> _partialHandlers = new Dictionary<Type, PartialHandlerExpression>();

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        protected void Add<TCtx>(Expression processor) //where TCtx : TIn
        {
            EnsureCanAdd();
            var ctxType = typeof(TCtx);
            var handler = GetPartialHandler(ctxType);
            if (handler.Processor != null) { return; }

            handler.Processor = processor;
            if (ctxType == ItemType) { _state = State.MatchAnyAdded; }
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        protected void Add<TCtx>(Expression processor, Expression condition) //where TCtx : TIn
        {
            EnsureCanAdd();
            var ctxType = typeof(TCtx);
            var handler = GetPartialHandler(ctxType);
            handler.PredicatedHandlers.Add(new PredicateAndHandlerExpression { Processor = processor, Condition = condition });
        }

        private PartialHandlerExpression GetPartialHandler(Type ctxType)
        {
            if (!_partialHandlers.TryGetValue(ctxType, out var handler))
            {
                handler = new PartialHandlerExpression();
                _partialHandlers[ctxType] = handler;
            }
            return handler;
        }

        private List<BlockExpression> GetCaseExpressions()
        {
            var caseExpressions = new List<BlockExpression>();

            foreach (var item in _partialHandlers)
            {
                var ctxType = item.Key;
                var partialHandler = item.Value;
                var predicatedHandlers = partialHandler.PredicatedHandlers;
                var processor = partialHandler.Processor;

                BlockExpression caseExpr;

                var bindResult = Expression.Variable(ctxType);
                var bindParams = new[] { bindResult };
                var allParams = bindParams.Concat(BindedParameters);

                List<Expression> GetPredicatedHandlerExpressions(bool isValueType)
                {
                    var list = new List<Expression>(2);
                    if (isValueType)
                    {
                        list.Add(Expression.Assign(bindResult, Expression.Convert(Parameter, ctxType)));
                    }
                    if (predicatedHandlers.Count > 0)
                    {
                        foreach (var predicatedHandler in predicatedHandlers)
                        {
                            list.Add(
                                Expression.IfThen(
                                    Expression.Invoke(predicatedHandler.Condition, bindResult),
                                    Expression.Return(RetPoint, Expression.Invoke(predicatedHandler.Processor, allParams))
                                )
                            );
                        }
                    }
                    if (processor != null)
                    {
                        list.Add(Expression.Return(RetPoint, Expression.Invoke(processor, allParams)));
                    }
                    return list;
                }

                if (ctxType.GetTypeInfo().IsValueType)
                {
                    caseExpr = Expression.Block(
                        Expression.IfThen(
                            Expression.TypeIs(Parameter, ctxType),
                            Expression.Block(bindParams, GetPredicatedHandlerExpressions(true))
                        )
                    );
                }
                else
                {
                    var exprList = new List<Expression>(2)
                    {
                        Expression.Assign(bindResult, Expression.TypeAs(Parameter, ctxType))
                    };
                    if (processor != null && predicatedHandlers.Count == 0)
                    {
                        exprList.Add(
                            Expression.IfThen(
                                Expression.NotEqual(bindResult, Expression.Constant(null)),
                                Expression.Return(RetPoint, Expression.Invoke(processor, allParams))
                            )
                        );
                    }
                    else if (null == processor && predicatedHandlers.Count == 1)
                    {
                        var predicatedHandler = predicatedHandlers[0];
                        exprList.Add(
                            Expression.IfThen(
                                Expression.AndAlso(
                                    Expression.NotEqual(bindResult, Expression.Constant(null)),
                                    Expression.Invoke(predicatedHandler.Condition, bindResult)
                                ),
                                Expression.Return(RetPoint, Expression.Invoke(predicatedHandler.Processor, allParams))
                            )
                        );
                    }
                    else
                    {
                        exprList.Add(
                            Expression.IfThen(
                                Expression.NotEqual(bindResult, Expression.Constant(null)),
                                Expression.Block(GetPredicatedHandlerExpressions(false))
                            )
                        );
                    }
                    caseExpr = Expression.Block(bindParams, exprList);
                }

                caseExpressions.Add(caseExpr);
            }

            return caseExpressions;
        }

        /// <summary>TBD</summary>
        protected void MatchAny(Expression<TDelegate> processor)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: processor);
        }

        /// <summary>TBD</summary>
        protected BlockExpression CreatePredicatedBasedExpr(Expression<Predicate<TIn>> condition, Expression<TDelegate> processor)
        {
            return Expression.Block(Expression.IfThen(
                Expression.Invoke(condition, Parameter),
                Expression.Return(RetPoint, Expression.Invoke(processor, Parameters))
                ));
        }

        private TDelegate CompileMatcher()
        {
            _state = State.Built;

            var caseExpressionsList = GetCaseExpressions();
            List<BlockExpression> caseExpressions;
            if (FinalExpr != null)
            {
                caseExpressions = new List<BlockExpression>(caseExpressionsList.Count + 1);
                caseExpressions.AddRange(caseExpressionsList);
                caseExpressions.Add(FinalExpr);
            }
            else
            {
                caseExpressions = caseExpressionsList;
            }
            LabelExpression labelExpr;
            if (IsActionDelegate)
            {
                labelExpr = Expression.Label(RetPoint);
            }
            else
            {
                labelExpr = Expression.Label(RetPoint, Expression.Default(typeof(TOut)));
            }
            var finalExpressions = new Expression[]
            {
                //Expression.Throw(Expression.Constant(new MatchException("Provided value was not matched with any case"))),
                Expression.Call(null, MatcherUtils.ThrowMatchExceptionMethod),
                labelExpr
            };

            var matcherExpression = Expression.Block(caseExpressions.Concat(finalExpressions));

            return Expression.Lambda<TDelegate>(matcherExpression, Parameters).Compile();
        }

        private TDelegate _matcher;

        /// <summary>TBD</summary>
        protected TDelegate MatcherFunc => _matcher ?? (_matcher = CompileMatcher());

        /// <summary>Creates delegate instance</summary>
        public TDelegate Build() => MatcherFunc;

        /// <summary>Throws an exception if a MatchAny handler has been added or the partial handler has been created.</summary>
        protected void EnsureCanAdd()
        {
            switch (_state)
            {
                case State.Adding:
                    return;
                case State.MatchAnyAdded:
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_MatchBuilder_MatchAnyAdded);
                    break;
                case State.Built:
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_MatchBuilder_Built);
                    break;
                default:
                    ThrowArgumentOutOfRangeException(_state);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentOutOfRangeException(State state)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException($"Whoa, this should not happen! Unknown state value={state}");
            }
        }

        /// <summary>TBD</summary>
        protected enum State
        {
            Adding, MatchAnyAdded, Built
        }

        sealed class PredicateAndHandlerExpression
        {
            public Expression Condition { get; set; }

            public Expression Processor { get; set; }
        }

        sealed class PartialHandlerExpression
        {
            public PartialHandlerExpression()
            {
                PredicatedHandlers = new List<PredicateAndHandlerExpression>();
            }

            public List<PredicateAndHandlerExpression> PredicatedHandlers { get; }

            public Expression Processor { get; set; }
        }
    }

    #endregion

    #region == class MatcherUtils ==

    internal static class MatcherUtils
    {
        public static readonly MethodInfo ThrowMatchExceptionMethod = typeof(MatcherUtils).GetMethod(nameof(MatcherUtils.ThrowMatchException));

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowMatchException()
        {
            throw GetException();
            MatchException GetException()
            {
                return new MatchException("Provided value was not matched with any case");
            }
        }
    }

    #endregion

    #region -- class MatchException --

    /// <summary>Represents error that occurs during pattern matching</summary>
    public class MatchException : ArgumentOutOfRangeException
    {
        /// <summary>Error message</summary>
        public new string Message { get; set; }

        internal MatchException(string message)
        {
            Message = message;
        }
    }

    #endregion
}