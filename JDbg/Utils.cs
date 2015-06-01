// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JDbg
{
    /// <summary>
    /// Utilities for converting Big-Endian to Little-Endian
    /// </summary>
    public static class Utils
    {
        public static UInt32 UInt32FromBigEndianBytes(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 4);

            int networkOder = BitConverter.ToInt32(bytes, 0);
            return (uint)IPAddress.NetworkToHostOrder(networkOder);
        }

        public static byte[] BigEndianBytesFromUInt32(UInt32 num)
        {
            int networkOrder = IPAddress.HostToNetworkOrder((int)num);
            return BitConverter.GetBytes(networkOrder);
        }

        public static ushort UInt16FromBigEndianBytes(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 2);

            Int16 networkOrder = BitConverter.ToInt16(bytes, 0);
            return (UInt16)IPAddress.NetworkToHostOrder(networkOrder);
        }

        /// <summary>
        /// This method returns a ulong for a variable length array of big endian bytes.
        /// </summary>
        /// <param name="bytes">Big endian bytes to convert</param>
        /// <returns></returns>
        public static ulong ULongFromBigEndiantBytes(byte[] bytes)
        {
            Debug.Assert(bytes.Length <= 8);

            byte[] ulongBytes = { 0, 0, 0, 0, 0, 0, 0, 0 };

            for (int i = 0; i < bytes.Length && i < 8; i++)
            {
                ulongBytes[bytes.Length - 1 - i] = bytes[i];
            }

            return BitConverter.ToUInt64(ulongBytes, 0);
        }
    }
}
