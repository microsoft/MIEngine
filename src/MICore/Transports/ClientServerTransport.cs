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
    public class ClientServerTransport : LocalTransport
    {
        private ServerTransport _serverTransport;
        private ManualResetEvent _event;
        private int _launchTimeout;

        public static bool ShouldStartServer(LaunchOptions options)
        {
            return !string.IsNullOrWhiteSpace((options as LocalLaunchOptions)?.DebugServer);
        }

        public ClientServerTransport(bool filterStdout, bool filterStderr)
        {
            _event = new ManualResetEvent(false);
            _serverTransport = new ServerTransport(_event, killOnClose:true, filterStderr: filterStderr, filterStdout: filterStdout);
        }

        public override void Init(ITransportCallback transportCallback, LaunchOptions options)
        {
            _launchTimeout = ((LocalLaunchOptions)options).ServerLaunchTimeout;
            _serverTransport.Init(transportCallback, options);
            WaitForStart();
            if (!IsClosed)
            {
                base.Init(transportCallback, options);
            }
        }

        private void WaitForStart()
        {
            if (!_event.WaitOne(_launchTimeout))  // wait for the server to start
            {
                _serverTransport.Close();
                throw new TimeoutException(MICoreResources.Error_DebugServerInitializationFailed);
            }
        }
        public override void Close()
        {
            base.Close();
            _serverTransport.Close();
        }
    }
}
