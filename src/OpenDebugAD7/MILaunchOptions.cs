// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.DebugEngineHost;
using Microsoft.DebugEngineHost.VSCode;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OpenDebug;

namespace OpenDebugAD7
{
    ///<summary>Generates launch options for launching the mi engine.</summary>
    internal static class MILaunchOptions
    {
        private const string DebuggerCommandMacro = "${debuggerCommand}";
        private enum RequestType
        {
            Launch,
            Attach
        }

        private enum LaunchOptionType
        {
            Local,
            Pipe,
            Tcp,
        }

        [JsonObject]
        private class JsonCommand
        {
            [JsonProperty]
            public bool IgnoreFailures { get; set; }

            [JsonProperty]
            public string Description { get; set; }

            [JsonProperty]
            public string Text { get; set; }
        }


        [JsonObject]
        private class JsonBaseLaunchOptions
        {
            [JsonProperty]
            [JsonRequired]
            public string Name { get; set; }

            [JsonProperty]
            [JsonRequired]
            public string Type { get; set; }

            [JsonProperty]
            public JsonCommand[] SetupCommands { get; set; }

            [JsonProperty]
            public JsonCommand[] CustomLaunchSetupCommands { get; set; }

            [JsonProperty]
            public string LaunchCompleteCommand { get; set; }

            [JsonProperty]
            [JsonRequired]
            [JsonConverter(typeof(StringEnumConverter))]
            public RequestType Request { get; set; }

            [JsonProperty]
            public string[] Args { get; set; }

            [JsonProperty]
            public string Cwd { get; set; }

            [JsonProperty]
            public string VisualizerFile { get; set; }

            [JsonProperty]
            public bool ShowDisplayString { get; set; }

            [JsonProperty]
            public bool StopAtEntry { get; set; }

            [JsonProperty]
            public string TargetArchitecture { get; set; }

            [JsonProperty]
            public string AdditionalSOLibSearchPath { get; set; }

            [JsonProperty]
            public string MIMode { get; set; }

            [JsonProperty]
            public string CoreDumpPath { get; set; }

            [JsonProperty]
            public SymbolLoadInfo SymbolLoadInfo { get; set; }
        }

        [JsonObject]
        private class EnvironmentEntry
        {
            [JsonProperty]
            [JsonRequired]
            public string Name { get; set; }

            [JsonProperty]
            [JsonRequired]
            public string Value { get; set; }
        }


        [JsonObject]
        private class JsonLocalLaunchOptions : JsonBaseLaunchOptions
        {
            [JsonProperty]
            public string MIDebuggerPath { get; set; }

            [JsonProperty]
            public string MIDebuggerArgs { get; set; }

            [JsonProperty]
            public string MIDebuggerServerAddress { get; set; }

            [JsonProperty]
            public int ProcessId { get; set; }

            [JsonProperty]
            public EnvironmentEntry[] Environment { get; set; }

            [JsonProperty]
            public Dictionary<string, string> Env { get; } = new Dictionary<string, string>();

            [JsonProperty]
            public string DebugServerPath { get; set; }

            [JsonProperty]
            public string DebugServerArgs { get; set; }

            [JsonProperty]
            public string ServerStarted { get; set; }

            [JsonProperty]
            public bool FilterStdout { get; set; }

            [JsonProperty]
            public bool FilterStderr { get; set; }

            [JsonProperty]
            public int ServerLaunchTimeout { get; set; }

            [JsonProperty]
            public bool ExternalConsole { get; set; }

            [JsonProperty]
            public bool AvoidWindowsConsoleRedirection { get; set; }
        }

        [JsonObject]
        private class SymbolLoadInfo
        {
            [JsonProperty]
            public bool? LoadAll { get; set; }

            [JsonProperty]
            public string ExceptionList { get; set; }
        }

        [JsonObject]
        private class JsonPipeLaunchOptions : JsonBaseLaunchOptions
        {
            [JsonProperty]
            [JsonRequired]
            public JsonPipeTransport PipeTransport { get; set; }

            [JsonProperty]
            public string ProcessId { get; set; }
        }

        [JsonObject]
        private class JsonPipeTransportOptions
        {
            [JsonProperty]
            [JsonRequired]
            public string PipeCwd { get; set; }

