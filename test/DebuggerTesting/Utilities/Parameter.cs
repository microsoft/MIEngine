// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace DebuggerTesting
{
    [DebuggerStepThrough]
    public static class Parameter
    {
        #region NotOfType

        /// <summary>
        /// Throws ArgumentException if the value is not of the specified type.
        /// </summary>
        public static void ThrowIfNotOfType<T>(object value, string paramName, bool allowNull = false)
        {
            AssertIfNotOfType<T>(value, paramName, allowNull);
            if (IsNotOfType<T>(value, allowNull))
                throw new ArgumentException(string.Empty, paramName);
        }

        /// <summary>
        /// Assert if the value is not of the specified type.
        /// </summary>
        [DebuggerHidden]
        [Conditional("DEBUG")]
        public static void AssertIfNotOfType<T>(object value, string paramName, bool allowNull = false)
        {
            if (IsNotOfType<T>(value, allowNull))
                ParameterAssert("Parameter {0} not of type {1}", paramName, typeof(T).Name);
        }

        public static void AssertIfNotOfType<T>(Type type, string paramName)
        {
            Parameter.AssertIfNull(type, paramName);
            if (!typeof(T).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                ParameterAssert("Parameter {0} does not implement {1}", paramName, typeof(T).Name);
        }

        private static bool IsNotOfType<T>(object value, bool allowNull)
        {
            return (null == value && !allowNull) || (null != value && !(value is T));
        }

        #endregion

        #region NullOrWhiteSpace

        /// <summary>
        /// Throws ArgumentNullException if the value is null or is white space.
        /// </summary>
        public static void ThrowIfNullOrWhiteSpace(string value, string paramName)
        {
            AssertIfNullOrWhiteSpace(value, paramName);
            if (IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Assert if the value is null or is white space.
        /// </summary>
        [DebuggerHidden]
        [Conditional("DEBUG")]
        public static void AssertIfNullOrWhiteSpace(string value, string paramName)
        {
            if (IsNullOrWhiteSpace(value))
                ParameterAssert("Parameter {0} is null or white space.", paramName);
        }

        private static bool IsNullOrWhiteSpace(string value)
        {
            return String.IsNullOrWhiteSpace(value);
        }

        #endregion

        #region Null

        /// <summary>
        /// Throws ArgumentNullException if the value is null.
        /// </summary>
        public static void ThrowIfNull<T>(T value, string paramName)
            where T : class
        {
            AssertIfNull(value, paramName);
            if (IsNull(value))
                throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Assert if the value is null.
        /// </summary>
        [DebuggerHidden]
        [Conditional("DEBUG")]
        public static void AssertIfNull<T>(T value, string paramName)
            where T : class
        {
            if (IsNull(value))
                ParameterAssert("Parameter {0} is null.", paramName);
        }

        private static bool IsNull<T>(T value)
            where T : class
        {
            return null == value;
        }

        #endregion

        #region IsInvalid

        /// <summary>
        /// Throws ArgumentNullException if the value matches an invalid value.
        /// </summary>
        public static void ThrowIfIsInvalid<T>(T value, T invalidValue, string paramName)
            where T : struct
        {
            AssertIfIsInvalid(value, invalidValue, paramName);
            if (IsInvalid(value, invalidValue))
                throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Assert if the value matches an invalid value.
        /// </summary>
        [DebuggerHidden]
        [Conditional("DEBUG")]
        public static void AssertIfIsInvalid<T>(T value, T invalidValue, string paramName)
            where T : struct
        {
            if (IsInvalid(value, invalidValue))
                ParameterAssert("Parameter {0} is invalid value.", paramName);
        }

        private static bool IsInvalid<T>(T value, T invalidValue)
        {
            return object.Equals(value, invalidValue);
        }

        #endregion

        #region OutOfRange

        /// <summary>
        /// Throw ArgumentOutOfRangeException if value is not between minValue and maxValue inclusively.
        /// </summary>
        public static void ThrowIfOutOfRange(int value, int minValue, int maxValue, string paramName)
        {
            AssertIfOutOfRange(value, minValue, maxValue, paramName);
            if (IsOutOfRange(value, minValue, maxValue))
                throw new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        /// Assert if value is not between minValue and maxValue inclusively.
        /// </summary>
        [DebuggerHidden]
        [Conditional("DEBUG")]
        public static void AssertIfOutOfRange(int value, int minValue, int maxValue, string paramName)
        {
            if (IsOutOfRange(value, minValue, maxValue))
                ParameterAssert("Parameter {0} is out of range. Should be between {1} and {2}.", paramName, minValue, maxValue);
        }

        private static bool IsOutOfRange(int value, int minValue, int maxValue)
        {
            return value < minValue || value > maxValue;
        }

        #endregion

        #region NotPositive

        /// <summary>
        /// Throws ArgumentOutOfRangeException if the value is not a negative or zero.
        /// </summary>
        public static void ThrowIfNegativeOrZero(int value, string paramName)
        {
            AssertIfNegativeOrZero(value, paramName);
            if (IsNegativeOrZero(value))
                throw new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        /// Assert if the value is not a negative or zero.
        /// </summary>
        [DebuggerHidden]
        [Conditional("DEBUG")]
        public static void AssertIfNegativeOrZero(int value, string paramName)
        {
            if (IsNegativeOrZero(value))
                ParameterAssert("Parameter Error: Parameter {0} is not positive.", paramName);
        }

        private static bool IsNegativeOrZero(int value)
        {
            return value <= 0;
        }

        #endregion

        #region ParameterAssert

        [DebuggerHidden]
        [Conditional("DEBUG")]
        private static void ParameterAssert(string format, params object[] parameters)
        {
            string message = "Parameter Error: ";
            if (!string.IsNullOrEmpty(format))
                message += string.Format(CultureInfo.InvariantCulture, format, parameters);

            Debug.WriteLine(message);
            Debug.Fail(message);
        }

        #endregion
    }
}