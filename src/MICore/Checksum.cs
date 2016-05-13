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
    public class Checksum
    {
#if CORECLR
        private string _checksumString = null;
        public byte[] Bytes { get; private set; } = null;
        public readonly HashAlgorithmName HashAlgorithmName;

        private Checksum(HashAlgorithmName hashAlgorithmName, byte[] checksumBytes)
        {
            HashAlgorithmName = hashAlgorithmName;
            Bytes = checksumBytes;
        }

        public static Checksum FromBytes(HashAlgorithmName hashAlgorithmName, byte[] checksumBytes)
        {
            if (checksumBytes == null)
            {
                throw new ArgumentNullException("checksumBytes");
            }

            return new Checksum(hashAlgorithmName, checksumBytes);
        }

        /// <summary>
        /// Creates a Checksum from a hex string representation of bytes
        /// </summary>
        /// <param name="hashAlgorithmName">The name of the hash algorithm used to calculate the hash bytes.</param>
        /// <param name="checksumString">Hex String representation of hash bytse. Example: "A0B1C2D3E4F5A6B7C8D9E0F1A2B3C4D5"</param>
        /// <returns></returns>
        public static Checksum FromString(HashAlgorithmName hashAlgorithmName, string checksumString)
        {
            if (checksumString == null)
            {
                throw new ArgumentNullException("checksumString");
            }

            Checksum checksum = new Checksum(hashAlgorithmName, StringToBytes(checksumString));
            checksum._checksumString = checksumString;
            return checksum;
        }

        public override string ToString()
        {
            if (_checksumString == null)
            {
                _checksumString = BytesToString(Bytes);
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
            return FormattableString.Invariant($"--{HashAlgorithmName.Name}checksum {ToString()}");
        }

        public static string GetMIString(IEnumerable<Checksum> checksums)
        {
            if (checksums == null)
            {
                return "";
            }

            IEnumerable<IGrouping<HashAlgorithmName, string>> checksumGroups = checksums.GroupBy(checksum => checksum.HashAlgorithmName, checksum => checksum.ToString());

            StringBuilder builder = new StringBuilder();
            foreach (IGrouping<HashAlgorithmName, string> group in checksumGroups)
            {
                builder.Append(FormattableString.Invariant($"--{group.Key.Name}checksum "));
                builder.Append(string.Join(",", group));
                builder.Append(" ");
            }

            return builder.ToString().Trim(); ;
        }
#else
        /// <summary>
        /// Dummy concstrutor for non coreclr scnearios
        /// </summary>
        public Checksum()
        {
        }
#endif
    }
}
