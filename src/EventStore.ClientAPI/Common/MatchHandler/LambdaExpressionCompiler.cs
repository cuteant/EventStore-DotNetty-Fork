//-----------------------------------------------------------------------
// <copyright file="LambdaExpressionCompiler.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Reflection.Emit;

#if CLIENTAPI
namespace EventStore.ClientAPI.Common.MatchHandler
#else
namespace EventStore.Common.MatchHandler
#endif
{
    /// <summary>
    /// TBD
    /// </summary>
    internal class LambdaExpressionCompiler : ILambdaExpressionCompiler
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="expression">TBD</param>
        /// <returns>TBD</returns>
        public Delegate Compile(LambdaExpression expression)
        {
            return expression.Compile();
        }

#if DESKTOPCLR
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="expression">TBD</param>
        /// <param name="method"></param>
        /// <returns>TBD</returns>
        public void CompileToMethod(LambdaExpression expression, MethodBuilder method)
        {
            expression.CompileToMethod(method);
        }
#endif
    }
}

