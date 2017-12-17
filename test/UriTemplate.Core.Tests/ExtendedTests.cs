﻿// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UriTemplate.Core.Tests
{
    using ICollection = System.Collections.ICollection;

    [TestFixture]
    public class ExtendedTests
    {
        private static readonly Dictionary<string, object> Variables1 =
            new Dictionary<string, object>
            {
                { "id", "person" },
                { "token", "12345" },
                { "fields", new[] { "id", "name", "picture" } },
                { "format", "json" },
                { "q", "URI Templates" },
                { "page", "5" },
                { "lang", "en" },
                { "geocode", new[] { "37.76", "-122.427" } },
                { "first_name", "John" },
                { "last.name", "Doe" },
                { "Some%20Thing", "foo" },
                { "number", 6 },
                { "long", 37.76 },
                { "lat", -122.427 },
                { "group_id", "12345" },
                { "query", "PREFIX dc: <http://purl.org/dc/elements/1.1/> SELECT ?book ?who WHERE { ?book dc:creator ?who }" },
                { "uri", "http://example.org/?uri=http%3A%2F%2Fexample.org%2F" },
                { "word", "drücken" },
                { "Stra%C3%9Fe", "Grüner Weg" },
                { "random", "šöäŸœñê€£¥‡ÑÒÓÔÕÖ×ØÙÚàáâãäåæçÿ" },
                { "assoc_special_chars", new Dictionary<string, string> { { "šöäŸœñê€£¥‡ÑÒÓÔÕ", "Ö×ØÙÚàáâãäåæçÿ" } } }
            };

        private static readonly HashSet<string> RequiredVariables1 =
            new HashSet<string>
            {
                "id",
                "token",
                "fields",
                "format",
                "q",
                "page",
                "lang",
                "geocode",
                "first_name",
                "last.name",
                "Some%20Thing",
                "number",
                "long",
                "lat",
                "group_id",
                "query",
                "uri",
                "word",
                "Stra%C3%9Fe",
                "random",
                "assoc_special_chars"
            };

        private static readonly Dictionary<string, object> Variables2 =
            new Dictionary<string, object>
            {
                { "id", new[] { "person", "albums" } },
                { "token", "12345" },
                { "fields", new[] { "id", "name", "picture" } },
                { "format", "atom" },
                { "q", "URI Templates" },
                { "page", "10" },
                { "start", "5" },
                { "lang", "en" },
                { "geocode", new[] { "37.76", "-122.427" } }
            };

        private static readonly HashSet<string> RequiredVariables2 =
            new HashSet<string>
            {
                "id",
                "token",
                "fields",
                "format",
                "q",
                "page",
                "start",
                "lang",
                "geocode",
            };

        private static readonly Dictionary<string, object> Variables3 =
            new Dictionary<string, object>
            {
                { "empty_list", new string[0] },
                { "empty_assoc", new Dictionary<string, string>() }
            };

        private static readonly ICollection<string> RequiredVariables3 = new string[0];

        private static readonly Dictionary<string, object> Variables4 =
            new Dictionary<string, object>
            {
                { "42", "The Answer to the Ultimate Question of Life, the Universe, and Everything" },
                { "1337", new[] { "leet", "as", "it", "can", "be" } },
                { "german", new Dictionary<string, string>
                    {
                        { "11", "elf" },
                        { "12", "zwölf" }
                    }
                }
            };

        private static readonly HashSet<string> RequiredVariables4 =
            new HashSet<string>
            {
                "42",
                "1337",
                "german",
            };

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestCompoundPathSegmentExpansion()
        {
            string template = "{/id*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            Assert.AreEqual("/person", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual(new[] { Variables1["id"] }, (ICollection)match.Bindings["id"].Value);

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual(new[] { Variables1["id"] }, (ICollection)match.Bindings["id"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.PathSegmentExpansion)]
        [Category(TestCategories.QueryExpansion)]
        public void TestCompoundPathSegmentExpansionWithQueryString()
        {
            string template = "{/id*}{?fields,first_name,last.name,token}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            string[] allowed =
                {
                    "/person?fields=id,name,picture&first_name=John&last.name=Doe&token=12345",
                    "/person?fields=id,picture,name&first_name=John&last.name=Doe&token=12345",
                    "/person?fields=picture,name,id&first_name=John&last.name=Doe&token=12345",
                    "/person?fields=picture,id,name&first_name=John&last.name=Doe&token=12345",
                    "/person?fields=name,picture,id&first_name=John&last.name=Doe&token=12345",
                    "/person?fields=name,id,picture&first_name=John&last.name=Doe&token=12345"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual(new[] { Variables1["id"] }, (ICollection)match.Bindings["id"].Value);
            CollectionAssert.AreEqual((ICollection)Variables1["fields"], (ICollection)match.Bindings["fields"].Value);
            Assert.AreEqual(Variables1["first_name"], match.Bindings["first_name"].Value);
            Assert.AreEqual(Variables1["last.name"], match.Bindings["last.name"].Value);
            Assert.AreEqual(Variables1["token"], match.Bindings["token"].Value);

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual(new[] { Variables1["id"] }, (ICollection)match.Bindings["id"].Value);
            CollectionAssert.AreEqual((ICollection)Variables1["fields"], (ICollection)match.Bindings["fields"].Value);
            Assert.AreEqual(Variables1["first_name"], match.Bindings["first_name"].Value);
            Assert.AreEqual(Variables1["last.name"], match.Bindings["last.name"].Value);
            Assert.AreEqual(Variables1["token"], match.Bindings["token"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.SimpleExpansion)]
        [Category(TestCategories.QueryExpansion)]
        public void TestSimpleExpansionWithQueryString()
        {
            string template = "/search.{format}{?q,geocode,lang,locale,page,result_type}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            string[] allowed =
                {
                    "/search.json?q=URI%20Templates&geocode=37.76,-122.427&lang=en&page=5",
                    "/search.json?q=URI%20Templates&geocode=-122.427,37.76&lang=en&page=5"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["format"], match.Bindings["format"].Value);
            Assert.AreEqual(Variables1["q"], match.Bindings["q"].Value);
            CollectionAssert.AreEqual((ICollection)Variables1["geocode"], (ICollection)match.Bindings["geocode"].Value);
            Assert.AreEqual(Variables1["lang"], match.Bindings["lang"].Value);
            Assert.IsFalse(match.Bindings.ContainsKey("locale"));
            Assert.AreEqual(Variables1["page"], match.Bindings["page"].Value);
            Assert.IsFalse(match.Bindings.ContainsKey("result_type"));

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["format"], match.Bindings["format"].Value);
            Assert.AreEqual(Variables1["q"], match.Bindings["q"].Value);
            CollectionAssert.AreEqual((ICollection)Variables1["geocode"], (ICollection)match.Bindings["geocode"].Value);
            Assert.AreEqual(Variables1["lang"], match.Bindings["lang"].Value);
            Assert.IsFalse(match.Bindings.ContainsKey("locale"));
            Assert.AreEqual(Variables1["page"], match.Bindings["page"].Value);
            Assert.IsFalse(match.Bindings.ContainsKey("result_type"));
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestPathSegmentExpansionWithEncodedCharacters()
        {
            string template = "/test{/Some%20Thing}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            Assert.AreEqual("/test/foo", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["Some%20Thing"], match.Bindings["Some%20Thing"].Value);

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["Some%20Thing"], match.Bindings["Some%20Thing"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestQueryExpansionWithIntegerVariable()
        {
            string template = "/set{?number}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            Assert.AreEqual("/set?number=6", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["number"].ToString(), match.Bindings["number"].Value);

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["number"].ToString(), match.Bindings["number"].Value);
        }

    

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.PathSegmentExpansion)]
        [Category(TestCategories.QueryExpansion)]
        public void TestEscapeSequences1()
        {
            string template = "/base{/group_id,first_name}/pages{/page,lang}{?format,q}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            Assert.AreEqual("/base/12345/John/pages/5/en?format=json&q=URI%20Templates", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["group_id"], match.Bindings["group_id"].Value);
            Assert.AreEqual(Variables1["first_name"], match.Bindings["first_name"].Value);
            Assert.AreEqual(Variables1["page"], match.Bindings["page"].Value);
            Assert.AreEqual(Variables1["lang"], match.Bindings["lang"].Value);
            Assert.AreEqual(Variables1["format"], match.Bindings["format"].Value);
            Assert.AreEqual(Variables1["q"], match.Bindings["q"].Value);

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["group_id"], match.Bindings["group_id"].Value);
            Assert.AreEqual(Variables1["first_name"], match.Bindings["first_name"].Value);
            Assert.AreEqual(Variables1["page"], match.Bindings["page"].Value);
            Assert.AreEqual(Variables1["lang"], match.Bindings["lang"].Value);
            Assert.AreEqual(Variables1["format"], match.Bindings["format"].Value);
            Assert.AreEqual(Variables1["q"], match.Bindings["q"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestEscapeSequences2()
        {
            string template = "/sparql{?query}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            Assert.AreEqual("/sparql?query=PREFIX%20dc%3A%20%3Chttp%3A%2F%2Fpurl.org%2Fdc%2Felements%2F1.1%2F%3E%20SELECT%20%3Fbook%20%3Fwho%20WHERE%20%7B%20%3Fbook%20dc%3Acreator%20%3Fwho%20%7D", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["query"], match.Bindings["query"].Value);

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["query"], match.Bindings["query"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestEscapeSequences3()
        {
            string template = "/go{?uri}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            Assert.AreEqual("/go?uri=http%3A%2F%2Fexample.org%2F%3Furi%3Dhttp%253A%252F%252Fexample.org%252F", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["uri"], match.Bindings["uri"].Value);

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["uri"], match.Bindings["uri"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestEscapeSequences4()
        {
            string template = "/service{?word}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            Assert.AreEqual("/service?word=dr%C3%BCcken", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["word"], match.Bindings["word"].Value);

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["word"], match.Bindings["word"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestEscapeSequences5()
        {
            string template = "/lookup{?Stra%C3%9Fe}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            Assert.AreEqual("/lookup?Stra%C3%9Fe=Gr%C3%BCner%20Weg", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["Stra%C3%9Fe"], match.Bindings["Stra%C3%9Fe"].Value);

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["Stra%C3%9Fe"], match.Bindings["Stra%C3%9Fe"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.SimpleExpansion)]
        public void TestEscapeSequences6()
        {
            string template = "{random}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            Assert.AreEqual("%C5%A1%C3%B6%C3%A4%C5%B8%C5%93%C3%B1%C3%AA%E2%82%AC%C2%A3%C2%A5%E2%80%A1%C3%91%C3%92%C3%93%C3%94%C3%95%C3%96%C3%97%C3%98%C3%99%C3%9A%C3%A0%C3%A1%C3%A2%C3%A3%C3%A4%C3%A5%C3%A6%C3%A7%C3%BF", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["random"], match.Bindings["random"].Value);

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables1["random"], match.Bindings["random"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestEscapeSequences7()
        {
            string template = "{?assoc_special_chars*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables1);
            Assert.AreEqual("?%C5%A1%C3%B6%C3%A4%C5%B8%C5%93%C3%B1%C3%AA%E2%82%AC%C2%A3%C2%A5%E2%80%A1%C3%91%C3%92%C3%93%C3%94%C3%95=%C3%96%C3%97%C3%98%C3%99%C3%9A%C3%A0%C3%A1%C3%A2%C3%A3%C3%A4%C3%A5%C3%A6%C3%A7%C3%BF", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables1["assoc_special_chars"], (ICollection)match.Bindings["assoc_special_chars"].Value);

            match = uriTemplate.Match(uri, RequiredVariables1, new[] { "fields", "geocode" }, new[] { "assoc_special_chars" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables1["assoc_special_chars"], (ICollection)match.Bindings["assoc_special_chars"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestCompoundPathSegmentExpansionCollectionVariable()
        {
            string template = "{/id*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables2);
            string[] allowed =
                {
                    "/person/albums",
                    "/albums/person"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "id", "fields", "geocode" }, new string[0]);
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables2["id"], (ICollection)match.Bindings["id"].Value);

            match = uriTemplate.Match(uri, RequiredVariables2, new[] { "id", "fields", "geocode" }, new string[0]);
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables2["id"], (ICollection)match.Bindings["id"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.PathSegmentExpansion)]
        [Category(TestCategories.QueryExpansion)]
        public void TestCompoundPathSegmentExpansionWithQueryStringCollectionVariable()
        {
            string template = "{/id*}{?fields,token}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables2);
            string[] allowed =
                {
                    "/person/albums?fields=id,name,picture&token=12345",
                    "/person/albums?fields=id,picture,name&token=12345",
                    "/person/albums?fields=picture,name,id&token=12345",
                    "/person/albums?fields=picture,id,name&token=12345",
                    "/person/albums?fields=name,picture,id&token=12345",
                    "/person/albums?fields=name,id,picture&token=12345",
                    "/albums/person?fields=id,name,picture&token=12345",
                    "/albums/person?fields=id,picture,name&token=12345",
                    "/albums/person?fields=picture,name,id&token=12345",
                    "/albums/person?fields=picture,id,name&token=12345",
                    "/albums/person?fields=name,picture,id&token=12345",
                    "/albums/person?fields=name,id,picture&token=12345"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "id", "fields", "geocode" }, new string[0]);
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables2["id"], (ICollection)match.Bindings["id"].Value);
            CollectionAssert.AreEqual((ICollection)Variables2["fields"], (ICollection)match.Bindings["fields"].Value);
            Assert.AreEqual(Variables2["token"], match.Bindings["token"].Value);

            match = uriTemplate.Match(uri, RequiredVariables2, new[] { "id", "fields", "geocode" }, new string[0]);
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables2["id"], (ICollection)match.Bindings["id"].Value);
            CollectionAssert.AreEqual((ICollection)Variables2["fields"], (ICollection)match.Bindings["fields"].Value);
            Assert.AreEqual(Variables2["token"], match.Bindings["token"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestPathSegmentExpansionEmptyList()
        {
            string template = "{/empty_list}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables3);
            Assert.AreEqual(string.Empty, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);

            match = uriTemplate.Match(uri, RequiredVariables3, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.PathSegmentExpansion)]
        public void TestCompoundPathSegmentExpansionEmptyList()
        {
            string template = "{/empty_list*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables3);
            Assert.AreEqual(string.Empty, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);

            match = uriTemplate.Match(uri, RequiredVariables3, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestQueryExpansionEmptyList()
        {
            string template = "{?empty_list}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables3);
            Assert.AreEqual(string.Empty, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);

            match = uriTemplate.Match(uri, RequiredVariables3, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestCompoundQueryExpansionEmptyList()
        {
            string template = "{?empty_list*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables3);
            Assert.AreEqual(string.Empty, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);

            match = uriTemplate.Match(uri, RequiredVariables3, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestQueryExpansionEmptyMap()
        {
            string template = "{?empty_assoc}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables3);
            Assert.AreEqual(string.Empty, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);

            match = uriTemplate.Match(uri, RequiredVariables3, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestCompoundQueryExpansionEmptyMap()
        {
            string template = "{?empty_assoc*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables3);
            Assert.AreEqual(string.Empty, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);

            match = uriTemplate.Match(uri, RequiredVariables3, new[] { "empty_list" }, new[] { "empty_assoc" });
            Assert.IsNotNull(match);
            Assert.AreEqual(0, match.Bindings.Count);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.SimpleExpansion)]
        public void TestSimpleExpansionNumericKey()
        {
            string template = "{42}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables4);
            Assert.AreEqual("The%20Answer%20to%20the%20Ultimate%20Question%20of%20Life%2C%20the%20Universe%2C%20and%20Everything", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "1337" }, new[] { "german" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables4["42"], match.Bindings["42"].Value);

            match = uriTemplate.Match(uri, RequiredVariables4, new[] { "1337" }, new[] { "german" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables4["42"], match.Bindings["42"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestQueryExpansionNumericKey()
        {
            string template = "{?42}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables4);
            Assert.AreEqual("?42=The%20Answer%20to%20the%20Ultimate%20Question%20of%20Life%2C%20the%20Universe%2C%20and%20Everything", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "1337" }, new[] { "german" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables4["42"], match.Bindings["42"].Value);

            match = uriTemplate.Match(uri, RequiredVariables4, new[] { "1337" }, new[] { "german" });
            Assert.IsNotNull(match);
            Assert.AreEqual(Variables4["42"], match.Bindings["42"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.SimpleExpansion)]
        public void TestSimpleExpansionNumericKeyCollectionVariable()
        {
            string template = "{1337}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables4);
            Assert.AreEqual("leet,as,it,can,be", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "1337" }, new[] { "german" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables4["1337"], (ICollection)match.Bindings["1337"].Value);

            match = uriTemplate.Match(uri, RequiredVariables4, new[] { "1337" }, new[] { "german" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables4["1337"], (ICollection)match.Bindings["1337"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestCompoundQueryExpansionNumericKeyCollectionVariable()
        {
            string template = "{?1337*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables4);
            Assert.AreEqual("?1337=leet&1337=as&1337=it&1337=can&1337=be", uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "1337" }, new[] { "german" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables4["1337"], (ICollection)match.Bindings["1337"].Value);

            match = uriTemplate.Match(uri, RequiredVariables4, new[] { "1337" }, new[] { "german" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables4["1337"], (ICollection)match.Bindings["1337"].Value);
        }

        [Test]
        [Category(TestCategories.Extended)]
        [Category(TestCategories.QueryExpansion)]
        public void TestCompoundQueryExpansionNumericKeyMapVariable()
        {
            string template = "{?german*}";
            global::UriTemplate.Core.UriTemplate uriTemplate = new global::UriTemplate.Core.UriTemplate(template);
            Uri uri = uriTemplate.BindByName(Variables4);
            string[] allowed =
                {
                    "?11=elf&12=zw%C3%B6lf",
                    "?12=zw%C3%B6lf&11=elf"
                };

            CollectionAssert.Contains(allowed, uri.OriginalString);

            UriTemplateMatch match = uriTemplate.Match(uri, new[] { "1337" }, new[] { "german" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables4["german"], (ICollection)match.Bindings["german"].Value);

            match = uriTemplate.Match(uri, RequiredVariables4, new[] { "1337" }, new[] { "german" });
            Assert.IsNotNull(match);
            CollectionAssert.AreEqual((ICollection)Variables4["german"], (ICollection)match.Bindings["german"].Value);
        }
    }
}
