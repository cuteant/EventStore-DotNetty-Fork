using System;
using Xunit;

namespace EventStore.ClientAPI.Tests
{
  public class StreamMetadataAttributeTest
  {
    [Fact]
    public void Run()
    {
      var metaAttr = new StreamMetadataAttribute()
      {
        MaxCount = 1000,
        MaxAge = TimeSpan.FromMinutes(60),
        CustomMetadata = "name=seabiscuit,age=18;city=shenzhen,isdel=true;id="
      };

      var meta = metaAttr.ToStreamMetadata();

      Assert.Equal(1000, meta.MaxCount.Value);
      Assert.Equal(TimeSpan.FromMinutes(60), meta.MaxAge.Value);

      Assert.Equal("seabiscuit", meta.GetValue<string>("name"));
      Assert.Equal("18", meta.GetValue<string>("age"));
      Assert.Equal(18, meta.GetValue<int>("age"));
      Assert.Equal("shenzhen", meta.GetValue<string>("city"));
      Assert.Equal("true", meta.GetValue<string>("isdel"));
      Assert.True(meta.GetValue<bool>("isdel"));
      Assert.Null(meta.GetValue<string>("id"));
      Assert.Equal((int?)null, meta.GetValue<int?>("id"));
    }
  }
}
