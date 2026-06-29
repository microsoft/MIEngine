// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SSHDebugPS.Docker
{
    public interface IContainerInstance : System.IEquatable<IContainerInstance>
    {
        string Id { get; }
        string Name { get; }
    }
}
