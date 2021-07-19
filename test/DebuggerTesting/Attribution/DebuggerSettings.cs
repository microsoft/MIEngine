// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DebuggerTesting.Utilities;

namespace DebuggerTesting
{
    internal sealed class DebuggerSettings :
        IDebuggerSettings
    {
        #region Constructor

        public DebuggerSettings(
            string debuggerName,
            SupportedDebugger debuggerType,
            string debuggerPath,
            string debuggerAdapterPath,
            string miMode,
            SupportedArchitecture debuggeeArchitecture,
            IDictionary<string, string> debuggerProperties)
        {
            this.DebuggeeArchitecture = debuggeeArchitecture;
            this.DebuggerName = debuggerName;
            this.DebuggerType = debuggerType;
            this.DebuggerPath = debuggerPath;
            this.DebuggerAdapterPath = debuggerAdapterPath;
            if (!string.IsNullOrWhiteSpace(miMode))
                this.MIMode = miMode;
            this.Properties = debuggerProperties ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        #endregion

        #region Methods

        public static bool operator ==(DebuggerSettings left, DebuggerSettings right)
        {
            if (Object.ReferenceEquals(left, null))
                return Object.ReferenceEquals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(DebuggerSettings left, DebuggerSettings right)
        {
            if (Object.ReferenceEquals(left, null))
                return !Object.ReferenceEquals(right, null);

            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as DebuggerSettings);
        }

        public bool Equals(DebuggerSettings obj)
        {
            if (Object.ReferenceEquals(obj, null))
                return false;

            if (this.DebuggeeArchitecture != obj.DebuggeeArchitecture)
                return false;

            if (!String.Equals(this.DebuggerName, obj.DebuggerName, StringComparison.Ordinal))
                return false;

            if (this.DebuggerType != obj.DebuggerType)
                return false;

            if (!String.Equals(this.DebuggerPath, obj.DebuggerPath, StringComparison.Ordinal))
                return false;

            if (!String.Equals(this.MIMode, obj.MIMode, StringComparison.Ordinal))
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
                this.DebuggeeArchitecture.GetHashCode(),
                this.DebuggerName?.GetHashCode() ?? 0,
                this.DebuggerType.GetHashCode(),
                this.DebuggerPath?.GetHashCode() ?? 0,
                this.MIMode?.GetHashCode() ?? 0);
        }

        public override string ToString()
        {
            return "Debugger - Name: {0} Type: {1} ({2}) Path: {3} MIMode: {4}".FormatInvariantWithArgs(this.DebuggerName, this.DebuggerType, this.DebuggeeArchitecture, this.DebuggerPath, this.MIMode);
        }

        #endregion

        #region Properties

        public SupportedArchitecture DebuggeeArchitecture { get; private set; }

        public string DebuggerName { get; private set; }

        public SupportedDebugger DebuggerType { get; private set; }

        public string DebuggerPath { get; private set; }

        public string DebuggerAdapterPath { get; private set; }

        public string MIMode { get; private set; }

        public IDictionary<string, string> Properties { get; private set; }

        #endregion
    }
}
