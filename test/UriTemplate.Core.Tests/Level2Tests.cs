// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UriTemplate.Core.Tests
{
    [TestFixture]
    public class Level2Tests
    {
        private static readonly IDictionary<string, object> Variables =
            new Dictionary<string, object>
            {
                { "var", "value" },
                { "hello", "Hello World!" },
                { "path", "/foo/bar" },
            };

        private static readonly HashSet<string> RequiredVariables =
            new HashSet<string>
            {
                "var",
                "hello",
                "path",
            };

        [Test]
        [Category(TestCategories.Level2)]
        [Category(TestCategories.ReservedExpansion)]
        public void TestReservedExpansion()
        {
            string template = "{+var}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("value", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["var"], match.Bindings["var"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["var"], match.Bindings["var"].Value);
        }

        [Test]
        [Category(TestCategories.Level2)]
        [Category(TestCategories.ReservedExpansion)]
        public void TestReservedExpansionEscaping()
        {
            string template = "{+hello}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("Hello%20World!", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["hello"], match.Bindings["hello"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["hello"], match.Bindings["hello"].Value);
        }

        [Test]
        [Category(TestCategories.Level2)]
        [Category(TestCategories.ReservedExpansion)]
        public void TestReservedExpansionReservedCharacters()
        {
            string template = "{+path}/here";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("/foo/bar/here", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["path"], match.Bindings["path"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["path"], match.Bindings["path"].Value);
        }

        [Test]
        [Category(TestCategories.Level2)]
        [Category(TestCategories.ReservedExpansion)]
        public void TestReservedExpansionReservedCharactersInQuery()
        {
            string template = "here?ref={+path}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables);
            Assert.AreEqual("here?ref=/foo/bar", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["path"], match.Bindings["path"].Value);

            match = uriTemplate.Match(uri, RequiredVariables);
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables["path"], match.Bindings["path"].Value);
        }
    }
}