            [JsonProperty]
            [JsonRequired]
            public string PipeProgram { get; set; }

            [JsonProperty]
            [JsonRequired]
            public string[] PipeArgs { get; set; }

            [JsonProperty]
            public string DebuggerPath { get; set; }

            [JsonProperty]
            public Dictionary<string, string> PipeEnv { get; set; }
        }

        [JsonObject]
        private class JsonPipeTransport : JsonPipeTransportOptions
        {
            [JsonProperty]
            public JsonPipeTransportOptions Windows { get; set; }

            [JsonProperty]
            public JsonPipeTransportOptions Linux { get; set; }

            [JsonProperty]
            public JsonPipeTransportOptions OSX { get; set; }
        }

        [JsonObject]
        private class JsonTcpLaunchOptions : JsonBaseLaunchOptions
        {
            [JsonProperty]
            [JsonRequired]
            public string HostName { get; set; }

            [JsonProperty]
            [JsonRequired]
            public string Port { get; set; }

            [JsonProperty()]
            [JsonRequired]
            public bool Secure { get; set; }
        }

        private static string FormatCommand(JsonCommand command)
        {
            return String.Concat(
                "        <Command IgnoreFailures='", 
                command.IgnoreFailures ? "true" : "false", 
                "' Description='", 
                XmlSingleQuotedAttributeEncode(command.Description), 
                "'>", 
                command.Text, 
                "</Command>\n"
                );
        }

        private static void AddBaseLaunchOptionsAttributes(
            StringBuilder xmlLaunchOptions,
            JsonBaseLaunchOptions jsonLaunchOptions,
            string program,
            string workingDirectory)
        {
            xmlLaunchOptions.Append(String.Concat("  ExePath='", XmlSingleQuotedAttributeEncode(program), "'\n"));

            if (!String.IsNullOrEmpty(workingDirectory))
            {
                xmlLaunchOptions.Append(String.Concat("  WorkingDirectory='", XmlSingleQuotedAttributeEncode(workingDirectory), "'\n"));
            }

            if (jsonLaunchOptions.TargetArchitecture != null)
            {
                xmlLaunchOptions.Append(String.Concat("  TargetArchitecture='", jsonLaunchOptions.TargetArchitecture, "'\n"));
            }

            if (jsonLaunchOptions.VisualizerFile != null)
            {
                xmlLaunchOptions.Append(String.Concat("  VisualizerFile='", jsonLaunchOptions.VisualizerFile, "'\n"));
            }

            if (jsonLaunchOptions.ShowDisplayString)
            {
                xmlLaunchOptions.Append(" ShowDisplayString='true'\n");
            }

            if (jsonLaunchOptions.AdditionalSOLibSearchPath != null)
            {
                xmlLaunchOptions.Append(String.Concat("  AdditionalSOLibSearchPath='", jsonLaunchOptions.AdditionalSOLibSearchPath, "'\n"));
            }

            if (!String.IsNullOrWhiteSpace(jsonLaunchOptions.CoreDumpPath))
            {
                xmlLaunchOptions.Append(String.Concat(" CoreDumpPath='", jsonLaunchOptions.CoreDumpPath, "'\n"));
            }

            string[] exeArgsArray = jsonLaunchOptions.Args;

            // Check to see if we need to redirect app stdin/out/err in Windows case for IntegratedTerminalSupport.
            if (Utilities.IsWindows()
                && HostRunInTerminal.IsRunInTerminalAvailable()
                && jsonLaunchOptions is JsonLocalLaunchOptions
                && String.IsNullOrWhiteSpace(jsonLaunchOptions.CoreDumpPath))
            {
                var localLaunchOptions = (JsonLocalLaunchOptions)jsonLaunchOptions;

                if (localLaunchOptions.ProcessId == 0 && // Only when launching the debuggee
                    !localLaunchOptions.ExternalConsole
                    && !localLaunchOptions.AvoidWindowsConsoleRedirection)
                {
                    exeArgsArray = TryAddWindowsDebuggeeConsoleRedirection(exeArgsArray);
                }
            }

            // ExeArguments
            // Build the exe's argument list as a string
            StringBuilder exeArguments = new StringBuilder();
            exeArguments.Append(CreateArgumentList(exeArgsArray));
            XmlSingleQuotedAttributeEncode(exeArguments);
            xmlLaunchOptions.Append(String.Concat("  ExeArguments='", exeArguments, "'\n"));

            if (jsonLaunchOptions.MIMode != null)
            {
                xmlLaunchOptions.Append(String.Concat("  MIMode='", jsonLaunchOptions.MIMode, "'\n"));
            }
        }

