// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Globalization;
using MICore;
using System.Threading.Tasks;
using System.Linq;

namespace Microsoft.MIDebugEngine
{
    public static class EngineUtils
    {
        internal static string AsAddr(ulong addr, bool is64bit)
        {
            string addrFormat = is64bit ? "x16" : "x8";
            return "0x" + addr.ToString(addrFormat, CultureInfo.InvariantCulture);
        }

        internal static string GetAddressDescription(DebuggedProcess proc, ulong ip)
        {
            string description = null;
            proc.WorkerThread.RunOperation(async () =>
            {
                description = await EngineUtils.GetAddressDescriptionAsync(proc, ip);
            }
            );

            return description;
        }

        internal static async Task<string> GetAddressDescriptionAsync(DebuggedProcess proc, ulong ip)
        {
            string location = null;
            IEnumerable<DisasmInstruction> instructions = await proc.Disassembly.FetchInstructions(ip, 1);
            if (instructions != null)
            {
                foreach (DisasmInstruction instruction in instructions)
                {
                    if (location == null && !String.IsNullOrEmpty(instruction.Symbol))
                    {
                        location = instruction.Symbol;
                        break;
                    }
                }
            }

            if (location == null)
            {
                string addrFormat = proc.Is64BitArch ? "x16" : "x8";
                location = ip.ToString(addrFormat, CultureInfo.InvariantCulture);
            }

            return location;
        }


        public static void CheckOk(int hr)
        {
            if (hr != 0)
            {
                throw new MIException(hr);
            }
        }

        public static void RequireOk(int hr)
        {
            if (hr != 0)
            {
                throw new InvalidOperationException();
            }
        }

        public static AD_PROCESS_ID GetProcessId(IDebugProcess2 process)
        {
            AD_PROCESS_ID[] pid = new AD_PROCESS_ID[1];
            EngineUtils.RequireOk(process.GetPhysicalProcessId(pid));
            return pid[0];
        }

        public static int UnexpectedException(Exception e)
        {
            Debug.Fail("Unexpected exception.");
            return Constants.RPC_E_SERVERFAULT;
        }

        internal static bool IsFlagSet(uint value, int flagValue)
        {
            return (value & flagValue) != 0;
        }

        internal static bool ProcIdEquals(AD_PROCESS_ID pid1, AD_PROCESS_ID pid2)
        {
            if (pid1.ProcessIdType != pid2.ProcessIdType)
            {
                return false;
            }
            else if (pid1.ProcessIdType == (int)enum_AD_PROCESS_ID.AD_PROCESS_ID_SYSTEM)
            {
                return pid1.dwProcessId == pid2.dwProcessId;
            }
            else
            {
                return pid1.guidProcessId == pid2.guidProcessId;
            }
        }


        /// <summary>
        /// This allows console commands to be sent through the eval channel via a '-exec ' or '`' preface
        /// </summary>
        /// <param name="command">raw command</param>
        /// <param name="strippedCommand">command stripped of the preface ('-exec ' or '`')</param>
        /// <returns>true if it is a console command</returns>
        internal static bool IsConsoleExecCmd(string command, out string prefix, out string strippedCommand)
        {
            prefix = string.Empty;
            strippedCommand = string.Empty;
            string execCommandString = "-exec ";
            if (command.StartsWith(execCommandString, StringComparison.Ordinal))
            {
                prefix = execCommandString;
                strippedCommand = command.Substring(execCommandString.Length);
                return true;
            }
            else if (command.Length > 0 && command[0] == '`')
            {
                prefix = "`";
                strippedCommand = command.Substring(1);
                return true;
            }
            return false;
        }

        //
        // The RegisterNameMap maps register names to logical group names. The architecture of 
        // the platform is described with all its varients. Any particular target may only contains a subset 
        // of the available registers.
        public class RegisterNameMap
        {
            private Entry[] _map;
            private struct Entry
            {
                public readonly string Name;
                public readonly bool IsRegex;
                public readonly string Group;
                public Entry(string name, bool isRegex, string group)
                {
                    Name = name;
                    IsRegex = isRegex;
                    Group = group;
                }
            };

            private static readonly Entry[] s_arm32Registers = new Entry[]
            {
                new Entry( "sp", false, "CPU"),
                new Entry( "lr", false, "CPU"),
                new Entry( "pc", false, "CPU"),
                new Entry( "cpsr", false, "CPU"),
                new Entry( "r[0-9]+", true, "CPU"),
                new Entry( "fpscr", false, "FPU"),
                new Entry( "f[0-9]+", true, "FPU"),
                new Entry( "s[0-9]+", true, "IEEE Single"),
                new Entry( "d[0-9]+", true, "IEEE Double"),
                new Entry( "q[0-9]+", true, "Vector"),
            };

