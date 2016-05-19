using System;
using System.Diagnostics;
using System.IO;
using System.Threading;


namespace DebugConsoleProxy
{
    internal class ConsoleProxy
    {
        Process _debuggerProcess;
        Process _terminalProcess;

        StreamWriter _stdinWrite;
        StreamReader _stdoutRead;
        StreamReader _stderrRead;

        System.Threading.Thread _writeThread;
        System.Threading.Thread _writeErrThread;
        System.Threading.Thread _readThread;


        public void ReadThread()
        {
            while (true)
            {
                string line = this._stdoutRead.ReadLine();
                Console.WriteLine(line);
            }
        }

        public void ReadErrThread()
        {
            while (true)
            {
                string line = this._stderrRead.ReadLine();
                Console.Error.WriteLine(line);
            }
        }

        void WriteThread()
        {
            while (true)
            {
                string line = Console.ReadLine();
                this._stdinWrite.WriteLine(line);
                this._stdinWrite.Flush();
            }
        }

        public void LaunchAndExecuteDebugger(string miDebuggerPath, string breakEventName, string breakEventResponseName, string cwd)
        {
            /*while (!Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }*/

            string ttyName;

            if (LaunchCygwinTerminal(miDebuggerPath, cwd, out ttyName))
            {
                if (StartGdb(miDebuggerPath, ttyName))
                {
                    WaitForBreak(breakEventName, breakEventResponseName);
                }
            }
            
            return;
        }

        // This fires if someone kills gdb while debugging.
        private void DebuggerProcess_Exited(object sender, EventArgs e)
        {
            if (_terminalProcess != null && !_terminalProcess.HasExited)
            {
                try
                {
                    _terminalProcess.Kill();
                }
                catch
                {

                }
            }

            System.Console.WriteLine("^exit");

            Process.GetCurrentProcess().Kill();
        }

        private static void WaitForBreak(string breakEventName, string breakEventResponseName)
        {
            // Block on a named event or some other way the ui can ask us to break the process.
            EventWaitHandle breakRequestEvent = new EventWaitHandle(false, EventResetMode.AutoReset, breakEventName);
            EventWaitHandle breakResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset, breakEventResponseName);
            while (breakRequestEvent.WaitOne())
            {
                // The signals appear to execute synchronously during this call. So, the response event is sufficient
                // to stop processing in the mi engine 
                WindowsNativeMethods.GenerateConsoleCtrlEvent(WindowsNativeMethods.ConsoleCtrlValues.CTRL_C_EVENT, 0);
                breakResponseEvent.Set();
            }
        }

