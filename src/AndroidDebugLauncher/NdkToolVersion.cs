// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidDebugLauncher
{
    internal struct NdkToolVersion : IComparable<NdkToolVersion>
    {
        /// <summary>
        /// [Optional] array of version parts (ex: { major, minor, etc }). This will be null if the default constructor is called.
        /// </summary>
        private readonly uint[] _versionParts;

        /// <summary>
        /// Creates a new NdkVersion structure
        /// </summary>
        /// <param name="versionParts">[Required] array of units for each part (ex: { major, minor, etc })</param>
        public NdkToolVersion(uint[] versionParts)
        {
            if (versionParts == null)
            {
                throw new ArgumentNullException("versionParts");
            }

            _versionParts = versionParts;
        }

        /// <summary>
        /// Compare this version to another
        /// </summary>
        /// <param name="other">value to compare against</param>
        /// <returns>
        /// Less than zero: This object is less than the other parameter.
        /// Zero: This object is equal to other. 
        /// Greater than zero: This object is greater than other.
        /// </returns>
        public int CompareTo(NdkToolVersion other)
        {
            for (int index = 0; true; index++)
            {
                if (index >= this.Length)
                {
                    if (index != other.Length)
                    {
                        return -1; // 'other' is larger
                    }
                    else
                    {
                        return 0; // equal
                    }
                }
                else if (index >= other.Length)
                {
                    return 1; // 'this' is larger
                }

                uint thisPart = _versionParts[index];
                uint otherPart = other._versionParts[index];

                if (thisPart != otherPart)
                {
                    if (thisPart > otherPart)
                        return 1;
                    else
                        return -1;
                }
            }
        }

        public static bool TryParse(string versionString, out NdkToolVersion version)
        {
            string[] versionPartStrings = versionString.Split('.');
            if (versionPartStrings.Length < 2)
            {
                version = new NdkToolVersion();
                return false;
            }

            uint[] versionParts = new uint[versionPartStrings.Length];
            for (int c = 0; c < versionParts.Length; c++)
            {
                if (!uint.TryParse(versionPartStrings[c], NumberStyles.None, CultureInfo.InvariantCulture, out versionParts[c]))
                {
                    version = new NdkToolVersion();
                    return false;
                }
            }

            version = new NdkToolVersion(versionParts);
            return true;
        }

        public override string ToString()
        {
            if (_versionParts == null)
            {
                throw new InvalidOperationException();
            }

            IEnumerable<string> parts = _versionParts.Select((x) => x.ToString(CultureInfo.InvariantCulture));
            return string.Join(".", parts);
        }

        private int Length
        {
            get { return _versionParts != null ? _versionParts.Length : 0; }
        }
    }
}
