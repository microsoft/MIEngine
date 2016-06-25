using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MICore
{
    /// <summary>
    /// Transport used when the debugger is started through IDebugUnixShellPort  (SSH, and possible other things in the future).
    /// </summary>
    public class UnixShellPortTransport : ITransport, IDebugUnixShellCommandCallback
    {
        private readonly object _closeLock = new object();
        private ITransportCallback _callback;
        private Logger _logger;
        private string _startRemoteDebuggerCommand;
        private IDebugUnixShellAsyncCommand _asyncCommand;
        private bool _bQuit;

        public UnixShellPortTransport()
        {
        }

        public void Init(ITransportCallback transportCallback, LaunchOptions options, Logger logger)
        {
            UnixShellPortLaunchOptions launchOptions = (UnixShellPortLaunchOptions)options;
            _callback = transportCallback;
            _logger = logger;
            _startRemoteDebuggerCommand = launchOptions.StartRemoteDebuggerComand;

            _callback.AppendToInitializationLog("Starting unix command: " + _startRemoteDebuggerCommand);
            launchOptions.UnixPort.BeginExecuteAsyncCommand(_startRemoteDebuggerCommand, this, out _asyncCommand);
        }
        
        public void Close()
        {
            lock (_closeLock)
            {
                if (_bQuit)
                    return;
                _bQuit = true;

                _asyncCommand.Abort();
            }
        }

        public void Send(string cmd)
        {
            _logger?.WriteLine("<-" + cmd);
            _logger?.Flush();
            _asyncCommand.WriteLine(cmd);
        }

        int ITransport.DebuggerPid
        {
            get
            {
                return 0; // this isn't known, and shouldn't be needed
            }
        }

        bool ITransport.IsClosed
        {
            get { return _bQuit; }
        }

        void IDebugUnixShellCommandCallback.OnOutputLine(string line)
        {
            _logger?.WriteLine("->" + line);
            _logger?.Flush();

            if (!string.IsNullOrEmpty(line))
            {
                _callback.OnStdOutLine(line);
            }
        }

        void IDebugUnixShellCommandCallback.OnExit(string exitCode)
        {
            if (!_bQuit)
            {
                _callback.AppendToInitializationLog(string.Format(CultureInfo.InvariantCulture, "{0} exited with code {1}.", _startRemoteDebuggerCommand, exitCode ?? "???"));

                _bQuit = true;
                try
                {
                    _callback.OnDebuggerProcessExit(exitCode);
                }
                catch
                {
                    // eat exceptions on this thread so we don't bring down VS
                }
            }
        }
    }
}
