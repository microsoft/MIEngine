// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.DebugEngineHost;
using Microsoft.DebugEngineHost.VSCode;
using OpenDebugAD7;

namespace OpenDebug
{
    internal class Program
    {
        private const int DEFAULT_PORT = 4711;

        private static int Main(string[] argv)
        {
            int port = -1;
            List<LoggingCategory> loggingCategories = new List<LoggingCategory>();

            // parse command line arguments
            foreach (var a in argv)
            {
                if ((a == null) || (a == "undefined"))
                {
                    continue;
                }
                switch (a)
                {
                    case "-h":
                    case "-?":
                    case "/?":
                    case "--help":
                        Console.WriteLine("OpenDebugAD7: Visual Studio Code debug adapter bridge for using Visual Studio");
                        Console.WriteLine("debug engines in VS Code");
                        Console.WriteLine();
                        Console.WriteLine("Available command line arguments:");
                        Console.WriteLine("--trace: print the requests coming from VS Code to the console.");
                        Console.WriteLine("--trace=response: print requests and response from VS Code to the console.");
                        Console.WriteLine("--engineLogging[=filePath]: Enable logging from the debug engine. If not");
                        Console.WriteLine("    specified, the log will go to the console.");
                        Console.WriteLine("--server[=port_num] : Start the debug adapter listening for requests on the");
                        Console.WriteLine("    specified TCP/IP port instead of stdin/out. If port is not specified");
                        Console.WriteLine("    TCP {0} will be used.", DEFAULT_PORT);
                        Console.WriteLine("--pauseForDebugger: Pause the OpenDebugAD7.exe process at startup until a");
                        Console.WriteLine("    debugger attaches.");
                        return 1;

                    case "--trace":
                        loggingCategories.Add(LoggingCategory.AdapterTrace);
                        break;
                    case "--trace=response":
                        loggingCategories.Add(LoggingCategory.AdapterTrace);
                        loggingCategories.Add(LoggingCategory.AdapterResponse);
                        break;
                    case "--engineLogging":
                        loggingCategories.Add(LoggingCategory.EngineLogging);
                        HostLogger.EnableHostLogging();
                        break;
                    case "--server":
                        port = DEFAULT_PORT;
                        break;
                    case "--pauseForDebugger":
                        Console.WriteLine("OpenDebugAD7.exe is waiting for a managed debugger to attach to it.");
                        while (!Debugger.IsAttached)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                        break;
                    default:
                        if (a.StartsWith("--server=", StringComparison.Ordinal))
                        {
                            string portString = a.Substring("--server=".Length);
                            if (!int.TryParse(portString, out port))
                            {
                                Console.Error.WriteLine("OpenDebugAD7: ERROR: Unable to parse port string '{0}'.", portString);
                                return -1;
                            }
                        }
                        else if (a.StartsWith("--engineLogging=", StringComparison.Ordinal))
                        {
                            HostLogger.EnableHostLogging();
                            try
                            {
                                HostLogger.Instance.LogFilePath = a.Substring("--engineLogging=".Length);
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine("OpenDebugAD7: ERROR: Unable to open log file. " + e.Message);
                                return -1;
                            }
                        }
                        else if (a.StartsWith("--adapterDirectory=", StringComparison.Ordinal))
                        {
                            string adapterDirectory = a.Substring("--adapterDirectory=".Length);
                            if (!Directory.Exists(adapterDirectory))
                            {
                                Console.Error.WriteLine("OpenDebugAD7: ERROR: adapter directory '{0}' does not exist.", adapterDirectory);
                                return -1;
                            }
                            EngineConfiguration.SetAdapterDirectory(adapterDirectory);
                        }
                        else
                        {
                            Console.Error.WriteLine("OpenDebugAD7: ERROR: Unknown command line argument '{0}'.", a);
                            return -1;
                        }
                        break;
                }
            }

            if (port > 0)
            {
                // TCP/IP server
                RunServer(port, loggingCategories);
            }

            try
            {
                // stdin/stdout
                Console.Error.WriteLine("waiting for v8 protocol on stdin/stdout");
                if (Utilities.IsWindows())
                {
                    // Avoid sending the BOM on Windows if the Beta Unicode feature is enabled in Windows 10
                    Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                    Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                }
                Dispatch(Console.OpenStandardInput(), Console.OpenStandardOutput(), loggingCategories);
            }
            catch (Exception e)
            {
                Utilities.ReportException(e);
            }

            return 0;
        }

        private static async void RunServer(int port, List<LoggingCategory> loggingCategories)
        {
            TcpListener serverSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            DisableInheritance(serverSocket.Server);
            serverSocket.Start();

            Console.Error.WriteLine("waiting for v8 protocol on port " + port);

            while (true)
            {
                var clientSocket = await serverSocket.AcceptSocketAsync();
                DisableInheritance(clientSocket);
                if (clientSocket != null)
                {
                    Console.Error.WriteLine(">> accepted connection from client");

                    new System.Threading.Thread(() =>
                    {
                        using (var networkStream = new NetworkStream(clientSocket))
                        {
                            try
                            {
                                Dispatch(networkStream, networkStream, loggingCategories);
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine("Exception: " + e);
                            }
                        }

                        clientSocket.Dispose();
                        Console.Error.WriteLine(">> client connection closed");
                    }).Start();
                }
            }
        }

        private static void Dispatch(Stream inputStream, Stream outputStream, List<LoggingCategory> loggingCategories)
        {
            AD7DebugSession debugSession = new AD7DebugSession(inputStream, outputStream, loggingCategories);

            debugSession.Protocol.Run();
            debugSession.Protocol.WaitForReader();
        }

        public static void DisableInheritance(Socket s)
        {
            if (Utilities.IsWindows())
            {
                Win32.DisableInheritance(s);
            }
        }

        private static class Win32
        {
            public static void DisableInheritance(Socket s)
            {
                Type t = s.GetType();
                PropertyInfo handle = t.GetTypeInfo().GetDeclaredProperty("Handle");
                if (handle != null)
                {
                    SetHandleInformation((IntPtr)handle.GetValue(s), HANDLE_FLAG_INHERIT, 0);
                }
            }

            private const int HANDLE_FLAG_INHERIT = 1;

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern int SetHandleInformation(IntPtr hObject, int dwMask, int dwFlags);
        }
    }
}
