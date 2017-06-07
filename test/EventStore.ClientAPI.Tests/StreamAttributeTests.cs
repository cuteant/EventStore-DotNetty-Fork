using System;
using Xunit;

namespace EventStore.ClientAPI.Tests
{
  public class StreamAttributeTests
  {
    [Fact]
    public void Run()
    {
      Assert.Throws<ArgumentNullException>(() => new StreamAttribute(null));

      var attr = new StreamAttribute("a_test_stream", null, "any");
      Assert.Equal("a_test_stream", attr.StreamId);
      Assert.Null(attr.EventType);
      Assert.Equal(ExpectedVersion.Any, attr.ExpectedVersion);

      attr = new StreamAttribute("a_test_stream", string.Empty, "EmptyStream");
      Assert.Equal(string.Empty, attr.EventType);
      Assert.Equal(ExpectedVersion.EmptyStream, attr.ExpectedVersion);

      attr = new StreamAttribute("a_test_stream", "et", "NostrEam");
      Assert.Equal("et", attr.EventType);
      Assert.Equal(ExpectedVersion.NoStream, attr.ExpectedVersion);

      attr = new StreamAttribute("a_test_stream", string.Empty, "Streamexists");
      Assert.Equal(string.Empty, attr.EventType);
      Assert.Equal(ExpectedVersion.StreamExists, attr.ExpectedVersion);
    }
  }
}
