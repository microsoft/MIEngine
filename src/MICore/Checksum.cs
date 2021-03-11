// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MICore
{
    public enum MIHashAlgorithmName
    {
        MD5,
        SHA1,
        SHA256
    }

    public class Checksum
    {
        private string _checksumString = null;
        private byte[] _bytes = null;

        public readonly MIHashAlgorithmName MIHashAlgorithmName;

        private Checksum(MIHashAlgorithmName hashAlgorithmName, byte[] checksumBytes)
        {
            MIHashAlgorithmName = hashAlgorithmName;
            _bytes = checksumBytes;
        }

        public static Checksum FromBytes(MIHashAlgorithmName hashAlgorithmName, byte[] checksumBytes)
        {
            if (checksumBytes == null)
            {
                throw new ArgumentNullException(nameof(checksumBytes));
            }

            return new Checksum(hashAlgorithmName, checksumBytes);
        }

        /// <summary>
        /// Creates a Checksum from a hex string representation of bytes
        /// </summary>
        /// <param name="hashAlgorithmName">The name of the hash algorithm used to calculate the hash bytes.</param>
        /// <param name="checksumString">Hex String representation of hash bytse. Example: "A0B1C2D3E4F5A6B7C8D9E0F1A2B3C4D5"</param>
        /// <returns></returns>
        public static Checksum FromString(MIHashAlgorithmName hashAlgorithmName, string checksumString)
        {
            if (checksumString == null)
            {
                throw new ArgumentNullException(nameof(checksumString));
            }

            Checksum checksum = new Checksum(hashAlgorithmName, StringToBytes(checksumString));
            checksum._checksumString = checksumString;
            return checksum;
        }

        public byte[] GetBytes()
        {
            return _bytes;
        }

        public override string ToString()
        {
            if (_checksumString == null)
            {
                _checksumString = BytesToString(_bytes);
            }
            return _checksumString;
        }

        private static byte[] StringToBytes(string checksumString)
        {
            if (checksumString.Length % 2 != 0)
            {
                throw new ArgumentException("checksumString is not a valid hex string");
            }

            byte[] checksumBytes = new byte[checksumString.Length / 2];
            for (int i = 0; i < checksumString.Length / 2; i++)
            {
                string hexByteString = checksumString.Substring(i * 2, 2);
                checksumBytes[i] = byte.Parse(hexByteString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return checksumBytes;
        }

        private static string BytesToString(byte[] checksumBytes)
        {
            StringBuilder builder = new StringBuilder(checksumBytes.Length * 2);
            foreach (byte checksumByte in checksumBytes)
            {
                builder.Append(checksumByte.ToString("x2", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        public string ToMIString()
        {
            return string.Format(CultureInfo.InvariantCulture, "--{0}checksum {1}", MIHashAlgorithmName.ToString(), this.ToString());
        }

        public static string GetMIString(IEnumerable<Checksum> checksums)
        {
            if (checksums == null)
            {
                return string.Empty;
            }

            IEnumerable<IGrouping<MIHashAlgorithmName, string>> checksumGroups = checksums.GroupBy(checksum => checksum.MIHashAlgorithmName, checksum => checksum.ToString());

            StringBuilder builder = new StringBuilder();
            foreach (IGrouping<MIHashAlgorithmName, string> group in checksumGroups)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(string.Format(CultureInfo.InvariantCulture, "--{0}checksum ", group.Key.ToString()));
                builder.Append(string.Join(",", group));
            }

            return builder.ToString();
        }
    }
}
