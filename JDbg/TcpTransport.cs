// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JDbg
{
    /// <summary>
    /// Delegate to call when a packet arrives
    /// </summary>
    /// <param name="packet">[Required] bytes of the packet</param>
    internal delegate void OnPacket(byte[] packet);

    /// <summary>
    /// Delegate to call when the target disconnects on a Socket error occurres
    /// </summary>
    /// <param name="exception">[Optional] socket exception that caused the disconnect</param>
    internal delegate void OnDisconnect(SocketException exception);

    /// <summary>
    /// Transport built on TCP to be used by Jdwp 
    /// </summary>
    internal class TcpTransport
    {
        private OnPacket _onPacket;
        private OnDisconnect _onDisconnect;
        private TcpClient _client;
        //NetworkStream m_stream;
        private Thread _thread;

        private bool _bQuit;

        public TcpTransport(string hostname, int port, OnPacket onPacket, OnDisconnect onDisconnect)
        {
            _bQuit = false;

            _client = new TcpClient();
            _client.NoDelay = true;
            _client.ReceiveBufferSize = 2048;
            _client.SendBufferSize = 2048;
            _client.ReceiveTimeout = 0;
            _client.SendTimeout = 30000;
            _client.LingerState = new LingerOption(true, 30);
            _client.Connect(hostname, port);
            //m_stream = m_client.GetStream();

            _onPacket = onPacket;
            _onDisconnect = onDisconnect;

            DoHandShake();

            StartThread("JDbg.TcpTransport");
        }

        private void StartThread(string name)
        {
            _thread = new Thread(TransportLoop);
            _thread.Name = name;
            _thread.Start();
        }

        private void DoHandShake()
        {
            string handShakeString = "JDWP-Handshake";
            byte[] handShakeStringBytes = Encoding.UTF8.GetBytes(handShakeString);

            Send(handShakeStringBytes);

            try
            {
                byte[] handShakeReply = new byte[handShakeStringBytes.Length];
                int bytesReceived = _client.Client.Receive(handShakeReply);
                string reply = Encoding.UTF8.GetString(handShakeReply);
                if (bytesReceived < handShakeStringBytes.Length || string.Compare(reply, handShakeString, StringComparison.Ordinal) != 0)
                {
                    if (bytesReceived == 0)
                    {
                        throw new JdwpException(ErrorCode.VMUnavailable, "VM is not accepting connections from the debugger");
                    }

                    throw new JdwpException(ErrorCode.InvalidResponse, "Invalid response to connect message");
                }
            }
            catch (SocketException e)
            {
                throw new JdwpException(ErrorCode.SocketError, "Handshake failed due to SocketException", e);
            }
        }

        //public void Send(byte[] packet)
        //{
        //    m_client.Client.Send(packet);
        //}

        private void TransportLoop()
        {
            SocketException disconnectException = null;

            try
            {
                while (!_bQuit)
                {
                    //the first four bytes will be the size of the whole packet
                    byte[] packetSizeBytes = new byte[4];

                    if (!TryReceive(packetSizeBytes) || _bQuit)
                    {
                        break;
                    }

                    uint packetSize = Utils.UInt32FromBigEndianBytes(packetSizeBytes);
                    if (packetSize < JdwpCommand.HEADER_SIZE)
                    {
                        Debug.Fail("How did we read 4 bytes that don't give us a size larger than the packet header?");
                        continue;
                    }

                    //the remainder of the packet is the size minus 4 (since we already read the size)
                    byte[] packetBytes = new byte[packetSize];
                    Array.Copy(packetSizeBytes, 0, packetBytes, 0, 4);

                    int remainingPacketByteCount = (int)packetSize - 4;

                    if (!TryReceive(packetBytes, 4, remainingPacketByteCount) || _bQuit)
                    {
                        break;
                    }

                    _onPacket(packetBytes);
                }
            }
            catch (SocketException e)
            {
                disconnectException = e;
            }

            if (!_bQuit)
            {
                _onDisconnect(disconnectException);
            }
        }

        public void Send(byte[] buffer)
        {
            try
            {
                int bytesSent = _client.Client.Send(buffer);
                if (bytesSent != buffer.Length)
                {
                    throw new JdwpException(ErrorCode.SendFailure, "Failed to send bytes.");
                }
            }
            catch (SocketException e)
            {
                throw new JdwpException(ErrorCode.SendFailure, "Failed to send bytes.", e);
            }
        }

        private bool TryReceive(byte[] buffer)
        {
            return TryReceive(buffer, 0, buffer.Length);
        }

        private bool TryReceive(byte[] buffer, int offset, int size)
        {
            int bytesReceived = 0;
            int sizeRemaining = size;

            while (bytesReceived < size)
            {
                int newBytes = _client.Client.Receive(buffer, offset, sizeRemaining, SocketFlags.None);

                if (newBytes == 0)
                {
                    return false;
                }

                bytesReceived += newBytes;
                offset += newBytes;
                sizeRemaining -= newBytes;
            }

            return true;
        }

        public void Close()
        {
            _bQuit = true;

            if (_client != null)
            {
                _client.Close();
            }
        }
    }
}
