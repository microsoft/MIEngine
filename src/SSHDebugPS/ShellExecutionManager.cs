using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS
{
    internal class ShellCommandCallback : IDebugUnixShellCommandCallback
    {
        private ManualResetEvent _commandCompleteEvent;
        private int _exitCode = -1;
        private readonly StringBuilder _outputBuilder = new StringBuilder();

        public int ExitCode => _exitCode;
        public string CommandOutput => _outputBuilder.ToString();

        public ShellCommandCallback(ManualResetEvent commandCompleteEvent)
        {
            _commandCompleteEvent = commandCompleteEvent;
        }

        public void OnOutputLine(string line)
        {
            _outputBuilder.AppendLine(line);
        }

        public void OnExit(string exitCode)
        {
            if (!string.IsNullOrWhiteSpace(exitCode))
            {
                int exitCodeValue;
                if (int.TryParse(exitCode.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out exitCodeValue))
                {
                    _exitCode = exitCodeValue;
                }
            }

            _commandCompleteEvent.Set();
        }
    }

    internal class ShellExecutionManager : IDisposable
    {
        private readonly ICommandRunner _shell;
        private AD7UnixAsyncShellCommand _currentCommand;
        private readonly ManualResetEvent _commandCompleteEvent = new ManualResetEvent(false);


        public ShellExecutionManager(ICommandRunner shell)
        {
            _shell = shell;
        }

        public int ExecuteCommand(string commandText, int timeout, out string commandOutput)
        {
            if (_currentCommand != null)
            {
                throw new InvalidOperationException("already a command processing");
            }
            _commandCompleteEvent.Reset();

            ShellCommandCallback commandCallback = new ShellCommandCallback(_commandCompleteEvent);
            AD7UnixAsyncShellCommand command = new AD7UnixAsyncShellCommand(_shell, commandCallback, false);

            try
            {
                _currentCommand = command;
                _currentCommand.Start(commandText);
                if (!_commandCompleteEvent.WaitOne(timeout))
                {
                    commandOutput = "Command Timeout";
                    return 1460; // ERROR_TIMEOUT
                }

                commandOutput = commandCallback.CommandOutput.Trim('\n', '\r'); // trim ending newlines
                return commandCallback.ExitCode;
            }
            finally
            {
                _currentCommand.Close();
                _currentCommand = null;
            }
        }

        void IDisposable.Dispose()
        {
            _shell.Dispose();
        }
    }
}
