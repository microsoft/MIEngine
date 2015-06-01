// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JDbg;

namespace JDbgUnitTests
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void UInt32FromBigEndianBytesTest()
        {
            uint num = Utils.UInt32FromBigEndianBytes(new byte[4] { 0xAA, 0xBB, 0xCC, 0xDD });
            Assert.AreEqual(0xAABBCCDD, num);
        }

        [TestMethod]
        public void BigEndianBytesFromUInt32Test()
        {
            byte[] bytes = Utils.BigEndianBytesFromUInt32(0xAABBCCDD);
            Assert.AreEqual(4, bytes.Length);
            Assert.AreEqual(0xAA, bytes[0]);
            Assert.AreEqual(0xBB, bytes[1]);
            Assert.AreEqual(0xCC, bytes[2]);
            Assert.AreEqual(0xDD, bytes[3]);
        }

        [TestMethod]
        public void BigEndianBytesFromUInt16Test()
        {
            UInt16 num = Utils.UInt16FromBigEndianBytes(new byte[2] { 0xAA, 0xBB });
            Assert.AreEqual(0xAABB, num);
        }

        [TestMethod]
        public void ULongFromBigEndianBytesTest()
        {
            ulong num = Utils.ULongFromBigEndiantBytes(new byte[2] { 0xAA, 0xBB });
            Assert.AreEqual(0xAABBul, num);

            num = Utils.ULongFromBigEndiantBytes(new byte[8] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAB, 0xCD });
            Assert.AreEqual(0xAABBCCDDEEFFABCD, num);
        }
    }
}
