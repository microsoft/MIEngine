// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using System.IO;
using System.Globalization;

namespace Microsoft.SSHDebugPS
{
    internal class Connection
    {
        private readonly liblinux.UnixSystem _remoteSystem;

        public Connection(liblinux.UnixSystem remoteSystem)
        {
            _remoteSystem = remoteSystem;
        }

        internal List<PSOutputParser.Process> ListProcesses()
        {
            var command = _remoteSystem.Shell.ExecuteCommand(PSOutputParser.CommandText);
            if (command.ExitCode != 0)
            {
                throw new CommandFailedException(StringResources.Error_PSFailed);
            }

            return PSOutputParser.Parse(command.Output);
        }

        internal void BeginExecuteAsyncCommand(string commandText, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand)
        {
            var command = new AD7UnixAsyncCommand(_remoteSystem.Shell.OpenStream(), callback);
            command.Start(commandText);
            asyncCommand = command;
        }
    }
}
