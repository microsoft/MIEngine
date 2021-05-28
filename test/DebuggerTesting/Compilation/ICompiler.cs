// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace DebuggerTesting.Compilation
{
    internal interface ICompiler
    {
        #region Methods

        void Compile(
            CompilerOutputType outputType,
            IEnumerable<string> libraries,
            IEnumerable<string> sourceFilePaths,
            string targetFilePath,
            CompilerOption options,
            IDictionary<string, string> defineConstants);

        #endregion
    }
}
