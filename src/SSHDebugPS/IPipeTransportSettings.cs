// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SSHDebugPS
{
    public interface IPipeTransportSettings
    {
        string ExeCommandArgs { get; }
        string ExeCommand { get; }
        string ExeNotFoundErrorMessage { get; }
    }
}
