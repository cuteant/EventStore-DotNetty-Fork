// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace UriTemplate.Core
{
    /// <summary>
    /// A class that represents the results of a match operation on a <see cref="UriTemplate"/> instance.
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    /// <preliminary/>
    public class UriTemplateMatch
    {
        /// <summary>
        /// This is the backing field for the <see cref="Template"/> property.
        /// </summary>
        private readonly UriTemplate _template;

        /// <summary>
        /// This collection is the original bindings array which provided the information in
        /// <see cref="_dictionary"/>.
        /// </summary>
        private readonly KeyValuePair<VariableReference, object>[] _bindings;

        /// <summary>
        /// This is the backing field for the <see cref="Bindings"/> property, which is a transformed view
        /// of the original <see cref="_bindings"/> array.
        /// </summary>
        private readonly Dictionary<string, KeyValuePair<VariableReference, object>> _dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="UriTemplateMatch"/> class with the
        /// specified URI Template and collection of variable bindings.
        /// </summary>
        /// <param name="template">The <see cref="UriTemplate"/> associated with this <see cref="UriTemplateMatch"/> instance.</param>
        /// <param name="bindings">A collection of variable bindings identified by the match operation.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>If <paramref name="template"/> is <see langword="null"/>.</para>
        /// <para>-or-</para>
        /// <para>If <paramref name="bindings"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException">
        /// <para>If <paramref name="bindings"/> contains two variable bindings for the same variable which do not have the same value, after considering the prefix modifier(s).</para>
        /// </exception>
        internal UriTemplateMatch(UriTemplate template, IEnumerable<KeyValuePair<VariableReference, object>> bindings)
        {
            if (template == null)
                throw new ArgumentNullException("template");
            if (bindings == null)
                throw new ArgumentNullException("bindings");

            this._template = template;
            this._bindings = new List<KeyValuePair<VariableReference, object>>(bindings).ToArray();

            Dictionary<string, KeyValuePair<VariableReference, object>> dictionary = new Dictionary<string, KeyValuePair<VariableReference, object>>();
            foreach (var pair in _bindings)
            {
                KeyValuePair<VariableReference, object> previous;
                if (!dictionary.TryGetValue(pair.Key.Name, out previous))
                {
                    dictionary.Add(pair.Key.Name, pair);
                    continue;
                }

                if (previous.Key.Prefix == pair.Key.Prefix)
                {
                    if (!previous.Value.Equals(pair.Value))
                        throw new FormatException(string.Format("The URI contains the variable '{0}' more than once with different values", previous.Key.Name));

                    continue;
                }

                if (previous.Key.Prefix == null || pair.Key.Prefix < previous.Key.Prefix)
                {
                    if (!((string)previous.Value).StartsWith((string)pair.Value))
                        throw new FormatException(string.Format("The URI contains the variable '{0}' more than once with different values", previous.Key.Name));
                }
                else
                {
                    if (!((string)pair.Value).StartsWith((string)previous.Value))
                        throw new FormatException(string.Format("The URI contains the variable '{0}' more than once with different values", previous.Key.Name));

                    dictionary[pair.Key.Name] = pair;
                }
            }

            foreach (var item in dictionary)
            {
                _boundVariables.Add(item.Key, item.Value.Value.ToString());
            }

            _dictionary = dictionary;
        }

        /// <summary>
        /// Gets the <see cref="UriTemplate"/> associated with this <see cref="UriTemplateMatch"/> instance.
        /// </summary>
        /// <value>
        /// The <see cref="UriTemplate"/> associated with this <see cref="UriTemplateMatch"/> instance.
        /// </value>
        public UriTemplate Template
        {
            get
            {
                return _template;
            }
        }

        /// <summary>
        /// Gets a collection of bindings from variable name to the actual URI Template variable reference and bound value.
        /// </summary>
        /// <value>
        /// A collection of bindings from variable name to the actual URI Template variable reference and bound value.
        /// </value>
        public IDictionary<string, KeyValuePair<VariableReference, object>> Bindings
        {
            get
            {
                return _dictionary;
            }
        }

        Uri baseUri;
        public Uri BaseUri   // the base address, untouched
        {
            get => this.baseUri;
            set => this.baseUri = value;
        }
        Uri requestUri;
        public Uri RequestUri  // uri on the wire, untouched
        {
            get => this.requestUri;
            set => this.requestUri = value;
        }

        private readonly NameValueCollection _boundVariables = new NameValueCollection();
        public NameValueCollection BoundVariables => _boundVariables;
        NameValueCollection queryParameters;
        public NameValueCollection QueryParameters  // the result of UrlUtility.ParseQueryString (keys and values are decoded)
        {
            get
            {
                if (this.queryParameters == null)
                {
                    PopulateQueryParameters();
                }
                return this.queryParameters;
            }
        }
        void PopulateQueryParameters()
        {
            if (this.requestUri != null)
            {
                this.queryParameters = ParseQueryString(this.requestUri.Query);
            }
            else
            {
                this.queryParameters = new NameValueCollection();
            }
        }

        public static NameValueCollection ParseQueryString(string query)
        {
            // We are adjusting the parsing of UrlUtility.ParseQueryString, which identify
            //  ?wsdl as a null key with wsdl as a value
            NameValueCollection result = UrlUtility.ParseQueryString(query);
            string nullKeyValuesString = result[(string)null];
            if (!string.IsNullOrEmpty(nullKeyValuesString))
            {
                result.Remove(null);
                string[] nullKeyValues = nullKeyValuesString.Split(',');
                for (int i = 0; i < nullKeyValues.Length; i++)
                {
                    result.Add(nullKeyValues[i], null);
                }
            }
            return result;
        }
    }
}
