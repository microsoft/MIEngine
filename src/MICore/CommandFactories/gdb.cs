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
using Microsoft.DebugEngineHost;

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
            // If current threadId is changed, reset _currentFrameLevel
            _currentFrameLevel = 0;
        }

        public override int CurrentThread { get { return _currentThreadId; } }

        public override bool SupportsStopOnDynamicLibLoad()
        {
            return true;
        }

        public override bool SupportsChildProcessDebugging()
        {
            return true;
        }

        public override bool AllowCommandsWhileRunning()
        {
            return false;
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
                throw new ArgumentNullException(nameof(lockToken));
            }

            if (threadId != _currentThreadId)
            {
                string command = string.Format(CultureInfo.InvariantCulture, "-thread-select {0}", threadId);
                await _debugger.ExclusiveCmdAsync(command, ResultClass.done, lockToken);
                _currentThreadId = threadId;
                _currentFrameLevel = 0;
            }
        }

        private async Task StackSelectFrame(uint frameLevel, ExclusiveLockToken lockToken)
        {
            if (ExclusiveLockToken.IsNullOrClosed(lockToken))
            {
                throw new ArgumentNullException(nameof(lockToken));
            }

            if (frameLevel != _currentFrameLevel)
            {
                string command = string.Format(CultureInfo.InvariantCulture, "-stack-select-frame {0}", frameLevel);
                await _debugger.ExclusiveCmdAsync(command, ResultClass.done, lockToken);
                _currentFrameLevel = frameLevel;
            }
        }

        public override async Task<Results> ThreadInfo(uint? threadId = null)
        {
            Results results = await base.ThreadInfo(threadId);
            if (results.ResultClass == ResultClass.done && results.Contains("current-thread-id"))
            {
                _currentThreadId = results.FindInt("current-thread-id");
            }
            return results;
        }

        public override async Task<List<ulong>> StartAddressesForLine(string file, uint line)
        {
            string cmd = "info line " + file + ":" + line;
            var result = await _debugger.ConsoleCmdAsync(cmd, allowWhileRunning: false);
            List<ulong> addresses = new List<ulong>();
            using (StringReader stringReader = new StringReader(result))
            {
                while (true)
                {
                    string resultLine = stringReader.ReadLine();
                    if (resultLine == null)
                        break;

                    int pos = resultLine.IndexOf("starts at address ", StringComparison.Ordinal);
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

        public override async Task EnableTargetAsyncOption()
        {
            // Linux attach TODO: GDB will fail this command when attaching. This is worked around
            // by using signals for that case.
            Results result = await _debugger.CmdAsync("-gdb-set mi-async on", ResultClass.None);

            // 'set mi-async on' will error on older versions of gdb (older than 11.x)
            // Try enabling with the older 'target-async' keyword.
            if (result.ResultClass == ResultClass.error)
            {
                await _debugger.CmdAsync("-gdb-set target-async on", ResultClass.None);
            }
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
                    throw new ArgumentException(null, nameof(size));
            }
        }

        public override async Task<Results> BreakWatch(string address, uint size, ResultClass resultClass = ResultClass.done)
        {
            string cmd = string.Format(CultureInfo.InvariantCulture, "-break-watch *({0}*)({1})", TypeBySize(size), address);
            return await _debugger.CmdAsync(cmd.ToString(), resultClass);
        }

        public override bool SupportsDataBreakpoints { get { return true; } }

        public override string GetTargetArchitectureCommand()
        {
            return "show architecture";
        }

        public override TargetArchitecture ParseTargetArchitectureResult(string result)
        {
            using (StringReader stringReader = new StringReader(result))
            {
                while (true)
                {
                    string resultLine = stringReader.ReadLine();
                    if (resultLine == null)
                        break;

                    if (resultLine.IndexOf("x86-64", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return TargetArchitecture.X64;
                    }
                    else if (resultLine.IndexOf("i386", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return TargetArchitecture.X86;
                    }
                    else if (resultLine.IndexOf("arm64", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return TargetArchitecture.ARM64;
                    }
                    else if (resultLine.IndexOf("aarch64", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return TargetArchitecture.ARM64;
                    }
                    else if (resultLine.IndexOf("arm", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return TargetArchitecture.ARM;
                    }
                }
            }
            return TargetArchitecture.Unknown;
        }

        public override string GetSetEnvironmentVariableCommand(string name, string value)
        {
            return string.Format(CultureInfo.InvariantCulture, "set env {0} {1}", name, value);
        }

        public override async Task Signal(string sig)
        {
            string command = String.Format(CultureInfo.InvariantCulture, "-interpreter-exec console \"signal {0}\"", sig);
            await _debugger.CmdAsync(command, ResultClass.running);
        }

        public override async Task Catch(string name, bool onlyOnce = false, ResultClass resultClass = ResultClass.done)
        {
            string command = onlyOnce ? "tcatch " : "catch ";
            await _debugger.ConsoleCmdAsync(command + name, allowWhileRunning: false);
        }

        public override async Task<string[]> AutoComplete(string command, int threadId, uint frameLevel)
        {
            command = "-complete \"" + command + "\"";
            Results res;
            if (threadId == -1)
                res = await _debugger.CmdAsync(command, ResultClass.done);
            else
                res = await ThreadFrameCmdAsync(command, ResultClass.done, threadId, frameLevel);

            var matchlist = res.Find<ValueListValue>("matches");

            if (int.Parse(res.FindString("max_completions_reached"), CultureInfo.InvariantCulture) != 0)
                _debugger.Logger.WriteLine(LogLevel.Verbose, "We reached max-completions!");

            return matchlist?.AsStrings;
        }

        public override IEnumerable<Guid> GetSupportedExceptionCategories()
        {
            const string CppExceptionCategoryString = "{3A12D0B7-C26C-11D0-B442-00A0244A1DD2}";
            return new Guid[] { new Guid(CppExceptionCategoryString) };
        }

        public override async Task<IEnumerable<long>> SetExceptionBreakpoints(Guid exceptionCategory, IEnumerable<string> exceptionNames, ExceptionBreakpointStates exceptionBreakpointStates)
        {
            string command;
            Results result;
            List<long> breakpointNumbers = new List<long>();

            if (exceptionNames == null) // set breakpoint for all exceptions in exceptionCategory
            {
                command = "-catch-throw";
                result = await _debugger.CmdAsync(command, ResultClass.None);
                switch (result.ResultClass)
                {
                    case ResultClass.done:
                        var breakpointNumber = result.Find("bkpt").FindUint("number");
                        breakpointNumbers.Add(breakpointNumber);
                        break;
                    case ResultClass.error:
                    default:
                        throw new NotSupportedException();
                }
            }
            else // set breakpoint for each exceptionName in exceptionNames
            {
                command = "-catch-throw -r \\b";
                foreach (string exceptionName in exceptionNames)
                {
                    result = await _debugger.CmdAsync(command + exceptionName + "\\b", ResultClass.None);
                    switch (result.ResultClass)
                    {
                        case ResultClass.done:
                            var breakpointNumber = result.Find("bkpt").FindUint("number");
                            breakpointNumbers.Add(breakpointNumber);
                            break;
                        case ResultClass.error:
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            return breakpointNumbers;
        }

        public override async Task RemoveExceptionBreakpoint(Guid exceptionCategory, IEnumerable<long> exceptionBreakpoints)
        {
            foreach (long breakpointNumber in exceptionBreakpoints)
            {
                await BreakDelete(breakpointNumber.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
