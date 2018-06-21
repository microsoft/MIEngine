// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DebugEngineHost;
using Microsoft.DebugEngineHost.VSCode;

namespace OpenDebug
{
    internal class Program
    {
        private const int DEFAULT_PORT = 4711;

        private static bool s_trace_requests;
        private static bool s_trace_responses;
        private static bool s_engine_logging;

        private static int Main(string[] argv)
        {
            int port = -1;

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
                        s_trace_requests = true;
                        break;
                    case "--trace=response":
                        s_trace_requests = true;
                        s_trace_responses = true;
                        break;
                    case "--engineLogging":
                        s_engine_logging = true;
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

#if !CORECLR
            if (Utilities.IsMono())
            {
                // Mono uses the threadpool heavily for its async/await implementation.  Make sure we have an acceptable
                //  lower limit on the threadpool size to avoid deadlocks.
                int currentMinWorkerThreads, currentMinIOCThreads;
                ThreadPool.GetMinThreads(out currentMinWorkerThreads, out currentMinIOCThreads);

                if (currentMinWorkerThreads < 8)
                {
                    ThreadPool.SetMinThreads(8, currentMinIOCThreads);
                }
            }
#endif

            if (port > 0)
            {
                // TCP/IP server
                RunServer(port);
            }

            try
            {
                // stdin/stdout
                Console.Error.WriteLine("waiting for v8 protocol on stdin/stdout");
                Dispatch(Console.OpenStandardInput(), Console.OpenStandardOutput());
            }
            catch (Exception e)
            {
                Utilities.ReportException(e);
            }

            return 0;
        }

        private static async void RunServer(int port)
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
                                Dispatch(networkStream, networkStream);
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

        private static void Dispatch(Stream inputStream, Stream outputStream)
        {
            DispatcherProtocol protocol = new DispatcherProtocol(inputStream, outputStream);

            Action<string> traceResponseCallback = s => Console.Error.WriteLine(s);

            if (s_trace_requests)
                protocol.TraceCallback = traceResponseCallback;

            if (s_trace_responses)
                protocol.ResponseCallback = traceResponseCallback;

            if (s_engine_logging && HostLogger.Instance?.LogFilePath == null)
                HostLogger.Instance.LogCallback = s => Console.WriteLine(s);

            IDebugSession debugSession = null;

            protocol.Start((string command, dynamic args, IResponder responder) =>
            {
                if (args == null)
                {
                    args = new { };
                }

                if (command == "initialize")
                {
                    string adapterID = Utilities.GetString(args, "adapterID");
                    if (adapterID == null)
                    {
                        responder.SetBody(new ErrorResponseBody(new Message(1101, "initialize: property 'adapterID' is missing or empty")));
                        return;
                    }

                    DebugProtocolCallbacks debugProtocolCallbacks = new DebugProtocolCallbacks()
                    {
                        Send = e => protocol.SendEvent(e.type, e),
                        SendRaw = e => protocol.SendRawEvent(e.type, e),
                        SendLater = e => protocol.SendEventLater(e.type, e),
                        SetTraceLogger = t => protocol.TraceCallback = t,
                        SetResponseLogger = t => protocol.ResponseCallback = t,
                        SetEngineLogger = t =>
                        {
                            HostLogger.EnableHostLogging();
                            HostLogger.Instance.LogCallback = t;
                        }
                    };

                    debugSession = OpenDebugAD7.EngineFactory.CreateDebugSession(adapterID, debugProtocolCallbacks);
                    if (debugSession == null)
                    {
                        responder.SetBody(new ErrorResponseBody(new Message(1103, "initialize: can't create debug session for adapter '{_id}'", new { _id = adapterID })));
                        return;
                    }
                }

                if (debugSession != null)
                {
                    try
                    {
                        DebugResult dr = debugSession.Dispatch(command, args);
                        if (dr != null)
                        {
                            responder.SetBody(dr.Body);

                            if (dr.Events != null)
                            {
                                foreach (var e in dr.Events)
                                {
                                    responder.AddEvent(e.type, e);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        AggregateException aggregateException = e as AggregateException;
                        bool bodySet = false;
                        if (aggregateException != null)
                        {
                            if (aggregateException.InnerExceptions.Count == 1)
                            {
                                e = aggregateException.InnerException;
                            }
                            else
                            {
                                string exceptionMessages = string.Join(", ", aggregateException.InnerExceptions.Select((x) => Utilities.GetExceptionDescription(x)));
                                responder.SetBody(new ErrorResponseBody(new Message(1104, "error while processing request '{_request}' (exceptions: {_messages})", new { _request = command, _messages = exceptionMessages })));
                                bodySet = true;
                            }
                        }

                        if (!bodySet)
                        {
                            if (Utilities.IsCorruptingException(e))
                            {
                                Utilities.ReportException(e);
                            }

                            if (e is OpenDebugAD7.AD7Exception)
                            {
                                responder.SetBody(new ErrorResponseBody(new Message(1104, e.Message)));
                            }
                            else
                            {
                                responder.SetBody(new ErrorResponseBody(new Message(1104, "error while processing request '{_request}' (exception: {_exception})", new { _request = command, _exception = Utilities.GetExceptionDescription(e) })));
                            }
                        }
                    }

                    if (command == "disconnect")
                    {
                        protocol.Stop();
                    }
                }
            }).Wait();
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
