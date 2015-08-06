// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO.Ports;

namespace MICore
{
    public class SerialTransport : ITransport
    {
        private ITransportCallback _callback;
        private Thread _thread;
        private bool _bQuit;
        private SerialPort _port;
        private string _lastCommand;

        public SerialTransport(string portname)
        {
            _port = new SerialPort(portname);
            _port.BaudRate = 115200;
            _port.DataBits = 8;
            _port.Handshake = Handshake.None;
            _port.Parity = Parity.None;
            _port.StopBits = StopBits.One;
            _port.NewLine = "\r";                // Sometimes its \r\n an sometimes \n\r - doh!
            _port.ReadTimeout = 1 * 1000;       // 1 second timeout
            _lastCommand = null;
        }

        public void Init(ITransportCallback transportCallback, LaunchOptions options)
        {
            SerialLaunchOptions serialOptions = (SerialLaunchOptions)options;

            string line;
            bool bLoggedIn = false;
            _bQuit = false;
            _callback = transportCallback;
            _thread = new Thread(TransportLoop);
            _port.Open();

            Echo("");

            for (;;)
            {
                line = GetLine();
                if (line != null)
                {
                    if (line.EndsWith("login: ", StringComparison.Ordinal) && !bLoggedIn)
                    {
                        Echo("root");
                        Debug.WriteLine("DBG:Logged into device");
                        bLoggedIn = true;
                    }
                    else if (line.EndsWith("$ ", StringComparison.Ordinal))
                    {
                        Debug.WriteLine("DBG:Command prompt detected");
                        break;
                    }
                }
            }

            Debug.WriteLine("DBG:GDB is starting");
            _thread.Start();
        }

        private void EchoAndWait(string cmd)
        {
            Echo(cmd);
            Thread.Sleep(333);             // give it 1/3 of a second to do something
            for (;;)
            {
                string line = GetLine();
                if (line != null && line.EndsWith(cmd, StringComparison.Ordinal))
                    break;
            }
        }

        public void Send(string cmd)
        {
            _lastCommand = cmd;
            Echo(cmd);
        }

        public void Close()
        {
            _bQuit = true;
            if (_thread != Thread.CurrentThread)
            {
                _thread.Join();            // wait for the worker thread to exit
            }
        }

        private void TransportLoop()
        {
            string line;

            while (!_bQuit)
            {
                line = GetLine();
                if (line != null)
                {
                    if (line == _lastCommand)
                    {
                        // Commands get echoed back, so ignore them
                        continue;
                    }
                    _lastCommand = null;
                    line = line.TrimEnd();
                    _callback.OnStdOutLine(line);
                }
            }
            _port.Close();
            _port = null;
        }

        private void Echo(string cmd)
        {
            Debug.WriteLine("->" + cmd + "<-");
            _port.WriteLine(cmd);
        }

        private string GetLine()
        {
            // read chars until we get a Newline, or until we get a long delay
            StringBuilder sb = new StringBuilder(300);
            for (;;)
            {
                try
                {
                    char c = (char)_port.ReadChar();
                    if (c == _port.NewLine[0])
                        break;
                    sb.Append(c);
                }
                catch (System.TimeoutException)
                {
                    break;
                }
            }
            if (sb.Length == 0)
            {
                return null;
            }
            else
            {
                string line = sb.ToString();
                line = line.Trim('\n');
                return line;
            }
        }
    }
}
