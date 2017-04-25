﻿using System;
using System.Threading;
using System.Threading.Tasks;

#if NET40
using SemaphoreSlim = CuteAnt.AsyncEx.AsyncSemaphore;
using Polly.Utilities;
#else
using SemaphoreSlim = System.Threading.SemaphoreSlim;
#endif

namespace Polly.Bulkhead
{
   internal static partial class BulkheadEngine
    {
       internal static async Task<TResult> ImplementationAsync<TResult>(
            Func<CancellationToken, Task<TResult>> action,
            Context context,
            Func<Context, Task> onBulkheadRejectedAsync,
            SemaphoreSlim maxParallelizationSemaphore,
            SemaphoreSlim maxQueuedActionsSemaphore,
            CancellationToken cancellationToken, 
            bool continueOnCapturedContext)
        {
            if (!await maxQueuedActionsSemaphore.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(continueOnCapturedContext))
            {
                await onBulkheadRejectedAsync(context).ConfigureAwait(continueOnCapturedContext);
                throw new BulkheadRejectedException();
            }
            try
            {
                await maxParallelizationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);

                try 
                {
                    return await action(cancellationToken).ConfigureAwait(continueOnCapturedContext);
                }
                finally
                {
                    maxParallelizationSemaphore.Release();
                }
            }
            finally
            {
                maxQueuedActionsSemaphore.Release();
            }
        }
    }
}