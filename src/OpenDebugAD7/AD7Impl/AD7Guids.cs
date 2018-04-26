// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDebugAD7.AD7Impl
{
    internal static class AD7Guids
    {
        public static readonly Guid guidSourceHashMD5 = new Guid("406ea660-64cf-4c82-b6f0-42d48172a799");
        public static readonly Guid guidSourceHashSHA1 = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");
        public static readonly Guid guidSourceHashSHA256 = new Guid("8829d00f-11b8-4213-878b-770e8597ac16");
        public static readonly Guid guidSourceHashSHA1Normalized = new Guid("1E090697-3EB3-4A01-B2F2-1336408F43C2");
        public static readonly Guid guidSourceHashSHA256Normalized = new Guid("AE85F156-6530-4A56-BC1F-051B5D2816EA");
    }
}
