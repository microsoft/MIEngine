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