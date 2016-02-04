// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        protected StreamReader _reader;
        protected StreamWriter _writer;
        bool _filterStdout;

        protected StreamTransport()
        { }

        protected StreamTransport(bool filterStdout)
        {
            _filterStdout = filterStdout;
        }

        public abstract void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer);
        protected virtual string GetThreadName() { return "MI.StreamTransport"; }

        public virtual void Init(ITransportCallback transportCallback, LaunchOptions options)
        {
            _callback = transportCallback;
            InitStreams(options, out _reader, out _writer);
            StartThread(GetThreadName());
        }

        public void StartThread(string name)
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
            string line;

            while (!_bQuit)
            {
                line = GetLine();
                if (line == null)
                    break;

                line = line.TrimEnd();
                Logger.WriteLine("->" + line);

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
            Logger.WriteLine("<-" + cmd);
            _writer.WriteLine(cmd);
            _writer.Flush();
        }

        private string GetLine()
        {
            try
            {
                return _reader.ReadLine();
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
            _bQuit = true;
            if (_reader != null)
            {
                _reader.Dispose(); // close the stream. This usually, but not always, causes the OS to give back our reader thread.
                _reader = null;
            }
        }

        public bool IsClosed
        {
            get { return _bQuit; }
        }

        protected ITransportCallback Callback
        {
            get { return _callback; }
        }
    }
}