        private bool LaunchCygwinTerminal(string miDebuggerPath, string cwd, out string ttyName)
        {
            ttyName = null;

            // These paths are windows relative. i.e. c:\\cygwin64\\tmp
            string cygwinBin = Path.GetDirectoryName(miDebuggerPath);
            string cygwinTmp = Path.Combine(Path.GetDirectoryName(cygwinBin), "tmp");

            // File that will contain the tty name relative to windows.
            string ttyTmpFile = Guid.NewGuid().ToString();
            string ttyTmpPath = Path.Combine(cygwinTmp, ttyTmpFile);

            string cygwinTtyFilePath = GetCygwinPath(cygwinBin, ttyTmpPath);

            string initScriptFile = Guid.NewGuid().ToString();
            string initScriptPath = Path.Combine(cygwinTmp, initScriptFile);

            // Path to init script relative to cygwin

            string cygwinInitScriptPath = GetCygwinPath(cygwinBin, initScriptPath);

            string cygwinCwd = GetCygwinPath(cygwinBin, cwd);

            // Generated script for initializing bash. This can't use -c 
            // because bash must launch in interactive mode. -c leaves it with
            // stdin not connected to the terminal.
            using (StreamWriter scriptWriter = new StreamWriter(initScriptPath))
            {
                // #!/bin/bash -x
                //
                // tty > /tmp/tempFile
                //
                // cd cwd
                scriptWriter.Write("#!/bin/bash -x\n");
                scriptWriter.Write("\n");
                scriptWriter.Write("tty > " + cygwinTtyFilePath + "\n");
                scriptWriter.Write("\n");
                scriptWriter.Write("cat /proc/$BASHPID/ppid >> " + cygwinTtyFilePath + "\n");
                scriptWriter.Write("\n");
                scriptWriter.Write("cd " + cygwinCwd);
            }

            if (!File.Exists(initScriptPath))
            {
                Console.Error.WriteLine("Failed to initialize terminal for debugger. Could not create init script");
                return false;
            }

            Process initChMod = new Process();
            initChMod.StartInfo.FileName = Path.Combine(cygwinBin, "chmod.exe");
            initChMod.StartInfo.Arguments = "777 " + cygwinInitScriptPath;
            initChMod.StartInfo.CreateNoWindow = true;
            initChMod.Start();
            initChMod.WaitForExit();

            // Launch new cygwin console.
            // Note this System.Diagnostics.Process will immediately shutdown and a new instance of the terminal process will start.
            Process terminalProcess = new Process();
            terminalProcess.StartInfo.CreateNoWindow = true;
            terminalProcess.StartInfo.FileName = Path.Combine(cygwinBin, "mintty.exe");
            terminalProcess.StartInfo.Arguments = "-t DebuggerTerminal -i /Cygwin-Terminal.ico -h always bash --init-file " + cygwinInitScriptPath;
            terminalProcess.Start();

            // Wait for the init script to finish
            // TODO: find a more reliable way to get this.
            Thread.Sleep(500);

            const int maxRetry = 20;
            int retryCount = 0;
            while (!File.Exists(ttyTmpPath) && retryCount < maxRetry)
            {
                Thread.Sleep(100);
                retryCount++;
            }

            if (!File.Exists(ttyTmpPath))
            {
                Console.Error.WriteLine("Failed to initialize terminal for debugger. Could not obtain the tty");
                return false;
            }

            using (StreamReader ttyFileReader = new StreamReader(ttyTmpPath))
            {
                ttyName = ttyFileReader.ReadLine();
                string terminalPid = ttyFileReader.ReadLine();

                // TODO: this pid might not match since it is a cygwin pid instead of a windows pid
                int termPid = Int32.Parse(terminalPid);
                try { _terminalProcess = Process.GetProcessById(termPid); } catch { }
            }

            try { File.Delete(initScriptPath); } catch { }
            try { File.Delete(ttyTmpFile); } catch { }

            return true;
        }

        private bool StartGdb(string miDebuggerPath, string ttyName)
        {
            if (!File.Exists(miDebuggerPath))
            {
                Console.Error.WriteLine("Failed to initialize debugger. Path to debugger not found");
                return false;
            }

            this._debuggerProcess = new Process();
            this._debuggerProcess.StartInfo.FileName = miDebuggerPath;
            this._debuggerProcess.StartInfo.Arguments = " --interpreter=mi --tty=" + ttyName;
            this._debuggerProcess.StartInfo.UseShellExecute = false;
            this._debuggerProcess.StartInfo.RedirectStandardInput = true;
            this._debuggerProcess.StartInfo.RedirectStandardOutput = true;
            this._debuggerProcess.StartInfo.RedirectStandardError = true;
            this._debuggerProcess.EnableRaisingEvents = true;
            this._debuggerProcess.Exited += DebuggerProcess_Exited;
            this._debuggerProcess.Start();

            this._stdoutRead = this._debuggerProcess.StandardOutput;
            this._stdinWrite = this._debuggerProcess.StandardInput;
            this._stderrRead = this._debuggerProcess.StandardError;

            _writeThread = new System.Threading.Thread(this.ReadThread);
            _writeThread.Start();

            _writeErrThread = new System.Threading.Thread(this.ReadErrThread);
            _writeErrThread.Start();

            _readThread = new System.Threading.Thread(this.WriteThread);
            _readThread.Start();

            return true;
        }

        

        private string GetCygwinPath(string cygwinBin, string windowsPath)
        {
            Process cygPathProcess = new Process();
            cygPathProcess.StartInfo.FileName = Path.Combine(cygwinBin, "cygpath.exe");
            cygPathProcess.StartInfo.Arguments = windowsPath;
            cygPathProcess.StartInfo.CreateNoWindow = true;
            cygPathProcess.StartInfo.UseShellExecute = false;
            cygPathProcess.StartInfo.RedirectStandardOutput = true;
            cygPathProcess.Start();
            cygPathProcess.WaitForExit();

            return cygPathProcess.StandardOutput.ReadLine();
        }
    }
}


