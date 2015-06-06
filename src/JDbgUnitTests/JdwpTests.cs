// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JDbg;

namespace JDbgUnitTests
{
    [TestClass]
    public class JdwpTests
    {
        [TestMethod]
        public void VersionPacketTest()
        {
            var versionCommand = new VersionCommand();

            byte[] packetBytes = versionCommand.GetPacketBytes();

            Assert.AreEqual(0x00, packetBytes[0]);
            Assert.AreEqual(0x00, packetBytes[1]);
            Assert.AreEqual(0x00, packetBytes[2]);
            Assert.AreEqual(0x0b, packetBytes[3]);
            Assert.AreEqual(0x00, packetBytes[4]);
            Assert.AreEqual(0x00, packetBytes[5]);
            Assert.AreEqual(0x00, packetBytes[6]);
            Assert.AreEqual(0x01, packetBytes[7]);
            Assert.AreEqual(0x00, packetBytes[8]);
            Assert.AreEqual(0x01, packetBytes[9]);
            Assert.AreEqual(0x01, packetBytes[10]);
        }
    }
}
