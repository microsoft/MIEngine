// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections.Generic;
using DebuggerTesting.Utilities;

namespace DebuggerTesting.Compilation
{
    internal sealed class XCodeCompiler :
        CompilerBase
    {
        #region Constructor

        public XCodeCompiler(ILoggingComponent logger, ICompilerSettings settings)
            : base(logger, settings, false)
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
            if (outputType != CompilerOutputType.MacOSApp)
            {
                throw new InvalidOperationException("Output type is: " + outputType);
            }

            //xcodebuild -project TestApp.xcodeproj -configuration Debug -scheme "TestApp (macOS)" CONFIGURATION_BUILD_DIR="./out/xcoderun/x64/"

            string project = string.Empty;
            foreach (string sourceFile in sourceFilePaths)
            {
                if (sourceFile.EndsWith(".xcodeproj"))
                {
                    project = sourceFile;
                }
            }

            if (string.IsNullOrWhiteSpace(project))
            {
                throw new InvalidOperationException(".xcodeproj is missing");
            }

            ArgumentBuilder builder = new ArgumentBuilder("-", " ");
            builder.AppendNamedArgument("project", project);
            builder.AppendNamedArgument("configuration", "Debug");
            builder.AppendNamedArgumentQuoted("scheme", "TestApp (macOS)");

            return 0 == this.RunCompiler(builder.ToString() + " CONFIGURATION_BUILD_DIR=\"" + Path.GetDirectoryName(targetFilePath) + "\"", targetFilePath);
        }

        #endregion
    }
}