        /// <summary>
        /// To support Windows RunInTerminal's IntegratedTerminal, we will check each argument to see if it is a redirection of stdin, stderr, stdout and then will add
        /// the redirection for the ones the user did not specify to go to Console.
        /// </summary>
        private static string[] TryAddWindowsDebuggeeConsoleRedirection(string[] arguments)
        {
            if (Utilities.IsWindows()) // Only do this on Windows
            {
                bool stdInRedirected = false;
                bool stdOutRedirected = false;
                bool stdErrRedirected = false;

                if (arguments != null)
                {
                    foreach (string rawArgument in arguments)
                    {
                        string argument = rawArgument.TrimStart();
                        if (argument.TrimStart().StartsWith("2>", StringComparison.Ordinal))
                        {
                            stdErrRedirected = true;
                        }
                        if (argument.TrimStart().StartsWith("1>", StringComparison.Ordinal) || argument.TrimStart().StartsWith(">", StringComparison.Ordinal))
                        {
                            stdOutRedirected = true;
                        }
                        if (argument.TrimStart().StartsWith("0>", StringComparison.Ordinal) || argument.TrimStart().StartsWith("<", StringComparison.Ordinal))
                        {
                            stdInRedirected = true;
                        }
                    }
                }

                // If one (or more) are not redirected, then add redirection
                if (!stdInRedirected || !stdOutRedirected || !stdErrRedirected)
                {
                    int argLength = arguments?.Length ?? 0;
                    List<string> argList = new List<string>(argLength + 3);
                    if (arguments != null)
                    {
                        argList.AddRange(arguments);
                    }

                    if (!stdErrRedirected)
                    {
                        argList.Add("2>CON");
                    }

                    if (!stdOutRedirected)
                    {
                        argList.Add("1>CON");
                    }

                    if (!stdInRedirected)
                    {
                        argList.Add("<CON");
                    }

                    return argList.ToArray<string>();
                }
            }

            return arguments;
        }

        private static string CreateArgumentList(IEnumerable<string> args)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (args != null)
            {
                foreach (string arg in args)
                {
                    if (stringBuilder.Length != 0)
                        stringBuilder.Append(' ');

                    stringBuilder.Append(QuoteArgument(arg));
                }
            }
            return stringBuilder.ToString();
        }

        // gdb does not like parenthesis without being quoted
        private static char[] s_CHARS_TO_QUOTE = { ' ', '\t', '(', ')' };
        private static string QuoteArgument(string argument)
        {
            // If user wants an empty or whitespace argument, make sure we quote it
            if (string.IsNullOrWhiteSpace(argument))
            {
                return '"' + argument + '"';
            }
            else
            {
                // ensure all quotes already in the string are escaped. If use has already escaped it too, undo our escaping
                argument = argument.Replace("\"", "\\\"").Replace("\\\\\"", "\\\"");
                if (argument.IndexOfAny(s_CHARS_TO_QUOTE) >= 0)
                {
                    return '"' + argument + '"';
                }
            }
            return argument;
        }

        private static void AddBaseLaunchOptionsElements(StringBuilder xmlLaunchOptions, JsonBaseLaunchOptions jsonLaunchOptions)
        {
            if (jsonLaunchOptions.SetupCommands != null)
            {
                xmlLaunchOptions.Append("    <SetupCommands>\n");
                foreach (JsonCommand command in jsonLaunchOptions.SetupCommands)
                {
                    xmlLaunchOptions.Append(FormatCommand(command));
                }
                xmlLaunchOptions.Append("    </SetupCommands>\n");
            }

            if (jsonLaunchOptions.CustomLaunchSetupCommands != null)
            {
                xmlLaunchOptions.Append("    <CustomLaunchSetupCommands>\n");
                foreach (JsonCommand command in jsonLaunchOptions.CustomLaunchSetupCommands)
                {
                    xmlLaunchOptions.Append(FormatCommand(command));
                }
                xmlLaunchOptions.Append("    </CustomLaunchSetupCommands>\n");
            }

            if (!String.IsNullOrEmpty(jsonLaunchOptions.LaunchCompleteCommand))
            {
                // The xml schema will validate the allowable values.
                xmlLaunchOptions.Append(String.Format(CultureInfo.InvariantCulture, "    <LaunchCompleteCommand>{0}</LaunchCompleteCommand>\n", jsonLaunchOptions.LaunchCompleteCommand));
            }
        }

