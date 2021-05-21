// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.Extensions;
using System;
using System.Globalization;
using System.Text;
using Xunit;

namespace DebuggerTesting.OpenDebug.CrossPlatCpp
{
    /// <summary>
    /// Provides helpers to give value types that are returned in different formats by different debugggers
    /// </summary>
    internal static class InspectorValueExtensions
    {
        #region Double

        public static void AssertValueAsDouble(this IVariableInspector valueInspector, double expectedValue)
        {
            AssertDoubleValue(valueInspector.DebuggerRunner.DebuggerSettings, valueInspector.Value, expectedValue);
        }

        public static void AssertEvaluateAsDouble(this IFrameInspector frameInspector, string expression, EvaluateContext context, double expectedValue)
        {
            string actualValue = frameInspector.Evaluate(expression, context);
            AssertDoubleValue(frameInspector.DebuggerRunner.DebuggerSettings, actualValue, expectedValue);
        }

        private static void AssertDoubleValue(IDebuggerSettings debuggerSettings, string actualValue, double expectedValue)
        {
            Assert.StartsWith(InspectorValueExtensions.GetDoubleValue(debuggerSettings, expectedValue), actualValue, StringComparison.Ordinal);
        }

        private static string GetDoubleValue(IDebuggerSettings debuggerSettings, double value)
        {
            if (double.IsPositiveInfinity(value))
            {
                switch (debuggerSettings.DebuggerType)
                {
                    case SupportedDebugger.Gdb_Gnu:
                    case SupportedDebugger.Gdb_Cygwin:
                    case SupportedDebugger.Gdb_MinGW:
                    case SupportedDebugger.VsDbg:
                        return "inf";
                    case SupportedDebugger.Lldb:
                        return "+Inf";
                    default:
                        Assert.True(false, "Debugger type doesn't have a specification for double value");
                        return null;
                }
            }

            switch (debuggerSettings.DebuggerType)
            {
                case SupportedDebugger.Gdb_Gnu:
                case SupportedDebugger.Gdb_Cygwin:
                case SupportedDebugger.Gdb_MinGW:
                case SupportedDebugger.Lldb:
                    //10.1
                    //0
                    return value.ToString("R", CultureInfo.InvariantCulture);
                case SupportedDebugger.VsDbg:
                    //10.100000000000000
                    //0.00000000000000000
                    return value.ToString("G10", CultureInfo.InvariantCulture);
                default:
                    Assert.True(false, "Debugger type doesn't have a specification for double value");
                    return null;
            }
        }

        #endregion

        #region Float

        public static void AssertValueAsFloat(this IVariableInspector valueInspector, float expectedValue)
        {
            AssertFloatValue(valueInspector.DebuggerRunner.DebuggerSettings, valueInspector.Value, expectedValue);
        }

        public static void AssertEvaluateAsFloat(this IFrameInspector frameInspector, string expression, EvaluateContext context, float expectedValue)
        {
            string actualValue = frameInspector.Evaluate(expression, context);
            AssertFloatValue(frameInspector.DebuggerRunner.DebuggerSettings, actualValue, expectedValue);
        }

        private static void AssertFloatValue(IDebuggerSettings debuggerSettings, string actualValue, float expectedValue)
        {
            Assert.StartsWith(InspectorValueExtensions.GetFloatValue(debuggerSettings, expectedValue), actualValue, StringComparison.Ordinal);
        }

        private static string GetFloatValue(IDebuggerSettings debuggerSettings, float value)
        {
            if (float.IsPositiveInfinity(value))
            {
                switch (debuggerSettings.DebuggerType)
                {
                    case SupportedDebugger.Gdb_Gnu:
                    case SupportedDebugger.Gdb_Cygwin:
                    case SupportedDebugger.Gdb_MinGW:
                    case SupportedDebugger.VsDbg:
                        return "inf";
                    case SupportedDebugger.Lldb:
                        return "+Inf";
                    default:
                        Assert.True(false, "Debugger type doesn't have a specification for float value");
                        return null;
                }
            }

            switch (debuggerSettings.DebuggerType)
            {
                case SupportedDebugger.Gdb_Gnu:
                case SupportedDebugger.Gdb_Cygwin:
                case SupportedDebugger.Gdb_MinGW:
                case SupportedDebugger.Lldb:
                    return value.ToString("R", CultureInfo.InvariantCulture);
                case SupportedDebugger.VsDbg:
                    return value.ToString("G5", CultureInfo.InvariantCulture);
                default:
                    Assert.True(false, "Debugger type doesn't have a specification for float value");
                    return null;
            }
        }

