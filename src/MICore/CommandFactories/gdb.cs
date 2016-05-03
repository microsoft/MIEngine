// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;

namespace MICore
{
    internal class GdbMICommandFactory : MICommandFactory
    {
        private int _currentThreadId = 0;
        private uint _currentFrameLevel = 0;

        public override string Name
        {
            get { return "GDB"; }
        }

        public override void DefineCurrentThread(int threadId)
        {
            _currentThreadId = threadId;
        }

        public override bool SupportsStopOnDynamicLibLoad()
        {
            return true;
        }

        public override bool AllowCommandsWhileRunning()
        {
            return false;
        }

        public override bool UseExternalConsoleForLocalLaunch(LocalLaunchOptions localLaunchOptions)
        {
            // NOTE: On Linux at least, there are issues if we try to have GDB launch the process as a child of VS 
            // code -- it will cause a deadlock during debuggee launch. So we always use the external console 
            // unless we are in a scenario where the debuggee will not be a child process. In the future we 
            // might want to change this for other OSs.
            return String.IsNullOrEmpty(localLaunchOptions.MIDebuggerServerAddress) && !localLaunchOptions.IsCoreDump;
        }

        protected override async Task<Results> ThreadFrameCmdAsync(string command, ResultClass expectedResultClass, int threadId, uint frameLevel)
        {
            // first aquire an exclusive lock. This is used as we don't want to fight with other commands that also require the current
            // thread to be set to a particular value
            ExclusiveLockToken lockToken = await _debugger.CommandLock.AquireExclusive();

            try
            {
                await ThreadSelect(threadId, lockToken);
                await StackSelectFrame(frameLevel, lockToken);

                // Before we execute the provided command, we need to switch to a shared lock. This is because the provided
                // command may be an expression evaluation command which could be long running, and we don't want to hold the
                // exclusive lock during this.
                lockToken.ConvertToSharedLock();
                lockToken = null;

                return await _debugger.CmdAsync(command, expectedResultClass);
            }
            finally
            {
                if (lockToken != null)
                {
                    // finally is executing before we called 'ConvertToSharedLock'
                    lockToken.Close();
                }
                else
                {
                    // finally is called after we called ConvertToSharedLock, we need to decerement the shared lock count
                    _debugger.CommandLock.ReleaseShared();
                }
            }
        }

        protected override async Task<Results> ThreadCmdAsync(string command, ResultClass expectedResultClass, int threadId)
        {
            // first aquire an exclusive lock. This is used as we don't want to fight with other commands that also require the current
            // thread to be set to a particular value
            ExclusiveLockToken lockToken = await _debugger.CommandLock.AquireExclusive();

            try
            {
                await ThreadSelect(threadId, lockToken);

                // Before we execute the provided command, we need to switch to a shared lock. This is because the provided
                // command may be an expression evaluation command which could be long running, and we don't want to hold the
                // exclusive lock during this.
                lockToken.ConvertToSharedLock();
                lockToken = null;

                return await _debugger.CmdAsync(command, expectedResultClass);
            }
            finally
            {
                if (lockToken != null)
                {
                    // finally is executing before we called 'ConvertToSharedLock'
                    lockToken.Close();
                }
                else
                {
                    // finally is called after we called ConvertToSharedLock, we need to decerement the shared lock count
                    _debugger.CommandLock.ReleaseShared();
                }
            }
        }

        private async Task ThreadSelect(int threadId, ExclusiveLockToken lockToken)
        {
            if (ExclusiveLockToken.IsNullOrClosed(lockToken))
            {
                throw new ArgumentNullException("lockToken");
            }

            if (threadId != _currentThreadId)
            {
                string command = string.Format("-thread-select {0}", threadId);
                await _debugger.ExclusiveCmdAsync(command, ResultClass.done, lockToken);
                _currentThreadId = threadId;
                _currentFrameLevel = 0;
            }
        }

        private async Task StackSelectFrame(uint frameLevel, ExclusiveLockToken lockToken)
        {
            if (ExclusiveLockToken.IsNullOrClosed(lockToken))
            {
                throw new ArgumentNullException("lockToken");
            }

            if (frameLevel != _currentFrameLevel)
            {
                string command = string.Format("-stack-select-frame {0}", frameLevel);
                await _debugger.ExclusiveCmdAsync(command, ResultClass.done, lockToken);
                _currentFrameLevel = frameLevel;
            }
        }
        public override async Task<Results> ThreadInfo()
        {
            Results results = await base.ThreadInfo();
            if (results.ResultClass == ResultClass.done && results.Contains("current-thread-id"))
            {
                _currentThreadId = results.FindInt("current-thread-id");
            }
            return results;
        }
        public override async Task<List<ulong>> StartAddressesForLine(string file, uint line)
        {
            string cmd = "info line " + file + ":" + line;
            var result = await _debugger.ConsoleCmdAsync(cmd);
            List<ulong> addresses = new List<ulong>();
            using (StringReader stringReader = new StringReader(result))
            {
                while (true)
                {
                    string resultLine = stringReader.ReadLine();
                    if (resultLine == null)
                        break;

                    int pos = resultLine.IndexOf("starts at address ");
                    if (pos > 0)
                    {
                        ulong address;
                        string addrStr = resultLine.Substring(pos + 18);
                        if (MICommandFactory.SpanNextAddr(addrStr, out address) != null)
                        {
                            addresses.Add(address);
                        }
                    }
                }
            }
            return addresses;
        }

        public override Task EnableTargetAsyncOption()
        {
            // Linux attach TODO: GDB will fail this command when attaching. This is worked around
            // by using signals for that case.
            return _debugger.CmdAsync("-gdb-set target-async on", ResultClass.None);
        }

        public override async Task Terminate()
        {
            // Although the mi documentation states that the correct command to terminate is -exec-abort
            // that isn't actually supported by gdb. 
            await _debugger.CmdAsync("kill", ResultClass.None);
        }
        private static string TypeBySize(uint size)
        {
            switch (size)
            {
                case 1:
                    return "char";
                case 2:
                    return "short";
                case 4:
                    return "int";
                case 8:
                    return "double";
                default:
                    throw new ArgumentException("size");
            }
        }

        public override async Task<Results> BreakWatch(string address, uint size, ResultClass resultClass = ResultClass.done)
        {
            string cmd = string.Format(CultureInfo.InvariantCulture, "-break-watch *({0}*)({1})", TypeBySize(size), address);
            return await _debugger.CmdAsync(cmd.ToString(), resultClass);
        }

        public override bool SupportsDataBreakpoints { get { return true; } }
    }
}
