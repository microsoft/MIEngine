// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication9
{
    internal class DebugListenerTool
    {
        private const int RemotePort = 3030;
        private const int LldbProxyPort = 3002;
        private const string MachineName = "Chucks-mac-mini";

        private static void Main(string[] args)
        {
            Task.Run(() =>
            {
                var handler = new WebRequestHandler();
                handler.ServerCertificateValidationCallback = (a, b, c, d) =>
                {
                    return true;
                };

                using (var client = new TcpClient())
                {
                    client.Connect(MachineName, LldbProxyPort);

                    Console.WriteLine("Connection made to debug proxy server.");

                    bool bRunning = true;

                    ManualResetEvent e = new ManualResetEvent(false);

                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        using (var reader = new StreamReader(client.GetStream()))
                        {
                            while (bRunning)
                            {
                                try
                                {
                                    string line = reader.ReadLine();
                                    if (line != null)
                                    {
                                        Console.WriteLine(line);
                                    }
                                }
                                catch (IOException)
                                {
                                }
                            }
                        }

                        e.Set();
                    });

                    using (var writer = new StreamWriter(client.GetStream()))
                    {
                        while (bRunning)
                        {
                            string line = Console.ReadLine();
                            writer.WriteLine(line);
                            writer.Flush();

                            if (string.Compare(line, "quit", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                bRunning = false;
                            }
                        }
                    }

                    e.WaitOne();
                }
            }).Wait();
        }
    }
}