        #endregion

        #region Char

        public static void AssertValueAsChar(this IVariableInspector valueInspector, char expectedValue)
        {
            AssertCharValue(valueInspector.DebuggerRunner.DebuggerSettings, valueInspector.Value, expectedValue);
        }

        public static void AssertEvaluateAsChar(this IFrameInspector frameInspector, string expression, EvaluateContext context, char expectedValue)
        {
            string actualValue = frameInspector.Evaluate(expression, context);
            AssertCharValue(frameInspector.DebuggerRunner.DebuggerSettings, actualValue, expectedValue);
        }

        private static void AssertCharValue(IDebuggerSettings debuggerSettings, string actualValue, char expectedValue)
        {
            Assert.Equal(InspectorValueExtensions.GetCharValue(debuggerSettings, expectedValue), actualValue);
        }

        private static string GetCharValue(IDebuggerSettings debuggerSettings, char value)
        {
            // Non-printable chars
            if (value < ' ')
            {
                switch (debuggerSettings.DebuggerType)
                {
                    case SupportedDebugger.Gdb_Gnu:
                    case SupportedDebugger.Gdb_Cygwin:
                    case SupportedDebugger.Gdb_MinGW:
                        // 0 '\000'
                        return @"{0} '\{0:D3}'".FormatInvariantWithArgs((int)value);
                    case SupportedDebugger.Lldb:
                    case SupportedDebugger.VsDbg:
                        // 0 '\0'
                        return @"{0} '\{0}'".FormatInvariantWithArgs((int)value);
                    default:
                        Assert.True(false, "Debugger type doesn't have a specification for char value");
                        return null;
                }
            }

            // Printable chars
            switch (debuggerSettings.DebuggerType)
            {
                case SupportedDebugger.Gdb_Gnu:
                case SupportedDebugger.Gdb_Cygwin:
                case SupportedDebugger.Gdb_MinGW:
                    return EscapeIfNeeded(c => c == '\'', value);
                case SupportedDebugger.VsDbg:
                    return EscapeIfNeeded(c => c == '"', value);
                case SupportedDebugger.Lldb:
                    return "{0} '{1}'".FormatInvariantWithArgs((int)value, value);
                default:
                    Assert.True(false, "Debugger type doesn't have a specification for char value");
                    return null;
            }
        }

        private static string EscapeIfNeeded(Func<char, bool> shouldEscape, char value)
        {
            string valueAsString = shouldEscape(value)
                ? "\\{0}".FormatInvariantWithArgs(value)
                : "{0}".FormatInvariantWithArgs(value);
            return "{0} '{1}'".FormatInvariantWithArgs((int)value, valueAsString);
        }

        #endregion

        #region WChar

        public static void AssertValueAsWChar(this IVariableInspector valueInspector, char expectedValue)
        {
            AssertWCharValue(valueInspector.DebuggerRunner.DebuggerSettings, valueInspector.Value, expectedValue);
        }

        public static void AssertEvaluateAsWChar(this IFrameInspector frameInspector, string expression, EvaluateContext context, char expectedValue)
        {
            string actualValue = frameInspector.Evaluate(expression, context);
            AssertWCharValue(frameInspector.DebuggerRunner.DebuggerSettings, actualValue, expectedValue);
        }

        private static void AssertWCharValue(IDebuggerSettings debuggerSettings, string actualValue, char expectedValue)
        {
            Assert.Equal(InspectorValueExtensions.GetWCharValue(debuggerSettings, expectedValue), actualValue);
        }