        /// <summary>
        /// Returns the path to lldb-mi which is installed when the extension is installed
        /// </summary>
        /// <returns>Path to lldb-mi or null if it doesn't exist</returns>
        private static string GetLLDBMIPath()
        {
            string exePath = null;
            string directory = EngineConfiguration.GetAdapterDirectory();
            DirectoryInfo dir = new DirectoryInfo(directory);

            // Remove /bin from the path to get to the debugAdapter folder
            string debugAdapterPath = dir.Parent?.FullName;

            if (!String.IsNullOrEmpty(debugAdapterPath))
            {
                // Path for lldb-mi 10.x and if it exists use it.
                exePath = Path.Combine(debugAdapterPath, "lldb-mi", "bin", "lldb-mi");
                if (!File.Exists(exePath))
                {
                    // Fall back to using path for lldb-mi 3.8
                    exePath = Path.Combine(debugAdapterPath, "lldb", "bin", "lldb-mi");
                    if (!File.Exists(exePath))
                    {
                        // Neither exist
                        return null;
                    }
                }
            }

            return exePath;
        }

        internal static string CreateLaunchOptions(
            string program,
            string workingDirectory,
            string args,
            bool isPipeLaunch,
            out bool stopAtEntry,
            out bool isCoreDump,
            out bool debugServerUsed,
            out bool isOpenOCD,
            out bool visualizerFileUsed)
        {
            stopAtEntry = false;
            isCoreDump = false;
            debugServerUsed = false;
            isOpenOCD = false;
            visualizerFileUsed = false;

            LaunchOptionType launchType = isPipeLaunch ? LaunchOptionType.Pipe : LaunchOptionType.Local;

            if (launchType == LaunchOptionType.Local)
            {
                JsonLocalLaunchOptions jsonLaunchOptions = JsonConvert.DeserializeObject<JsonLocalLaunchOptions>(args);
                StringBuilder xmlLaunchOptions = new StringBuilder();
                xmlLaunchOptions.Append("<LocalLaunchOptions xmlns='http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014'\n");
                AddBaseLaunchOptionsAttributes(xmlLaunchOptions, jsonLaunchOptions, program, workingDirectory);

                string lldbPath = null;
                if (String.Equals(jsonLaunchOptions.MIMode, "lldb", StringComparison.Ordinal)
                    && String.IsNullOrEmpty(jsonLaunchOptions.MIDebuggerPath))
                {
                    //find LLDBMI
                    lldbPath = GetLLDBMIPath();
                }
                xmlLaunchOptions.Append(String.Concat("  MIDebuggerPath='", lldbPath ?? jsonLaunchOptions.MIDebuggerPath, "'\n"));
                if (!String.IsNullOrEmpty(jsonLaunchOptions.MIDebuggerArgs))
                {
                    string miDebuggerArgs = XmlSingleQuotedAttributeEncode(jsonLaunchOptions.MIDebuggerArgs);
                    xmlLaunchOptions.Append(String.Concat("  MIDebuggerArgs='", miDebuggerArgs, "'\n"));
                }

                // If we get SymbolLoadInfo and an ExceptionList or LoadAll is set. We will need to have WaitDynamicLibLoad.
                if (jsonLaunchOptions.SymbolLoadInfo != null && 
                    (!String.IsNullOrWhiteSpace(jsonLaunchOptions.SymbolLoadInfo.ExceptionList) || jsonLaunchOptions.SymbolLoadInfo.LoadAll.HasValue))
                {
                    xmlLaunchOptions.Append(String.Concat("  WaitDynamicLibLoad='true'\n"));
                }
                else
                {
                    xmlLaunchOptions.Append(String.Concat("  WaitDynamicLibLoad='false'\n"));
                }

                if (jsonLaunchOptions.MIDebuggerServerAddress != null)
                {
                    xmlLaunchOptions.Append(String.Concat("  MIDebuggerServerAddress='", jsonLaunchOptions.MIDebuggerServerAddress, "'\n"));
                }

                if (jsonLaunchOptions.ProcessId != 0)
                {
                    xmlLaunchOptions.Append(String.Concat("  ProcessId='", jsonLaunchOptions.ProcessId, "'\n"));
                }

                isCoreDump = !String.IsNullOrEmpty(jsonLaunchOptions.CoreDumpPath);
                stopAtEntry = !isCoreDump && jsonLaunchOptions.StopAtEntry;

                if (jsonLaunchOptions.DebugServerPath != null)
                {
                    xmlLaunchOptions.Append(String.Concat("  DebugServer='", jsonLaunchOptions.DebugServerPath, "'\n"));

                    if (!String.IsNullOrWhiteSpace(jsonLaunchOptions.DebugServerPath))
                    {
                        debugServerUsed = true;
                        StringComparison comparison = StringComparison.Ordinal;
                        if (Utilities.IsWindows())
                        {
                            comparison = StringComparison.OrdinalIgnoreCase;
                        }
                        isOpenOCD = Path.GetFileNameWithoutExtension(jsonLaunchOptions.DebugServerPath).Equals("openocd", comparison);
                    }
                }
                if (jsonLaunchOptions.DebugServerArgs != null)
                {
                    xmlLaunchOptions.Append(String.Concat("  DebugServerArgs='", jsonLaunchOptions.DebugServerArgs, "'\n"));
                }
                if (jsonLaunchOptions.ServerStarted != null)
                {
                    xmlLaunchOptions.Append(String.Concat("  ServerStarted='", jsonLaunchOptions.ServerStarted, "'\n"));
                }
                if (jsonLaunchOptions.FilterStdout)
                {
                    xmlLaunchOptions.Append(String.Concat("  FilterStdout='", jsonLaunchOptions.FilterStdout ? "true" : "false", "'\n"));
                }
                if (jsonLaunchOptions.FilterStderr)
                {
                    xmlLaunchOptions.Append(String.Concat("  FilterStderr='", jsonLaunchOptions.FilterStderr ? "true" : "false", "'\n"));
                }
                if (jsonLaunchOptions.ServerLaunchTimeout != 0)
                {
                    xmlLaunchOptions.Append(String.Concat("  ServerLaunchTimeout='", jsonLaunchOptions.ServerLaunchTimeout, "'\n"));
                }
                if (jsonLaunchOptions.ExternalConsole)
                {
                    xmlLaunchOptions.Append("  ExternalConsole='true'\n");
                }

                xmlLaunchOptions.Append(">\n");

                AddBaseLaunchOptionsElements(xmlLaunchOptions, jsonLaunchOptions);

                bool environmentDefined = jsonLaunchOptions.Environment?.Length > 0;
                if (environmentDefined)
                {
                    xmlLaunchOptions.Append("    <Environment>\n");
                    foreach (EnvironmentEntry envEntry in jsonLaunchOptions.Environment)
                    {
                        AddEnvironmentVariable(xmlLaunchOptions, envEntry.Name, envEntry.Value);
                    }
                    xmlLaunchOptions.Append("    </Environment>\n");
                }

                if (jsonLaunchOptions.Env.Count > 0)
                {
                    if (environmentDefined)
                    {
                        throw new ArgumentException(AD7Resources.Error_ConflictingEnvProps);
                    }

                    xmlLaunchOptions.Append("    <Environment>\n");
                    foreach (KeyValuePair<string, string> pair in jsonLaunchOptions.Env)
                    {
                        AddEnvironmentVariable(xmlLaunchOptions, pair.Key, pair.Value);
                    }
                    xmlLaunchOptions.Append("    </Environment>\n");
                }

                if (jsonLaunchOptions.SymbolLoadInfo != null)
                {
                    xmlLaunchOptions.Append("    <SymbolLoadInfo ");
                    if (jsonLaunchOptions.SymbolLoadInfo.LoadAll.HasValue)
                    {
                        xmlLaunchOptions.Append("LoadAll = '" + jsonLaunchOptions.SymbolLoadInfo.LoadAll.Value.ToString().ToLowerInvariant() + "' ");
                    }

                    if (!String.IsNullOrWhiteSpace(jsonLaunchOptions.SymbolLoadInfo.ExceptionList))
                    {
                        xmlLaunchOptions.Append("ExceptionList='" + XmlSingleQuotedAttributeEncode(jsonLaunchOptions.SymbolLoadInfo.ExceptionList) + "' ");
                    }

                    xmlLaunchOptions.Append("/>\n");
                }

                xmlLaunchOptions.Append("</LocalLaunchOptions>");

                visualizerFileUsed = jsonLaunchOptions.VisualizerFile != null;

                return xmlLaunchOptions.ToString();
            }
            else if (launchType == LaunchOptionType.Pipe)
            {
                JsonPipeLaunchOptions jsonLaunchOptions = JsonConvert.DeserializeObject<JsonPipeLaunchOptions>(args.ToString());

                stopAtEntry = jsonLaunchOptions.StopAtEntry;

                StringBuilder xmlLaunchOptions = new StringBuilder();
                xmlLaunchOptions.Append("<PipeLaunchOptions xmlns='http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014'\n");

                AddBaseLaunchOptionsAttributes(xmlLaunchOptions, jsonLaunchOptions, program, workingDirectory);

                string pipeCwd = jsonLaunchOptions.PipeTransport.PipeCwd;
                string pipeProgram = jsonLaunchOptions.PipeTransport.PipeProgram;
                string[] pipeArgs = jsonLaunchOptions.PipeTransport.PipeArgs;
                string processId = jsonLaunchOptions.ProcessId;
                Dictionary<string, string> pipeEnv = jsonLaunchOptions.PipeTransport.PipeEnv;

                JsonPipeTransportOptions platformSpecificTransportOptions = null;
                if (Utilities.IsOSX() && jsonLaunchOptions.PipeTransport.OSX != null)
                {
                    platformSpecificTransportOptions = jsonLaunchOptions.PipeTransport.OSX;
                }
                else if (Utilities.IsLinux() && jsonLaunchOptions.PipeTransport.Linux != null)
                {
                    platformSpecificTransportOptions = jsonLaunchOptions.PipeTransport.Linux;
                }
                else if (Utilities.IsWindows() && jsonLaunchOptions.PipeTransport.Windows != null)
                {
                    platformSpecificTransportOptions = jsonLaunchOptions.PipeTransport.Windows;
                }

                if (platformSpecificTransportOptions != null)
                {
                    pipeProgram = platformSpecificTransportOptions.PipeProgram ?? pipeProgram;
                    pipeArgs = platformSpecificTransportOptions.PipeArgs ?? pipeArgs;
                    pipeCwd = platformSpecificTransportOptions.PipeCwd ?? pipeCwd;
                    pipeEnv = platformSpecificTransportOptions.PipeEnv ?? pipeEnv;
                }

                if (string.IsNullOrWhiteSpace(pipeProgram))
                {
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "Invalid pipeProgram option. pipeProgram should not be null or empty. Args: {0}", args));
                }

