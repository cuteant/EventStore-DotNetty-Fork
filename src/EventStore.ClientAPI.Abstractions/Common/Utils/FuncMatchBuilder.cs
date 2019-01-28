﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


#if CLIENTAPI
namespace EventStore.ClientAPI.Common.Utils
#else
namespace EventStore.Common.Utils
#endif
{
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public sealed class FuncMatchBuilder<TIn, TArg1, TOut> : FuncMatchBuilderBase<Func<TIn, TArg1, TOut>, TIn, TOut>
    {
        private ParameterExpression _parameter1;
        /// <summary>Expression representing matching parameter</summary>
        private ParameterExpression Parameter1 => _parameter1 ?? (_parameter1 = Expression.Parameter(typeof(TArg1), "arg1"));

        private ParameterExpression[] _parameters;
        protected override ParameterExpression[] Parameters => _parameters ?? (_parameters = new[] 
            { 
                Parameter, Parameter1
            });

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TArg1, TOut> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: (_, arg1) => handler(_, arg1));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TArg1, TOut>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TIn, TCtx>> binder, Expression<Func<TCtx, TArg1, TOut>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult, Parameter1))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Func<TIn, TArg1, TOut> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1) => handler(_, arg1));
        }

        /// <summary>Performs match on the given value</summary>
        public TOut Match(TIn value, TArg1 arg1) => MatcherFunc(value, arg1);

        /// <summary>Converts matcher into Func&lt;T&gt; instance</summary>
        public static implicit operator Func<TIn, TArg1, TOut>(FuncMatchBuilder<TIn, TArg1, TOut> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public sealed class FuncMatchBuilder<TIn, TArg1, TArg2, TOut> : FuncMatchBuilderBase<Func<TIn, TArg1, TArg2, TOut>, TIn, TOut>
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

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TArg1, TArg2, TOut> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: (_, arg1, arg2) => handler(_, arg1, arg2));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TArg1, TArg2, TOut>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TIn, TCtx>> binder, Expression<Func<TCtx, TArg1, TArg2, TOut>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult, Parameter1, Parameter2))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Func<TIn, TArg1, TArg2, TOut> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2) => handler(_, arg1, arg2));
        }

        /// <summary>Performs match on the given value</summary>
        public TOut Match(TIn value, TArg1 arg1, TArg2 arg2) => MatcherFunc(value, arg1, arg2);

        /// <summary>Converts matcher into Func&lt;T&gt; instance</summary>
        public static implicit operator Func<TIn, TArg1, TArg2, TOut>(FuncMatchBuilder<TIn, TArg1, TArg2, TOut> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public sealed class FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TOut> : FuncMatchBuilderBase<Func<TIn, TArg1, TArg2, TArg3, TOut>, TIn, TOut>
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

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TArg1, TArg2, TArg3, TOut> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: (_, arg1, arg2, arg3) => handler(_, arg1, arg2, arg3));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TArg1, TArg2, TArg3, TOut>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TIn, TCtx>> binder, Expression<Func<TCtx, TArg1, TArg2, TArg3, TOut>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult, Parameter1, Parameter2, Parameter3))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Func<TIn, TArg1, TArg2, TArg3, TOut> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3) => handler(_, arg1, arg2, arg3));
        }

        /// <summary>Performs match on the given value</summary>
        public TOut Match(TIn value, TArg1 arg1, TArg2 arg2, TArg3 arg3) => MatcherFunc(value, arg1, arg2, arg3);

        /// <summary>Converts matcher into Func&lt;T&gt; instance</summary>
        public static implicit operator Func<TIn, TArg1, TArg2, TArg3, TOut>(FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TOut> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public sealed class FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TOut> : FuncMatchBuilderBase<Func<TIn, TArg1, TArg2, TArg3, TArg4, TOut>, TIn, TOut>
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

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TArg1, TArg2, TArg3, TArg4, TOut> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: (_, arg1, arg2, arg3, arg4) => handler(_, arg1, arg2, arg3, arg4));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TOut>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TIn, TCtx>> binder, Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TOut>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult, Parameter1, Parameter2, Parameter3, Parameter4))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Func<TIn, TArg1, TArg2, TArg3, TArg4, TOut> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4) => handler(_, arg1, arg2, arg3, arg4));
        }

        /// <summary>Performs match on the given value</summary>
        public TOut Match(TIn value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) => MatcherFunc(value, arg1, arg2, arg3, arg4);

        /// <summary>Converts matcher into Func&lt;T&gt; instance</summary>
        public static implicit operator Func<TIn, TArg1, TArg2, TArg3, TArg4, TOut>(FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TOut> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TArg5">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public sealed class FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TOut> : FuncMatchBuilderBase<Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TOut>, TIn, TOut>
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

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TOut> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: (_, arg1, arg2, arg3, arg4, arg5) => handler(_, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TOut>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TIn, TCtx>> binder, Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TOut>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TOut> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5) => handler(_, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>Performs match on the given value</summary>
        public TOut Match(TIn value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5);

        /// <summary>Converts matcher into Func&lt;T&gt; instance</summary>
        public static implicit operator Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TOut>(FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TOut> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TArg5">Argument type</typeparam>
    /// <typeparam name="TArg6">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public sealed class FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TOut> : FuncMatchBuilderBase<Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TOut>, TIn, TOut>
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

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TOut> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TOut>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TIn, TCtx>> binder, Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TOut>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TOut> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>Performs match on the given value</summary>
        public TOut Match(TIn value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5, arg6);

        /// <summary>Converts matcher into Func&lt;T&gt; instance</summary>
        public static implicit operator Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TOut>(FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TOut> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TArg5">Argument type</typeparam>
    /// <typeparam name="TArg6">Argument type</typeparam>
    /// <typeparam name="TArg7">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public sealed class FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TOut> : FuncMatchBuilderBase<Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TOut>, TIn, TOut>
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

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TOut> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TOut>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TIn, TCtx>> binder, Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TOut>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TOut> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>Performs match on the given value</summary>
        public TOut Match(TIn value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        /// <summary>Converts matcher into Func&lt;T&gt; instance</summary>
        public static implicit operator Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TOut>(FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TOut> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TArg5">Argument type</typeparam>
    /// <typeparam name="TArg6">Argument type</typeparam>
    /// <typeparam name="TArg7">Argument type</typeparam>
    /// <typeparam name="TArg8">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public sealed class FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TOut> : FuncMatchBuilderBase<Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TOut>, TIn, TOut>
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

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TOut> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TOut>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TIn, TCtx>> binder, Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TOut>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7, Parameter8))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TOut> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
        }

        /// <summary>Performs match on the given value</summary>
        public TOut Match(TIn value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        /// <summary>Converts matcher into Func&lt;T&gt; instance</summary>
        public static implicit operator Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TOut>(FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TOut> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
    /// <typeparam name="TArg1">Argument type</typeparam>
    /// <typeparam name="TArg2">Argument type</typeparam>
    /// <typeparam name="TArg3">Argument type</typeparam>
    /// <typeparam name="TArg4">Argument type</typeparam>
    /// <typeparam name="TArg5">Argument type</typeparam>
    /// <typeparam name="TArg6">Argument type</typeparam>
    /// <typeparam name="TArg7">Argument type</typeparam>
    /// <typeparam name="TArg8">Argument type</typeparam>
    /// <typeparam name="TArg9">Argument type</typeparam>
    /// <typeparam name="TOut">Return type</typeparam>
    public sealed class FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TOut> : FuncMatchBuilderBase<Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TOut>, TIn, TOut>
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

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TOut> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TOut>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TIn, TCtx>> binder, Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TOut>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7, Parameter8, Parameter9))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TOut> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
        }

        /// <summary>Performs match on the given value</summary>
        public TOut Match(TIn value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);

        /// <summary>Converts matcher into Func&lt;T&gt; instance</summary>
        public static implicit operator Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TOut>(FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TOut> matcher) => matcher.MatcherFunc;
    }
    /// <summary>Pattern matcher</summary>
    /// <typeparam name="TIn">Argument type</typeparam>
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
    /// <typeparam name="TOut">Return type</typeparam>
    public sealed class FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TOut> : FuncMatchBuilderBase<Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TOut>, TIn, TOut>
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

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TOut> handler) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TOut>> processor) where TCtx : class
        {
            Match(binder: _ => _ as TCtx, processor: processor);
        }

        /// <summary>Adds context-based matching case</summary>
        /// <typeparam name="TCtx">Context type</typeparam>
        public void Match<TCtx>(Expression<Func<TIn, TCtx>> binder, Expression<Func<TCtx, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TOut>> processor) where TCtx : class
        {
            var bindResult = Expression.Variable(typeof(TCtx), "binded");
            var caseExpr = Expression.Block(
                new[] { bindResult },
                Expression.Assign(bindResult, Expression.Invoke(binder, Parameter)),
                Expression.IfThen(
                    Expression.NotEqual(Expression.Convert(bindResult, typeof(object)), Expression.Constant(null)),
                    Expression.Return(RetPoint, Expression.Invoke(processor, bindResult, Parameter1, Parameter2, Parameter3, Parameter4, Parameter5, Parameter6, Parameter7, Parameter8, Parameter9, Parameter10))
                ));
            CaseExpressionsList.Add(caseExpr);
        }

        public void MatchAny(Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TOut> handler)
        {
            if (FinalExpr != null) { return; }
            FinalExpr = CreatePredicatedBasedExpr(condition: _ => true, processor: (_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => handler(_, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
        }

        /// <summary>Performs match on the given value</summary>
        public TOut Match(TIn value, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10) => MatcherFunc(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);

        /// <summary>Converts matcher into Func&lt;T&gt; instance</summary>
        public static implicit operator Func<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TOut>(FuncMatchBuilder<TIn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TOut> matcher) => matcher.MatcherFunc;
    }
}