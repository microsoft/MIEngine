// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DebugEngineHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MICore
{
    public abstract class StreamTransport : ITransport
    {
        private ITransportCallback _callback;
        private Thread _thread;
        private bool _bQuit;
        private CancellationTokenSource _streamReadCancellationTokenSource = new CancellationTokenSource();
        protected StreamReader _reader;
        protected StreamWriter _writer;
        private bool _filterStdout;
        private Object _locker = new object();

        protected Logger Logger
        {
            get; private set;
        }

        protected StreamTransport()
        { }

        protected StreamTransport(bool filterStdout)
        {
            _filterStdout = filterStdout;
        }

        public abstract void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer);
        protected virtual string GetThreadName() { return "MI.StreamTransport"; }

        public virtual void Init(ITransportCallback transportCallback, LaunchOptions options, Logger logger, HostWaitLoop waitLoop = null)
        {
            Logger = logger;
            _callback = transportCallback;
            InitStreams(options, out _reader, out _writer);
            StartThread(GetThreadName());
        }

        private void StartThread(string name)
        {
            _thread = new Thread(TransportLoop);
            _thread.Name = name;
            _thread.Start();
        }

        protected virtual string FilterLine(string line)
        {
            return line;
        }

        private void TransportLoop()
        {
            try
            {
                while (!_bQuit)
                {
                    string line = GetLine();
                    if (line == null)
                        break;

                    line = line.TrimEnd();
                    Logger?.WriteLine("->" + line);
                    Logger?.Flush();

                    try
                    {
                        if (_filterStdout)
                        {
                            line = FilterLine(line);
                        }
                        if (!String.IsNullOrWhiteSpace(line) && !line.StartsWith("-", StringComparison.Ordinal))
                        {
                            _callback.OnStdOutLine(line);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        Debug.Assert(_bQuit);
                        break;
                    }
                }
                if (!_bQuit)
                {
                    OnReadStreamAborted();
                }
            }
            finally
            {
                lock (_locker)
                {
                    _bQuit = true;
                    _streamReadCancellationTokenSource.Dispose();

                    // If we are shutting down without notice from the debugger (e.g., the terminal
                    // where the debugger was hosted was closed), at this point it's possible that
                    // there is a thread blocked doing a read() syscall.
                    ForceDisposeStreamReader(_reader);

                    try
                    {
                        _writer.Dispose();
                        _writer = null;
                    }
                    catch
                    {
                        // This can fail flush side effects if the debugger goes down. When this happens we don't want
                        // to crash OpenDebugAD7/VS. Stack:
                        //   System.IO.UnixFileStream.WriteNative(Byte[] array, Int32 offset, Int32 count)
                        //   System.IO.UnixFileStream.FlushWriteBuffer()
                        //   System.IO.UnixFileStream.Dispose(Boolean disposing)
                        //   System.IO.FileStream.Dispose(Boolean disposing)
                        //   System.IO.Stream.Close()
                        //   System.IO.StreamWriter.Dispose(Boolean disposing)
                        //   System.IO.TextWriter.Dispose()
                    }
                }
            }
        }

        protected virtual void OnReadStreamAborted()
        {
            try
            {
                _callback.OnDebuggerProcessExit(null);
            }
            catch
            {
                // eat exceptions on this thread so we don't bring down VS
            }
        }
        protected void Echo(string cmd)
        {
            if (!String.IsNullOrWhiteSpace(cmd))
            {
                Logger?.WriteLine("<-" + cmd);
                Logger?.Flush();
            }

            lock (_locker)
            {
                _writer?.WriteLine(cmd);
                _writer?.Flush();
            }
        }

        private string GetLine()
        {
            try
            {
                Task<string> task = _reader.ReadLineAsync();
                task.Wait(_streamReadCancellationTokenSource.Token);
                return task.Result;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            // I have seen the StreamReader throw both an ObjectDisposedException (which makes sense) and a NullReferenceException
            // (which seems like a bug) after it is closed. Since we have no exception back stop here, we are catching all exceptions
            // here (we don't want to crash VS).
            catch
            {
                Debug.Assert(_bQuit, "Exception throw from ReadLine when we haven't quit yet");
                return null;
            }
        }

        public void Send(string cmd)
        {
            Echo(cmd);
        }

        public virtual void Close()
        {
            lock (_locker)
            {
                if (!_bQuit)
                {
                    _bQuit = true;
                    _streamReadCancellationTokenSource.Cancel();
                }
            }
        }

        public bool IsClosed
        {
            get { return _bQuit; }
        }

        public abstract int DebuggerPid { get; }

        protected ITransportCallback Callback
        {
            get { return _callback; }
        }

        /// <summary>
        /// In some scenarios, a StreamReader will be blocked on read() when trying to dispose it.
        /// On OS X under mono, read() will block any close() syscalls on the file descriptor for the stream.
        /// Therefore, we write a byte to unblock the read() and allow close() to succeed.
        /// Callers to this function should document why read() might be blocked.
        /// </summary>
        /// <param name="reader">The StreamReader to forcibly dispose</param>
        protected static void ForceDisposeStreamReader(StreamReader reader)
        {
            try
            {
                reader?.BaseStream.WriteByte(0);
            }
            catch
            {
            }

            reader?.Dispose();
        }

        public abstract int ExecuteSyncCommand(string commandDescription, string commandText, int timeout, out string output, out string error);
        public abstract bool CanExecuteCommand();
    }
}