                xmlLaunchOptions.Append(String.Concat("  PipePath='", pipeProgram, "'\n"));

                if (pipeArgs != null)
                {
                    string pipeCommandArgs = CreateArgumentList(pipeArgs);
                    IEnumerable<string> allPipeArguments = pipeArgs;
                    string debuggerPath = jsonLaunchOptions.PipeTransport.DebuggerPath ?? "";
                    if (!string.IsNullOrEmpty(debuggerPath))
                    {
                        string fullDebuggerCommandline = string.Format(CultureInfo.InvariantCulture, "{0} --interpreter=mi", debuggerPath);
                        if (allPipeArguments.Any(x => x.Contains(DebuggerCommandMacro)))
                        {
                            allPipeArguments = allPipeArguments.Select(x => x.Replace(DebuggerCommandMacro, fullDebuggerCommandline));
                        }
                        else
                        {
                            allPipeArguments = allPipeArguments.Concat(new string[] { fullDebuggerCommandline });
                        }
                    }

                    string allArguments = CreateArgumentList(allPipeArguments);
                    xmlLaunchOptions.Append(String.Concat("  PipeArguments='", MILaunchOptions.XmlSingleQuotedAttributeEncode(allArguments), "'\n"));

                    // debuggerPath has to be specified. if it isn't then the debugger is specified in PipeArg which means we can't use the same arguments for pipeCommandArgs
                    if (!string.IsNullOrEmpty(debuggerPath))
                    {
                        xmlLaunchOptions.Append(String.Concat("  PipeCommandArguments='", MILaunchOptions.XmlSingleQuotedAttributeEncode(pipeCommandArgs), "'\n"));
                    }
                }

