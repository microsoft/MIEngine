// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using JDbg;
using Xunit;

namespace JDbgUnitTests
{
    public class JdwpTests
    {
        [Fact]
        public void VersionPacketTest()
        {
            var versionCommand = new VersionCommand();

            byte[] packetBytes = versionCommand.GetPacketBytes();

            Assert.Equal(0x00, packetBytes[0]);
            Assert.Equal(0x00, packetBytes[1]);
            Assert.Equal(0x00, packetBytes[2]);
            Assert.Equal(0x0b, packetBytes[3]);
            Assert.Equal(0x00, packetBytes[4]);
            Assert.Equal(0x00, packetBytes[5]);
            Assert.Equal(0x00, packetBytes[6]);
            Assert.Equal(0x01, packetBytes[7]);
            Assert.Equal(0x00, packetBytes[8]);
            Assert.Equal(0x01, packetBytes[9]);
            Assert.Equal(0x01, packetBytes[10]);
        }
    }
}
