// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace MICore
{
    public class LocalCygwinTransport : StreamTransport
    {
        private int _debuggerPid;
        private EventWaitHandle _breakRequestEvent;
        private EventWaitHandle _breakResponseEvent;
        private StreamReader _stdErrReader;

        public override int DebuggerPid
        {
            get
            {
                return this._debuggerPid;
            }
        }

        public void BreakProcess()
        {
            _breakRequestEvent.Set();
            _breakResponseEvent.WaitOne();
        }

        internal Boolean ConsoleCtrlHandler(WindowsNativeMethods.ConsoleCtrlValues dwCtrlEvent)
        {
            if (dwCtrlEvent == WindowsNativeMethods.ConsoleCtrlValues.CTRL_C_EVENT)
            {
                return true;
            }

            return false;
        }

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            WindowsNativeMethods.SetConsoleCtrlHandler(new WindowsNativeMethods.ConsoleCtrlDelegate(this.ConsoleCtrlHandler), true);

            reader = null;
            writer = null;

            // Create the anonymous pipe that will act as stdout for the process
            WindowsNativeMethods.SECURITY_ATTRIBUTES pPipeSecOut = new WindowsNativeMethods.SECURITY_ATTRIBUTES();
            pPipeSecOut.bInheritHandle = 1;
            pPipeSecOut.nLength = Marshal.SizeOf(pPipeSecOut);
            SafeFileHandle stdoutRead;
            SafeFileHandle stdoutWrite;
            if (!WindowsNativeMethods.CreatePipe(out stdoutRead, out stdoutWrite, ref pPipeSecOut, 4096))
            {
                Debug.Fail("Unexpected failure CreatePipe in InitStreams");
                return;
            }
            WindowsNativeMethods.SetHandleInformation(stdoutRead, WindowsNativeMethods.HANDLE_FLAGS.INHERIT, 0);

            // Create the anonymous pipe that will act as stdin for the process
            WindowsNativeMethods.SECURITY_ATTRIBUTES pPipeSecIn = new WindowsNativeMethods.SECURITY_ATTRIBUTES();
            pPipeSecIn.bInheritHandle = 1;
            pPipeSecIn.nLength = Marshal.SizeOf(pPipeSecIn);
            SafeFileHandle stdinRead;
            SafeFileHandle stdinWrite;
            if (!WindowsNativeMethods.CreatePipe(out stdinRead, out stdinWrite, ref pPipeSecIn, 4096))
            {
                Debug.Fail("Unexpected failure CreatePipe in InitStreams");
                return;
            }
            WindowsNativeMethods.SetHandleInformation(stdinWrite, WindowsNativeMethods.HANDLE_FLAGS.INHERIT, 0);

            // Create the anonymous pipe that will act as stderr for the process
            WindowsNativeMethods.SECURITY_ATTRIBUTES pPipeSecErr = new WindowsNativeMethods.SECURITY_ATTRIBUTES();
            pPipeSecOut.bInheritHandle = 1;
            pPipeSecOut.nLength = Marshal.SizeOf(pPipeSecOut);
            SafeFileHandle stderrRead;
            SafeFileHandle stderrWrite;
            if (!WindowsNativeMethods.CreatePipe(out stderrRead, out stderrWrite, ref pPipeSecErr, 4096))
            {
                Debug.Fail("Unexpected failure CreatePipe in InitStreams");
                return;
            }
            WindowsNativeMethods.SetHandleInformation(stderrRead, WindowsNativeMethods.HANDLE_FLAGS.INHERIT, 0);

            // TODO: move this into flags enum
            const int STARTF_USESTDHANDLES = 0x00000100;
            const int STARTF_USESHOWWINDOW = 0x00000001;
            WindowsNativeMethods.STARTUPINFO startupInfo = new WindowsNativeMethods.STARTUPINFO();
            startupInfo.dwFlags = 0;
            startupInfo.wShowWindow = 0;
            startupInfo.dwFlags = STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW;
            startupInfo.hStdOutput = stdoutWrite;
            startupInfo.hStdInput = stdinRead;
            startupInfo.hStdError = stderrWrite;
            startupInfo.cb = Marshal.SizeOf(startupInfo);

            WindowsNativeMethods.PROCESS_INFORMATION processInfo = new WindowsNativeMethods.PROCESS_INFORMATION();
            WindowsNativeMethods.SECURITY_ATTRIBUTES processSecurityAttributes = new WindowsNativeMethods.SECURITY_ATTRIBUTES();
            processSecurityAttributes.nLength = Marshal.SizeOf(processSecurityAttributes);
            WindowsNativeMethods.SECURITY_ATTRIBUTES threadSecurityAttributes = new WindowsNativeMethods.SECURITY_ATTRIBUTES();
            threadSecurityAttributes.nLength = Marshal.SizeOf(threadSecurityAttributes);

            LocalLaunchOptions localLaunchOptions = (LocalLaunchOptions)options;

            string miDebuggerDir = Path.GetDirectoryName(localLaunchOptions.MIDebuggerPath);

            // On Windows, GDB locally requires that the directory be on the PATH, being the working directory isn't good enough
            System.Collections.Generic.Dictionary<string, string> envVariables = this.GetEnvironmentVariables();
            foreach (EnvironmentEntry entry in localLaunchOptions.Environment)
            {
                envVariables[entry.Name] = entry.Value;
            }

            GCHandle environmentBlock = CreateEnvironmentBlock(envVariables);
            try
            {
                IntPtr environmentPtr = environmentBlock.AddrOfPinnedObject();

                string breakEventName = "miengine_break_" + Guid.NewGuid().ToString();
                string breakEventResponseName = breakEventName + "response";
                this._breakRequestEvent = new EventWaitHandle(false, EventResetMode.AutoReset, breakEventName);
                this._breakResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset, breakEventResponseName);

                string thisModulePath = typeof(LocalCygwinTransport).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName;
                string binFolder = Path.GetDirectoryName(thisModulePath);
                string commandLine = Path.Combine(binFolder, "DebugConsoleProxy.exe") + " " +
                    localLaunchOptions.MIDebuggerPath + " " +
                    breakEventName + " " +
                    breakEventResponseName + " " +
                    localLaunchOptions.WorkingDirectory;

                const int CREATE_NEW_PROCESS_GROUP = 0x00000200;
                const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
                int flags = CREATE_NEW_PROCESS_GROUP | CREATE_UNICODE_ENVIRONMENT;
                if (!WindowsNativeMethods.CreateProcess(
                        null,
                        commandLine,
                        ref processSecurityAttributes,
                        ref threadSecurityAttributes,
                        true,
                        (uint)flags,
                        environmentPtr,
                        miDebuggerDir,
                        ref startupInfo,
                        out processInfo
                        ))
                {
                    Debug.Fail("Launching debug proxy failed");
                    return;
                }

                SafeFileHandle processSH = new SafeFileHandle(processInfo.hProcess, true);
                SafeFileHandle threadSH = new SafeFileHandle(processInfo.hThread, true);
            }
            finally
            {
                environmentBlock.Free();
            }

            FileStream fsReader = new FileStream(stdoutRead, FileAccess.Read);
            reader = new StreamReader(fsReader);

            FileStream fsWriter = new FileStream(stdinWrite, FileAccess.Write);
            writer = new StreamWriter(fsWriter);

            FileStream fsErrReader = new FileStream(stderrRead, FileAccess.Read);
            this._stdErrReader = new StreamReader(fsErrReader);

            this._debuggerPid = processInfo.dwProcessId;
            AsyncReadFromStdError();

            return;
        }

        private Dictionary<string, string> GetEnvironmentVariables()
        {
            IDictionary envVars = Environment.GetEnvironmentVariables();

            Dictionary<string, string> environmentVariables = new Dictionary<string, string>(envVars.Count, StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in envVars)
            {
                environmentVariables.Add((string)entry.Key, (string)entry.Value);
            }

            return environmentVariables;
        }

        private static GCHandle CreateEnvironmentBlock(Dictionary<string, string> envVariables)
        { 
            string[] variableNames = new string[envVariables.Count]; 
            byte[] envBlock = null;
            envVariables.Keys.CopyTo(variableNames, 0); 
       
            // windows requires the block to be sorted.
            Array.Sort(variableNames, StringComparer.OrdinalIgnoreCase);

            // generate the double null terminated list of var=value entries.
            // see https://msdn.microsoft.com/en-us/library/windows/desktop/ms682653(v=vs.85).aspx
            System.Text.StringBuilder stringBuff = new System.Text.StringBuilder(); 
            for (int i = 0; i < envVariables.Count; ++i) 
            foreach (string currEnvVar in variableNames)
            { 
                stringBuff.Append(currEnvVar); 
                stringBuff.Append('='); 
                stringBuff.Append(envVariables[currEnvVar]); 
                stringBuff.Append('\0'); 
            } 

            stringBuff.Append('\0'); 
            envBlock = System.Text.Encoding.Unicode.GetBytes(stringBuff.ToString());

            return GCHandle.Alloc(envBlock, GCHandleType.Pinned);
        }

        private async void AsyncReadFromStdError()
        {
            try
            {
                while (true)
                {
                    string line = await _stdErrReader.ReadLineAsync();
                    if (line == null)
                        break;

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        this.Callback.OnStdErrorLine(line);
                    }
                }
            }
            catch (Exception)
            {
                // If anything goes wrong, don't crash VS
            }
        }

    }
}