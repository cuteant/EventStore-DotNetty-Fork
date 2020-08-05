﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;

namespace EventStore.Common.Utils
{
    public class IPAddressConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            var valueAsString = value as string;
            if (valueAsString is object)
            {
                return IPAddress.Parse(valueAsString);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
