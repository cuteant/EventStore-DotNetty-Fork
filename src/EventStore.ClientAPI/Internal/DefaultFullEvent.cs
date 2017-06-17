using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventStore.ClientAPI.Internal
{
  internal sealed class DefaultFullEvent : DefaultFullEvent<object>, IFullEvent
  {
    internal new static readonly DefaultFullEvent Null = new DefaultFullEvent { Descriptor = NullEventDescriptor.Instance, Value = default(object) };
  }
  internal class DefaultFullEvent<T> : IFullEvent<T> where T : class
  {
    internal static readonly DefaultFullEvent<T> Null = new DefaultFullEvent<T> { Descriptor = NullEventDescriptor.Instance, Value = default(T) };

    public IEventDescriptor Descriptor { get; set; }

    public T Value { get; set; }
  }
}
