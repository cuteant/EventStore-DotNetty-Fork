﻿//-----------------------------------------------------------------------
// <copyright file="HandlerKind.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

#if CLIENTAPI
namespace EventStore.ClientAPI.Common.MatchHandler
#else
namespace EventStore.Common.MatchHandler
#endif
{
    /// <summary>
    /// TBD
    /// </summary>
    internal enum HandlerKind
    {
        /// <summary>The handler is a Action&lt;T&gt;</summary>
        Action,

        /// <summary>The handler is a Action&lt;T&gt; and a Predicate&lt;T&gt; is specified</summary>
        ActionWithPredicate, 

        /// <summary>The handler is a Func&lt;T, bool&gt;</summary>
        Func
    };
}

