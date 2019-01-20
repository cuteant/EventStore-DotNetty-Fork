using System;
using System.Text;
using EventStore.ClientAPI.Common.Utils;
using Xunit;

namespace EventStore.ClientAPI.Tests
{
    public class SimpleMatchBuilderTests
    {
        [Fact]
        public void WildCardMatchTest()
        {
            var builder = new SimpleMatchBuilder<string, string>();
            builder.Match(condition: _ => true, processor: s => s);
            var func = (Func<string, string>)builder;
            Assert.Equal("test", func("test"));

            builder = new SimpleMatchBuilder<string, string>();
            builder.MatchAny(processor: s => s + "s");
            func = builder;
            Assert.Equal("tests", func("test"));

            var str = "test";
            var builder1 = new SimpleMatchBuilder<string>();
            builder1.MatchAny(handler: s => LocalHandle(s));
            var action = (Action<string>)builder1;
            action(str);
            Assert.Equal("test1", str);
            void LocalHandle(string s)
            {
                str = s + "1";
            }

            var builder2 = new FuncMatchBuilder<string, string, string>();
            builder2.Match(condition: _ => true, processor: (s1, s2) => s1 + s2);
            var func2 = (Func<string, string, string>)builder2;
            Assert.Equal("ab", func2("a", "b"));

            builder2 = new FuncMatchBuilder<string, string, string>();
            builder2.MatchAny(processor: (s1, s2) => s1 + s2);
            func2 = builder2;
            Assert.Equal("ab", func2("a", "b"));

            var builder3 = new ActionMatchBuilder<string, string>();
            builder3.MatchAny(handler: (s1, s2) => LocalHandle2(s1, s2));
            var action3 = (Action<string, string>)builder3;
            action3("a", "b");
            Assert.Equal("ab", str);
            void LocalHandle2(string s1, string s2)
            {
                str = s1 + s2;
            }
        }

        [Fact]
        public void StringMatching()
        {
            var builder = new SimpleMatchBuilder<string, int>();
            builder.Match(s => string.IsNullOrEmpty(s), (string _) => 0);
            builder.MatchAny(processor: s => s.Length);
            var match = builder.Build();
            Assert.Equal(0, match(null));
            Assert.Equal(4, match("test"));
        }

        [Fact]
        public void MultipleTypesMatching()
        {
            var builder = new SimpleMatchBuilder<object, string>();
            builder.Match(s => s as string, s => s);
            builder.Match(sb => sb as StringBuilder, sb => sb.ToString());
            builder.MatchAny(processor: _ => "Unknown object");
            var match = builder.Build();
            Assert.Equal("string", match("string"));
            Assert.Equal(new StringBuilder("string builder").ToString(), match(new StringBuilder("string builder")));
            Assert.Equal("Unknown object", match(100500));
        }

        [Fact]
        public void MultipleTypesMatchingWithoutWildCard()
        {
            var builder = new SimpleMatchBuilder<object, string>();
            builder.Match(s => s as string, s => s);
            builder.Match(sb => sb as StringBuilder, sb => sb.ToString());
            var match = builder.Build();
            Assert.Equal("string", match("string"));
            Assert.Equal(new StringBuilder("string builder").ToString(), match(new StringBuilder("string builder")));
            Assert.Throws<MatchException>(() => match(100500));
        }
    }
}
