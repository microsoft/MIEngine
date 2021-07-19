// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DebuggerTesting.Utilities;

namespace DebuggerTesting
{
    internal sealed class CompilerSettings :
        ICompilerSettings
    {
        #region Constructor

        public CompilerSettings(
            string compilerName, 
            SupportedCompiler compilerType, 
            string compilerPath, 
            SupportedArchitecture debuggeeArchitecture,
            IDictionary<string, string> compilerProperties)
        {
            this.CompilerName = compilerName;
            this.CompilerType = compilerType;
            this.CompilerPath = compilerPath;
            this.DebuggeeArchitecture = debuggeeArchitecture;
            this.Properties = compilerProperties ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        #endregion

        #region Methods

        public static bool operator ==(CompilerSettings left, CompilerSettings right)
        {
            if (Object.ReferenceEquals(left, null))
                return Object.ReferenceEquals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(CompilerSettings left, CompilerSettings right)
        {
            if (Object.ReferenceEquals(left, null))
                return !Object.ReferenceEquals(right, null);

            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as CompilerSettings);
        }

        public bool Equals(CompilerSettings obj)
        {
            if (Object.ReferenceEquals(obj, null))
                return false;

            if (!String.Equals(this.CompilerName, obj.CompilerName, StringComparison.Ordinal))
                return false;

            if (this.CompilerType != obj.CompilerType)
                return false;

            if (!String.Equals(this.CompilerPath, obj.CompilerPath, StringComparison.Ordinal))
                return false;

            if (this.DebuggeeArchitecture != obj.DebuggeeArchitecture)
                return false;

            if (!Enumerable.SequenceEqual(this.Properties.Keys, obj.Properties.Keys, StringComparer.Ordinal))
                return false;

            if (!Enumerable.SequenceEqual(this.Properties.Values, obj.Properties.Values, StringComparer.Ordinal))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashUtilities.CombineHashCodes(
                this.CompilerName?.GetHashCode() ?? 0,
                this.CompilerType.GetHashCode(),
                this.CompilerPath?.GetHashCode() ?? 0,
                this.DebuggeeArchitecture.GetHashCode());
        }

        public override string ToString()
        {
            return "Compiler - Name: {0} Type: {1} ({2}) Path: {3}".FormatInvariantWithArgs(this.CompilerName, this.CompilerType, this.DebuggeeArchitecture, this.CompilerPath);
        }

        #endregion

        #region Properties

        public string CompilerName { get; private set; }

        public SupportedCompiler CompilerType { get; private set; }

        public string CompilerPath { get; private set; }

        public SupportedArchitecture DebuggeeArchitecture { get; private set; }

        public IDictionary<string,string> Properties { get; private set; }

        #endregion
    }
}