        private static string GetWCharValue(IDebuggerSettings debuggerSettings, char value)
        {
            switch (debuggerSettings.DebuggerType)
            {
                case SupportedDebugger.Gdb_Gnu:
                case SupportedDebugger.Gdb_Cygwin:
                case SupportedDebugger.Gdb_MinGW:
                    return "{0} L'{1}'".FormatInvariantWithArgs((int)value, value);
                case SupportedDebugger.Lldb:
                    return "L'{0}'".FormatInvariantWithArgs(value);
                case SupportedDebugger.VsDbg:
                    return "{0} '{1}'".FormatInvariantWithArgs((int)value, value);
                default:
                    Assert.True(false, "Debugger type doesn't have a specification for wchar value");
                    return null;
            }
        }

        #endregion

        #region Undefined Variable

        public static void AssertEvaluateAsError(this IFrameInspector frameInspector, string variableName, EvaluateContext context)
        {
            string actualErrorMessage = frameInspector.Evaluate(variableName, context);
            string expectedErrorMessage = GetUndefinedError(frameInspector.DebuggerRunner.DebuggerSettings, variableName);
            Assert.Contains(expectedErrorMessage, actualErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetUndefinedError(IDebuggerSettings debuggerSettings, string variableName)
        {
            switch (debuggerSettings.DebuggerType)
            {
                case SupportedDebugger.Gdb_Gnu:
                case SupportedDebugger.Gdb_Cygwin:
                case SupportedDebugger.Gdb_MinGW:
                    return "-var-create: unable to create variable object";
                case SupportedDebugger.Lldb:
                    return "error: error: use of undeclared identifier '{0}'".FormatInvariantWithArgs(variableName);
                case SupportedDebugger.VsDbg:
                    return @"identifier ""{0}"" is undefined".FormatInvariantWithArgs(variableName);
                default:
                    Assert.True(false, "This debugger type doesn't have a error message test.");
                    return null;
            }
        }

        #endregion

        #region Vector

        public static void AssertValueAsVector(this IVariableInspector valueInspector, int vectorCount)
        {
            AssertVectorValue(valueInspector.DebuggerRunner.DebuggerSettings, valueInspector.Value, vectorCount);
        }

        public static void AssertEvaluateAsVector(this IFrameInspector frameInspector, string expression, EvaluateContext context, int vectorCount)
        {
            string actualValue = frameInspector.Evaluate(expression, context);
            AssertVectorValue(frameInspector.DebuggerRunner.DebuggerSettings, actualValue, vectorCount);
        }

        private static void AssertVectorValue(IDebuggerSettings debuggerSettings, string actualValue, int vectorCount)
        {
            Assert.Equal(InspectorValueExtensions.GetVectorValue(debuggerSettings, vectorCount), actualValue);
        }

        private static string GetVectorValue(IDebuggerSettings debuggerSettings, int vectorCount)
        {
            switch (debuggerSettings.DebuggerType)
            {
                case SupportedDebugger.Gdb_Gnu:
                case SupportedDebugger.Gdb_Cygwin:
                case SupportedDebugger.Gdb_MinGW:
                    return "{...}";
                case SupportedDebugger.VsDbg:
                    return "{{ size={0} }}".FormatInvariantWithArgs(vectorCount);
                case SupportedDebugger.Lldb:
                    return "size={0}".FormatInvariantWithArgs(vectorCount);
                default:
                    Assert.True(false, "This debugger type doesn't have a vector format.");
                    return null;
            }
        }

        #endregion

        #region Object

        public static void AssertValueAsObject(this IVariableInspector valueInspector, params string[] expectedObjectProperties)
        {
            AssertObjectValue(valueInspector.DebuggerRunner.DebuggerSettings, valueInspector.Value, expectedObjectProperties);
        }

        public static void AssertEvaluateAsObject(this IFrameInspector frameInspector, string expression, EvaluateContext context, params string[] expectedObjectProperties)
        {
            string actualValue = frameInspector.Evaluate(expression, context);
            AssertObjectValue(frameInspector.DebuggerRunner.DebuggerSettings, actualValue, expectedObjectProperties);
        }

        private static void AssertObjectValue(IDebuggerSettings debuggerSettings, string actualValue, params string[] expectedObjectProperties)
        {
            Assert.Equal(InspectorValueExtensions.GetObjectValue(debuggerSettings, expectedObjectProperties), actualValue);
        }

        private static string GetObjectValue(IDebuggerSettings debuggerSettings, params string[] properties)
        {
            if (properties.Length % 2 != 0)
            {
                throw new ArgumentException("Missing a property!");
            }

            switch (debuggerSettings.DebuggerType)
            {
                case SupportedDebugger.Gdb_Gnu:
                case SupportedDebugger.Gdb_Cygwin:
                case SupportedDebugger.Gdb_MinGW:
                case SupportedDebugger.Lldb:
                    return "{...}";
                case SupportedDebugger.VsDbg:
                    StringBuilder sb = new StringBuilder("{");

                    for (int i = 0; i < properties.Length; i += 2)
                    {
                        string name = properties[i];
                        string value = properties[i + 1];
                        sb.Append(name);
                        sb.Append("=");
                        sb.Append(value);
                        sb.Append(" ");
                    }
                    sb.Append("}");
                    return sb.ToString();
                default:
                    Assert.True(false, "This debugger type doesn't have an object format.");
                    return null;
            }
        }

        #endregion

        #region IntArray

        public static void AssertValueAsIntArray(this IVariableInspector valueInspector, params int[] expectedValue)
        {
            AssertIntArrayValue(valueInspector.DebuggerRunner.DebuggerSettings, valueInspector.Value, expectedValue);
        }

        public static void AssertEvaluateAsIntArray(this IFrameInspector frameInspector, string expression, EvaluateContext context, params int[] expectedValue)
        {
            string actualValue = frameInspector.Evaluate(expression, context);
            AssertIntArrayValue(frameInspector.DebuggerRunner.DebuggerSettings, actualValue, expectedValue);
        }


        private static void AssertIntArrayValue(IDebuggerSettings debuggerSettings, string actualValue, params int[] expectedValue)
        {
            Assert.Contains(GetIntArray(debuggerSettings, expectedValue), actualValue, StringComparison.Ordinal);
        }

        private static string GetIntArray(IDebuggerSettings debuggerSettings, params int[] value)
        {
            switch (debuggerSettings.DebuggerType)
            {
                case SupportedDebugger.Gdb_Gnu:
                case SupportedDebugger.Gdb_Cygwin:
                case SupportedDebugger.Gdb_MinGW:
                case SupportedDebugger.Lldb:
                    return "[{0}]".FormatInvariantWithArgs(value.Length);
                case SupportedDebugger.VsDbg:
                    StringBuilder sb = new StringBuilder("{");
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (i != 0)
                            sb.Append(", ");
                        sb.Append(value[i]);
                    }
                    sb.Append("}");
                    return sb.ToString();
                default:
                    Assert.True(false, "This debugger type doesn't have an object format.");
                    return null;
            }

        }

        #endregion

        #region String / char*

        /// <summary>
        /// This method has only been used for const char* so far.
        /// If required to work with other string-like types, please test it and
        /// expand this method to support those types.
        /// </summary>
        /// <param name="frameInspector"></param>
        /// <param name="expression"></param>
        /// <param name="context"></param>
        /// <param name="expectedValue"></param>
        public static void AssertEvaluateAsString(
            this IFrameInspector frameInspector,
            string expression,
            EvaluateContext context,
            string expectedValue)
        {
            if (expectedValue == null)
            {
                AssertEvaluateAsNull(frameInspector, expression, context);
                return;
            }

            for (int i = 0; i <= expectedValue.Length; i++)
            {
                string actualValue = frameInspector.Evaluate(
                    string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", expression, i), context);
                char expectedChar; ;
                if (i == expectedValue.Length)
                {
                    expectedChar = '\0';
                }
                else
                {
                    expectedChar = expectedValue[i];
                }

                AssertCharValue(frameInspector.DebuggerRunner.DebuggerSettings, actualValue, expectedChar);
            }
        }

        #endregion

        #region Pointer

        /// <summary>
        /// Checks whether a pointer value is null.
        /// This function will return incorrect results if not called with an expression of type pointer.
        /// </summary>
        /// <param name="frameInspector"></param>
        /// <param name="expression"></param>
        /// <param name="context"></param>
        public static void AssertEvaluateAsNull(
            this IFrameInspector frameInspector,
            string expression,
            EvaluateContext context)
        {
            string actualValue = frameInspector.Evaluate(
                string.Format(CultureInfo.InvariantCulture, "{0}==0", expression), context);
            Assert.Equal("true", actualValue);
        }

        #endregion
    }
}
