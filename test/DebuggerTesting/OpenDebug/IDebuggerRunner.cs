// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using DebuggerTesting.OpenDebug.Commands;
using DarRunner = DebugAdapterRunner.DebugAdapterRunner;

namespace DebuggerTesting.OpenDebug
{
    /// <summary>
    /// A wrapper around the DAR debugger adapter runner. 
    /// The abstracts parameters that need to be passed to the DAR runner.
    /// It also has logic to check for leaked processes and files.
    /// </summary>
    public interface IDebuggerRunner : ILoggingComponent, IDisposable
    {
        /// <summary>
        /// The debug adapter runner this wraps.
        /// </summary>
        DarRunner DarRunner { get; }

        /// <summary>
        /// If an error is encountered running a previous command, this gets set to true
        /// and subsequent commands are not issued.
        /// </summary>
        bool ErrorEncountered { get; set; }

        /// <summary>
        /// When a break event occurs this reports the thread id of the current thread.
        /// </summary>
        int StoppedThreadId { get; }

        /// <summary>
        /// Runs a debug adapter command.
        /// </summary>
        /// <param name="command">The command to run</param>
        /// <param name="expectedEvents">[OPTIONAL] If the command expects to raise events, provide the list of events to look for.</param>
        void RunCommand(ICommand command, params IEvent[] expectedEvents);

        R RunCommand<R>(ICommandWithResponse<R> command, params IEvent[] expectedEvents);

        /// <summary>
        /// Provides a builder for describing expected events and command.
        /// First provides events in the order they are expected, then finish
        /// with the command to be run.
        /// </summary>
        IRunBuilder Expects { get; }

        /// <summary>
        /// Performs a disconnct command and verifies that child processes are closed.
        /// If they are not, this fails the test.
        /// If you just call Dispose, the processes are closed, but the test does not fail.
        /// </summary>
        void DisconnectAndVerify();

        InitializeResponseValue InitializeResponse { get; }

        /// <summary>
        /// Gets the settings for the debugger
        /// </summary>
        IDebuggerSettings DebuggerSettings { get;  }
    }
}