                if (pipeCwd != null)
                {
                    xmlLaunchOptions.Append(String.Concat("  PipeCwd='", MILaunchOptions.XmlSingleQuotedAttributeEncode(pipeCwd), "'\n"));
                }

                if (!String.IsNullOrEmpty(processId))
                {
                    xmlLaunchOptions.Append(String.Concat("  ProcessId='", processId, "'\n"));
                }

                xmlLaunchOptions.Append(">\n");

                AddBaseLaunchOptionsElements(xmlLaunchOptions, jsonLaunchOptions);

                if (pipeEnv != null && pipeEnv.Count > 0)
                {
                    xmlLaunchOptions.Append("    <PipeEnvironment>\n");
                    foreach (KeyValuePair<string, string> pair in pipeEnv)
                    {
                        AddEnvironmentVariable(xmlLaunchOptions, pair.Key, pair.Value);
                    }
                    xmlLaunchOptions.Append("    </PipeEnvironment>\n");
                }

                xmlLaunchOptions.Append("</PipeLaunchOptions>");

                return xmlLaunchOptions.ToString();
            }
            // Commented out for now as we aren't supporting these options.
            //else if (args.transport == "Tcp")
            //{
            //    JsonTcpLaunchOptions jsonLaunchOptions = JsonConvert.DeserializeObject<JsonTcpLaunchOptions>(args.ToString());

            //    StringBuilder xmlLaunchOptions = new StringBuilder();
            //    xmlLaunchOptions.Append("<TcpLaunchOptions xmlns='http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014'\n");
            //    AddBaseLaunchOptionsAttributes(xmlLaunchOptions, jsonLaunchOptions, program, workingDirectory);

            //    xmlLaunchOptions.Append(String.Concat("  HostName='", jsonLaunchOptions.HostName, "'\n"));
            //    xmlLaunchOptions.Append(String.Concat("  PipePath='", jsonLaunchOptions.Port, "'\n"));
            //    // NOTE: depending on default value of bool to be false.
            //    xmlLaunchOptions.Append(String.Concat("  Secure='", jsonLaunchOptions.Secure ? "true" : "false", "'\n"));

            //    xmlLaunchOptions.Append(">\n");

            //    AddBaseLaunchOptionsElements(xmlLaunchOptions, jsonLaunchOptions);

            //    xmlLaunchOptions.Append("</TcpLaunchOptions>");

            //    return xmlLaunchOptions.ToString();
            //}

            Debug.Fail("We should not get here. All the launch types should be handled above.");
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_Internal_Launch, launchType, args));
        }

        private static void AddEnvironmentVariable(StringBuilder xmlLaunchOptions, string name, string value)
        {
            xmlLaunchOptions.AppendFormat(CultureInfo.InvariantCulture, "        <EnvironmentEntry Name='{0}' Value='{1}' />\n", name, XmlSingleQuotedAttributeEncode(value));
        }

        internal static string XmlSingleQuotedAttributeEncode(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.IndexOfAny(s_specialXmlSingleQuotedAttributeChars) < 0)
            {
                return value;
            }
            else
            {
                StringBuilder sb = new StringBuilder(value);
                XmlSingleQuotedAttributeEncode(sb);
                return sb.ToString();
            }
        }

        /// <summary>
        /// Escape a string that will be used as an XML attribute enclosed using single quotes (').
        /// </summary>
        /// <param name="stringBuilder">StringBuilder to update</param>
        private static void XmlSingleQuotedAttributeEncode(StringBuilder stringBuilder)
        {
            stringBuilder.Replace("&", "&amp;");
            stringBuilder.Replace("<", "&lt;");
            stringBuilder.Replace(">", "&gt;");
            stringBuilder.Replace("'", "&apos;");
        }

        private static char[] s_specialXmlSingleQuotedAttributeChars = { '&', '<', '>', '\'' };
    }
}
