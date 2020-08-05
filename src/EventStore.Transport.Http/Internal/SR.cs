﻿using System;
using System.Resources;

namespace EventStore.Transport.Http.Internal
{
    internal sealed partial class SR : Strings
    {
        // Needed for debugger integration
        internal static string GetResourceString(string resourceKey)
        {
            return GetResourceString(resourceKey, String.Empty);
        }

        internal static string GetResourceString(string resourceKey, string defaultString)
        {
            string resourceString = null;
            try { resourceString = ResourceManager.GetString(resourceKey, null); }
            catch (MissingManifestResourceException) { }

            if (defaultString is object && string.Equals(resourceKey, resourceString))
            {
                return defaultString;
            }

            return resourceString;
        }

        internal static string Format(string resourceFormat, params object[] args)
        {
            if (args is object)
            {
                return String.Format(resourceFormat, args);
            }

            return resourceFormat;
        }

        internal static string Format(string resourceFormat, object p1)
        {
            return String.Format(resourceFormat, p1);
        }

        internal static string Format(string resourceFormat, object p1, object p2)
        {
            return String.Format(resourceFormat, p1, p2);
        }

        internal static string Format(string resourceFormat, object p1, object p2, object p3)
        {
            return String.Format(resourceFormat, p1, p2, p3);
        }
    }
}
