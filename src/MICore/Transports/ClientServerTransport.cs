// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;
using System.Collections;
using System.Text.RegularExpressions;

namespace MICore
{
    // Manage both the client and server transports for a debugging session
    public class ClientServerTransport : ITransport
    {
        private ISignalingTransport _serverTransport;
        private ITransport _clientTransport;
        private int _launchTimeout;

        public ClientServerTransport(ITransport clientTransport, ISignalingTransport serverTransport)
        {
            _clientTransport = clientTransport;
            _serverTransport = serverTransport;
        }

        public void Init(ITransportCallback transportCallback, LaunchOptions options)
        {
            _launchTimeout = ((LocalLaunchOptions)options).ServerLaunchTimeout;
            _serverTransport.Init(transportCallback, options);
            WaitForStart();
            if (!_clientTransport.IsClosed)
            {
                _clientTransport.Init(transportCallback, options);
            }
        }

        private void WaitForStart()
        {
            if (!_serverTransport.StartedEvent.WaitOne(_launchTimeout))  // wait for the server to start
            {
                _serverTransport.Close();
                throw new TimeoutException(MICoreResources.Error_DebugServerInitializationFailed);
            }
        }
        public void Close()
        {
            _clientTransport.Close();
            _serverTransport.Close();
        }

        public bool IsClosed { get { return _clientTransport.IsClosed; } }

        public void Send(string cmd)
        {
            _clientTransport.Send(cmd);
        }
    }
}
