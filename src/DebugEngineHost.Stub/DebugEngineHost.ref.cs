// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;


////////////////////////////////////////////////////////////////////////////////////////
// FILE SUMMARY
//
// This file defines the contract for the Microsoft.DebugEngineHost assembly. The
// Microsoft.DebugEngineHost provides services to a debug engine. There are two
// different implementations of this contract -- one for running the engine in Visual Studio,
// and one for running in VS Code.
//


namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Enumeration of Host User Interfaces that an engine can be run from.
    /// This must be kept in sync with all DebugEngineHost implentations
    /// </summary>
    public enum HostUIIdentifier
    {
        /// <summary>
        /// Visual Studio IDE
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        VSIDE = 0,
        /// <summary>
        /// Visual Studio Code
        /// </summary>
        VSCode = 1,
        /// <summary>
        /// Xamarin Studio
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        XamarinStudio = 2
    }

    /// <summary>
    /// Static class which provides the initialization method for
    /// Microsoft.DebugEngineHost.
    /// </summary>
    public static class Host
    {
        /// <summary>
        /// Called by a debug engine to ensure that the main thread is initialized.
        /// </summary>
        public static void EnsureMainThreadInitialized()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called by a debug engine to determine which UI is using it.
        /// </summary>
        /// <returns></returns>
        public static HostUIIdentifier GetHostUIIdentifier()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Abstraction over a named section within the HostConfigurationStore. This provides
    /// the ability to enumerate values within the section.
    /// </summary>
    public sealed class HostConfigurationSection : IDisposable
    {
        private HostConfigurationSection()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Releases any resources held by the HostConfigurationSection.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Obtains the value of the specified valueName
        /// </summary>
        /// <param name="valueName">Name of the value to obtain</param>
        /// <returns>[Optional] null if the value doesn't exist, otherwise the value
        /// </returns>
        public object GetValue(string valueName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Enumerates the names of all the values defined in this section
        /// </summary>
        /// <returns>Enumerator of strings</returns>
        public IEnumerable<string> GetValueNames()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Provides access to settings for the engine
    /// </summary>
    public sealed class HostConfigurationStore
    {
        /// <summary>
        /// Constructs a new HostConfigurationStore object. This API should generally be
        /// called from an engine's implementation of IDebugEngine2.SetRegistryRoot.
        /// </summary>
        /// <param name="registryRoot">registryRoot value provided in SetRegistryRoot.
        /// In Visual Studio, this will be something like 'Software\\Microsoft\\VisualStudio\\14.0'.
        /// In VS Code this will not really be a registry value but rather a key used to
        /// find the right configuration file.</param>
        public HostConfigurationStore(string registryRoot)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the Guid of the engine being hosted. This should only be set once for each HostConfigurationStore instance.
        /// </summary>
        /// <param name="value">The new engine GUID to set</param>
        public void SetEngineGuid(Guid value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Provides the registry root string. This is NOT supported in VS Code, and this property may eventually be removed.
        /// </summary>
        public string RegistryRoot
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Reads the specified engine metric.
        /// </summary>
        /// <param name="metric">The metric to read.</param>
        /// <returns>[Optional] value of the metric. Null if the metric is not defined.</returns>
        public object GetEngineMetric(string metric)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obtains exception settings for the specified exception category.
        /// </summary>
        /// <param name="categoryId">The GUID used to identify the exception category</param>
        /// <param name="categoryConfigSection">The configuration section where exception values can be obtained.</param>
        /// <param name="categoryName">The name of the exception category.</param>
        public void GetExceptionCategorySettings(Guid categoryId, out HostConfigurationSection categoryConfigSection, out string categoryName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Read the debugger setting
        ///
        /// In VS, this is wired up to read setting value from RegistryRoot\\Debugger\\
        /// </summary>
        /// <returns>value of the setting</returns>
        public T GetDebuggerConfigurationSetting<T>(string settingName, T defaultValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Load the launcher that understands these options
        /// </summary>
        /// <param name="launcherTypeName">launch options type name</param>
        /// <returns></returns>
        public object GetCustomLauncher(string launcherTypeName)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Level of logging used for HostLogChannel
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Logs that contain the most detailed messages.
        /// These messages may contain sensitive application data.
        /// These messages are disabled by default and should never be enabled in a production environment.
        /// </summary>
        Trace,
        /// <summary>
        /// Logs that are used for interactive investigation during development.
        /// These logs should primarily contain information useful for debugging and have no long-term value.
        /// </summary>
        Debug,
        /// <summary>
        /// Logs that track the general flow of the application.
        /// These logs should have long-term value.
        /// </summary>
        Information,
        /// <summary>
        /// Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the application execution to stop.
        /// </summary>
        Warning,
        /// <summary>
        /// Logs that highlight when the current flow of execution is stopped due to a failure.
        /// These should indicate a failure in the current activity, not an application-wide failure.
        /// </summary>
        Error,
        /// <summary>
        /// Logs that describe an unrecoverable application or system crash, or a catastrophic failure that requires immediate attention.
        /// </summary>
        Critical,
        /// <summary>
        /// Not used for writing log messages.
        /// Specifies that a logging category should not write any messages.
        /// </summary>
        None
    }

    /// <summary>
    /// The channel used for logging messages.
    /// Channels are used if there are multiple types of logs,
    /// e.g. Engine logs and Natvis logs
    /// </summary>
    public class HostLogChannel
    {
        /// <summary>
        /// Writes the given message with a newline to the log channel.
        /// </summary>
        /// <param name="level">The level of the log</param>
        /// <param name="message">The message string to send.</param>
        public void WriteLine(LogLevel level, string message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes the given formatted message with the additional values with a newline to the log channel.
        /// </summary>
        /// <param name="level">The level of the log</param>
        /// <param name="format">Format to use.</param>
        /// <param name="values">Values to use within the provided format.</param>
        public void WriteLine(LogLevel level, string format, params object[] values)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// If the log is implemented as a file, this flushes the file.
        /// </summary>
        public void Flush()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// If the log is implemented as a file, this closes the file.
        /// </summary>
        public void Close()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class HostLogger
    {
        // EnableNatvisLogger is only used in OpenDebugAD7

        /// <summary>
        /// Enables engine logging if not already enabled.
        /// </summary>
        /// <param name="callback">The callback to use to send the engine log.</param>
        /// <param name="level">The level of the log to filter the channel on.</param>
        public static void EnableHostLogging(Action<string> callback, LogLevel level = LogLevel.Information)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the log file to write to.
        /// </summary>
        /// <param name="logFile">The file to write engine logs to.</param>
        public static void SetEngineLogFile(string logFile)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets the engine log channel created by 'EnableHostLogging'
        /// </summary>
        /// <returns></returns>
        public static HostLogChannel GetEngineLogChannel()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the Natvis log channel if its been created.
        /// </summary>
        /// <returns></returns>
        public static HostLogChannel GetNatvisLogChannel()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Reset()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Provides support for loading dependent assemblies using information from the configuration store.
    /// </summary>
    public static class HostLoader
    {
        /// <summary>
        /// Looks up the specified CLSID in the VS registry and loads it
        /// </summary>
        /// <param name="configStore">Registry root to lookup the type</param>
        /// <param name="clsid">CLSID to CoCreate</param>
        /// <returns>[Optional] loaded object. Null if the type is not registered, or points to a type that doesn't exist</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Co")]
        public static object VsCoCreateManagedObject(HostConfigurationStore configStore, Guid clsid)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This class provides marshalling helper methods to a debug engine.
    ///
    /// When run in Visual Studio, these methods deal with COM marshalling.
    ///
    /// When run in Visual Studio code, these methods are stubs to allow the AD7 API to function without COM.
    /// </summary>
    public static class HostMarshal
    {
        /// <summary>
        /// Registers the specified code context if it isn't already registered and returns an IntPtr that can be
        /// used by the host to get back to the object.
        /// </summary>
        /// <param name="codeContext">Object to register</param>
        /// <returns>In VS, the IntPtr to a native COM object which can be returned to VS. In VS Code, an identifier
        /// that allows VS Code to get back to the object.</returns>
        public static IntPtr RegisterCodeContext(IDebugCodeContext2 codeContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obtains a document position interface given the specified IntPtr of the document position.
        /// </summary>
        /// <param name="documentPositionId">In VS, the IUnknown pointer to QI for a document position. In VS Code,
        /// the identifier for the document position</param>
        /// <returns>Document position object</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ForInt")]
        public static IDebugDocumentPosition2 GetDocumentPositionForIntPtr(IntPtr documentPositionId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obtains a function position interface given the specified IntPtr of the location.
        /// </summary>
        /// <param name="locationId">In VS, the IUnknown pointer to QI for a function position. In VS Code,
        /// the identifier for the function position</param>
        /// <returns>Function position object</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ForInt")]
        public static IDebugFunctionPosition2 GetDebugFunctionPositionForIntPtr(IntPtr locationId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obtain the string expression from the bpLocation union for a BPLT_DATA_STRING breakpoint.
        /// </summary>
        /// <param name="stringId"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ForInt")]
        public static string GetDataBreakpointStringForIntPtr(IntPtr stringId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return the string form of the address of a bound data breakpoint
        /// </summary>
        /// <param name="address">address string</param>
        /// <returns>IntPtr to a BSTR which can be returned to VS.</returns>
        public static IntPtr GetIntPtrForDataBreakpointAddress(string address)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obtains a code context interface given the specified IntPtr of the location.
        /// </summary>
        /// <param name="contextId">In VS, the IUnknown pointer to QI for a code context. In VS Code,
        /// the identifier for the code context</param>
        /// <returns>code context object</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ForInt")]
        public static IDebugCodeContext2 GetDebugCodeContextForIntPtr(IntPtr contextId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obtains an event callback interface that can be used to send events on any threads
        /// </summary>
        /// <param name="ad7Callback">The underlying event call back which was obtained from the port</param>
        /// <returns>In VS, a thread-safe wrapper on top of the underlying SDM event callback which allows
        /// sending events on any thread. In VS Code, this just returns the provided ad7Callback. </returns>
        public static IDebugEventCallback2 GetThreadSafeEventCallback(IDebugEventCallback2 ad7Callback)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In VS, calls Marshal.Release on IUnknown to derefcount when we are done with object.
        /// </summary>
        /// <param name="unknownId">In VS, the IUnknown</param>
        /// <returns>In VS, the result from calling Marshal.Release</returns>
        public static int Release(IntPtr unknownId)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Provides interactions with the host's source workspace to locate and load any natvis files
    /// in the project.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Natvis")]
    public static class HostNatvisProject
    {
        /// <summary>
        /// Delegate which is fired to process a natvis file.
        /// </summary>
        /// <param name="path"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Natvis")]
        public delegate void NatvisLoader(string path);

        /// <summary>
        /// Searches the solution and VSIXs for natvis files, invoking the loader on any which are found.
        /// </summary>
        /// <param name="loader">Natvis loader method to invoke</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Natvis")]
        public static void FindNatvis(NatvisLoader loader)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return the solution's root directory, null if no solution
        /// </summary>
        public static string FindSolutionRoot()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Provides interactions with the host's debugger
    /// </summary>
    public static class HostDebugger
    {
        /// <summary>
        /// Ask the host to async spin up a new instance of the debug engine and go through the launch sequence using the specified options
        /// </summary>
        public static void StartDebugChildProcess(string filePath, string options, Guid engineId)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Provides ability to launch in a VS Code protocol supported terminal
    /// </summary>
    public static class HostRunInTerminal
    {
        /// <summary>
        /// Checks if RunInTerminal is available
        /// </summary>
        /// <returns></returns>
        public static bool IsRunInTerminalAvailable()
        {
            return false;
        }

        /// <summary>
        /// Send command to VS Code to run the commandArgs in its version of a terminal window
        /// </summary>
        /// <param name="title">Title for the terminal window</param>
        /// <param name="workingDirectory">Working directory to start in the terminal window</param>
        /// <param name="useExternalConsole">True: External Console, False: Integrated Terminal</param>
        /// <param name="commandArgs">the list of commands, in order, to run.</param>
        /// <param name="environmentVariables">Environment Variables</param>
        /// <param name="success">Success callback. In some cases, id contains the pid of the process that was started.</param>
        /// <param name="failure">Failure callback. Contains the protocol exception that was thrown.</param>
        /// <returns>true if the message is sent, false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "cwd")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static void RunInTerminal(string title, string workingDirectory, bool useExternalConsole, IReadOnlyList<string> commandArgs, IReadOnlyDictionary<string, string> environmentVariables, Action<int?> success, Action<string> failure)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Provides direct access to the underlying output window without going through debug events
    /// </summary>
    public static class HostOutputWindow
    {
        /// <summary>
        /// Write text to the Debug VS Output window pane directly. This is used to write information before the session create event.
        /// </summary>
        /// <param name="outputMessage">Message to write</param>
        public static void WriteLaunchError(string outputMessage)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Provides an optional wait dialog to allow long running operations to be canceled. In VS Code,
    /// this is currently stubbed out to do nothing.
    /// </summary>
    public sealed class HostWaitDialog : IDisposable
    {
        /// <summary>
        /// Construct a new instance of the HostWaitDialog class
        /// </summary>
        /// <param name="format">Format string used to create the wait dialog's body along with the 'item' argument to ShowWaitDialog.</param>
        /// <param name="caption">Caption of the dialog</param>
        public HostWaitDialog(string format, string caption)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the wait dialog for a new item.
        /// </summary>
        /// <param name="item">Item argument to when formatting the format string to create the wait dialog text.</param>
        public void ShowWaitDialog(string item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ends the wait dialog
        /// </summary>
        public void EndWaitDialog()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ends the wait dialog
        /// </summary>
        public void Dispose()
        {
            EndWaitDialog();
        }
    }

    /// <summary>
    /// Provides an abstraction over a wait loop that shows a dialog. In VS Code, this is currently
    /// stubbed out to wait without a dialog.
    /// </summary>
    public sealed class HostWaitLoop
    {
        /// <summary>
        /// Constructs a new HostWaitLoop
        /// </summary>
        /// <param name="message">The text of the wait dialog</param>
        public HostWaitLoop(string message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the text of the dialog without changing the progress.
        /// </summary>
        /// <param name="text">Text to set.</param>
        public void SetText(string text)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Wait for the specified handle to be signaled.
        /// </summary>
        /// <param name="handle">Handle to wait on.</param>
        /// <param name="cancellationSource">Cancellation token source to cancel if the user hits the cancel button.</param>
        public void Wait(WaitHandle handle, CancellationTokenSource cancellationSource)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the progress of the dialog
        /// </summary>
        /// <param name="totalSteps">New total number of steps.</param>
        /// <param name="currentStep">The step that is currently finished.</param>
        /// <param name="progressText">Text describing the current progress.</param>
        public void SetProgress(int totalSteps, int currentStep, string progressText)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Static class providing telemetry reporting services to debug engines. Telemetry
    /// reports go to Microsoft, and so in general this functionality should not be used
    /// by non-Microsoft implemented debug engines.
    /// </summary>
    public static class HostTelemetry
    {
        /// <summary>
        /// Reports a telemetry event to Microsoft. This method is a nop in non-lab configurations.
        /// </summary>
        /// <param name="eventName">Name of the event. This should generally start with the
        /// prefix 'VS/Diagnostics/Debugger/'</param>
        /// <param name="eventProperties">0 or more properties of the event. Property names
        /// should generally start with the prefix 'VS.Diagnostics.Debugger.'</param>
        [Conditional("LAB")]
        public static void SendEvent(string eventName, params KeyValuePair<string, object>[] eventProperties)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reports the current exception to Microsoft's telemetry service.
        ///
        /// *NOTE*: This should only be called from a 'catch(...) when' handler.
        /// </summary>
        /// <param name="currentException">Exception object to report.</param>
        /// <param name="engineName">Name of the engine reporting the exception. Ex:Microsoft.MIEngine</param>
        public static void ReportCurrentException(Exception currentException, string engineName)
        {
            throw new NotImplementedException();
        }
    }
}
