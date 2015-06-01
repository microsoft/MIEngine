// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AndroidDebugLauncher;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MICoreUnitTests
{
    [TestClass]
    public class NdkVersionTests
    {
        [TestMethod]
        public void TestNdkToolVersion1()
        {
            NdkToolVersion v;
            Assert.IsTrue(NdkToolVersion.TryParse("1.2.3.4.5", out v));
            Assert.AreEqual(v.ToString(), "1.2.3.4.5");

            Assert.IsTrue(NdkToolVersion.TryParse("6.7", out v));
            Assert.AreEqual(v.ToString(), "6.7");

            Assert.IsFalse(NdkToolVersion.TryParse("1", out v));
            Assert.IsFalse(NdkToolVersion.TryParse("1.0-beta", out v));
            Assert.IsFalse(NdkToolVersion.TryParse("No.Version", out v));
        }

        [TestMethod]
        public void TestNdkToolVersion2()
        {
            NdkToolVersion v1, v2, v3;
            Assert.IsTrue(NdkToolVersion.TryParse("1.2.3", out v1));
            Assert.IsTrue(NdkToolVersion.TryParse("1.2", out v2));
            Assert.IsTrue(NdkToolVersion.TryParse("2.0", out v3));

            Assert.IsTrue(v1.CompareTo(v2) > 0);
            Assert.IsTrue(v2.CompareTo(v1) < 0);

            Assert.IsTrue(v2.CompareTo(v3) < 0);
            Assert.IsTrue(v3.CompareTo(v2) > 0);

            Assert.IsTrue(v1.CompareTo(new NdkToolVersion()) > 0);

            Assert.IsTrue(v1.CompareTo(v1) == 0);
            Assert.IsTrue(v2.CompareTo(v2) == 0);
            Assert.IsTrue(v3.CompareTo(v3) == 0);

            NdkToolVersion[] versions = { v1, v2, v3 };
            IEnumerable<string> versionStrings = versions.OrderByDescending((x) => x).Select((x) => x.ToString());
            string orderedResults = string.Join(", ", versionStrings);
            Assert.AreEqual(orderedResults, "2.0, 1.2.3, 1.2");
        }

        [TestMethod]
        public void TestNdkReleaseId1()
        {
            NdkReleaseId r;
            Assert.IsTrue(NdkReleaseId.TryParse("r1", out r));
            Assert.AreEqual(r.ToString(), "r1");

            Assert.IsTrue(NdkReleaseId.TryParse("r1a", out r));
            Assert.AreEqual(r.ToString(), "r1a");

            Assert.IsTrue(NdkReleaseId.TryParse("r10", out r));
            Assert.AreEqual(r.ToString(), "r10");

            Assert.IsTrue(NdkReleaseId.TryParse("r10b (64-bit)", out r));
            Assert.AreEqual(r.ToString(), "r10b (64-bit)");

            Assert.IsFalse(NdkReleaseId.TryParse("100", out r));
            Assert.IsFalse(NdkReleaseId.TryParse("r", out r));
            Assert.IsFalse(NdkReleaseId.TryParse("ra", out r));
            Assert.IsFalse(NdkReleaseId.TryParse("r10 jam", out r));
            Assert.IsFalse(NdkReleaseId.TryParse("r10!", out r));
        }

        [TestMethod]
        public void TestNdkReleaseId2()
        {
            NdkReleaseId r10 = new NdkReleaseId(10, (char)0, true);
            NdkReleaseId r10_64bit = new NdkReleaseId(10, (char)0, false);
            NdkReleaseId r10b = new NdkReleaseId(10, 'b', true);
            NdkReleaseId r9d = new NdkReleaseId(9, 'd', true);

            Assert.IsTrue(r10.CompareVersion(r10) == 0);
            Assert.IsTrue(r10.CompareVersion(r10_64bit) == 0);
            Assert.IsTrue(r10.CompareVersion(r10b) < 0);
            Assert.IsTrue(r10b.CompareVersion(r10) > 0);
            Assert.IsTrue(r10.CompareVersion(r9d) > 0);
            Assert.IsTrue(r9d.CompareVersion(r10) < 0);
        }
    }
}
