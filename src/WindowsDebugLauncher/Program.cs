// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace WindowsDebugLauncher
{
    internal class Program
    {
        private static int Main(string[] argv)
        {
            DebugLauncher.LaunchParameters parameters = new DebugLauncher.LaunchParameters();
            parameters.PipeServer = "."; // Currently only supporting local pipe connections

            // Avoid sending the BOM on Windows if the Beta Unicode feature is enabled in Windows 10
            if (Console.InputEncoding.CodePage == 65001)
            {
                Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            }

            foreach (var a in argv)
            {
                if (String.IsNullOrEmpty(a))
                {
                    continue;
                }

                switch (a)
                {
                    case "-h":
                    case "-?":
                    case "/?":
                    case "--help":
                        HelpMessage();
                        return 1;
                    case "--pauseForDebugger":
                        {

                            while (!Debugger.IsAttached)
                            {
                                Thread.Sleep(500);
                            }
                        }
                        break;
                    default:
                        if (a.StartsWith("--stdin=", StringComparison.OrdinalIgnoreCase))
                        {
                            string stdin = a.Substring("--stdin=".Length);
                            if (string.IsNullOrWhiteSpace(stdin))
                            {
                                GenerateError("--stdin");
                                return -1;
                            }
                            parameters.StdInPipeName = stdin;
                        }
                        else if (a.StartsWith("--stdout=", StringComparison.OrdinalIgnoreCase))
                        {
                            string stdout = a.Substring("--stdout=".Length);
                            if (string.IsNullOrWhiteSpace(stdout))
                            {
                                GenerateError("--stdout");
                                return -1;
                            }
                            parameters.StdOutPipeName = stdout;
                        }
                        else if (a.StartsWith("--stderr=", StringComparison.OrdinalIgnoreCase))
                        {
                            string stderr = a.Substring("--stderr=".Length);
                            if (string.IsNullOrWhiteSpace(stderr))
                            {
                                GenerateError("--stderr");
                                return -1;
                            }
                            parameters.StdErrPipeName = stderr;
                        }
                        else if (a.StartsWith("--pid=", StringComparison.OrdinalIgnoreCase))
                        {
                            string pid = a.Substring("--pid=".Length);
                            if (string.IsNullOrWhiteSpace(pid))
                            {
                                GenerateError("--pid");
                                return -1;
                            }
                            parameters.PidPipeName = pid;
                        }
                        else if (a.StartsWith("--dbgExe=", StringComparison.OrdinalIgnoreCase))
                        {
                            string dbgExe = a.Substring("--dbgExe=".Length);
                            if (String.IsNullOrEmpty(dbgExe) || !File.Exists(dbgExe))
                            {
                                GenerateError("--dbgExe");
                                return -1;
                            }
                            parameters.DbgExe = dbgExe;
                        }
                        else
                        {
                            parameters.DbgExeArgs.AddRange(ParseDebugExeArgs(a));
                        }
                        break;
                }
            }

            if (!parameters.ValidateParameters())
            {
                Console.Error.WriteLine("One or more required values are missing.");
                HelpMessage();
                return -1;
            }

            DebugLauncher launcher = new DebugLauncher(parameters);
            launcher.StartPipeConnection();
            return 0;
        }

        private static void GenerateError(string flag)
        {
            Console.Error.WriteLine(FormattableString.Invariant($"Value for flag:'{flag}' is missing or incorrect."));
            HelpMessage();
        }

        /// <summary>
        /// Parse dbgargs for spaces and quoted strings
        /// </summary>
        private static List<string> ParseDebugExeArgs(string line)
        {
            List<string> args = new List<string>();
            bool inQuotedString = false;
            bool isEscape = false;

            StringBuilder builder = new StringBuilder();
            foreach (char c in line)
            {
                if (isEscape)
                {
                    switch (c)
                    {
                        case 'n':
                            builder.Append("\n");
                            break;
                        case 'r':
                            builder.Append("\r");
                            break;
                        case '\\':
                            builder.Append("\\");
                            break;
                        case '"':
                            builder.Append("\"");
                            break;
                        case ' ':
                            builder.Append(" ");
                            break;
                        default:
                            throw new ArgumentException(FormattableString.Invariant($"Invalid escape sequence: \\{c}"));
                    }

                    isEscape = false;
                    continue;
                }

                if (c == '\\')
                {
                    isEscape = true;
                    continue;
                }

                if (!inQuotedString && c == '"')
                {
                    inQuotedString = true;
                    continue;
                }

                if (inQuotedString && c == '"')
                {
                    inQuotedString = false;
                    continue;
                }

                if (!inQuotedString && c == ' ')
                {
                    if (builder.Length > 0)
                    {
                        args.Add(builder.ToString());
                        builder.Clear();
                    }

                    continue;
                }

                builder.Append(c);
            }

            if (builder.Length > 0)
            {
                args.Add(builder.ToString());
            }

            return args;
        }

        private static void HelpMessage()
        {
            Console.WriteLine("WindowsDebugLauncher: Launching debuggers for use with MIEngine in a separate process.");
            Console.WriteLine("--stdin=<value>        '<value>' is NamedPipeName for debugger stdin");
            Console.WriteLine("--stdout=<value>       '<value>' is NamedPipeName for debugger stdout");
            Console.WriteLine("--stderr=<value>       '<value>' is NamedPipeName for debugger stderr");
            Console.WriteLine("--pid=<value>          '<value>' is NamedPipeName for debugger pid");
            Console.WriteLine("--dbgExe=<value>       '<value>' is the path to the debugger");
        }
    }
}
