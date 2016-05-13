using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
#if CORECLR
    public class HashAlgorithmId
    {
        // The AD7 Guid representing this hash algorithm
        public readonly Guid AD7GuidHashAlgorithm = Guid.Empty;

        // Size in bytes of a hash returned by this hash algorithm 
        public readonly uint HashSize = 0;

        public readonly HashAlgorithmName HashAlgorithmName;

        private HashAlgorithmId(Guid guidHashAlgorithm, uint size, HashAlgorithmName hashAlgorithmName)
        {
            AD7GuidHashAlgorithm = guidHashAlgorithm;
            HashSize = size;
            HashAlgorithmName = hashAlgorithmName;
        }

        public static HashAlgorithmId MD5 = new HashAlgorithmId(AD7Guids.guidSourceHashMD5, 16, HashAlgorithmName.MD5);
        public static HashAlgorithmId SHA1 = new HashAlgorithmId(AD7Guids.guidSourceHashSHA1, 20, HashAlgorithmName.SHA1);
        public static HashAlgorithmId SHA1Normalized = new HashAlgorithmId(AD7Guids.guidSourceHashSHA1Normalized, 20, HashAlgorithmName.SHA1);
        public static HashAlgorithmId SHA256 = new HashAlgorithmId(AD7Guids.guidSourceHashSHA256, 32, HashAlgorithmName.SHA256);
        public static HashAlgorithmId SHA256Normalized = new HashAlgorithmId(AD7Guids.guidSourceHashSHA256Normalized, 32, HashAlgorithmName.SHA256);
    }
#endif
}
