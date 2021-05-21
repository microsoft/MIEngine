// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace DebuggerTesting.Utilities
{
    /// <summary>
    /// A class that builds an arguments string
    /// </summary>
    public class ArgumentBuilder
    {
        #region Constructor

        /// <summary>
        /// Constructs a new argument builder
        /// </summary>
        /// <param name="prefix">String to use in front of the name portion of an argument.</param>
        /// <param name="suffix">String to use in between the name and value portions of an argument.</param>
        public ArgumentBuilder(string prefix, string suffix)
        {
            this.builder = new StringBuilder();
            this.prefix = prefix;
            this.suffix = suffix;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Appends an argument value to the builder
        /// </summary>
        public void AppendArgument(string value)
        {
            if (!String.IsNullOrWhiteSpace(value))
            {
                this.builder.Append(" ");
                this.builder.Append(value);
            }
        }

        /// <summary>
        /// Appends an argument value with surrounding quotes to the builder
        /// </summary>
        public void AppendArgumentQuoted(string value)
        {
            if (!String.IsNullOrWhiteSpace(value))
                this.AppendArgument("\"" + value + "\"");
        }

        /// <summary>
        /// Appends an argument name to the builder
        /// </summary>
        private void AppendName(string name)
        {
            Parameter.ThrowIfNullOrWhiteSpace(name, nameof(name));

            builder.Append(" ");
            if (!String.IsNullOrEmpty(this.prefix))
            {
                builder.Append(this.prefix);
            }
            builder.Append(name);
        }

        /// <summary>
        /// Appends an argument name and value to the builder
        /// </summary>
        public void AppendNamedArgument(string name, string value, string overrideSuffix = null)
        {
            Parameter.ThrowIfNullOrWhiteSpace(name, nameof(name));

            this.AppendName(name);
            if (!String.IsNullOrWhiteSpace(value))
            {
                string suffix = overrideSuffix ?? this.suffix;
                if (!String.IsNullOrEmpty(suffix))
                {
                    builder.Append(suffix);
                }
                this.builder.Append(value);
            }
        }

        /// <summary>
        /// Appends an argument name and value with surrounding quotes to the builder
        /// </summary>
        public void AppendNamedArgumentQuoted(string name, string value, string overrideSuffix = null)
        {
            Parameter.ThrowIfNullOrWhiteSpace(name, nameof(name));

            if (!String.IsNullOrWhiteSpace(value))
                this.AppendNamedArgument(name, ArgumentBuilder.MakeQuoted(value), overrideSuffix);
        }

        public static string MakeQuotedIfRequired(string value)
        {
            if (value.Contains("\"") || value.Contains("'") || value.Contains(" "))
                return MakeQuoted(value);
            return value;
        }

        public static string MakeQuoted(string value)
        {
            if (null == value)
                return null;

            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        /// <summary>
        /// Renders the argument string from the builder
        /// </summary>
        public override string ToString()
        {
            return this.builder?.ToString().Trim();
        }

        #endregion

        #region Fields

        private StringBuilder builder = null;
        private string prefix = null;
        private string suffix = null;

        #endregion
    }
}
