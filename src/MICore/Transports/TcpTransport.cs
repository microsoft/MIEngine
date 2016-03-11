// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MICore
{
    public class TcpTransport : StreamTransport
    {
        private TcpClient _client;

        public TcpTransport()
        {
        }

        protected override string GetThreadName()
        {
            return "MI.TcpTransport";
        }

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            TcpLaunchOptions tcpOptions = (TcpLaunchOptions)options;

            _client = new TcpClient();
            _client.Connect(tcpOptions.Hostname, tcpOptions.Port);

            if (tcpOptions.Secure)
            {
                RemoteCertificateValidationCallback callback;

                if (tcpOptions.ServerCertificateValidationCallback == null)
                {
                    //if no callback specified, accept any certificate
                    callback = delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                    {
                        return sslPolicyErrors == SslPolicyErrors.None;
                    };
                }
                else
                {
                    //else use the callback specified
                    callback = tcpOptions.ServerCertificateValidationCallback;
                }

                var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                certStore.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                SslStream sslStream = new SslStream(
                    _client.GetStream(),
                    false /* leaveInnerStreamOpen */,
                    callback,
                    null /*UserCertificateSelectionCallback */
                    );

                sslStream.AuthenticateAsClient(tcpOptions.Hostname, certStore.Certificates, System.Security.Authentication.SslProtocols.Tls, false /* checkCertificateRevocation */);
                reader = new StreamReader(sslStream);
                writer = new StreamWriter(sslStream);
            }
            else
            {
                reader = new StreamReader(_client.GetStream());
                writer = new StreamWriter(_client.GetStream());
            }
        }

        public override void Close()
        {
            base.Close();
            ((IDisposable)_client).Dispose();
        }
    }
}
