using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

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
    public sealed class SimpleMatchBuilder<TItem> : ActionMatchBuilderBase<Action<TItem>, TItem>
    {
        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx> handler) where TCtx : TItem
        {
            Match<TCtx>(processor: _ => handler(_));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Action<TCtx>> processor) where TCtx : TItem
        {
            EnsureCanAdd();
            var ctxType = typeof(TCtx);
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            BlockExpression caseExpr;
            if (ctxType.GetTypeInfo().IsValueType)
            {
                caseExpr = Expression.Block(
                    Expression.IfThen(
                        Expression.TypeIs(Parameter, ctxType),
                        Expression.Block(
                            new[] { bindResult },
                            Expression.Assign(bindResult, Expression.Convert(Parameter, ctxType)),
                            Expression.Return(RetPoint, Expression.Invoke(processor, bindResult))
                        )
                    )
                );
            }
            else
            {
                caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.TypeAs(Parameter, ctxType)),
                Expression.IfThen(
                    Expression.NotEqual(bindResult, Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult))
                ));
            }
            CaseExpressionsList.Add(caseExpr);
            if (ctxType == ItemType) { _state = State.MatchAnyAdded; }
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Action<TCtx> handler, Predicate<TCtx> shouldHandle) where TCtx : TItem
        {
            Match<TCtx>(processor: _ => handler(_), condition: _ => shouldHandle(_));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Action<TCtx>> processor, Expression<Predicate<TCtx>> condition) where TCtx : TItem
        {
            EnsureCanAdd();
            var ctxType = typeof(TCtx);
            var bindResult = Expression.Variable(ctxType, "binded");
            BlockExpression caseExpr;
            if (ctxType.GetTypeInfo().IsValueType)
            {
                caseExpr = Expression.Block(
                    Expression.IfThen(
                        Expression.TypeIs(Parameter, ctxType),
                        Expression.Block(
                            new[] { bindResult },
                            Expression.Assign(bindResult, Expression.Convert(Parameter, ctxType)),
                            Expression.IfThen(
                                Expression.Invoke(condition, bindResult),
                                Expression.Return(RetPoint, Expression.Invoke(processor, bindResult))
                            )
                        )
                    )
                );
            }
            else
            {
                caseExpr = Expression.Block(
                    new[] { bindResult },
                    Expression.Assign(bindResult, Expression.TypeAs(Parameter, ctxType)),
                    Expression.IfThen(
                        Expression.AndAlso(
                            Expression.NotEqual(bindResult, Expression.Constant(null)),
                            Expression.Invoke(condition, bindResult)
                            ),
                        Expression.Return(RetPoint, Expression.Invoke(processor, bindResult))
                    )
                );
            }
            CaseExpressionsList.Add(caseExpr);
        }

        /// <summary>Adds predicated-based matching case</summary>
        public void Match(Predicate<TItem> shouldHandle, Action<TItem> handler)
        {
            Match(condition: _ => shouldHandle(_), processor: _ => handler(_));
        }

        public void MatchAny(Action<TItem> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: _ => handler(_));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Action<TItem> handler)
        {
            if (FinalExpr != null) { return false; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: _ => handler(_));
            _state = State.MatchAnyAdded;
            return true;
        }

        /// <summary>Performs match on the given value</summary>
        public void Match(TItem value) => MatcherFunc(value);

        /// <summary>Converts matcher into Action&lt;T&gt; instance</summary>
        public static implicit operator Action<TItem>(SimpleMatchBuilder<TItem> matcher) => matcher.MatcherFunc;
    }

    #endregion

    #region -- class ActionMatchBuilderBase<TDelegate, TItem> --

    /// <summary>Pattern matcher with void return type</summary>
    /// <typeparam name="TDelegate">Delegate type</typeparam>
    /// <typeparam name="TItem">Matcher argument type</typeparam>
    public abstract class ActionMatchBuilderBase<TDelegate, TItem>
        where TDelegate : Delegate
    {
        protected static readonly Type ItemType = typeof(TItem);

        /// <summary>List of case expressions</summary>
        protected readonly List<BlockExpression> CaseExpressionsList = new List<BlockExpression>();

        private ParameterExpression _parameter;
        /// <summary>Expression representing matching parameter</summary>
        protected ParameterExpression Parameter => _parameter ?? (_parameter = Expression.Parameter(typeof(TItem), "inputValue"));

        private ParameterExpression[] _parameters;
        protected virtual ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] { Parameter });

        protected BlockExpression FinalExpr;

        private LabelTarget _retPoint;
        /// <summary>Expression representing return point</summary>
        protected LabelTarget RetPoint => _retPoint ?? (_retPoint = Expression.Label());

        protected State _state;

        public void MatchAny(Expression<TDelegate> processor)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: processor);
        }

        /// <summary>Adds predicated-based matching case</summary>
        public void Match(Expression<Predicate<TItem>> condition, Expression<TDelegate> processor)
        {
            EnsureCanAdd();
            var caseExpr = CreatePredicatedBasedExpr(condition, processor);
            CaseExpressionsList.Add(caseExpr);
        }

        protected BlockExpression CreatePredicatedBasedExpr(Expression<Predicate<TItem>> condition, Expression<TDelegate> processor)
        {
            return Expression.Block(Expression.IfThen(
                Expression.Invoke(condition, Parameter),
                Expression.Return(RetPoint, Expression.Invoke(processor, Parameters))
                ));
        }

        private TDelegate CompileMatcher()
        {
            _state = State.Built;

            List<BlockExpression> caseExpressionsList;
            if (FinalExpr != null)
            {
                caseExpressionsList = new List<BlockExpression>(CaseExpressionsList.Count + 1);
                caseExpressionsList.AddRange(CaseExpressionsList);
                caseExpressionsList.Add(FinalExpr);
            }
            else
            {
                caseExpressionsList = CaseExpressionsList;
            }
            var finalExpressions = new Expression[]
            {
                //Expression.Throw(Expression.Constant(new MatchException("Provided value was not matched with any case"))),
                Expression.Call(null, MatcherUtils.ThrowMatchExceptionMethod),
                Expression.Label(RetPoint)
            };

            var matcherExpression = Expression.Block(caseExpressionsList.Concat(finalExpressions));

            return Expression.Lambda<TDelegate>(matcherExpression, Parameters).Compile();
        }

        private TDelegate _matcher;

        protected TDelegate MatcherFunc => _matcher ?? (_matcher = CompileMatcher());

        /// <summary>Creates Action&lt;T&gt; instance</summary>
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

        protected enum State
        {
            Adding, MatchAnyAdded, Built
        }
    }

    #endregion

    #region -- class SimpleMatchBuilder<TIn, TOut> --

    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public sealed class SimpleMatchBuilder<TIn, TOut> : FuncMatchBuilderBase<Func<TIn, TOut>, TIn, TOut>
    {
        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TOut> handler) where TCtx : TIn
        {
            Match<TCtx>(processor: _ => handler(_));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TOut>> processor) where TCtx : TIn
        {
            EnsureCanAdd();
            var ctxType = typeof(TCtx);
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            BlockExpression caseExpr;
            if (ctxType.GetTypeInfo().IsValueType)
            {
                caseExpr = Expression.Block(
                    Expression.IfThen(
                        Expression.TypeIs(Parameter, ctxType),
                        Expression.Block(
                            new[] { bindResult },
                            Expression.Assign(bindResult, Expression.Convert(Parameter, ctxType)),
                            Expression.Return(RetPoint, Expression.Invoke(processor, bindResult))
                        )
                    )
                );
            }
            else
            {
                caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.TypeAs(Parameter, ctxType)),
                Expression.IfThen(
                    Expression.NotEqual(bindResult, Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult))
                ));
            }
            CaseExpressionsList.Add(caseExpr);
            if (ctxType == ItemType) { _state = State.MatchAnyAdded; }
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TOut> handler, Predicate<TCtx> shouldHandle) where TCtx : TIn
        {
            Match<TCtx>(processor: _ => handler(_), condition: _ => shouldHandle(_));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TOut>> processor, Expression<Predicate<TCtx>> condition) where TCtx : TIn
        {
            EnsureCanAdd();
            var ctxType = typeof(TCtx);
            var bindResult = Expression.Variable(ctxType, "binded");
            BlockExpression caseExpr;
            if (ctxType.GetTypeInfo().IsValueType)
            {
                caseExpr = Expression.Block(
                    Expression.IfThen(
                        Expression.TypeIs(Parameter, ctxType),
                        Expression.Block(
                            new[] { bindResult },
                            Expression.Assign(bindResult, Expression.Convert(Parameter, ctxType)),
                            Expression.IfThen(
                                Expression.Invoke(condition, bindResult),
                                Expression.Return(RetPoint, Expression.Invoke(processor, bindResult))
                            )
                        )
                    )
                );
            }
            else
            {
                caseExpr = Expression.Block(
                    new[] { bindResult },
                    Expression.Assign(bindResult, Expression.TypeAs(Parameter, ctxType)),
                    Expression.IfThen(
                        Expression.AndAlso(
                            Expression.NotEqual(bindResult, Expression.Constant(null)),
                            Expression.Invoke(condition, bindResult)
                            ),
                        Expression.Return(RetPoint, Expression.Invoke(processor, bindResult))
                    )
                );
            }
            CaseExpressionsList.Add(caseExpr);
        }

        /// <summary>Adds predicated-based matching case</summary>
        public void Match(Predicate<TIn> shouldHandle, Func<TIn, TOut> handler)
        {
            Match(condition: _ => shouldHandle(_), processor: _ => handler(_));
        }

        public void MatchAny(Func<TIn, TOut> handler)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: _ => handler(_));
            _state = State.MatchAnyAdded;
        }

        public bool TryMatchAny(Func<TIn, TOut> handler)
        {
            if (FinalExpr != null) { return false; }
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

    #region -- class FuncMatchBuilderBase<TDelegate, TIn, TOut> --

    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TDelegate">Delegate type</typeparam>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public abstract class FuncMatchBuilderBase<TDelegate, TIn, TOut>
        where TDelegate : Delegate
    {
        protected static readonly Type ItemType = typeof(TIn);

        /// <summary>List of case expressions</summary>
        protected readonly List<BlockExpression> CaseExpressionsList = new List<BlockExpression>();

        private ParameterExpression _parameter;
        /// <summary>Expression representing matching parameter</summary>
        protected ParameterExpression Parameter => _parameter ?? (_parameter = Expression.Parameter(typeof(TIn), "inputValue"));

        private ParameterExpression[] _parameters;
        protected virtual ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] { Parameter });

        protected BlockExpression FinalExpr;

        private LabelTarget _retPoint;
        /// <summary>Expression representing return point</summary>
        protected LabelTarget RetPoint => _retPoint ?? (_retPoint = Expression.Label(typeof(TOut)));

        protected State _state;

        public void MatchAny(Expression<TDelegate> processor)
        {
            EnsureCanAdd();
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: processor);
        }

        /// <summary>Adds predicated-based matching case</summary>
        public void Match(Expression<Predicate<TIn>> condition, Expression<TDelegate> processor)
        {
            EnsureCanAdd();
            var caseExpr = CreatePredicatedBasedExpr(condition, processor);
            CaseExpressionsList.Add(caseExpr);
        }

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

            List<BlockExpression> caseExpressionsList;
            if (FinalExpr != null)
            {
                caseExpressionsList = new List<BlockExpression>(CaseExpressionsList.Count + 1);
                caseExpressionsList.AddRange(CaseExpressionsList);
                caseExpressionsList.Add(FinalExpr);
            }
            else
            {
                caseExpressionsList = CaseExpressionsList;
            }
            var finalExpressions = new Expression[]
            {
                //Expression.Throw(Expression.Constant(new MatchException("Provided value was not matched with any case"))),
                Expression.Call(null, MatcherUtils.ThrowMatchExceptionMethod),
                Expression.Label(RetPoint, Expression.Default(typeof(TOut)))
            };

            var matcherExpression = Expression.Block(caseExpressionsList.Concat(finalExpressions));

            return Expression.Lambda<TDelegate>(matcherExpression, Parameters).Compile();
        }

        private TDelegate _matcher;

        protected TDelegate MatcherFunc => _matcher ?? (_matcher = CompileMatcher());

        /// <summary>Creates Func&lt;T&gt; instance</summary>
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

        protected enum State
        {
            Adding, MatchAnyAdded, Built
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