// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.IO;
using Microsoft.MIDebugEngine;
using System.Collections.Generic;
using MICore;

namespace Microsoft.MIDebugPackage
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidMIDebugPackagePkgString)]
#if LAB
    // Adding this dll location to VS probe path. Custom launcher types are referenced by Linux/Azure Sphere workloads. 
    [ProvideBindingPath]
#endif
    public sealed class MIDebugPackagePackage : Package, IOleCommandTarget
    {
        private IOleCommandTarget _packageCommandTarget;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public MIDebugPackagePackage()
        {
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            _packageCommandTarget = GetService(typeof(IOleCommandTarget)) as IOleCommandTarget;
        }
        #endregion

        int IOleCommandTarget.Exec(ref Guid cmdGroup, uint nCmdID, uint nCmdExecOpt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (cmdGroup == GuidList.guidMIDebugPackageCmdSet)
            {
                switch (nCmdID)
                {
                    case PkgCmdIDList.cmdidLaunchMIDebug:
                        return LaunchMIDebug(nCmdExecOpt, pvaIn, pvaOut);

                    case PkgCmdIDList.cmdidMIDebugExec:
                        return MIDebugExec(nCmdExecOpt, pvaIn, pvaOut);

                    case PkgCmdIDList.cmdidMIDebugLog:
                        return MIDebugLog(nCmdExecOpt, pvaIn, pvaOut);

                    default:
                        Debug.Fail("Unknown command id");
                        return VSConstants.E_NOTIMPL;
                }
            }

            return _packageCommandTarget.Exec(cmdGroup, nCmdID, nCmdExecOpt, pvaIn, pvaOut);
        }

        int IOleCommandTarget.QueryStatus(ref Guid cmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (cmdGroup == GuidList.guidMIDebugPackageCmdSet)
            {
                switch (prgCmds[0].cmdID)
                {
                    case PkgCmdIDList.cmdidLaunchMIDebug:
                    case PkgCmdIDList.cmdidMIDebugExec:
                    case PkgCmdIDList.cmdidMIDebugLog:
                        prgCmds[0].cmdf |= (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_INVISIBLE);
                        return VSConstants.S_OK;

                    default:
                        Debug.Fail("Unknown command id");
                        return VSConstants.E_NOTIMPL;
                }
            }

            return _packageCommandTarget.QueryStatus(ref cmdGroup, cCmds, prgCmds, pCmdText);
        }

        /// <summary>
        /// The syntax for the MIDebugLaunch command. Notes:
        /// ':' : The switch takes a value
        /// '!' : The value is required
        /// '(' ... ')' : Auto complete list for the switch (I don't know what is valid here except for 'd')
        /// d : A path
        /// </summary>
        private const string LaunchMIDebugCommandSyntax = "E,Executable:!(d) O,OptionsFile:!(d)";
        // NOTE: This must be in the same order as the syntax string
        private enum LaunchMIDebugCommandSwitchEnum
        {
            Executable,
            OptionsFile
        }

        private int LaunchMIDebug(uint nCmdExecOpt, IntPtr pvaIn, IntPtr pvaOut)
        {
            int hr;

            if (IsQueryParameterList(pvaIn, pvaOut, nCmdExecOpt))
            {
                Marshal.GetNativeVariantForObject("$ /switchdefs:\"" + LaunchMIDebugCommandSyntax + "\"", pvaOut);
                return VSConstants.S_OK;
            }

            string arguments;
            hr = EnsureString(pvaIn, out arguments);
            if (hr != VSConstants.S_OK)
                return hr;

            IVsParseCommandLine parseCommandLine = (IVsParseCommandLine)GetService(typeof(SVsParseCommandLine));
            hr = parseCommandLine.ParseCommandTail(arguments, iMaxParams: -1);
            if (ErrorHandler.Failed(hr))
                return hr;

            hr = parseCommandLine.HasParams();
            if (ErrorHandler.Failed(hr))
                return hr;
            if (hr == VSConstants.S_OK || parseCommandLine.HasSwitches() != VSConstants.S_OK)
            {
                string message = string.Concat("Unexpected syntax for MIDebugLaunch command. Expected:\n",
                    "Debug.MIDebugLaunch /Executable:<path_or_logical_name> /OptionsFile:<path>");
                throw new ApplicationException(message);
            }

            hr = parseCommandLine.EvaluateSwitches(LaunchMIDebugCommandSyntax);
            if (ErrorHandler.Failed(hr))
                return hr;

            string executable;
            if (parseCommandLine.GetSwitchValue((int)LaunchMIDebugCommandSwitchEnum.Executable, out executable) != VSConstants.S_OK ||
                string.IsNullOrWhiteSpace(executable))
            {
                throw new ArgumentException("Executable must be specified");
            }

            bool checkExecutableExists = false;
            string options = string.Empty;

            string optionsFilePath;
            if (parseCommandLine.GetSwitchValue((int)LaunchMIDebugCommandSwitchEnum.OptionsFile, out optionsFilePath) == 0)
            {
                // When using the options file, we want to allow the executable to be just a logical name, but if
                // one enters a real path, we should make sure it isn't mistyped. If the path contains a slash, we assume it 
                // is meant to be a real path so enforce that it exists
                checkExecutableExists = (executable.IndexOf('\\') >= 0);

                if (string.IsNullOrWhiteSpace(optionsFilePath))
                    throw new ArgumentException("Value expected for '/OptionsFile' option");

                if (!File.Exists(optionsFilePath))
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Options file '{0}' does not exist", optionsFilePath));

                options = File.ReadAllText(optionsFilePath);
            }

            if (checkExecutableExists)
            {
                if (!File.Exists(executable))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Executable '{0}' does not exist", executable));
                }

                executable = Path.GetFullPath(executable);
            }

            LaunchDebugTarget(executable, options);

            return 0;
        }

        private int MIDebugExec(uint nCmdExecOpt, IntPtr pvaIn, IntPtr pvaOut)
        {
            int hr;

            if (IsQueryParameterList(pvaIn, pvaOut, nCmdExecOpt))
            {
                Marshal.GetNativeVariantForObject("$", pvaOut);
                return VSConstants.S_OK;
            }

            string arguments;
            hr = EnsureString(pvaIn, out arguments);
            if (hr != VSConstants.S_OK)
                return hr;

            if (string.IsNullOrWhiteSpace(arguments))
                throw new ArgumentException("Expected an MI command to execute (ex: Debug.MIDebugExec info sharedlibrary)");

            MIDebugExecAsync(arguments);

            return VSConstants.S_OK;
        }


        /// <summary>
        /// The syntax for the MIDebugLog command. Notes:
        /// ':' : The switch takes a value
        /// '!' : The value is required
        /// '(' ... ')' : Auto complete list for the switch (I don't know what is valid here except for 'd')
        /// d : A path
        /// </summary>
        private const string LogMIDebugCommandSyntax = "O,On:(d) OutputWindow Off";
        private enum LogMIDebugCommandSwitchEnum
        {
            On,
            OutputWindow,
            Off
        }


        private int MIDebugLog(uint nCmdLogOpt, IntPtr pvaIn, IntPtr pvaOut)
        {
            int hr;

            if (IsQueryParameterList(pvaIn, pvaOut, nCmdLogOpt))
            {
                Marshal.GetNativeVariantForObject("$ /switchdefs:\"" + LogMIDebugCommandSyntax + "\"", pvaOut);
                return VSConstants.S_OK;
            }

            string arguments;
            hr = EnsureString(pvaIn, out arguments);
            if (hr != VSConstants.S_OK)
                return hr;

            IVsParseCommandLine parseCommandLine = (IVsParseCommandLine)GetService(typeof(SVsParseCommandLine));
            hr = parseCommandLine.ParseCommandTail(arguments, iMaxParams: -1);
            if (ErrorHandler.Failed(hr))
                return hr;

            hr = parseCommandLine.HasParams();
            if (ErrorHandler.Failed(hr))
                return hr;
            if (hr == VSConstants.S_OK || parseCommandLine.HasSwitches() != VSConstants.S_OK)
            {
                string message = string.Concat("Unexpected syntax for MIDebugLaunch command. Expected:\n",
                    "Debug.MIDebugLog [/On:<optional_path> [/OutputWindow] | /Off]");
                throw new ApplicationException(message);
            }

            hr = parseCommandLine.EvaluateSwitches(LogMIDebugCommandSyntax);
            if (ErrorHandler.Failed(hr))
                return hr;

            string logPath = string.Empty;
            bool logToOutput = false;
            if (parseCommandLine.GetSwitchValue((int)LogMIDebugCommandSwitchEnum.On, out logPath) == VSConstants.S_OK)
            {
                logToOutput = parseCommandLine.IsSwitchPresent((int)LogMIDebugCommandSwitchEnum.OutputWindow) == VSConstants.S_OK;
                if (parseCommandLine.IsSwitchPresent((int)LogMIDebugCommandSwitchEnum.Off) == VSConstants.S_OK)
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "/On and /Off cannot both appear on command line"));
                }
                if (!logToOutput && string.IsNullOrEmpty(logPath))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Must specify a log file (/On:<path>) or /OutputWindow"));
                }
            }
            else if (parseCommandLine.IsSwitchPresent((int)LogMIDebugCommandSwitchEnum.Off) != VSConstants.S_OK)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "One of /On or /Off must be present on command line"));
            }

            EnableLogging(logToOutput, logPath);

            return 0;
        }

        private async void MIDebugExecAsync(string command)
        {
            var commandWindow = (IVsCommandWindow)GetService(typeof(SVsCommandWindow));
            bool atBreak = false;
            var debugger = GetService(typeof(SVsShellDebugger)) as IVsDebugger;
            if (debugger != null)
            {
                DBGMODE[] mode = new DBGMODE[1];
                if (debugger.GetMode(mode) == MIDebugEngine.Constants.S_OK)
                {
                    atBreak = mode[0] == DBGMODE.DBGMODE_Break;
                }
            }

            string results = null;

            try
            {
                if (atBreak)
                {
                    commandWindow.ExecuteCommand(String.Format(CultureInfo.InvariantCulture, "Debug.EvaluateStatement -exec {0}", command));
                }
                else
                {
                    results = await MIDebugCommandDispatcher.ExecuteCommand(command);
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    e = e.InnerException;

                UnexpectedMIResultException miException = e as UnexpectedMIResultException;
                string message;
                if (miException != null && miException.MIError != null)
                    message = miException.MIError;
                else
                    message = e.Message;

                commandWindow.Print(string.Format(CultureInfo.CurrentCulture, "Error: {0}\r\n", message));
                return;
            }

            if (results != null && results.Length > 0)
            {
                // Make sure that we are printing whole lines
                if (!results.EndsWith("\n", StringComparison.Ordinal) && !results.EndsWith("\r\n", StringComparison.Ordinal))
                {
                    results = results + "\n";
                }

                commandWindow.Print(results);
            }
        }

        private void LaunchDebugTarget(string filePath, string options)
        {
            IVsDebugger4 debugger = (IVsDebugger4)GetService(typeof(IVsDebugger));
            VsDebugTargetInfo4[] debugTargets = new VsDebugTargetInfo4[1];
            debugTargets[0].dlo = (uint)DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;
            debugTargets[0].bstrExe = filePath;
            debugTargets[0].bstrOptions = options;
            debugTargets[0].guidLaunchDebugEngine = Microsoft.MIDebugEngine.EngineConstants.EngineId;
            VsDebugTargetProcessInfo[] processInfo = new VsDebugTargetProcessInfo[debugTargets.Length];

            debugger.LaunchDebugTargets4(1, debugTargets, processInfo);
        }

        private void EnableLogging(bool sendToOutputWindow, string logFile)
        {
            try
            {
                MIDebugCommandDispatcher.EnableLogging(sendToOutputWindow, logFile);
            }
            catch (Exception e)
            {
                var commandWindow = (IVsCommandWindow)GetService(typeof(SVsCommandWindow));
                commandWindow.Print(string.Format(CultureInfo.CurrentCulture, "Error: {0}\r\n", e.Message));
            }
        }

        static private int EnsureString(IntPtr pvaIn, out string arguments)
        {
            arguments = null;
            if (pvaIn == IntPtr.Zero)
            {
                // No arguments.
                return VSConstants.E_INVALIDARG;
            }

            object vaInObject = Marshal.GetObjectForNativeVariant(pvaIn);
            if (vaInObject == null || vaInObject.GetType() != typeof(string))
            {
                return VSConstants.E_INVALIDARG;
            }

            arguments = vaInObject as string;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Used to determine if the shell is querying for the parameter list.
        /// </summary>
        static private bool IsQueryParameterList(System.IntPtr pvaIn, System.IntPtr pvaOut, uint nCmdexecopt)
        {
            ushort lo = (ushort)(nCmdexecopt & (uint)0xffff);
            ushort hi = (ushort)(nCmdexecopt >> 16);
            if (lo == (ushort)OLECMDEXECOPT.OLECMDEXECOPT_SHOWHELP)
            {
                if (hi == VsMenus.VSCmdOptQueryParameterList)
                {
                    if (pvaOut != IntPtr.Zero)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
