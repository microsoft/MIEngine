// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting.OpenDebug
{
    public interface IEvent : IResponse
    {
        string Name { get; }

        /// <summary>
        /// Called after an event is hit if the event response matches.
        /// This method is called with the actual match.
        /// This can be used to read data from the event.
        /// </summary>
        void ProcessActualResponse(IActualResponse response);
    }
}
