// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OpenDebug;
using Microsoft.DebugEngineHost.VSCode;

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
            return String.Concat("        <Command IgnoreFailures='", command.IgnoreFailures ? "true" : "false", "' Description='", command.Description, "'>", command.Text, "</Command>\n");
        }

        private static void AddBaseLaunchOptionsAttributes(
            StringBuilder xmlLaunchOptions,
            JsonBaseLaunchOptions jsonLaunchOptions,
            string program,
            string workingDirectory)
        {
            StringBuilder exeArguments = new StringBuilder();

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

            if (exeArguments.Length > 0 && jsonLaunchOptions.Args != null && jsonLaunchOptions.Args.Length > 0)
            {
                exeArguments.Append(' ');
            }

            // ExeArguments
            exeArguments.Append(CreateArgumentList(jsonLaunchOptions.Args));
            XmlSingleQuotedAttributeEncode(exeArguments);
            xmlLaunchOptions.Append(String.Concat("  ExeArguments='", exeArguments, "'\n"));

            if (jsonLaunchOptions.MIMode != null)
            {
                xmlLaunchOptions.Append(String.Concat(" MIMode='", jsonLaunchOptions.MIMode, "'\n"));
            }
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
            if (!string.IsNullOrWhiteSpace(argument))
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

        private static LaunchOptionType GetLaunchType(dynamic args)
        {
            if (args.pipeTransport != null)
            {
                return LaunchOptionType.Pipe;
            }

            return LaunchOptionType.Local;
        }

        /// <summary>
        /// Returns the path to lldb-mi which is installed when the extension is installed
        /// </summary>
        /// <returns>Path to lldb-mi or null if it doesn't exist</returns>
        private static string GetLLDBMIPath()
        {
            string directory = EngineConfiguration.GetAdapterDirectory();
            DirectoryInfo dir = new DirectoryInfo(directory);

            // Remove /bin from the path to get to the debugAdapter folder
            string debugAdapterPath = dir.Parent?.FullName;

            if (!String.IsNullOrEmpty(debugAdapterPath))
            {
                string exePath = Path.Combine(debugAdapterPath, "lldb", "bin", "lldb-mi");
                if (File.Exists(exePath))
                {
                    return exePath;
                }
            }
            return null;
        }

        internal static string CreateLaunchOptions(
            string program,
            string workingDirectory,
            dynamic args,
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

            LaunchOptionType launchType = GetLaunchType(args);
            if (launchType == LaunchOptionType.Local)
            {
                JsonLocalLaunchOptions jsonLaunchOptions = JsonConvert.DeserializeObject<JsonLocalLaunchOptions>(args.ToString());

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
                xmlLaunchOptions.Append(String.Concat("  WaitDynamicLibLoad='false'\n"));

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
            if (value.IndexOfAny(s_specialXmlSingleQuotedAttributeChars) < 0)
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