            private static readonly Entry[] s_X86Registers = new Entry[]
            {
                new Entry( "rax", false, "CPU" ),
                new Entry( "rcx", false, "CPU" ),
                new Entry( "rdx", false, "CPU" ),
                new Entry( "rbx", false, "CPU" ),
                new Entry( "rsp", false, "CPU" ),
                new Entry( "rbp", false, "CPU" ),
                new Entry( "rsi", false, "CPU" ),
                new Entry( "rdi", false, "CPU" ),
                new Entry( "rip", false, "CPU" ),
                new Entry( "r[0-9]+$", true, "CPU" ),
                new Entry( "eax", false, "CPU" ),
                new Entry( "ecx", false, "CPU" ),
                new Entry( "edx", false, "CPU" ),
                new Entry( "ebx", false, "CPU" ),
                new Entry( "esp", false, "CPU" ),
                new Entry( "ebp", false, "CPU" ),
                new Entry( "esi", false, "CPU" ),
                new Entry( "edi", false, "CPU" ),
                new Entry( "eip", false, "CPU" ),
                new Entry( "eflags", false, "CPU" ),
                new Entry( "cs", false, "Segs" ),
                new Entry( "ss", false, "Segs" ),
                new Entry( "ds", false, "Segs" ),
                new Entry( "es", false, "Segs" ),
                new Entry( "fs", false, "Segs" ),
                new Entry( "gs", false, "Segs" ),
                new Entry( "st", true, "FPU" ),
                new Entry( "fctrl", false, "FPU" ),
                new Entry( "fstat", false, "FPU" ),
                new Entry( "ftag", false, "FPU" ),
                new Entry( "fiseg", false, "FPU" ),
                new Entry( "fioff", false, "FPU" ),
                new Entry( "foseg", false, "FPU" ),
                new Entry( "fooff", false, "FPU" ),
                new Entry( "fop", false, "FPU" ),
                new Entry( "mxcsr", false, "SSE" ),
                new Entry( "xmm[0-9]+", true, "SSE" ),
                new Entry( "ymm[0-9]+", true, "AVX" ),
                new Entry( "mm[0-7][0-7]", true, "AMD3DNow" ),
                new Entry( "mm[0-7]", true, "MMX" ),
            };

            private static readonly Entry[] s_allRegisters = new Entry[]
            {
                    new Entry( ".+", true, "CPU"),
            };

            public static RegisterNameMap Create(string[] registerNames)
            {
                // TODO: more robust mechanism for determining processor architecture
                RegisterNameMap map = new RegisterNameMap();
                if (registerNames.Contains("lr"))
                {
                    map._map = s_arm32Registers;
                }
                else if (registerNames.Contains("eax")) // x86 register set
                {
                    map._map = s_X86Registers;
                }
                else
                {
                    // report one global register set
                    map._map = s_allRegisters;
                }
                return map;
            }

            public string GetGroupName(string regName)
            {
                foreach (var e in _map)
                {
                    if (e.IsRegex)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(regName, e.Name))
                        {
                            return e.Group;
                        }
                    }
                    else if (e.Name == regName)
                    {
                        return e.Group;
                    }
                }
                return "Other Registers";
            }
        };

        internal static string GetExceptionDescription(Exception exception)
        {
            if (!ExceptionHelper.IsCorruptingException(exception))
            {
                return exception.Message;
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_CorruptingException, exception.GetType().FullName, exception.StackTrace);
            }
        }

        internal class SignalMap : Dictionary<string, uint>
        {
            private static SignalMap s_instance;
            private SignalMap()
            {
                this["SIGHUP"] = 1;
                this["SIGINT"] = 2;
                this["SIGQUIT"] = 3;
                this["SIGILL"] = 4;
                this["SIGTRAP"] = 5;
                this["SIGABRT"] = 6;
                this["SIGIOT"] = 6;
                this["SIGBUS"] = 7;
                this["SIGFPE"] = 8;
                this["SIGKILL"] = 9;
                this["SIGUSR1"] = 10;
                this["SIGSEGV"] = 11;
                this["SIGUSR2"] = 12;
                this["SIGPIPE"] = 13;
                this["SIGALRM"] = 14;
                this["SIGTERM"] = 15;
                this["SIGSTKFLT"] = 16;
                this["SIGCHLD"] = 17;
                this["SIGCONT"] = 18;
                this["SIGSTOP"] = 19;
                this["SIGTSTP"] = 20;
                this["SIGTTIN"] = 21;
                this["SIGTTOU"] = 22;
                this["SIGURG"] = 23;
                this["SIGXCPU"] = 24;
                this["SIGXFSZ"] = 25;
                this["SIGVTALRM"] = 26;
                this["SIGPROF"] = 27;
                this["SIGWINCH"] = 28;
                this["SIGIO"] = 29;
                this["SIGPOLL"] = 29;
                this["SIGPWR"] = 30;
                this["SIGSYS"] = 31;
                this["SIGUNUSED"] = 31;
            }
            public static SignalMap Instance
            {
                get
                {
                    if (s_instance == null)
                    {
                        s_instance = new SignalMap();
                    }
                    return s_instance;
                }
            }
        }
    }
}
