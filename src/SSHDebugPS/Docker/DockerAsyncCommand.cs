
using System;
using System.Globalization;
using System.Text;
using Microsoft.DebugEngineHost;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS.Docker
{
    internal class DockerAsyncCommand : IDebugUnixShellAsyncCommand
    {
        private ICommandRunner _runner;
        private IDebugUnixShellCommandCallback _callback;
        private StringBuilder _errorBuilder = new StringBuilder();
        public DockerAsyncCommand(ICommandRunner runner, IDebugUnixShellCommandCallback callback)
        {
            _callback = callback;
            _runner = runner;
            _runner.OutputReceived += OnOutputReceived;
            _runner.ErrorOccured += OnErrorOccured;
            _runner.Closed += OnClose;

            _runner.Start();
        }

        private void OnClose(object sender, int e)
        {
            _callback.OnExit(e.ToString());
        }

        private void OnErrorOccured(object sender, ErrorOccuredEventArgs args)
        {
            _errorBuilder.Append(args.ErrorMessage);
        }

        private void OnOutputReceived(object sender, string e)
        {
            if (!string.IsNullOrEmpty(e))
                _callback.OnOutputLine(e);
        }

        public void Write(string text)
        {
            _runner.Write(text);
        }

        public void WriteLine(string text)
        {
            _runner.WriteLine(text);
        }

        public void Abort()
        {
            Close();
        }

        public string ErrorMessage => _errorBuilder.ToString();

        public void Close()
        {
            if (_runner != null)
            {
                _runner.Dispose();
                _runner.OutputReceived -= OnOutputReceived;
                _runner.ErrorOccured -= OnErrorOccured;
                _runner.Closed -= OnClose;
                _runner = null;
            }
            _callback = null;
        }
    }
}
