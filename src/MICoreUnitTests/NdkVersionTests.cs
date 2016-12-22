// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AndroidDebugLauncher;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MICoreUnitTests
{
    public class NdkVersionTests
    {
        [Fact]
        public void TestNdkToolVersion1()
        {
            NdkToolVersion v;
            Assert.True(NdkToolVersion.TryParse("1.2.3.4.5", out v));
            Assert.Equal(v.ToString(), "1.2.3.4.5");

            Assert.True(NdkToolVersion.TryParse("6.7", out v));
            Assert.Equal(v.ToString(), "6.7");

            Assert.False(NdkToolVersion.TryParse("1", out v));
            Assert.False(NdkToolVersion.TryParse("1.0-beta", out v));
            Assert.False(NdkToolVersion.TryParse("No.Version", out v));
        }

        [Fact]
        public void TestNdkToolVersion2()
        {
            NdkToolVersion v1, v2, v3;
            Assert.True(NdkToolVersion.TryParse("1.2.3", out v1));
            Assert.True(NdkToolVersion.TryParse("1.2", out v2));
            Assert.True(NdkToolVersion.TryParse("2.0", out v3));

            Assert.True(v1.CompareTo(v2) > 0);
            Assert.True(v2.CompareTo(v1) < 0);

            Assert.True(v2.CompareTo(v3) < 0);
            Assert.True(v3.CompareTo(v2) > 0);

            Assert.True(v1.CompareTo(new NdkToolVersion()) > 0);

            Assert.True(v1.CompareTo(v1) == 0);
            Assert.True(v2.CompareTo(v2) == 0);
            Assert.True(v3.CompareTo(v3) == 0);

            NdkToolVersion[] versions = { v1, v2, v3 };
            IEnumerable<string> versionStrings = versions.OrderByDescending((x) => x).Select((x) => x.ToString());
            string orderedResults = string.Join(", ", versionStrings);
            Assert.Equal(orderedResults, "2.0, 1.2.3, 1.2");
        }

        [Fact]
        public void TestNdkReleaseIdReleaseFile()
        {
            NdkReleaseId r;
            Assert.True(NdkReleaseId.TryParse("r1", out r));
            Assert.Equal(r.ToString(), "r1");

            Assert.True(NdkReleaseId.TryParse("r1a", out r));
            Assert.Equal(r.ToString(), "r1");

            Assert.True(NdkReleaseId.TryParse("r10", out r));
            Assert.Equal(r.ToString(), "r10");

            Assert.True(NdkReleaseId.TryParse("r10b (64-bit)", out r));
            Assert.Equal(r.ToString(), "r10b");

            Assert.False(NdkReleaseId.TryParse("100", out r));
            Assert.False(NdkReleaseId.TryParse("r", out r));
            Assert.False(NdkReleaseId.TryParse("ra", out r));
            Assert.False(NdkReleaseId.TryParse("r10 jam", out r));
            Assert.False(NdkReleaseId.TryParse("r10!", out r));
        }

        [Fact]
        public void TestNdkReleaseIdCompare()
        {
            NdkReleaseId r10 = new NdkReleaseId(10, (char)0);
            NdkReleaseId r10b = new NdkReleaseId(10, 'b');
            NdkReleaseId r9d = new NdkReleaseId(9, 'd');

            Assert.True(r10.CompareVersion(r10) == 0);
            Assert.True(r10.CompareVersion(r10b) < 0);
            Assert.True(r10b.CompareVersion(r10) > 0);
            Assert.True(r10.CompareVersion(r9d) > 0);
            Assert.True(r9d.CompareVersion(r10) < 0);
        }

        [Fact]
        public void TestNdkReleaseIdPropertiesFile()
        {
            NdkReleaseId r;
            Assert.True(NdkReleaseId.TryParseRevision("1.0.0", out r));
            Assert.Equal("r1", r.ToString());

            Assert.True(NdkReleaseId.TryParseRevision("1.0.1234", out r));
            Assert.Equal("r1", r.ToString());

            Assert.True(NdkReleaseId.TryParseRevision("13.2.0", out r));
            Assert.Equal(r.ToString(), "r13c");

            Assert.False(NdkReleaseId.TryParseRevision("100", out r));
            Assert.False(NdkReleaseId.TryParseRevision("r11b", out r));
            Assert.False(NdkReleaseId.TryParseRevision("-100", out r));
            Assert.False(NdkReleaseId.TryParseRevision("10.0", out r));
        }
    }
}
