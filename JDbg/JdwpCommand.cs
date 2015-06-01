// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JDbg
{
    /// <summary>
    /// This is the base clas for all Jdwp Commands. Commands that have payloads should override 
    /// GetPayloadBytes. Commands that expect payloads in succesful replies should override DecodeSuccessReply.
    /// Commands that expect payloads in error replies should override DecodeErrorReply.
    /// </summary>
    public class JdwpCommand
    {
        //This class represents a two byte tuple made up of the Command Set ID and the Command ID 
        protected class CommandId : Tuple<byte, byte>
        {
            public byte CommandSet { get { return this.Item1; } }

            public byte Command { get { return this.Item2; } }

            public CommandId(byte commandSet, byte command)
                : base(commandSet, command)
            {
            }
        }

        public struct IDSizes
        {
            public IDSizes(int fieldIDSize, int methodIDSize, int objectIDSize, int referenceTypeIDSize, int frameIDSize)
            {
                _fieldIDSize = fieldIDSize;
                _methodIDSize = methodIDSize;
                _objectIDSize = objectIDSize;
                _referenceTypeIDSize = referenceTypeIDSize;
                _frameIDSize = frameIDSize;
            }

            private int _fieldIDSize;
            public int FieldIDSize
            {
                get
                {
                    if (_fieldIDSize == 0)
                    {
                        throw new JdwpException(ErrorCode.FailedToInitialize, "IDSizes was not intialized!");
                    }
                    return _fieldIDSize;
                }
            }

            private int _methodIDSize;
            public int MethodIDSize
            {
                get
                {
                    if (_methodIDSize == 0)
                    {
                        throw new JdwpException(ErrorCode.FailedToInitialize, "IDSizes was not intialized!");
                    }
                    return _methodIDSize;
                }
            }

            private int _objectIDSize;
            public int ObjectIDSize
            {
                get
                {
                    if (_objectIDSize == 0)
                    {
                        throw new JdwpException(ErrorCode.FailedToInitialize, "IDSizes was not intialized!");
                    }
                    return _objectIDSize;
                }
            }

            private int _referenceTypeIDSize;
            public int ReferenceTypeIDSize
            {
                get
                {
                    if (_referenceTypeIDSize == 0)
                    {
                        throw new JdwpException(ErrorCode.FailedToInitialize, "IDSizes was not intialized!");
                    }
                    return _referenceTypeIDSize;
                }
            }

            private int _frameIDSize;
            public int FrameIDSize
            {
                get
                {
                    if (_frameIDSize == 0)
                    {
                        throw new JdwpException(ErrorCode.FailedToInitialize, "IDSizes was not intialized!");
                    }
                    return _frameIDSize;
                }
            }
        }

        /// <summary>
        /// Class to hold command id tuples for the VirtualMachine command set (1). 
        /// See http://docs.oracle.com/javase/7/docs/technotes/guides/jpda/jdwp-spec.html for more info.
        /// </summary>
        protected class VirtualMachineCommandSet : CommandId
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet Version = new VirtualMachineCommandSet(1);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet ClassesBySignature = new VirtualMachineCommandSet(2);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet AllClasses = new VirtualMachineCommandSet(3);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet AllThreads = new VirtualMachineCommandSet(4);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet TopLevelThreadGroups = new VirtualMachineCommandSet(5);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet Dispose = new VirtualMachineCommandSet(6);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet IDSizes = new VirtualMachineCommandSet(7);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet Suspend = new VirtualMachineCommandSet(8);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet Resume = new VirtualMachineCommandSet(9);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet Exit = new VirtualMachineCommandSet(10);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet CreateString = new VirtualMachineCommandSet(11);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet Capabilities = new VirtualMachineCommandSet(12);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet ClassPaths = new VirtualMachineCommandSet(13);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet DisposeObjects = new VirtualMachineCommandSet(14);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet HoldEvents = new VirtualMachineCommandSet(15);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet ReleaseEvents = new VirtualMachineCommandSet(16);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet CapabilitiesNew = new VirtualMachineCommandSet(17);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet RedefineClasses = new VirtualMachineCommandSet(18);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet SetDefaultStratum = new VirtualMachineCommandSet(19);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet AllClassesWithGeneric = new VirtualMachineCommandSet(20);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly VirtualMachineCommandSet InstanceCounts = new VirtualMachineCommandSet(21);
            public VirtualMachineCommandSet(byte command) :
                base(1, command)
            {
            }
        }

        protected class EventRequestCommandSet : CommandId
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly EventRequestCommandSet Set = new EventRequestCommandSet(1);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly EventRequestCommandSet Clear = new EventRequestCommandSet(2);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly EventRequestCommandSet ClearAllBreakpoints = new EventRequestCommandSet(3);

            public EventRequestCommandSet(byte command) :
                base(15, command)
            {
            }
        }

        public const uint HEADER_SIZE = 11;

        private static uint s_nextPacketId = 0;

        private CommandId _commandId;
        public uint PacketId { get; private set; }

        protected JdwpCommand(CommandId commandId)
        {
            _commandId = commandId;
            this.PacketId = ++s_nextPacketId;
        }

        //overridden by commands that have paylaods
        protected virtual byte[] GetPayloadBytes()
        {
            return new byte[0];
        }

        //overriden by commands that need to decode reply payloads
        internal virtual void DecodeSuccessReply(ReplyPacketParser bytes) { }
        internal virtual void DecodeFailureReply(ReplyPacketParser bytes) { }

        public byte[] GetPacketBytes()
        {
            byte[] payloadBytes = GetPayloadBytes();
            if (payloadBytes == null)
            {
                payloadBytes = new byte[0];
            }

            uint packetSize = HEADER_SIZE + (uint)payloadBytes.Length;

            byte[] packetBytes = new byte[packetSize];

            //bytes 0-3 are the length, big-endian
            byte[] lenghtBytes = Utils.BigEndianBytesFromUInt32(packetSize);
            Array.Copy(lenghtBytes, 0, packetBytes, 0, 4);

            //bytes 4-7 are the command ID, big-endian
            byte[] idBytes = Utils.BigEndianBytesFromUInt32(PacketId);
            Array.Copy(idBytes, 0, packetBytes, 4, 4);

            //byte 8 is flags, always zero for command packets
            packetBytes[8] = 0;

            //byte 9 is  the command set
            packetBytes[9] = _commandId.CommandSet;

            //byte 10 is the command
            packetBytes[10] = _commandId.Command;

            //remainder of packet is the payload bytes
            Array.Copy(payloadBytes, 0, packetBytes, HEADER_SIZE, payloadBytes.Length);

            return packetBytes;
        }
    }

    /// <summary>
    /// Version Command. Retreives the version of the JVM. No command payload. Reply payload is described by VersionCommand.Reply.
    /// </summary>
    public class VersionCommand : JdwpCommand
    {
        public struct Reply
        {
            public string description;
            public int jdwpMajor;
            public int jdwpMinor;
            public string vmVersion;
            public string vmName;
        }

        private Reply _reply;

        public VersionCommand()
            : base(VirtualMachineCommandSet.Version)
        {
        }

        internal override void DecodeSuccessReply(ReplyPacketParser reply)
        {
            _reply = new Reply();
            _reply.description = reply.ReadString();
            _reply.jdwpMajor = (int)reply.ReadUInt32();
            _reply.jdwpMinor = (int)reply.ReadUInt32();
            _reply.vmVersion = reply.ReadString();
            _reply.vmName = reply.ReadString();
        }

        public Reply GetReply()
        {
            return _reply;
        }
    }

    /// <summary>
    /// Retrieves the sizes of ID's from the JVM
    /// </summary>
    public class IDSizesCommand : JdwpCommand
    {
        private IDSizes _reply;

        public IDSizesCommand()
            : base(VirtualMachineCommandSet.IDSizes)
        {
        }

        internal override void DecodeSuccessReply(ReplyPacketParser bytes)
        {
            int fieldIDSize = (int)bytes.ReadUInt32();
            int methodIDSize = (int)bytes.ReadUInt32();
            int objectIDsize = (int)bytes.ReadUInt32();
            int referenceTypeIDSize = (int)bytes.ReadUInt32();
            int frameIDSize = (int)bytes.ReadUInt32();

            _reply = new IDSizes(fieldIDSize, methodIDSize, objectIDsize, referenceTypeIDSize, frameIDSize);
        }

        public IDSizes GetReply()
        {
            return _reply;
        }
    }

    /// <summary>
    /// Retrieves all class names (with generics) from the JVM
    /// </summary>
    public class AllClassesWithGenericCommand : JdwpCommand
    {
        public struct ClassData
        {
            public byte refTypeFlag;
            public ulong typeID;
            public string signature;
            public string genericSignature;
            public int status;
        }

        public AllClassesWithGenericCommand()
            : base(VirtualMachineCommandSet.AllClassesWithGeneric)
        {
        }

        private List<ClassData> _classData;
        public List<ClassData> GetClassData()
        {
            return _classData;
        }

        internal override void DecodeSuccessReply(ReplyPacketParser bytes)
        {
            int countClasses = (int)bytes.ReadUInt32();

            _classData = new List<ClassData>();
            for (int i = 0; i < countClasses; i++)
            {
                ClassData classData = new ClassData();
                classData.refTypeFlag = bytes.ReadByte();
                classData.typeID = bytes.ReadReferenceTypeID();
                classData.signature = bytes.ReadString();
                classData.genericSignature = bytes.ReadString();
                classData.status = (int)bytes.ReadUInt32();

                _classData.Add(classData);
            }
        }
    }

    /// <summary>
    /// Dispose Command. Closes the communication with the current JVM, equivalent of detach. No command payload or reply payload.
    /// </summary>
    public class DisposeCommand : JdwpCommand
    {
        public DisposeCommand()
            : base(VirtualMachineCommandSet.Dispose)
        {
        }
    }

    /// <summary>
    /// Resume Command. Resumes the JVM after it has been suspended or stopped due to an event. No command payload and no reply payload.
    /// </summary>
    public class ResumeCommand : JdwpCommand
    {
        public ResumeCommand()
            : base(VirtualMachineCommandSet.Resume)
        {
        }
    }
}
