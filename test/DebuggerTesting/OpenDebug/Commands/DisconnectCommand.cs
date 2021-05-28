// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting.OpenDebug.Commands
{
    #region DisconnectCommandArgs

    public sealed class DisconnectCommandArgs : JsonValue
    {
        public sealed class ExtensionHostData
        {
            public bool restart;
        }

        public ExtensionHostData extensionHostData = new ExtensionHostData();
    }

    #endregion

    /// <summary>
    /// Disconnects the debugger.
    /// </summary>
    public class DisconnectCommand : Command<DisconnectCommandArgs>
    {
        public DisconnectCommand() : base("disconnect")
        {
            this.Args.extensionHostData.restart = false;
        }
    }
}
