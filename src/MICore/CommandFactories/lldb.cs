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
using Microsoft.VisualStudio.Debugger.Interop;

namespace MICore
{
    internal class LlldbMICommandFactory : MICommandFactory
    {
        public override string Name
        {
            get { return "LLDB"; }
        }

        public override bool SupportsStopOnDynamicLibLoad()
        {
            return false;
        }

        public override bool SupportsChildProcessDebugging()
        {
            return false;
        }

        public override bool AllowCommandsWhileRunning()
        {
            return false;
        }

        public override bool SupportsFrameFormatting
        {
            get
            {
                // LLDB already adds the parameter list to the function name when a -stack-list-frame or -stack-info-frame
                // call is made so setting this to true so we don't append the parameters on stack frames.
                return true;
            }
        }

        protected override StringBuilder BuildBreakInsert(string condition, bool enabled)
        {
            // LLDB's use of the pending flag requires an optional parameter or else it fails.
            // We will use "on" for now. 
            // TODO: Fix this on LLDB-MI's side
            string pendingFlag = "-f on ";

            StringBuilder cmd = new StringBuilder("-break-insert ");
            cmd.Append(pendingFlag);

            if (condition != null)
            {
                cmd.Append("-c \"");
                cmd.Append(condition);
                cmd.Append("\" ");
            }
            if (!enabled)
            {
                cmd.Append("-d ");
            }
            return cmd;
        }

        public override async Task<Results> VarCreate(string expression, int threadId, uint frameLevel, enum_EVALFLAGS dwFlags, ResultClass resultClass = ResultClass.done)
        {
            string command = string.Format("-var-create - - \"{0}\"", expression);  // use '-' to indicate that "--frame" should be used to determine the frame number
            Results results = await ThreadFrameCmdAsync(command, resultClass, threadId, frameLevel);

            return results;
        }

        protected override async Task<Results> ThreadFrameCmdAsync(string command, ResultClass exepctedResultClass, int threadId, uint frameLevel)
        {
            string threadFrameCommand = string.Format(@"{0} --thread {1} --frame {2}", command, threadId, frameLevel);

            return await _debugger.CmdAsync(threadFrameCommand, exepctedResultClass);
        }

        protected override async Task<Results> ThreadCmdAsync(string command, ResultClass expectedResultClass, int threadId)
        {
            string threadCommand = string.Format(@"{0} --thread {1}", command, threadId);

            return await _debugger.CmdAsync(threadCommand, expectedResultClass);
        }

        public override Task<List<ulong>> StartAddressesForLine(string file, uint line)
        {
            return Task.FromResult<List<ulong>>(null);
        }

        public override Task EnableTargetAsyncOption()
        {
            // lldb-mi doesn't support target-async mode, and doesn't seem to need to
            return Task.FromResult((object)null);
        }

        public override async Task<TargetArchitecture> GetTargetArchitecture()
        {
            string cmd = "platform status";
            var result = await _debugger.ConsoleCmdAsync(cmd);
            using (StringReader stringReader = new StringReader(result))
            {
                while (true)
                {
                    string resultLine = stringReader.ReadLine();
                    if (resultLine == null)
                        break;

                    if (resultLine.IndexOf("Triple:", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (resultLine.IndexOf("x86_64", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return TargetArchitecture.X64;
                        }
                        else if (resultLine.IndexOf("x86", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return TargetArchitecture.X86;
                        }
                        else if (resultLine.IndexOf("aarch64", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return TargetArchitecture.ARM64;
                        }
                        else if (resultLine.IndexOf("arm", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return TargetArchitecture.ARM;
                        }
                        break;
                    }
                }
            }
            return TargetArchitecture.Unknown;
        }

        public override Task Signal(string sig)
        {
            throw new NotImplementedException("lldb signal command");
        }

        public override Task Catch(string name, bool onlyOnce = false, ResultClass resultClass = ResultClass.done)
        {
            throw new NotImplementedException("lldb catch command");
        }
    }
}