using System;
using System.Reactive.Linq;

namespace EventStore.ClientAPI.Rx
{
  internal static class ObservableExtensions
  {
    public static IObservable<TSource> TakeUntilInclusive<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
      return Observable.Create<TSource>(
          observer => source.Subscribe(
              item =>
              {
                observer.OnNext(item);
                if (predicate(item)) { observer.OnCompleted(); }
              },
              observer.OnError,
              observer.OnCompleted
              )
          );
    }
  }
}