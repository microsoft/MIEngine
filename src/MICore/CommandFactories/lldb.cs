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

        public async override Task<StringBuilder> BuildBreakInsert(string condition, bool enabled)
        {
            const string pendingFlag = "-f ";

            StringBuilder cmd = new StringBuilder("-break-insert ");
            cmd.Append(pendingFlag);

            // LLDB's 3.5 use of the pending flag requires an optional parameter or else it fails.
            // We will use "on" for now. 
            if (await RequiresOnKeywordForBreakInsert())
            {
                const string pendingFlagParameter = "on ";
                cmd.Append(pendingFlagParameter);
            }

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
            string quoteEscapedExpression = EscapeQuotes(expression);
            string command = string.Format("-var-create - - \"{0}\"", quoteEscapedExpression);  // use '-' to indicate that "--frame" should be used to determine the frame number
            Results results = await ThreadFrameCmdAsync(command, resultClass, threadId, frameLevel);

            return results;
        }
        
        public override async Task<Results> VarListChildren(string variableReference, enum_DEBUGPROP_INFO_FLAGS dwFlags, ResultClass resultClass = ResultClass.done)
        {
            // This override is necessary because lldb treats any object with children as not a simple object.
            // This prevents char* and char** from returning a value when queried by -var-list-children
            // Limit the number of children expanded to 1000 in case memory is uninitialized
            string command = string.Format("-var-list-children --all-values \"{0}\" 0 1000", variableReference);
            Results results = await _debugger.CmdAsync(command, resultClass);

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

        public override string GetTargetArchitectureCommand()
        {
            return "platform status";
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

        public override string GetSetEnvironmentVariableCommand(string name, string value)
        {
            // LLDB requires surrounding values with quotes if the values contain spaces.
            // This is because LLDB allows setting multiple environment variables with one command,
            // using a space as the delimiter between variables.
            return string.Format(CultureInfo.InvariantCulture, "settings set target.env-vars {0}=\"{1}\"", name, EscapeQuotes(value));
        }

        public override Task Signal(string sig)
        {
            throw new NotImplementedException("lldb signal command");
        }

        public override Task Catch(string name, bool onlyOnce = false, ResultClass resultClass = ResultClass.done)
        {
            throw new NotImplementedException("lldb catch command");
        }

        /// <summary>
        /// Assigns the value of an expression to a variable.
        /// Since LLDB only accepts assigning values to variables, the expression may need to be evaluated.
        /// However, since the result of evaluating an expression in LLDB can return some extra information:
        /// e.g., 'a' --> 97 'a'. We don't want to assign the value "97 'a'". Instead, we first try
        /// assigning what the user passed, only falling back to evaluation if the first assignment fails.
        /// </summary>
        public async override Task<string> VarAssign(string variableName, string expression, int threadId, uint frameLevel)
        {
            try
            {
                return await base.VarAssign(variableName, expression, threadId, frameLevel);
            }
            catch (UnexpectedMIResultException)
            {
                Results results = await VarCreate(expression, threadId, frameLevel, 0, ResultClass.done);
                string value = results.FindString("value");
                return await base.VarAssign(variableName, value, threadId, frameLevel);
            }
        }

        private bool? _requiresOnKeywordForBreakInsert;
        private const string OldLLDBMIVersionString = "lldb-350.99.0";

        // In LLDB 3.5, -break-insert -f requires a string before the actual method name.
        // We use a placeholder 'on' for this.
        // Later versions do not require the 'on' keyword.
        private async Task<bool> RequiresOnKeywordForBreakInsert()
        {
            if (!_requiresOnKeywordForBreakInsert.HasValue)
            {
                // Query for the version.
                string version = await Version();
                if (!string.IsNullOrWhiteSpace(version) && version.Trim().Equals(OldLLDBMIVersionString, StringComparison.Ordinal))
                {
                    _requiresOnKeywordForBreakInsert = true;
                }
                else
                {
                    _requiresOnKeywordForBreakInsert = false;
                }
            }

            return _requiresOnKeywordForBreakInsert.Value;
        }

        private async Task<string> Version()
        {
            // 'Version' does not require the debuggee to be stopped.
            return await _debugger.ConsoleCmdAsync("version", allowWhileRunning: true);
        }
    }
}
