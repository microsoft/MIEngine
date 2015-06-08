// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using JDbg;
using Xunit;

namespace JDbgUnitTests
{
    public class UtilsTests
    {
        [Fact]
        public void UInt32FromBigEndianBytesTest()
        {
            uint num = Utils.UInt32FromBigEndianBytes(new byte[4] { 0xAA, 0xBB, 0xCC, 0xDD });
            Assert.Equal(0xAABBCCDD, num);
        }

        [Fact]
        public void BigEndianBytesFromUInt32Test()
        {
            byte[] bytes = Utils.BigEndianBytesFromUInt32(0xAABBCCDD);
            Assert.Equal(4, bytes.Length);
            Assert.Equal(0xAA, bytes[0]);
            Assert.Equal(0xBB, bytes[1]);
            Assert.Equal(0xCC, bytes[2]);
            Assert.Equal(0xDD, bytes[3]);
        }

        [Fact]
        public void BigEndianBytesFromUInt16Test()
        {
            UInt16 num = Utils.UInt16FromBigEndianBytes(new byte[2] { 0xAA, 0xBB });
            Assert.Equal(0xAABB, num);
        }

        [Fact]
        public void ULongFromBigEndianBytesTest()
        {
            ulong num = Utils.ULongFromBigEndiantBytes(new byte[2] { 0xAA, 0xBB });
            Assert.Equal(0xAABBul, num);

            num = Utils.ULongFromBigEndiantBytes(new byte[8] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAB, 0xCD });
            Assert.Equal(0xAABBCCDDEEFFABCD, num);
        }
    }
}
