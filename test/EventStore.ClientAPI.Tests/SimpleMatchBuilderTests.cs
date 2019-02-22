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
            builder.Match<string>(s => s);
            var func = (Func<string, string>)builder;
            Assert.Equal("test", func("test"));

            builder = new SimpleMatchBuilder<string, string>();
            builder.MatchAny(s => s + "s");
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
            builder2.MatchAny((s1, s2) => s1 + s2);
            var func2 = (Func<string, string, string>)builder2;
            Assert.Equal("ab", func2("a", "b"));

            builder2 = new FuncMatchBuilder<string, string, string>();
            builder2.MatchAny((s1, s2) => s1 + s2);
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
            builder.Match((string _) => 100, s => s.StartsWith("t"));
            builder.MatchAny(s => s != null ? s.Length : 0);
            var match = builder.Build();
            Assert.Equal(0, match(null));
            Assert.Equal(100, match("test"));
        }

        [Fact]
        public void StringMatching2()
        {
            var builder = new SimpleMatchBuilder<object, int>();
            builder.Match((string _) => 100, s => s.StartsWith("t"));
            var match = builder.Build();
            Assert.Equal(100, match("test"));
            Assert.Throws<MatchException>(() => match("error"));
        }

        [Fact]
        public void AfterMatchAny()
        {
            var builder = new SimpleMatchBuilder<object, int>();
            builder.MatchAny(o => 0);
            Assert.Throws<InvalidOperationException>(() => builder.Match((string _) => 100, s => s.StartsWith("t")));
        }

        [Fact]
        public void AfterBuild()
        {
            var builder = new SimpleMatchBuilder<object, int>();
            builder.Build();
            Assert.Throws<InvalidOperationException>(() => builder.Match((string _) => 100, s => s.StartsWith("t")));
        }

        [Fact]
        public void MultipleTypesMatching()
        {
            var builder = new SimpleMatchBuilder<object, string>();
            builder.Match<string>(handler: s => s);
            builder.Match<StringBuilder>(sb => sb.ToString());
            builder.Match<int>(handler: i => (i * 3).ToString());
            builder.MatchAny(_ => "Unknown object");
            var match = builder.Build();
            Assert.Equal("string", match("string"));
            Assert.Equal(new StringBuilder("string builder").ToString(), match(new StringBuilder("string builder")));
            Assert.Equal("6", match(2));
            Assert.Equal("Unknown object", match(100500M));
        }

        [Fact]
        public void MultipleTypesMatchingWithCondition()
        {
            var builder = new SimpleMatchBuilder<object, string>();
            builder.Match<string>(handler: s => s + "1", shouldHandle: _ => _.StartsWith("a", StringComparison.OrdinalIgnoreCase));
            builder.Match<string>(s => s + "2", _ => _.StartsWith("b", StringComparison.OrdinalIgnoreCase));
            builder.Match<StringBuilder>(handler: sb => sb.Append("empty").ToString(), _ => _.Length == 0);
            builder.Match<StringBuilder>(sb => sb.Append("x").ToString(), _ => _.Length > 0);
            builder.Match<int>(handler: i => (i * 3).ToString(), _ => _ < 15);
            builder.Match<int>(handler: i => (i + 5).ToString(), _ => _ >= 15);
            builder.MatchAny(_ => "Unknown object");
            var match = builder.Build();

            Assert.Equal("ab1", match("ab"));
            Assert.Equal("b2", match("b"));
            Assert.Equal("empty", match(new StringBuilder()));
            Assert.Equal("hx", match(new StringBuilder("h")));
            Assert.Equal("6", match(2));
            Assert.Equal("20", match(15));
            Assert.Equal("Unknown object", match(100500M));
        }

        [Fact]
        public void MultipleTypesMatchingWithCondition2()
        {
            var builder = new FuncMatchBuilder<object, int, string>();
            builder.Match<string>((s, v) => s + v, shouldHandle: _ => _.StartsWith("a", StringComparison.OrdinalIgnoreCase));
            builder.Match<string>((s, v) => s + v, _ => _.StartsWith("b", StringComparison.OrdinalIgnoreCase));
            builder.Match<StringBuilder>((sb, v) => sb.Append("empty" + v).ToString(), _ => _.Length == 0);
            builder.Match<StringBuilder>((sb, v) => sb.Append("x".PadRight(v, 'x')).ToString(), _ => _.Length > 0);
            builder.Match<int>((i, v) => (i * v).ToString(), _ => _ < 15);
            builder.Match<int>((i, v) => (i + v).ToString(), _ => _ >= 15);
            builder.MatchAny((_, v) => "Unknown object");
            var match = builder.Build();

            Assert.Equal("ab1", match("ab", 1));
            Assert.Equal("b2", match("b", 2));
            Assert.Equal("empty123", match(new StringBuilder(), 123));
            Assert.Equal("hxxxxx", match(new StringBuilder("h"), 5));
            Assert.Equal("6", match(2, 3));
            Assert.Equal("20", match(15, 5));
            Assert.Equal("Unknown object", match(100500M, 0));
        }

        [Fact]
        public void MultipleTypesMatchingWithConditionAction()
        {
            var str = string.Empty;
            void LocalHandle1(string s)
            {
                str = s + "1";
            }
            void LocalHandle2(string s)
            {
                str = s + "2";
            }
            StringBuilder sb = null;
            void LocalHandle3(StringBuilder s)
            {
                sb = s;
                s.Append("empty");
            }
            void LocalHandle4(StringBuilder s)
            {
                sb = s;
                s.Append("x");
            }
            void UnHandle(object obj)
            {
                str = "Unknown object";
            }

            var builder = new SimpleMatchBuilder<object>();
            builder.Match<string>(handler: s => LocalHandle1(s), shouldHandle: _ => _.StartsWith("a", StringComparison.OrdinalIgnoreCase));
            builder.Match<string>(handler: s => LocalHandle2(s), shouldHandle: _ => _.StartsWith("b", StringComparison.OrdinalIgnoreCase));
            builder.Match<StringBuilder>(handler: _ => LocalHandle3(_), _ => _.Length == 0);
            builder.Match<StringBuilder>(handler: _ => LocalHandle4(_), _ => _.Length > 0);
            builder.MatchAny(handler: _ => UnHandle(_));
            var match = builder.Build();

            match("ab");
            Assert.Equal("ab1", str);
            match("b");
            Assert.Equal("b2", str);
            match(new StringBuilder());
            Assert.Equal("empty", sb.ToString());
            match(new StringBuilder("h"));
            Assert.Equal("hx", sb.ToString());
            match(100500);
            Assert.Equal("Unknown object", str);
        }

        [Fact]
        public void MultipleTypesMatchingWithConditionAction2()
        {
            var str = string.Empty;
            void LocalHandle1(string s, int v)
            {
                str = s + v;
            }
            void LocalHandle2(string s, int v)
            {
                str = s + v;
            }
            StringBuilder sb = null;
            void LocalHandle3(StringBuilder s, int v)
            {
                sb = s;
                s.Append("empty" + v);
            }
            void LocalHandle4(StringBuilder s, int v)
            {
                sb = s;
                s.Append("x".PadRight(v, 'x'));
            }
            void UnHandle(object obj, int v)
            {
                str = "Unknown object";
            }

            var builder = new ActionMatchBuilder<object, int>();
            builder.Match<string>(handler: (s, v) => LocalHandle1(s, v), shouldHandle: _ => _.StartsWith("a", StringComparison.OrdinalIgnoreCase));
            builder.Match<string>(handler: (s, v) => LocalHandle2(s, v), shouldHandle: _ => _.StartsWith("b", StringComparison.OrdinalIgnoreCase));
            builder.Match<StringBuilder>(handler: (_, v) => LocalHandle3(_, v), _ => _.Length == 0);
            builder.Match<StringBuilder>(handler: (_, v) => LocalHandle4(_, v), _ => _.Length > 0);
            builder.MatchAny(handler: (_, v) => UnHandle(_, v));
            var match = builder.Build();

            match("ab", 1);
            Assert.Equal("ab1", str);
            match("b", 2);
            Assert.Equal("b2", str);
            match(new StringBuilder(), 123);
            Assert.Equal("empty123", sb.ToString());
            match(new StringBuilder("h"), 5);
            Assert.Equal("hxxxxx", sb.ToString());
            match(100500, 0);
            Assert.Equal("Unknown object", str);
        }

        [Fact]
        public void MultipleTypesMatchingWithoutWildCard()
        {
            var builder = new SimpleMatchBuilder<object, string>();
            builder.Match<string>(handler: s => s);
            builder.Match<StringBuilder>(sb => sb.ToString());
            var match = builder.Build();
            Assert.Equal("string", match("string"));
            Assert.Equal(new StringBuilder("string builder").ToString(), match(new StringBuilder("string builder")));
            Assert.Throws<MatchException>(() => match(100500));
        }
    }
}
