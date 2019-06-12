using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using liblinux;
using liblinux.Shell;

namespace Microsoft.SSHDebugPS.SSH
{
    /// <summary>
    /// Wrapper for liblinux's StreamingShell
    /// </summary>
    internal class SSHRemoteShell : ICommandRunner
    {
        private StreamingShell _shell;

        public SSHRemoteShell(UnixSystem remoteSystem)
        {
            _shell = new StreamingShell(remoteSystem);
            _shell.OutputReceived += OnOutputReceived;
            _shell.Closed += OnClosedOrDisconnected;
            _shell.Disconnected += OnClosedOrDisconnected;
            _shell.ErrorOccured += OnError;
        }

        public void Start()
        {
            _shell.BeginOutputRead();
        }


        public event EventHandler<string> OutputReceived;
        public event EventHandler<int> Closed;
        public event EventHandler<ErrorOccuredEventArgs> ErrorOccured;

        private void OnOutputReceived(object sender, OutputReceivedEventArgs e)
        {
            OutputReceived?.Invoke(sender, e?.Output);
        }

        private void OnClosedOrDisconnected(object sender, EventArgs e)
        {
            // No exit code here, so assume success?
            Closed?.Invoke(sender, 0);
        }

        private void OnError(object sender, liblinux.ErrorOccuredEventArgs e)
        {
            ErrorOccured?.Invoke(sender, new ErrorOccuredEventArgs(e.Exception));
        }

        public void Dispose()
        {
            if (_shell != null)
            {
                _shell.OutputReceived -= OnOutputReceived;
                _shell.Closed -= OnClosedOrDisconnected;
                _shell.Disconnected -= OnClosedOrDisconnected;
                _shell.ErrorOccured -= OnError;

                _shell.Dispose();
                _shell = null;
            }
        }

        public void Write(string text)
        {
            _shell.Write(text);
            _shell.Flush();
        }

        public void WriteCommandStart(string startCommand)
        {
            _shell.WriteLine(startCommand);
        }

        public void WriteLine(string text)
        {
            _shell.WriteLine(text);
            _shell.Flush();
        }
    }
}
