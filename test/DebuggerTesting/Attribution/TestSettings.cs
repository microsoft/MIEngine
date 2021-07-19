// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using DebuggerTesting.Utilities;

namespace DebuggerTesting
{
    internal sealed class TestSettings :
        ITestSettings
    {
        #region Constructor

        internal TestSettings(
            SupportedArchitecture debuggeeArchitecture,
            string compilerName,
            SupportedCompiler compilerType,
            string compilerPath,
            IDictionary<string, string> compilerProperties,
            string debuggerName,
            SupportedDebugger debuggerType,
            string debuggerPath,
            string debuggerAdapterPath,
            string miMode,
            IDictionary<string, string> debuggerProperties)
        {
            this.CompilerSettings = new CompilerSettings(compilerName, compilerType, compilerPath, debuggeeArchitecture, compilerProperties);
            this.DebuggerSettings = new DebuggerSettings(debuggerName, debuggerType, debuggerPath, debuggerAdapterPath, miMode, debuggeeArchitecture, debuggerProperties);
        }

        private TestSettings(ITestSettings original, string name)
        {
            this.Name = name;
            this.CompilerSettings = original.CompilerSettings;
            this.DebuggerSettings = original.DebuggerSettings;
        }

        #endregion

        #region Methods

        public static bool operator ==(TestSettings left, TestSettings right)
        {
            if (Object.ReferenceEquals(left, null))
                return Object.ReferenceEquals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(TestSettings left, TestSettings right)
        {
            if (Object.ReferenceEquals(left, null))
                return !Object.ReferenceEquals(right, null);

            return !left.Equals(right);
        }

        internal static ITestSettings CloneWithName(ITestSettings original, string name)
        {
            return new TestSettings(original, name);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as TestSettings);
        }

        public bool Equals(TestSettings obj)
        {
            if (Object.ReferenceEquals(obj, null))
                return false;

            if (!String.Equals(this.Name, obj.Name, StringComparison.Ordinal))
                return false;

            if (this.CompilerSettings != obj.CompilerSettings)
                return false;

            if (this.DebuggerSettings != obj.DebuggerSettings)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashUtilities.CombineHashCodes(
                this.Name?.GetHashCode() ?? 0,
                this.CompilerSettings?.GetHashCode() ?? 0,
                this.DebuggerSettings?.GetHashCode() ?? 0);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.CompilerSettings.CompilerName);
            builder.Append("/");
            builder.Append(this.CompilerSettings.DebuggeeArchitecture);
            builder.Append("/");
            builder.Append(this.DebuggerSettings.DebuggerName);
            return builder.ToString();
        }

        #endregion

        #region Properties

        public string Name { get; private set; }

        public ICompilerSettings CompilerSettings { get; private set; }

        public IDebuggerSettings DebuggerSettings { get; private set; }

        #endregion
    }
}
