// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DebuggerTesting.Utilities;

namespace DebuggerTesting.Compilation
{
    internal class VisualCPlusPlusCompiler : CompilerBase
    {
        public VisualCPlusPlusCompiler(ILoggingComponent logger, ICompilerSettings settings)
            : base(logger, settings, addDebuggerDirToPath: false)
        {
        }

        protected override bool CompileCore(
            CompilerOutputType outputType,
            SupportedArchitecture architecture,
            IEnumerable<string> libraries,
            IEnumerable<string> sourceFilePaths,
            string targetFilePath,
            CompilerOption options,
            IDictionary<string, string> defineConstants)
        {
            ArgumentBuilder clBuilder = new ArgumentBuilder("/", ":");
            ArgumentBuilder linkBuilder = new ArgumentBuilder("/", ":");

            foreach (var defineConstant in defineConstants)
            {
                DefineConstant(clBuilder, defineConstant.Key, defineConstant.Value);
            }
            DefineConstant(clBuilder, "_WIN32");

            // Suppresses error C4996 for 'getenv' (in debuggees/kitchensink/src/environment.cpp)
            DefineConstant(clBuilder, "_CRT_SECURE_NO_WARNINGS");

            if (options.HasFlag(CompilerOption.GenerateSymbols))
            {
                clBuilder.AppendNamedArgument("ZI", null);
                clBuilder.AppendNamedArgument("Debug", null);
            }

            if (!options.HasFlag(CompilerOption.OptimizeLevel1) &&
                !options.HasFlag(CompilerOption.OptimizeLevel2) &&
                !options.HasFlag(CompilerOption.OptimizeLevel3))
            {
                // Disable optimization
                clBuilder.AppendNamedArgument("Od", null);
            }

            // Add options that are set by default in VS
            AddDefaultOptions(clBuilder);
            
            if (this.Settings.Properties != null)
            {
                // Get the include folders from the compiler properties
                string rawIncludes;
                if(!this.Settings.Properties.TryGetValue("Include", out rawIncludes))
                {
                    rawIncludes = String.Empty;
                }

                IEnumerable<string> includes = rawIncludes.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                foreach (string include in includes)
                {
                    clBuilder.AppendNamedArgumentQuoted("I", include, string.Empty);
                }

                // Get the lib folders from the compiler properties
                string rawLibs;
                if (!this.Settings.Properties.TryGetValue("Lib", out rawLibs))
                {
                    rawLibs = String.Empty;
                }

                IEnumerable<string> libs = rawLibs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                foreach (string lib in libs)
                {
                    linkBuilder.AppendNamedArgumentQuoted("LIBPATH", lib);
                }
            }

            switch (outputType)
            {
                case CompilerOutputType.SharedLibrary:
                    // Create.DLL
                    clBuilder.AppendNamedArgument("LD", null);
                    clBuilder.AppendNamedArgument("Fo", Path.GetDirectoryName(targetFilePath) + Path.DirectorySeparatorChar);
                    clBuilder.AppendNamedArgument("Fe", targetFilePath);
                    break;
                case CompilerOutputType.ObjectFile:
                    // Name obj
                    clBuilder.AppendNamedArgument("Fo", targetFilePath);
                    break;
                case CompilerOutputType.Unspecified:
                // Treat Unspecified as Executable, since executable is the default
                case CompilerOutputType.Executable:
                    // Compiling an executable does not have a command line option, it's the default
                    // Name exe
                    clBuilder.AppendNamedArgument("Fo", Path.GetDirectoryName(targetFilePath) + Path.DirectorySeparatorChar);
                    clBuilder.AppendNamedArgument("Fe", targetFilePath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outputType), "Unhandled output type: " + outputType);
            }

            // Specify the PDB name
            clBuilder.AppendNamedArgument("Fd", Path.ChangeExtension(targetFilePath, "pdb"));

            // Define a constant for the platform name.
            DefineConstant(clBuilder, "DEBUGGEE_PLATFORM", GetPlatformName());
            DefineConstant(clBuilder, "DEBUGGEE_COMPILER", "Visual C++");

            foreach (string sourceFilePath in sourceFilePaths)
            {
                clBuilder.AppendArgument(sourceFilePath);
            }

            foreach (string library in libraries)
            {
                clBuilder.AppendArgument(library);
            }

            switch (architecture)
            {
                case SupportedArchitecture.x64:
                    DefineConstant(clBuilder, "DEBUGGEE_ARCH", "64");
                    linkBuilder.AppendNamedArgument("MACHINE", "X64");
                    break;
                case SupportedArchitecture.x86:
                    DefineConstant(clBuilder, "DEBUGGEE_ARCH", "32");
                    linkBuilder.AppendNamedArgument("MACHINE", "X86");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(architecture), "Unhandled target architecture: " + architecture);
            }

            // Pass the linker arguments (Note, the link parameter doesn't use the ":" suffix)
            clBuilder.AppendNamedArgument("link", linkBuilder.ToString(), overrideSuffix: " ");

            return 0 == this.RunCompiler(clBuilder.ToString(), targetFilePath);
        }

        protected static void DefineConstant(ArgumentBuilder clBuilder, string name, string value = null)
        {
            string constant = name;
            if (value != null)
                constant += "=" + ArgumentBuilder.MakeQuotedIfRequired(value);
            clBuilder.AppendNamedArgument("D", constant, overrideSuffix: string.Empty);
        }

        private static void AddDefaultOptions(ArgumentBuilder clBuilder)
        {
            // Make wchar_t a native type
            clBuilder.AppendNamedArgument("Zc", "wchar_t");

            // Remove unreferenced function or data
            clBuilder.AppendNamedArgument("Zc", "inline");

            // Use standard C++ scope rules
            clBuilder.AppendNamedArgument("Zc", "forScope");

            // Minimal rebuild
            clBuilder.AppendNamedArgument("Gm", null);

            // Enable C++ exceptions
            clBuilder.AppendNamedArgument("EHsc", null);

            // Multi-threaded debug dll
            clBuilder.AppendNamedArgument("MDd", null);

            // Floating point mode
            clBuilder.AppendNamedArgument("fp", "precise");

            // Turn on warnings and treat as errors
            clBuilder.AppendNamedArgument("W3", null);
            clBuilder.AppendNamedArgument("WX", null);

            // Turn on sdl warnings
            clBuilder.AppendNamedArgument("sdl", null);

            // __cdecl calling convention
            clBuilder.AppendNamedArgument("Gd", null);
        }
    }
}
