// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting.OpenDebug
{
    /// <summary>
    /// Represents a Debug Adapter command.
    /// </summary>
    public interface ICommand
    {
        string Name { get; }

        /// <summary>
        /// The object that gets converted to JSON to represent the commands args.
        /// </summary>
        object DynamicArgs { get; }

        /// <summary>
        /// Represents the expected response from the command.
        /// If the actual response does not match this value, the command will throw.
        /// </summary>
        IResponse ExpectedResponse { get; }

        /// <summary>
        /// Sets the expectation of the commands success value
        /// </summary>
        bool ExpectsSuccess { get; set; }

        /// <summary>
        /// After the command is run if the command response matches ExpectedResponse,
        /// this method is called with the actual match.
        /// This can be used to read data from the response.
        /// </summary>
        void ProcessActualResponse(IActualResponse response);

        /// <summary>
        /// Set this to override the default timeout for the duration of this command.
        /// </summary>
        TimeSpan Timeout { get; set; }

        void Run(IDebuggerRunner runner, params IEvent[] expectedEvents);
    }

    /// <summary>
    /// Applies to a Debug Adapter command that can return results. 
    /// </summary>
    public interface ICommandWithResponse<T> : ICommand
    {
        T ActualResponse { get; }

        new T Run(IDebuggerRunner runner, params IEvent[] expectedEvents);
    }

    /// <summary>
    /// Provides a way to get the command to interperet the actual result
    /// that comes back from the Debug Adapter.
    /// </summary>
    public interface IActualResponse
    {
        R Convert<R>();
    }
}
