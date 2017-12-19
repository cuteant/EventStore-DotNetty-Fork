// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;

namespace UriTemplate.Core
{
    /// <preliminary/>
    partial class UriTemplate
    {
        private UriTemplateMatch Match(Uri baseAddress, Uri candidate, ICollection<string> requiredVariables, ICollection<string> arrayVariables, ICollection<string> mapVariables)
        {
            Uri relative = baseAddress.MakeRelativeUri(candidate);
            StringBuilder pattern = new StringBuilder();
            pattern.Append('^');
            for (int i = 0; i < _parts.Length; i++)
            {
                string group = "part" + i;
                _parts[i].BuildPattern(pattern, group, requiredVariables, arrayVariables, mapVariables);
            }

            pattern.Append('$');

            Match match = Regex.Match(relative.OriginalString, pattern.ToString());
            if (match == null || !match.Success)
                return null;

            List<KeyValuePair<VariableReference, object>> bindings = new List<KeyValuePair<VariableReference, object>>();
            for (int i = 0; i < _parts.Length; i++)
            {
                Group group = match.Groups["part" + i];
                if (!group.Success)
                    return null;

                KeyValuePair<VariableReference, object>[] binding = _parts[i].Match(group.Value, requiredVariables, arrayVariables, mapVariables);
                if (binding == null)
                    return null;

                bindings.AddRange(binding);
            }

            return new UriTemplateMatch(this, bindings)
            {
                BaseUri = baseAddress,
                RequestUri = candidate
            };
        }
    }
}
