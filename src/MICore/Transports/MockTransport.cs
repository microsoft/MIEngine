// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace MICore
{
    //
    // MockTransport
    // Reads a text file and makes it look like a transport
    // For lines that start with "-" then it waits until someone Sends a command, which should match
    // (gdb) lines are ignored
    // For all other lines they are sent to the callback

    public class MockTransport : ITransport
    {
        private ITransportCallback _callback;
        private Thread _thread;
        private string _nextCommand;
        private bool _bQuit;
        private TextReader _reader;
        private AutoResetEvent _commandEvent;
        private string _filename;
        private int _lineNumber;

        public MockTransport(string logfilename)
        {
            _filename = logfilename;
        }

        public void Init(ITransportCallback transportCallback, LaunchOptions options)
        {
            _bQuit = false;
            _callback = transportCallback;
            _commandEvent = new AutoResetEvent(false);
            _reader = new StreamReader(File.OpenRead(_filename));
            _thread = new Thread(TransportLoop);
            _nextCommand = null;
            _thread.Start();
        }

        public void Send(string cmd)
        {
            Debug.Assert(_nextCommand == null);
            _nextCommand = cmd;
            _commandEvent.Set();
        }

        public void Close()
        {
            _bQuit = true;
            if (_thread != Thread.CurrentThread)
            {
                _thread.Join();
            }
        }

        private void TransportLoop()
        {
            _lineNumber = 0;

            // discard first line
            _reader.ReadLine();
            _lineNumber = 1;

            while (!_bQuit)
            {
                string line = _reader.ReadLine();
                if (line == null)
                {
                    break;
                }
                line = line.TrimEnd();
                _lineNumber++;
                Debug.WriteLine("#{0}:{1}", _lineNumber, line);

                if (line[0] == '-')
                {
                    _commandEvent.WaitOne();               // wait for a command
                    if (line != _nextCommand)
                    {
                        Debug.Assert(false, "Unexpected command sent " + line + " expecting " + _nextCommand);
                        break;
                    }
                    _nextCommand = null;
                }
                else if (!line.StartsWith("-", StringComparison.Ordinal))
                {
                    _callback.OnStdOutLine(line);
                }
            }
        }
    }
}
