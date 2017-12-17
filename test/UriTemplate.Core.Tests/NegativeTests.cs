// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace UriTemplate.Core.Tests
{
    [TestFixture]
    public class NegativeTests
    {
        private static readonly Dictionary<string, object> Variables =
            new Dictionary<string, object>
            {
                { "id", "thing" },
                { "var", "value" },
                { "hello", "Hello World!" },
                { "with space", "fail" },
                { " leading_space", "Hi!" },
                { "trailing_space ", "Bye!" },
                { "empty", string.Empty },
                { "path", "/foo/bar" },
                { "x", "1024" },
                { "y", "768" },
                { "list", new[] { "red", "green", "blue" } },
                { "keys", new Dictionary<string, object> { { "semi", ";" }, { "dot", "." }, { "comma", "," } } },
                { "example", "red" },
                { "searchTerms", "uri templates" },
                { "~thing", "some-user" },
                { "default-graph-uri", new[] { "http://www.example/book/", "http://www.example/papers/" } },
                { "query", "PREFIX dc: <http://purl.org/dc/elements/1.1/> SELECT ?book ?who WHERE { ?book dc:creator ?who }" }
            };

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestUnclosedTemplate()
        {
            string template = "{/id*";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestUnopenedTemplate()
        {
            string template = "/id*}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestTwoOperatorsTemplate()
        {
            string template = "{/?id}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestNonIntegralPrefixTemplate()
        {
            string template = "{var:prefix}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestCompositePrefixTemplate()
        {
            string template = "{hello:2*}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestDuplicateOperatorTemplate()
        {
            string template = "{??hello}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestSpaceInExpressionTemplate()
        {
            string template = "{with space}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestSpaceAtStartOfExpressionTemplate()
        {
            string template = "{ leading_space}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestSpaceAtEndOfExpressionTemplate()
        {
            string template = "{trailing_space }";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestEqualsOperatorTemplate()
        {
            string template = "{=path}";
            Should.Throw<NotSupportedException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestDollarOperatorTemplate()
        {
            string template = "{$var}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestPipeOperatorTemplate()
        {
            string template = "{|var*}";
            Should.Throw<NotSupportedException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestReverseOperatorTemplate()
        {
            string template = "{*keys?}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestInvalidQueryTemplate()
        {
            string template = "{?empty=default,var}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestInvalidAlternativesTemplate()
        {
            string template = "{var}{-prefix|/-/|var}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestInvalidPrefixTemplate()
        {
            string template = "?q={searchTerms}&amp;c={example:color?}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestAlternativesQueryTemplate()
        {
            string template = "x{?empty|foo=none}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestCompoundFragmentTemplate()
        {
            string template = "/h{#hello+}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestInvalidFragmentTemplate()
        {
            string template = "/h#{hello+}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestPrefixAssociativeMapTemplate()
        {
            string template = "{keys:1}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Should.Throw<InvalidOperationException>(() => uriTemplate.BindByName(Variables));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestPlusOperatorAssociativeMapTemplate()
        {
            string template = "{+keys:1}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Should.Throw<InvalidOperationException>(() => uriTemplate.BindByName(Variables));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestCompountPathParameterPrefixAssociativeMapTemplate()
        {
            string template = "{;keys:1*}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestInvalidPipeOperatorTemplate()
        {
            string template = "?{-join|&|var,list}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestInvalidTildeOperatorTemplate()
        {
            string template = "/people/{~thing}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestHypenatedVariableNameTemplate()
        {
            string template = "/{default-graph-uri}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestHypenatedVariableName2Template()
        {
            string template = "/sparql{?query,default-graph-uri}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestMismatchedBracesTemplate()
        {
            string template = "/sparql{?query){&default-graph-uri*}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }

        [Test]
        [Category(TestCategories.InvalidTemplates)]
        public void TestSpaceAfterCommaTemplate()
        {
            string template = "/resolution{?x, y}";
            Should.Throw<FormatException>(() => new global::UriTemplate.Core.UriTemplate(template));
        }
    }
}
