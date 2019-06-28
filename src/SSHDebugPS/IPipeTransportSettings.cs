// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SSHDebugPS
{
    /// <summary>
    ///  This interface returns the tranport's executalbe command and the base set of arguments to use.
    /// </summary>
    public interface IPipeTransportSettings
    {
        string CommandArgs { get; }
        string Command { get; }
    }
}
