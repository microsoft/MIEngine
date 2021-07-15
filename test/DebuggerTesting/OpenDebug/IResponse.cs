// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting.OpenDebug
{
    /// <summary>
    /// Represents a Debug Adapter responses
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// The object that gets converted to JSON to represent a debug adapter response
        /// </summary>
        object DynamicResponse { get; }

        /// <summary>
        /// Set to true if the order of the items in the response is not important (like variable lists)
        /// </summary>
        bool IgnoreOrder { get; }
    }
}
