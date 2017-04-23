﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Timeout
{
    public partial class TimeoutPolicy
    {
        internal TimeoutPolicy(Func<Func<CancellationToken, Task>, Context, CancellationToken, bool, Task> asyncExceptionPolicy)
           : base(asyncExceptionPolicy, Enumerable.Empty<ExceptionPredicate>())
        {
        }

    }

    public partial class TimeoutPolicy<TResult>
    {
        internal TimeoutPolicy(
            Func<Func<CancellationToken, Task<TResult>>, Context, CancellationToken, bool, Task<TResult>> asyncExecutionPolicy
            ) : base(asyncExecutionPolicy, Enumerable.Empty<ExceptionPredicate>(), Enumerable.Empty<ResultPredicate<TResult>>())
        {
        }
    }
}