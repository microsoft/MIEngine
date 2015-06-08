// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JDbg
{
    /// <summary>
    /// Wrapper for JDWP reply packets. Automatically parses reply headers and exposes several
    /// helpful utilities for decoding reply payloads.
    /// </summary>
    internal class ReplyPacketParser
    {
        private BinaryReader _packetReader;

        private JdwpCommand.IDSizes _IDSizes;

        public uint Size { get; private set; }
        public uint Id { get; private set; }
        public UInt16 ErrorCode { get; private set; }
        public bool Succeeded { get; private set; }

        /// <summary>
        /// Construct a new ReplyPacketParser from the raw bytes of the reply packet. After the ReplyPacketParser is constrcuted, 
        /// the header has already been decoded, the position of the underlying byte stream is at the beginning of the payload.
        /// Use the methods on this class to read bytes from the payload as structured data.
        /// </summary>
        /// <param name="replyBytes"></param>
        public ReplyPacketParser(byte[] replyBytes, JdwpCommand.IDSizes idSizes)
        {
            _packetReader = new BinaryReader(new MemoryStream(replyBytes));
            _IDSizes = idSizes;

            Size = this.ReadUInt32();
            Id = this.ReadUInt32();
            this.ReadByte(); //flags byte
            ErrorCode = this.ReadUInt16();

            if (ErrorCode == 0)
            {
                Succeeded = true;
            }
            else
            {
                Succeeded = false;
            }
        }

        /// <summary>
        /// Reads the next four bytes in the payload as a UInt32
        /// </summary>
        /// <returns></returns>
        public UInt32 ReadUInt32()
        {
            byte[] bytes = _packetReader.ReadBytes(4);
            return Utils.UInt32FromBigEndianBytes(bytes);
        }

        /// <summary>
        /// Reads the next two bytes in the payload as a UInt16
        /// </summary>
        /// <returns></returns>
        public UInt16 ReadUInt16()
        {
            byte[] bytes = _packetReader.ReadBytes(2);
            return Utils.UInt16FromBigEndianBytes(bytes);
        }

        /// <summary>
        /// Reads the next byte from the payload
        /// </summary>
        /// <returns></returns>
        public byte ReadByte()
        {
            return _packetReader.ReadByte();
        }

        /// <summary>
        /// Reads the next bytes of the payload as a string.
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            UInt32 size = ReadUInt32();
            byte[] stringBytes = _packetReader.ReadBytes((int)size);

            return Encoding.UTF8.GetString(stringBytes);
        }

        /// <summary>
        /// Reads the next bytes of the payload os a ReferenceTypeID
        /// </summary>
        /// <returns></returns>
        public ulong ReadReferenceTypeID()
        {
            byte[] bytes = _packetReader.ReadBytes(_IDSizes.ReferenceTypeIDSize);
            return Utils.ULongFromBigEndiantBytes(bytes);
        }
    }
}
