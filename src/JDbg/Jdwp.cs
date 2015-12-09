// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace JDbg
{
    /// <summary>
    /// Implemtation of a JDWP infrastructure. Facilitates the sending and receiving of jdwp packets on an underling tranposrt.
    /// 
    /// NOTE: Current transport is hardcoded to TCP. This could be interface'd in the future to support other transports.
    /// </summary>
    internal class Jdwp
    {
        private TcpTransport _transport;
        private JdwpCommand.IDSizes _IDSizes;

        private Jdwp(string hostname, int port)
        {
            _transport = new TcpTransport(hostname, port, OnPacketReceived, OnDisconnect);
        }

        /// <summary>
        /// Attach to hostname:port for jdwp communication.
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static Jdwp Attach(string hostname, int port)
        {
            Jdwp jdbg = new Jdwp(hostname, port);
            return jdbg;
        }

        private void OnPacketReceived(byte[] packet)
        {
            Debug.Assert(packet.Length >= JdwpCommand.HEADER_SIZE);

            if (packet.Length < JdwpCommand.HEADER_SIZE)
            {
                throw new ArgumentException("buffer too small to be a full packet", "packet");
            }

            //byte 8 of the packet header tells us whether this is a reply or not
            if ((packet[8] & 0x80) > 0)
            {
                OnReplyPacketReceived(packet);
            }
            else
            {
                OnCommandPacketReceived(packet);
            }
        }

        private void OnDisconnect(/*OPTIONAL*/ SocketException socketException)
        {
            // This handles disconnects if, for example, the app exits

            // If we have any waiting operations, we want to abort them
            List<WaitingOperationDescriptor> operationsToAbort = null;
            lock (_waitingOperations)
            {
                operationsToAbort = new List<WaitingOperationDescriptor>(_waitingOperations.Values);
                _waitingOperations.Clear();
            }
            foreach(var operation in operationsToAbort)
            {
                if (socketException == null)
                {
                    operation.Abort();
                }
                else
                {
                    operation.OnSocketError(socketException);
                }
            }

            // Otherwise just drop the connection
        }

        private void OnReplyPacketReceived(byte[] packetBytes)
        {
            ReplyPacketParser packet = new ReplyPacketParser(packetBytes, _IDSizes);
            uint id = packet.Id;

            WaitingOperationDescriptor waitingOperation = null;
            lock (_waitingOperations)
            {
                if (_waitingOperations.TryGetValue(id, out waitingOperation))
                {
                    _waitingOperations.Remove(id);
                }
            }
            if (waitingOperation != null)
            {
                try
                {
                    waitingOperation.OnComplete(packet);
                }
                catch (JdwpException e)
                {
                    waitingOperation.OnJdwpException(e);
                }
            }
            else
            {
                Debug.Fail("How did we get a reply packet that we don't have a waiting operation for?");
            }
        }

        private void OnCommandPacketReceived(byte[] packet)
        {
            //TODO This is where we would handle commands sent from the VM to the debugger. 
            //The most common case of the VM sending command packets to the debugger is for Events.
        }

        /// <summary>
        /// Send a command packet to the VM
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public Task SendCommandAsync(JdwpCommand command)
        {
            var waitingOperation = new WaitingOperationDescriptor(command);

            lock (_waitingOperations)
            {
                _waitingOperations.Add(command.PacketId, waitingOperation);
            }

            SendToTransport(command);

            return waitingOperation.Task;
        }

        private class WaitingOperationDescriptor
        {
            private readonly JdwpCommand _command;
            private readonly TaskCompletionSource<object> _completionSource = new TaskCompletionSource<object>();

            public WaitingOperationDescriptor(JdwpCommand command)
            {
                _command = command;
            }

            internal void OnComplete(ReplyPacketParser replyPacket)
            {
                if (replyPacket.Succeeded)
                {
                    _command.DecodeSuccessReply(replyPacket);
                    _completionSource.SetResult(null);
                }
                else
                {
                    _command.DecodeFailureReply(replyPacket);
                    _completionSource.SetException(new JdwpException(ErrorCode.CommandFailure, string.Format(CultureInfo.CurrentCulture, "JDWP Error. Id: {0} Error Code: {1}", replyPacket.Id, replyPacket.ErrorCode)));
                }
            }

            public Task<object> Task { get { return _completionSource.Task; } }

            internal void Abort()
            {
                _completionSource.TrySetException(new OperationCanceledException());
            }

            internal void OnSocketError(SocketException socketException)
            {
                _completionSource.TrySetException(new JdwpException(ErrorCode.SocketError, "Socket error reading the result", socketException));
            }

            internal void OnJdwpException(JdwpException jdwpException)
            {
                _completionSource.SetException(jdwpException);
            }
        }

        private readonly Dictionary<uint, WaitingOperationDescriptor> _waitingOperations = new Dictionary<uint, WaitingOperationDescriptor>();

        private void SendToTransport(JdwpCommand command)
        {
            _transport.Send(command.GetPacketBytes());
        }

        public void Close()
        {
            if (_transport != null)
            {
                _transport.Close();
            }

            //Abort remaining WaitingOperations
            List<WaitingOperationDescriptor> operationsToAbort = null;
            lock (_waitingOperations)
            {
                operationsToAbort = new List<WaitingOperationDescriptor>(_waitingOperations.Values);
                _waitingOperations.Clear();
            }
            foreach(var operation in operationsToAbort)
            {
                operation.Abort();
            }
        }

        public void SetIDSizes(JdwpCommand.IDSizes idSizes)
        {
            _IDSizes = idSizes;
        }
    }
}
