// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using DebuggerTesting.Utilities;

namespace DebuggerTesting.Compilation
{
    internal abstract class GppStyleCompiler :
        CompilerBase
    {
        #region Constructor

        public GppStyleCompiler(ILoggingComponent logger, ICompilerSettings settings)
            : base(logger, settings, PlatformUtilities.IsWindows)
        {
        }

        #endregion

        #region Methods

        protected override bool CompileCore(
            CompilerOutputType outputType,
            SupportedArchitecture architecture,
            IEnumerable<string> libraries,
            IEnumerable<string> sourceFilePaths,
            string targetFilePath,
            CompilerOption options,
            IDictionary<string, string> defineConstants)
        {
            ArgumentBuilder builder = new ArgumentBuilder("-", String.Empty);
            if (options.HasFlag(CompilerOption.GenerateSymbols))
            {
                builder.AppendNamedArgument("g", null);
            }

            if (options.HasFlag(CompilerOption.OptimizeLevel1))
            {
                builder.AppendNamedArgument("O1", null);
            }
            else if (options.HasFlag(CompilerOption.OptimizeLevel2))
            {
                builder.AppendNamedArgument("O2", null);
            }
            else if (options.HasFlag(CompilerOption.OptimizeLevel3))
            {
                builder.AppendNamedArgument("O3", null);
            }
            else
            {
                builder.AppendNamedArgument("O0", null);
            }

            // Enable pthreads
            if (options.HasFlag(CompilerOption.SupportThreading))
            {
                builder.AppendNamedArgument("pthread", null);
            }

            // Just use C++ 11
            builder.AppendNamedArgument("std", "c++11", "=");

            switch (outputType)
            {
                case CompilerOutputType.SharedLibrary:
                    builder.AppendNamedArgument("shared", null);
                    builder.AppendNamedArgument("fpic", null);
                    break;
                case CompilerOutputType.ObjectFile:
                    builder.AppendNamedArgument("c", null);
                    builder.AppendNamedArgument("fpic", null);
                    break;
                case CompilerOutputType.Unspecified:
                // Treat Unspecified as Executable, since executable is the default
                case CompilerOutputType.Executable:
                    // Compiling an executable does not have a command line option, it's the default
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outputType), "Unhandled output type: " + outputType);
            }

            switch (architecture)
            {
                case SupportedArchitecture.x64:
                    builder.AppendNamedArgument("m", "64");
                    DefineConstant(builder, "DEBUGGEE_ARCH", "64");
                    break;
                case SupportedArchitecture.x86:
                    builder.AppendNamedArgument("m", "32");
                    DefineConstant(builder, "DEBUGGEE_ARCH", "32");
                    break;
                case SupportedArchitecture.arm:
                    builder.AppendNamedArgument("m", "arm");
                    DefineConstant(builder, "DEBUGGEE_ARCH", "ARM");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(architecture), "Unhandled target architecture: " + architecture);
            }

            // Define a constant for the platform name.
            DefineConstant(builder, "DEBUGGEE_PLATFORM", GetPlatformName());

            this.SetAdditionalArguments(builder);

            builder.AppendNamedArgument("o", targetFilePath);

            foreach (string sourceFilePath in sourceFilePaths)
            {
                builder.AppendArgument(sourceFilePath);
            }

            foreach (string library in libraries)
            {
                builder.AppendNamedArgument("l", library);
            }

            foreach (var defineConstant in defineConstants)
            {
                DefineConstant(builder, defineConstant.Key, defineConstant.Value);
            }

            return 0 == this.RunCompiler(builder.ToString(), targetFilePath);
        }

        protected static void DefineConstant(ArgumentBuilder builder, string name, string value = null)
        {
            string constant = name;
            if (value != null)
                constant += "=" + ArgumentBuilder.MakeQuotedIfRequired(value);
            builder.AppendNamedArgument("D", constant);
        }

        /// <summary>
        /// Add any compiler specific aguments to the command line
        /// </summary>
        protected virtual void SetAdditionalArguments(ArgumentBuilder builder)
        {
        }

        #endregion
    }
}
