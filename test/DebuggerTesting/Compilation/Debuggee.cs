// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DebuggerTesting.Settings;
using DebuggerTesting.Utilities;
using Xunit.Abstractions;

namespace DebuggerTesting.Compilation
{
    public class Debuggee :
        IDebuggee, ILoggingComponent
    {
        #region Constructor

        private Debuggee(ILoggingComponent logger, ICompilerSettings settings, string debuggeeName, int debuggeeMoniker, string outputName, CompilerOutputType outputType)
        {
            Parameter.ThrowIfNull(logger, nameof(logger));
            Parameter.ThrowIfNullOrWhiteSpace(debuggeeName, nameof(debuggeeName));
            Parameter.ThrowIfNegativeOrZero(debuggeeMoniker, nameof(debuggeeMoniker));
            Parameter.ThrowIfNull(settings, nameof(settings));

            this.OutputHelper = logger.OutputHelper;
            this.compilerDefineConstants = new Dictionary<string, string>();
            this.debuggeeName = debuggeeName;
            this.debuggeeMoniker = debuggeeMoniker;
            this.CompilerOptions = CompilerOption.GenerateSymbols;
            this.libraries = new List<string>();
            this.outputType = outputType;
            this.settings = settings;
            this.sourceFilePaths = new List<string>();

            if (String.IsNullOrEmpty(outputName))
            {
                // If the outputName was not provided, assume it is called "a.out" and update the extension
                outputName = Debuggee.UpdateOutputName("a.out", outputType);
            }
            else if (String.IsNullOrEmpty(Path.GetExtension(outputName)))
            {
                // If the outputName has no extension, add the appropriate extension
                outputName = Debuggee.UpdateOutputName(outputName, outputType);
            }

            this.outputName = outputName;
        }

        private Debuggee(Debuggee debuggee)
        {
            Parameter.ThrowIfNull(debuggee, nameof(debuggee));

            this.OutputHelper = debuggee.OutputHelper;
            this.compilerDefineConstants = new Dictionary<string, string>(debuggee.compilerDefineConstants);
            this.debuggeeName = debuggee.debuggeeName;
            this.debuggeeMoniker = debuggee.debuggeeMoniker;
            this.CompilerOptions = debuggee.CompilerOptions;
            this.libraries = debuggee.libraries.ToList();
            this.outputName = debuggee.outputName;
            this.outputType = debuggee.outputType;
            this.settings = debuggee.settings;
            this.sourceFilePaths = debuggee.sourceFilePaths.ToList();
        }

        #endregion

        #region IDebuggee Members

        public void AddDefineConstant(string name, string value = null)
        {
            this.compilerDefineConstants.Add(name, value);
        }

        void IDebuggee.AddLibraries(params string[] libraries)
        {
            this.libraries.AddRange(libraries);
        }

        void IDebuggee.AddSourceFiles(params string[] fileNames)
        {
            this.sourceFilePaths.AddRange(fileNames.Select(n => Path.Combine(this.SourceRoot, n)));
        }

        IDebuggee IDebuggee.Clone()
        {
            return new Debuggee(this);
        }

        void IDebuggee.Compile()
        {
            ICompiler compiler = Debuggee.CreateCompiler(this, this.settings);
            compiler.Compile(
                this.outputType,
                this.libraries,
                this.sourceFilePaths,
                this.OutputPath,
                this.CompilerOptions,
                this.compilerDefineConstants);
        }

        Process IDebuggee.Launch(params string[] arguments)
        {
            string allArguments = string.Join(" ", arguments);
            this.WriteLine("Launching debuggee {0} {1}", this.OutputPath, allArguments);
            Process p = ProcessHelper.CreateProcess(this.OutputPath, allArguments);
            if (PlatformUtilities.IsWindows)
            {
                p.AddToPath(Path.GetDirectoryName(this.settings.CompilerPath));
            }
            p.Start();
            return p;
        }

        public CompilerOption CompilerOptions { get; set; }

        public string OutputPath
        {
            get { return Path.Combine(this.GetOutputRoot(this.settings), this.outputName); }
        }

        public CompilerOutputType OutputType
        {
            get { return this.outputType; }
        }

        public string SourceRoot
        {
            get { return Path.Combine(this.DebuggeeInstance, "src"); }
        }

        #endregion

        #region ILoggingComponent Members

        public ITestOutputHelper OutputHelper { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new debuggee instance. This will copy the debuggee into a new folder based on the debuggeeMoniker.
        /// This should only be called once per debuggee.
        /// </summary>
        public static IDebuggee Create(ILoggingComponent logger, ICompilerSettings settings, string debuggeeName, int debuggeeMoniker, string outputName = null, CompilerOutputType outputType = CompilerOutputType.Executable)
        {
            Debuggee newDebuggee = new Debuggee(logger, settings, debuggeeName, debuggeeMoniker, outputName, outputType);
            newDebuggee.CopySource();
            return newDebuggee;
        }

        /// <summary>
        /// Opens an existing debuggee instance.
        /// </summary>
        public static IDebuggee Open(ILoggingComponent logger, ICompilerSettings settings, string debuggeeName, int debuggeeMoniker, string outputName = null, CompilerOutputType outputType = CompilerOutputType.Executable)
        {
            return new Debuggee(logger, settings, debuggeeName, debuggeeMoniker, outputName, outputType);
        }


        private static CompilerBase CreateCompiler(ILoggingComponent logger, ICompilerSettings settings)
        {
            switch (settings.CompilerType)
            {
                case SupportedCompiler.GPlusPlus:
                    return new GppCompiler(logger, settings);
                case SupportedCompiler.ClangPlusPlus:
                    return new ClangCompiler(logger, settings);
                case SupportedCompiler.VisualCPlusPlus:
                    return new VisualCPlusPlusCompiler(logger, settings);
                case SupportedCompiler.XCodeBuild:
                    return new XCodeCompiler(logger, settings);
                default:
                    throw new ArgumentOutOfRangeException(nameof(settings.CompilerType), "Unhandled compiler toolset: " + settings.CompilerType);
            }
        }

        private string GetOutputRoot(ICompilerSettings settings)
        {
            return Path.Combine(this.DebuggeeInstance, "out", settings.CompilerName, settings.DebuggeeArchitecture.ToArchitectureString());
        }

        private void CopySource()
        {
            try
            {
                this.WriteLine("Creating instance of debuggee by copying source.");
                FileUtilities.DirectoryCopy(this.DebuggeeRoot, this.DebuggeeInstance, true);
            }
            catch (IOException ex)
            {
                this.WriteLine("Error copying debuggee folder. Copying from '{0}' to '{1}'.", this.DebuggeeRoot, this.DebuggeeInstance);
                this.WriteLine(UDebug.ExceptionToString(ex));
                throw;
            }
        }

        private static string UpdateOutputName(string outputName, CompilerOutputType outputType)
        {
            switch (outputType)
            {
                case CompilerOutputType.Executable:
                    if (PlatformUtilities.IsWindows)
                        return Path.ChangeExtension(outputName, "exe");
                    else
                        return outputName;
                case CompilerOutputType.SharedLibrary:
                    if (PlatformUtilities.IsWindows)
                        return Path.ChangeExtension(outputName, "dll");
                    else
                        return Path.ChangeExtension(outputName, "so");
                case CompilerOutputType.MacOSApp:
                        return Path.ChangeExtension(outputName, "app");
                default:
                    throw new NotSupportedException("Support for output type '{0}' has not been implemented.".FormatInvariantWithArgs(outputType));
            }
        }

        #endregion

        #region Properties

        // The folder that contains the original source of the debuggee
        private string DebuggeeRoot
        {
            get { return Path.Combine(PathSettings.DebuggeesPath, this.debuggeeName); }
        }

        // The folder that contains the specific instance of the debuggee (suffixed by the debuggee moniker)
        private string DebuggeeInstance
        {
            get
            {
                string debuggeeInstanceName = "{0}-{1}".FormatInvariantWithArgs(this.debuggeeName, this.debuggeeMoniker);
                return Path.Combine(PathSettings.DebuggeesPath, "instance", debuggeeInstanceName);
            }
        }

        #endregion

        #region Fields

        private Dictionary<string, string> compilerDefineConstants;
        private string debuggeeName;
        private int debuggeeMoniker;
        private List<string> libraries;
        private string outputName;
        private CompilerOutputType outputType;
        private ICompilerSettings settings;
        private List<string> sourceFilePaths;

        #endregion
    }
}
