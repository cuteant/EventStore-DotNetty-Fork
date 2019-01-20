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
        public void Match<TCtx>(Action<TCtx> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: _ => handler(_));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Action<TCtx>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TItem, TCtx>> binder, Expression<Action<TCtx>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Action<TItem> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: _ => handler(_));
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

        public void MatchAny(Expression<TDelegate> processor)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: processor);
        }

        /// <summary>Adds predicated-based matching case</summary>
        public void Match(Expression<Predicate<TItem>> condition, Expression<TDelegate> processor)
        {
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
        public void Match<TCtx>(Func<TCtx, TOut> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: _ => handler(_));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TOut>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TIn, TCtx>> binder, Expression<Func<TCtx, TOut>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Func<TIn, TOut> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: _ => handler(_));
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

        public void MatchAny(Expression<TDelegate> processor)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: processor);
        }

        /// <summary>Adds predicated-based matching case</summary>
        public void Match(Expression<Predicate<TIn>> condition, Expression<TDelegate> processor)
        {
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

            var lambda = Expression.Lambda<TDelegate>(matcherExpression, Parameters);
            var matcher = lambda.Compile();
            return matcher;
        }

        private TDelegate _matcher;

        protected TDelegate MatcherFunc => _matcher ?? (_matcher = CompileMatcher());

        /// <summary>Creates Func&lt;T&gt; instance</summary>
        public TDelegate Build() => MatcherFunc;
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