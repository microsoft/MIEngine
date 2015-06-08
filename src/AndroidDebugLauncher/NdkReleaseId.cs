// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidDebugLauncher
{
    /// <summary>
    /// Parsed version of the NDK release number from 'release.txt' in the root of the NDK
    /// </summary>
    internal struct NdkReleaseId
    {
        /// <summary>
        /// Numeric portion of the release (ex: for r10b, this is '10')
        /// </summary>
        public readonly uint ReleaseNumber;

        /// <summary>
        /// Alphabetic portion of the release (ex: for r10b, this is 'b'). It is '0' for the initial release.
        /// </summary>
        public readonly char SubRelease;

        /// <summary>
        /// Indicates if the NDK is 32-bit or 64-bit
        /// </summary>
        public bool Is32Bit;

        public NdkReleaseId(uint releaseNumber, char subRelease, bool is32Bit)
        {
            if (releaseNumber == 0)
            {
                throw new ArgumentOutOfRangeException("releaseNumber");
            }

            this.ReleaseNumber = releaseNumber;
            this.SubRelease = subRelease;
            this.Is32Bit = is32Bit;
        }

        public bool IsValid
        {
            get { return this.ReleaseNumber != 0; }
        }

        /// <summary>
        /// Try to parse the version number from the ndk release.txt file.
        /// </summary>
        /// <param name="ndkReleaseVersionFile">[Required] path to the NDK release.txt file. This file must exist</param>
        /// <param name="result">On success, a valid version</param>
        /// <returns>true if successful</returns>
        public static bool TryParseFile(string ndkReleaseVersionFile, out NdkReleaseId result)
        {
            result = new NdkReleaseId();

            using (StreamReader reader = File.OpenText(ndkReleaseVersionFile))
            {
                while (true)
                {
                    string version = reader.ReadLine();
                    if (version == null)
                        return false; // end of file

                    version = version.Trim();
                    if (version.Length == 0)
                        continue; // ignore any blank lines. I don't expect there to be any, but it seems reasonable to do

                    return TryParse(version, out result);
                }
            }
        }

        public static bool TryParse(string value, out NdkReleaseId result)
        {
            // The version number should look like:
            //   r10
            //   -or-
            //   r10b
            //   -or-
            //   r10b (64-bit)

            result = new NdkReleaseId();

            if (value.Length < 2)
                return false;

            if (value[0] != 'r')
                return false;

            int currentPosition = 1;
            while (currentPosition < value.Length && char.IsDigit(value[currentPosition]))
            {
                currentPosition++;
            }

            if (currentPosition == 1)
            {
                return false; // no number
            }

            string numberString = value.Substring(1, currentPosition - 1);
            uint releaseNumber;
            if (!uint.TryParse(numberString, NumberStyles.None, CultureInfo.InvariantCulture, out releaseNumber) ||
                releaseNumber == 0)
            {
                return false;
            }

            char subRelease = (char)0;
            bool is32bit = true;

            if (currentPosition < value.Length)
            {
                subRelease = value[currentPosition];
                currentPosition++;

                if (subRelease < 'a' || subRelease > 'z')
                    return false;

                // skip past any spaces
                while (currentPosition < value.Length && value[currentPosition] == ' ')
                {
                    currentPosition++;
                }

                if (currentPosition < value.Length)
                {
                    string suffix = value.Substring(currentPosition);
                    if (!suffix.Equals("(64-bit)", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    is32bit = false;
                }
            }

            result = new NdkReleaseId(releaseNumber, subRelease, is32bit);
            return true;
        }

        /// <summary>
        /// Compare this version to another, ignoring 32-bit vs. 64-bit.
        /// </summary>
        /// <param name="other">value to compare against</param>
        /// <returns>
        /// Less than zero: This object is less than the other parameter.
        /// Zero: This object is equal to other. 
        /// Greater than zero: This object is greater than other.
        /// </returns>
        public int CompareVersion(NdkReleaseId other)
        {
            if (this.ReleaseNumber != other.ReleaseNumber)
            {
                if (this.ReleaseNumber < other.ReleaseNumber)
                    return -1;
                else
                    return 1;
            }

            if (this.SubRelease != other.SubRelease)
            {
                if (this.SubRelease < other.SubRelease)
                    return -1;
                else
                    return 1;
            }

            return 0;
        }

        public override string ToString()
        {
            if (!this.IsValid)
            {
                return "<invalid>";
            }
            else
            {
                string suffix = string.Empty;
                if (!this.Is32Bit)
                {
                    suffix = " (64-bit)";
                }

                object subRelease = string.Empty;
                if (this.SubRelease != (char)0)
                    subRelease = this.SubRelease;

                return string.Format(CultureInfo.InvariantCulture, "r{0}{1}{2}", this.ReleaseNumber, subRelease, suffix);
            }
        }
    }
}
