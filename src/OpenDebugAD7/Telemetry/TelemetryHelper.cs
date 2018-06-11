// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDebugAD7
{
    internal static class TelemetryHelper
    {
        /// <summary>
        /// Ensures event name is properly prefixed.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <returns>properly prefixed eventname</returns>
        public static string EnsureEventNamePrefix(string eventNamePrefix, string eventName)
        {
            if (!eventName.StartsWith(eventNamePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return String.Concat(eventNamePrefix, eventName);
            }

            return eventName;
        }

        /// <summary>
        /// Ensures Property names in the dictionary are properly prefixed. If not, it will convert the eventname and append it as the prefix
        /// </summary>
        /// <param name="eventName">Name of the telemetry event</param>
        /// <param name="properties">Dictionary of telemetry properties</param>
        /// <returns>prefixed properties array</returns>
        public static Dictionary<string, object> EnsurePropertyPrefix(string propertyNamePrefix, IDictionary<string, object> properties)
        {
            Debug.Assert(properties != null, "properties dictionary should not be null");

            if (properties == null)
                return null;

            Dictionary<string, object> prefixedProperties = new Dictionary<string, object>(properties.Count);

            foreach (var item in properties)
            {
                string key = item.Key;
                if (!key.StartsWith(propertyNamePrefix, StringComparison.OrdinalIgnoreCase))
                    key = String.Concat(propertyNamePrefix, key);

                prefixedProperties[key] = item.Value;
            }

            return prefixedProperties;
        }

        /// <summary>
        /// Merges items from target into destination. if there are duplicate keys, target wins.
        /// </summary>
        public static void Merge(this Dictionary<string, object> destination, Dictionary<string, object> target)
        {
            if (target == null)
            {
                return;
            }

            if (destination == null)
            {
                destination = new Dictionary<string, object>(target);
                return;
            }

            foreach (var item in target)
            {
                destination[item.Key] = item.Value;
            }
        }
    }
}
