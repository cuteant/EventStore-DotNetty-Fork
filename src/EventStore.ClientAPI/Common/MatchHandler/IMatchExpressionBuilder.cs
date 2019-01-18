//-----------------------------------------------------------------------
// <copyright file="IMatchExpressionBuilder.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

#if CLIENTAPI
namespace EventStore.ClientAPI.Common.MatchHandler
#else
namespace EventStore.Common.MatchHandler
#endif
{
    /// <summary>
    /// TBD
    /// </summary>
    internal interface IMatchExpressionBuilder
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="typeHandlers">TBD</param>
        /// <returns>TBD</returns>
        MatchExpressionBuilderResult BuildLambdaExpression(IReadOnlyList<TypeHandler> typeHandlers);

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="arguments">TBD</param>
        /// <returns>TBD</returns>
        object[] CreateArgumentValuesArray(IReadOnlyList<Argument> arguments);
    }
}

