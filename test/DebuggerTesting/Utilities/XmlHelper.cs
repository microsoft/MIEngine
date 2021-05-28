// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace DebuggerTesting.Utilities
{
    public static class XmlHelper
    {
#if CORECLR
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver", Justification = "XmlResolver property does not exist in CoreCLR.")]
#endif
        public static XmlReader CreateXmlReader(string uri)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.DtdProcessing = DtdProcessing.Prohibit;
#if !CORECLR
            settings.XmlResolver = null;
#endif
            return XmlReader.Create(uri, settings);
        }

        public static string GetAttributeValue(this XElement element, string attributeName)
        {
            return element.Attribute(attributeName)?.Value;
        }

        public static TEnum GetAttributeEnum<TEnum>(this XElement element, string attributeName)
            where TEnum : struct
        {
            string attributeValue = GetAttributeValue(element, attributeName);
            TEnum value;
            if (!Enum.TryParse(attributeValue, true, out value))
                throw new ArgumentOutOfRangeException(nameof(attributeValue), "Unhandled " + typeof(TEnum).Name + " value " + attributeValue);
            return value;
        }

        public static bool? GetAttributeValueAsBool(this XElement element, string attributeName)
        {
            string value = element.GetAttributeValue(attributeName);
            if (string.IsNullOrEmpty(value))
                return null;

            return bool.Parse(value);
        }

        public static double? GetAttributeValueAsDouble(this XElement element, string attributeName)
        {
            string value = element.GetAttributeValue(attributeName);
            if (string.IsNullOrEmpty(value))
                return null;

            return double.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}