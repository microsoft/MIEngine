// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.Utilities;

namespace DebuggerTesting.Compilation
{
    internal sealed class GppCompiler :
        GppStyleCompiler
    {
        public GppCompiler(ILoggingComponent logger, ICompilerSettings settings)
            : base(logger, settings)
        {
        }

        protected override void SetAdditionalArguments(ArgumentBuilder builder)
        {
            base.SetAdditionalArguments(builder);
            DefineConstant(builder, "DEBUGGEE_COMPILER", "G++");
        }
    }
}
