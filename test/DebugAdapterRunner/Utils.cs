// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text.RegularExpressions;

namespace DebugAdapterRunner
{
    internal class Utils
    {
        /// <summary>Returns whether the given array matches the pattern specified in the 'expected' array</summary>
        /// <returns>True if matching, false otherwise</returns>
        /// <remarks>The 'expected' array contains a list of elements that are searched in the actual array
        /// The actual array can have more elements. Whether the order of elements matters is up to
        /// the 'ignoreOrder' flag</remarks>
        private static bool CompareArrays(JArray expected, JArray actual, bool ignoreOrder)
        {
            return ignoreOrder ? CompareArraysUnordered(expected, actual) : CompareArraysInOrder(expected, actual);
        }

        /// <summary>Returns whether the given array contains each of the patterns specified in the 'expected' array in order.</summary>
        /// <returns>True if matching, false otherwise</returns>
        /// <remarks>The 'expected' array contains a list of elements that are searched in the actual array
        /// The actual array can have more elements. The order of elements in the 'actual' array must
        /// match the order found in the expected array</remarks>
        private static bool CompareArraysInOrder(JArray expected, JArray actual)
        {
            int currentExpectedIndex = 0;
            int currentActualIndex = 0;

            JToken[] expectedChildren = expected.Children().ToArray();
            JToken[] actualChildren = actual.Children().ToArray();

            while (currentExpectedIndex < expectedChildren.Length)
            {
                JToken expectedMember = expectedChildren[currentExpectedIndex];
                bool foundMatching = false;

                while (currentActualIndex < actualChildren.Length)
                {
                    JToken actualMember = actualChildren[currentActualIndex];

                    if (!CompareObjects(expectedMember.Value<object>(), actualMember.Value<object>(), ignoreOrder: false))
                    {
                        currentActualIndex++;
                    }
                    else
                    {
                        foundMatching = true;
                        break;
                    }
                }

                if (!foundMatching)
                {
                    return false;
                }

                currentActualIndex++;
                currentExpectedIndex++;
            }

            return true;
        }

        /// <summary>Returns whether the given array contains each of the patterns specified in the 'expected' array.</summary>
        /// <returns>True if matching, false otherwise</returns>
        /// <remarks>The 'expected' array contains a list of elements that are searched in the actual array.
        /// The actual array can have more elements, and the order of elements in the 'actual' array does not
        /// need to match the order found in the expected array. If there are multiple identical expected responses,
        /// there must be at least that many identical actual responses.</remarks>
        private static bool CompareArraysUnordered(JArray expected, JArray actual)
        {
            JArray actualCopy = new JArray(actual);
            foreach (JToken expectedMember in expected)
            {
                JToken foundMember = null;

                foreach (JToken actualMember in actualCopy)
                {
                    if (CompareObjects(expectedMember.Value<object>(), actualMember.Value<object>(), ignoreOrder: true))
                    {
                        foundMember = actualMember;
                        break;
                    }
                }

                if (foundMember != null)
                {
                    // Don't compare against this item again.
                    actualCopy.Remove(foundMember);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompareSimpleValues(object expected, object actual)
        {
            return Regex.IsMatch(actual.ToString(), expected.ToString(), RegexOptions.IgnoreCase);
        }

        /// <summary>Returns whether a given object matches the pattern specified by a given 'expected' object</summary>
        /// <returns>True, if matching, false otherwise</returns>
        /// <remarks>
        ///  - 'expected' object fields should specify a regex pattern to be matched in the target object
        ///  - Only the fields specified in the 'expected' object are matched (i.e. the target object can have more fields)
        ///  - Fields that are arrays are matched by searching the target array for the elements specified in the array field of the 'expected' object
        ///  - Arrays are searched in order by default. To ignore the order, specify 'ignoreOrder = true'
        /// </remarks>
        public static bool CompareObjects(object expected, object actual, bool ignoreOrder = false)
        {
            JObject expectedObject = JObject.FromObject(expected);
            JObject actualObject = JObject.FromObject(actual);

            foreach (JToken expectedToken in expectedObject.Children())
            {
                JProperty property = expectedToken as JProperty;
                JToken actualToken = actualObject.Property(property.Name);

                if (actualToken == null || !(actualToken is JProperty)) // Property not found
                    return false;

                JProperty actualProperty = actualToken as JProperty;

                if (property.Value is JArray)
                {
                    if (!(actualProperty.Value is JArray))
                        return false;

                    if (!CompareArrays(property.Value as JArray, actualProperty.Value as JArray, ignoreOrder))
                        return false;
                }
                else if (property.Value.HasValues)
                {
                    if (!actualProperty.Value.HasValues)
                        return false;

                    if (!CompareObjects(property.Value.Value<object>(), actualProperty.Value.Value<object>(), ignoreOrder))
                        return false;
                }
                else if (!property.Value.HasValues && property.Value is JObject && actualProperty.Value is JObject)
                {
                    // If property.Value is an empty object, we want to ignore actualProperty.Value's values as long as they are both JObject
                }
                else
                {
                    if (!CompareSimpleValues(property.Value.Value<object>(), actualProperty.Value.Value<object>()))
                        return false;
                }
            }

            return true;
        }
    }
}
