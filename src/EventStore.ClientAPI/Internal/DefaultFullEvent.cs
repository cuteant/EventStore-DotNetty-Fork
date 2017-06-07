using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventStore.ClientAPI.Internal
{
  internal sealed class DefaultFullEvent : DefaultFullEvent<object>, IFullEvent
  {
  }
  internal class DefaultFullEvent<T> : IFullEvent<T> where T : class
  {
    public IEventDescriptor Descriptor { get; set; }

    public T Value { get; set; }
  }
}
