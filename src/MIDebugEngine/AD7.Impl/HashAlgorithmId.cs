// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MICore;

namespace Microsoft.MIDebugEngine
{
    public class HashAlgorithmId
    {
        // The AD7 Guid representing this hash algorithm
        public readonly Guid AD7GuidHashAlgorithm = Guid.Empty;

        // Size in bytes of a hash returned by this hash algorithm 
        public readonly uint HashSize = 0;

        public readonly MIHashAlgorithmName MIHashAlgorithmName;

        private HashAlgorithmId(Guid guidHashAlgorithm, uint size, MIHashAlgorithmName hashAlgorithmName)
        {
            AD7GuidHashAlgorithm = guidHashAlgorithm;
            HashSize = size;
            MIHashAlgorithmName = hashAlgorithmName;
        }

        public static HashAlgorithmId MD5 = new HashAlgorithmId(AD7Guids.guidSourceHashMD5, 16, MIHashAlgorithmName.MD5);
        public static HashAlgorithmId SHA1 = new HashAlgorithmId(AD7Guids.guidSourceHashSHA1, 20, MIHashAlgorithmName.SHA1);
        public static HashAlgorithmId SHA1Normalized = new HashAlgorithmId(AD7Guids.guidSourceHashSHA1Normalized, 20, MIHashAlgorithmName.SHA1);
        public static HashAlgorithmId SHA256 = new HashAlgorithmId(AD7Guids.guidSourceHashSHA256, 32, MIHashAlgorithmName.SHA256);
        public static HashAlgorithmId SHA256Normalized = new HashAlgorithmId(AD7Guids.guidSourceHashSHA256Normalized, 32, MIHashAlgorithmName.SHA256);
    }
}
