// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.MIDebugEngine
{
    // These are well-known guids in AD7. Most of these are used to specify filters in what the debugger UI is requesting.
    // For instance, guidFilterLocals can be passed to IDebugStackFrame2::EnumProperties to specify only locals are requested.
    internal static class AD7Guids
    {
        public static readonly Guid guidFilterRegisters = new Guid("223ae797-bd09-4f28-8241-2763bdc5f713");
        public static readonly Guid guidFilterLocals = new Guid("b200f725-e725-4c53-b36a-1ec27aef12ef");
        public static readonly Guid guidFilterAllLocals = new Guid("196db21f-5f22-45a9-b5a3-32cddb30db06");
        public static readonly Guid guidFilterArgs = new Guid("804bccea-0475-4ae7-8a46-1862688ab863");
        public static readonly Guid guidFilterLocalsPlusArgs = new Guid("e74721bb-10c0-40f5-807f-920d37f95419");
        public static readonly Guid guidFilterAllLocalsPlusArgs = new Guid("939729a8-4cb0-4647-9831-7ff465240d5f");
        public static readonly Guid guidLanguageCpp = new Guid("3a12d0b7-c26c-11d0-b442-00a0244a1dd2");
        public static readonly Guid guidLanguageC = new Guid("63A08714-FC37-11D2-904C-00C04FA302A1");
        public static readonly Guid guidSourceHashMD5 = new Guid("406ea660-64cf-4c82-b6f0-42d48172a799");
        public static readonly Guid guidSourceHashSHA1 = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");
        public static readonly Guid guidSourceHashSHA256 = new Guid("8829d00f-11b8-4213-878b-770e8597ac16");
        public static readonly Guid guidSourceHashSHA1Normalized = new Guid("1E090697-3EB3-4A01-B2F2-1336408F43C2");
        public static readonly Guid guidSourceHashSHA256Normalized = new Guid("AE85F156-6530-4A56-BC1F-051B5D2816EA");
    }
}
