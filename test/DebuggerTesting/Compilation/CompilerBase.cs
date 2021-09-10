// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using DebuggerTesting.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace DebuggerTesting.Compilation
{
    internal abstract class CompilerBase : ICompiler, ILoggingComponent
    {
        #region Constructor

        public CompilerBase(ILoggingComponent logger, ICompilerSettings settings, bool addDebuggerDirToPath)
        {
            Parameter.ThrowIfNull(logger, nameof(logger));
            Parameter.ThrowIfNull(settings, nameof(settings));

            this.addDebuggerDirToPath = addDebuggerDirToPath;
            this.OutputHelper = logger.OutputHelper;
            this.Settings = settings;
        }

        #endregion

        #region ICompiler Members

        public void Compile(
            CompilerOutputType outputType,
            IEnumerable<string> libraries,
            IEnumerable<string> sourceFilePaths,
            string targetFilePath,
            CompilerOption options,
            IDictionary<string, string> defineConstants)
        {
            Parameter.ThrowIfNull(libraries, nameof(libraries));
            Parameter.ThrowIfNull(sourceFilePaths, nameof(sourceFilePaths));
            Parameter.ThrowIfNullOrWhiteSpace(targetFilePath, nameof(targetFilePath));
            Parameter.ThrowIfNull(defineConstants, nameof(defineConstants));

            bool result = this.CompileCore(outputType,
                this.Settings.DebuggeeArchitecture,
                libraries,
                sourceFilePaths,
                targetFilePath,
                options,
                defineConstants);

            Assert.True(result, "The compilation failed.");

            // If the output file was not created, then error
            if (outputType == CompilerOutputType.MacOSApp)
            {
                // .app on macOS is a special folder
                Assert.True(Directory.Exists(targetFilePath), "The compiler did not create the expected output. " + targetFilePath);
            }
            else
            {
                Assert.True(File.Exists(targetFilePath), "The compiler did not create the expected output. " + targetFilePath);
            }
        }

        #endregion

        #region ILoggingComponent Members

        public ITestOutputHelper OutputHelper { get; private set; }

        #endregion

        #region Methods

        protected abstract bool CompileCore(
            CompilerOutputType outputType,
            SupportedArchitecture architecture,
            IEnumerable<string> libraries,
            IEnumerable<string> sourceFilePaths,
            string targetFilePath,
            CompilerOption options,
            IDictionary<string, string> defineConstants);

        protected int RunCompiler(string compilerArguments, string targetFilePath)
        {
            this.WriteLine("Compiling target \"{0}\"", targetFilePath);
            this.WriteLine("Calling compiler \"{0}\" with arguments \"{1}\"", this.Settings.CompilerPath, compilerArguments);

            string targetDir = Path.GetDirectoryName(targetFilePath);
            if (!Directory.Exists(targetDir))
            {
                this.WriteLine("Creating \"{0}\" because it does not exist.", targetDir);
                Directory.CreateDirectory(targetDir);
            }

            using (Process process = ProcessHelper.CreateProcess(this.Settings.CompilerPath, compilerArguments))
            {
                if (this.addDebuggerDirToPath)
                {
                    process.AddToPath(Path.GetDirectoryName(this.Settings.CompilerPath));
                }

                process.Start();
                process.WaitForExit(60 * 1000);

                if (!process.StandardOutput.EndOfStream)
                {
                    this.WriteLine("Compiler standard output:");
                    this.WriteLines(process.StandardOutput);
                }
                if (!process.StandardError.EndOfStream)
                {
                    this.WriteLine("Compiler standard error:");
                    this.WriteLines(process.StandardError);
                }
                this.WriteLine("Compiler exited with code {0}", process.ExitCode);

                return process.ExitCode;
            }
        }

        protected static string GetPlatformName()
        {
            if (PlatformUtilities.IsWindows)
                return "WINDOWS";
            else if (PlatformUtilities.IsLinux)
                return "LINUX";
            else if (PlatformUtilities.IsOSX)
                return "OSX";
            else
                return "UNKNOWN";
        }


        #endregion

        #region Properties

        protected ICompilerSettings Settings { get; private set; }

        #endregion

        #region Fields

        private bool addDebuggerDirToPath;

        #endregion
    }
}
