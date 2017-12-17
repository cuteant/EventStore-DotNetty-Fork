// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UriTemplate.Core.Tests
{
    using ICollection = System.Collections.ICollection;

    [TestFixture]
    public class Level4Tests
    {
        private static readonly IDictionary<string, object> Variables =
            new Dictionary<string, object>
            {
                { "var", "value" },
                { "hello", "Hello World!" },
                { "path", "/foo/bar" },
                { "list", new[] { "red", "green", "blue" } },
                { "keys", new Dictionary<string, string> { { "semi", ";" }, { "dot", "." }, { "comma", "," } } }
            };

        private static readonly HashSet<string> RequiredVariables =
            new HashSet<string>
            {
                "var",
                "hello",
                "path",
                "list",
                "keys",
            };

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.SimpleExpansion)]
        public void TestSimpleExpansionPrefix()
        {
            string template = "{var:3}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("val", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("val", match.Bindings["var"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("val", match.Bindings["var"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.SimpleExpansion)]
        public void TestSimpleExpansionLongPrefix()
        {
            string template = "{var:30}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("value", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["var"], match.Bindings["var"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["var"], match.Bindings["var"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.SimpleExpansion)]
        public void TestSimpleExpansionCollectionVariable()
        {
            string template = "{list}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("red,green,blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.SimpleExpansion)]
        public void TestCompoundSimpleExpansionCollectionVariable()
        {
            string template = "{list*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("red,green,blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.SimpleExpansion)]
        public void TestSimpleExpansionAssociativeMapVariable()
        {
            string template = "{keys}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "comma,%2C,dot,.,semi,%3B",
                    "comma,%2C,semi,%3B,dot,.",
                    "dot,.,comma,%2C,semi,%3B",
                    "dot,.,semi,%3B,comma,%2C",
                    "semi,%3B,comma,%2C,dot,.",
                    "semi,%3B,dot,.,comma,%2C"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.SimpleExpansion)]
        public void TestCompoundSimpleExpansionAssociativeMapVariable()
        {
            string template = "{keys*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "comma=%2C,dot=.,semi=%3B",
                    "comma=%2C,semi=%3B,dot=.",
                    "dot=.,comma=%2C,semi=%3B",
                    "dot=.,semi=%3B,comma=%2C",
                    "semi=%3B,comma=%2C,dot=.",
                    "semi=%3B,dot=.,comma=%2C"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.ReservedExpansion)]
        public void TestReservedExpansionPrefixVariable()
        {
            string template = "{+path:6}/here";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("/foo/b/here", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("/foo/b", match.Bindings["path"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("/foo/b", match.Bindings["path"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.ReservedExpansion)]
        public void TestReservedExpansionCollectionVariable()
        {
            string template = "{+list}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("red,green,blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.ReservedExpansion)]
        public void TestCompoundReservedExpansionCollectionVariable()
        {
            string template = "{+list*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("red,green,blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.ReservedExpansion)]
        public void TestReservedExpansionAssociativeMapVariable()
        {
            string template = "{+keys}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "comma,,,dot,.,semi,;",
                    "comma,,,semi,;,dot,.",
                    "dot,.,comma,,,semi,;",
                    "dot,.,semi,;,comma,,",
                    "semi,;,comma,,,dot,.",
                    "semi,;,dot,.,comma,,"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.ReservedExpansion)]
        public void TestCompoundReservedExpansionAssociativeMapVariable()
        {
            string template = "{+keys*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "comma=,,dot=.,semi=;",
                    "comma=,,semi=;,dot=.",
                    "dot=.,comma=,,semi=;",
                    "dot=.,semi=;,comma=,",
                    "semi=;,comma=,,dot=.",
                    "semi=;,dot=.,comma=,"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.FragmentExpansion)]
        public void TestFragmentExpansionPrefixVariable()
        {
            string template = "{#path:6}/here";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("#/foo/b/here", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual(((string)Variables["path"]).Substring(0, 6), match.Bindings["path"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual(((string)Variables["path"]).Substring(0, 6), match.Bindings["path"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.FragmentExpansion)]
        public void TestFragmentExpansionCollectionVariable()
        {
            string template = "{#list}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("#red,green,blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.FragmentExpansion)]
        public void TestCompoundFragmentExpansionCollectionVariable()
        {
            string template = "{#list*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("#red,green,blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.FragmentExpansion)]
        public void TestFragmentExpansionAssociativeMapVariable()
        {
            string template = "{#keys}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "#comma,,,dot,.,semi,;",
                    "#comma,,,semi,;,dot,.",
                    "#dot,.,comma,,,semi,;",
                    "#dot,.,semi,;,comma,,",
                    "#semi,;,comma,,,dot,.",
                    "#semi,;,dot,.,comma,,"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.FragmentExpansion)]
        public void TestCompoundFragmentExpansionAssociativeMapVariable()
        {
            string template = "{#keys*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "#comma=,,dot=.,semi=;",
                    "#comma=,,semi=;,dot=.",
                    "#dot=.,comma=,,semi=;",
                    "#dot=.,semi=;,comma=,",
                    "#semi=;,comma=,,dot=.",
                    "#semi=;,dot=.,comma=,"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.LabelExpansion)]
        public void TestLabelExpansionPrefix()
        {
            string template = "X{.var:3}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("X.val", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("val", match.Bindings["var"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("val", match.Bindings["var"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.LabelExpansion)]
        public void TestLabelExpansionCollectionVariable()
        {
            string template = "X{.list}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("X.red,green,blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.LabelExpansion)]
        public void TestCompoundLabelExpansionCollectionVariable()
        {
            string template = "X{.list*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("X.red.green.blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.LabelExpansion)]
        public void TestLabelExpansionAssociativeMapVariable()
        {
            string template = "X{.keys}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "X.comma,%2C,dot,.,semi,%3B",
                    "X.comma,%2C,semi,%3B,dot,.",
                    "X.dot,.,comma,%2C,semi,%3B",
                    "X.dot,.,semi,%3B,comma,%2C",
                    "X.semi,%3B,comma,%2C,dot,.",
                    "X.semi,%3B,dot,.,comma,%2C"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestPathSegmentExpansionMultipleReferencesPrefix()
        {
            string template = "{/var:1,var}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("/v/value", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["var"], match.Bindings["var"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["var"], match.Bindings["var"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestPathSegmentExpansionCollectionVariable()
        {
            string template = "{/list}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("/red,green,blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestCompoundPathSegmentExpansionCollectionVariable()
        {
            string template = "{/list*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("/red/green/blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestCompoundPathSegmentExpansionCollectionVariableAndPrefixVariableReference()
        {
            string template = "{/list*,path:4}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("/red/green/blue/%2Ffoo", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
            Assert.AreEqual(((string)Variables["path"]).Substring(0, 4), match.Bindings["path"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
            Assert.AreEqual(((string)Variables["path"]).Substring(0, 4), match.Bindings["path"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestPathSegmentExpansionAssociativeMapVariable()
        {
            string template = "{/keys}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "/comma,%2C,dot,.,semi,%3B",
                    "/comma,%2C,semi,%3B,dot,.",
                    "/dot,.,comma,%2C,semi,%3B",
                    "/dot,.,semi,%3B,comma,%2C",
                    "/semi,%3B,comma,%2C,dot,.",
                    "/semi,%3B,dot,.,comma,%2C"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestCompoundPathSegmentExpansionAssociativeMapVariable()
        {
            string template = "{/keys*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "/comma=%2C/dot=./semi=%3B",
                    "/comma=%2C/semi=%3B/dot=.",
                    "/dot=./comma=%2C/semi=%3B",
                    "/dot=./semi=%3B/comma=%2C",
                    "/semi=%3B/comma=%2C/dot=.",
                    "/semi=%3B/dot=./comma=%2C"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.PathParameterExpansion)]
        public void TestPathParameterExpansionPrefixVariable()
        {
            string template = "{;hello:5}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual(";hello=Hello", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("Hello", match.Bindings["hello"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("Hello", match.Bindings["hello"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.PathParameterExpansion)]
        public void TestPathParameterExpansionCollectionVariable()
        {
            string template = "{;list}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual(";list=red,green,blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.PathParameterExpansion)]
        public void TestCompoundPathParameterExpansionCollectionVariable()
        {
            string template = "{;list*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual(";list=red;list=green;list=blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.PathParameterExpansion)]
        public void TestPathParameterExpansionAssociativeMapVariable()
        {
            string template = "{;keys}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    ";keys=comma,%2C,dot,.,semi,%3B",
                    ";keys=comma,%2C,semi,%3B,dot,.",
                    ";keys=dot,.,comma,%2C,semi,%3B",
                    ";keys=dot,.,semi,%3B,comma,%2C",
                    ";keys=semi,%3B,comma,%2C,dot,.",
                    ";keys=semi,%3B,dot,.,comma,%2C"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.PathParameterExpansion)]
        public void TestCompoundPathParameterExpansionAssociativeMapVariable()
        {
            string template = "{;keys*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    ";comma=%2C;dot=.;semi=%3B",
                    ";comma=%2C;semi=%3B;dot=.",
                    ";dot=.;comma=%2C;semi=%3B",
                    ";dot=.;semi=%3B;comma=%2C",
                    ";semi=%3B;comma=%2C;dot=.",
                    ";semi=%3B;dot=.;comma=%2C"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.QueryExpansion)]
        public void TestQueryExpansionPrefixVariable()
        {
            string template = "{?var:3}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("?var=val", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("val", match.Bindings["var"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("val", match.Bindings["var"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.QueryExpansion)]
        public void TestQueryExpansionCollectionVariable()
        {
            string template = "{?list}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("?list=red,green,blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.QueryExpansion)]
        public void TestCompoundQueryExpansionCollectionVariable()
        {
            string template = "{?list*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("?list=red&list=green&list=blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.QueryExpansion)]
        public void TestQueryExpansionAssociativeMapVariable()
        {
            string template = "{?keys}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "?keys=comma,%2C,dot,.,semi,%3B",
                    "?keys=comma,%2C,semi,%3B,dot,.",
                    "?keys=dot,.,comma,%2C,semi,%3B",
                    "?keys=dot,.,semi,%3B,comma,%2C",
                    "?keys=semi,%3B,comma,%2C,dot,.",
                    "?keys=semi,%3B,dot,.,comma,%2C"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.QueryExpansion)]
        public void TestCompoundQueryExpansionAssociativeMapVariable()
        {
            string template = "{?keys*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "?comma=%2C&dot=.&semi=%3B",
                    "?comma=%2C&semi=%3B&dot=.",
                    "?dot=.&comma=%2C&semi=%3B",
                    "?dot=.&semi=%3B&comma=%2C",
                    "?semi=%3B&comma=%2C&dot=.",
                    "?semi=%3B&dot=.&comma=%2C"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.QueryContinuationExpansion)]
        public void TestQueryContinuationExpansionPrefixVariable()
        {
            string template = "{&var:3}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("&var=val", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("val", match.Bindings["var"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            Assert.AreEqual("val", match.Bindings["var"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.QueryContinuationExpansion)]
        public void TestQueryContinuationExpansionCollectionVariable()
        {
            string template = "{&list}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("&list=red,green,blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.QueryContinuationExpansion)]
        public void TestCompoundQueryContinuationExpansionCollectionVariable()
        {
            string template = "{&list*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("&list=red&list=green&list=blue", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["list"], (ICollection)match.Bindings["list"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.QueryContinuationExpansion)]
        public void TestQueryContinuationExpansionAssociativeMapVariable()
        {
            string template = "{&keys}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "&keys=comma,%2C,dot,.,semi,%3B",
                    "&keys=comma,%2C,semi,%3B,dot,.",
                    "&keys=dot,.,comma,%2C,semi,%3B",
                    "&keys=dot,.,semi,%3B,comma,%2C",
                    "&keys=semi,%3B,comma,%2C,dot,.",
                    "&keys=semi,%3B,dot,.,comma,%2C"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }

        [Test]
        [Category(TestCategories.Level4)]
        [Category(TestCategories.QueryContinuationExpansion)]
        public void TestCompoundQueryContinuationExpansionAssociativeMapVariable()
        {
            string template = "{&keys*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            string[] allowed =
                {
                    "&comma=%2C&dot=.&semi=%3B",
                    "&comma=%2C&semi=%3B&dot=.",
                    "&dot=.&comma=%2C&semi=%3B",
                    "&dot=.&semi=%3B&comma=%2C",
                    "&semi=%3B&comma=%2C&dot=.",
                    "&semi=%3B&dot=.&comma=%2C"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);

            match = uriTemplate.Match(uri, RequiredVariables, new[] { "list" }, new[] { "keys" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables["keys"], (ICollection)match.Bindings["keys"].Value);
        }
    }
}
