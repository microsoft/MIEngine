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

        public NdkReleaseId(uint releaseNumber, char subRelease)
        {
            if (releaseNumber == 0)
            {
                throw new ArgumentOutOfRangeException("releaseNumber");
            }

            this.ReleaseNumber = releaseNumber;
            this.SubRelease = subRelease;
        }

        public bool IsValid
        {
            get { return this.ReleaseNumber != 0; }
        }

        /// <summary>
        /// Try to parse the revision from the ndk source.properties file
        /// </summary>
        /// <param name="ndkSourcePropertiesFilePath">[Required] path to the NDK source.properties file. This file must exist</param>
        /// <param name="result"></param>
        /// <returns>true if successful</returns>
        public static bool TryParsePropertiesFile(string ndkSourcePropertiesFilePath, out NdkReleaseId result)
        {
            result = new NdkReleaseId();

            using (StreamReader reader = File.OpenText(ndkSourcePropertiesFilePath))
            {
                Dictionary<string, string> properties = new Dictionary<string, string>();
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                        break; // end of file

                    line = line.Trim();
                    if (line.Length == 0)
                        continue; // ignore any blank lines. I don't expect there to be any, but it seems reasonable to do

                    // .properties files can theoretically have '=' characters in the value portion,
                    // but I don't expect this will happen and our parsing logic below can't handle it anyway
                    string[] keyValue = line.Split('=');
                    if (keyValue.Length != 2)
                    {
                        return false;
                    }

                    properties.Add(keyValue[0].Trim(), keyValue[1].Trim());
                }

                string revision;
                if (properties.TryGetValue("Pkg.Revision", out revision))
                {
                    return TryParseRevision(revision, out result);
                }

                return false;
            }
        }

        /// <summary>
        /// Try to parse the revision from the ndk
        /// </summary>
        /// <param name="revision">[Required] revision of the ndk in the format major.hotfix.build</param>
        /// <param name="result">On success, a valid version</param>
        /// <returns>true if successful</returns>
        public static bool TryParseRevision(string revision, out NdkReleaseId result)
        {
            result = new NdkReleaseId();

            string[] numbers = revision.Split('.');
            if (numbers.Length != 3)
            {
                return false;
            }

            string releaseString = numbers[0];
            uint releaseNumber;

            if (!uint.TryParse(releaseString, NumberStyles.None, CultureInfo.InvariantCulture, out releaseNumber) ||
                releaseNumber == 0)
            {
                return false;
            }

            string subReleaseString = numbers[1];
            char subReleaseChar = 'a';
            int subReleaseNumber;
            if (!int.TryParse(subReleaseString, out subReleaseNumber))
            {
                return false;
            }

            subReleaseChar += Convert.ToChar(subReleaseNumber);

            result = new NdkReleaseId(releaseNumber, subReleaseChar);
            return true;
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

            if (currentPosition < value.Length)
            {
                subRelease = value[currentPosition];
                currentPosition++;

                if (subRelease < 'a' || subRelease > 'z')
                    return false;
            }

            result = new NdkReleaseId(releaseNumber, subRelease);
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
                object subRelease = string.Empty;
                if (this.SubRelease != (char)0 && this.SubRelease != 'a')
                    subRelease = this.SubRelease;

                return string.Format(CultureInfo.InvariantCulture, "r{0}{1}", this.ReleaseNumber, subRelease);
            }
        }
    }
}
