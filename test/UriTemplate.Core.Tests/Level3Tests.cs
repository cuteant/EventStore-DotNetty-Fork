// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UriTemplate.Core.Tests
{
    [TestFixture]
    public class Level3Tests
    {
        private static readonly IDictionary<string, object> Variables =
            new Dictionary<string, object>
            {
                { "var", "value" },
                { "hello", "Hello World!" },
                { "empty", string.Empty },
                { "path", "/foo/bar" },
                { "x", "1024" },
                { "y", "768" },
            };

        private static readonly HashSet<string> RequiredVariables =
            new HashSet<string>
            {
                "var",
                "hello",
                "empty",
                "path",
                "x",
                "y",
            };

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.SimpleExpansion)]
        public void TestSimpleExpansionMultipleVariablesInQuery()
        {
            string template = "map?{x,y}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("map?1024,768", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.SimpleExpansion)]
        public void TestSimpleExpansionMultipleVariables()
        {
            string template = "{x,hello,y}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("1024,Hello%20World%21,768", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["hello"], match.Bindings["hello"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["hello"], match.Bindings["hello"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.ReservedExpansion)]
        public void TestReservedExpansionMultipleVariables()
        {
            string template = "{+x,hello,y}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("1024,Hello%20World!,768", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["hello"], match.Bindings["hello"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["hello"], match.Bindings["hello"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.ReservedExpansion)]
        public void TestReservedExpansionMultipleVariablesWithSlash()
        {
            string template = "{+path,x}/here";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("/foo/bar,1024/here", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["path"], match.Bindings["path"].Value);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["path"], match.Bindings["path"].Value);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.FragmentExpansion)]
        public void TestFragmentExpansionMultipleVariables()
        {
            string template = "{#x,hello,y}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("#1024,Hello%20World!,768", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["hello"], match.Bindings["hello"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["hello"], match.Bindings["hello"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.FragmentExpansion)]
        public void TestFragmentExpansionMultipleVariablesAndLiteral()
        {
            string template = "{#path,x}/here";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("#/foo/bar,1024/here", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["path"], match.Bindings["path"].Value);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["path"], match.Bindings["path"].Value);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.LabelExpansion)]
        public void TestLabelExpansionMultipleVariables()
        {
            string template = "X{.x,y}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("X.1024.768", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestPathSegmentExpansion()
        {
            string template = "{/var}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("/value", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["var"], match.Bindings["var"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["var"], match.Bindings["var"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestPathSegmentExpansionMultipleVariables()
        {
            string template = "{/var,x}/here";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("/value/1024/here", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["var"], match.Bindings["var"].Value);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["var"], match.Bindings["var"].Value);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.PathParameterExpansion)]
        public void TestPathParameterExpansionMultipleVariables()
        {
            string template = "{;x,y}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual(";x=1024;y=768", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.PathParameterExpansion)]
        public void TestPathParameterExpansionEmptyValue()
        {
            string template = "{;x,y,empty}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual(";x=1024;y=768;empty", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
            Assert.AreEqual(Variables["empty"], match.Bindings["empty"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
            Assert.AreEqual(Variables["empty"], match.Bindings["empty"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.QueryExpansion)]
        public void TestQueryExpansionMultipleVariables()
        {
            string template = "{?x,y}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("?x=1024&y=768", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.QueryExpansion)]
        public void TestQueryExpansionEmptyValue()
        {
            string template = "{?x,y,empty}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("?x=1024&y=768&empty=", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
            Assert.AreEqual(Variables["empty"], match.Bindings["empty"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
            Assert.AreEqual(Variables["empty"], match.Bindings["empty"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.QueryContinuationExpansion)]
        public void TestQueryContinuationExpansion()
        {
            string template = "?fixed=yes{&x}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("?fixed=yes&x=1024", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
        }

        [Test]
        [Category(TestCategories.Level3)]
        [Category(TestCategories.QueryContinuationExpansion)]
        public void TestQueryContinuationExpansionMultipleVariables()
        {
            string template = "{&x,y,empty}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("&x=1024&y=768&empty=", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
            Assert.AreEqual(Variables["empty"], match.Bindings["empty"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["x"], match.Bindings["x"].Value);
            Assert.AreEqual(Variables["y"], match.Bindings["y"].Value);
            Assert.AreEqual(Variables["empty"], match.Bindings["empty"].Value);
        }
    }
}
