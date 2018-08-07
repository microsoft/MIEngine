// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace OpenDebugAD7
{
    /// <summary>The various categories of debugger event logging.</summary>
    internal enum LoggingCategory
    {
        /// <summary>Standard out stream from the debuggee.</summary>
        StdOut,
        /// <summary>Standard error stream from the debuggee.</summary>
        StdErr,
        /// <summary>Status messages from the debugger.</summary>
        DebuggerStatus,
        /// <summary>Error messages from the debugger.</summary>
        DebuggerError,
        /// <summary>Diagnostic engine logs.</summary>
        EngineLogging,
        /// <summary>Event tracing between the debug adapter and VS Code.</summary>
        AdapterTrace,
        /// <summary>Response tracing between the debug adapter and VS Code.</summary>
        AdapterResponse,
        /// <summary>Telemetry messages.</summary>
        Telemetry,
        /// <summary>Exception messages.</summary>
        Exception,
        /// <summary>Module load/unload events.</summary>
        Module,
        /// <summary>Process exit message.</summary>
        ProcessExit,
    }

    /// <summary>Logging class to handle when and how various classes of output should be logged.</summary>
    internal class DebugEventLogger
    {
        private readonly Dictionary<LoggingCategory, bool> _isLoggingEnabled;
        private Action<DebugEvent> _sendToOutput;

        private Action<DebugEvent> _traceCallback;

        /// <summary>
        /// Create a new <see cref="DebugEventLogger"/> to log events to the given logging callback.
        /// </summary>
        /// <param name="outputCallback">Callback to manage the logging output.</param>
        public DebugEventLogger(Action<DebugEvent> outputCallback, List<LoggingCategory> loggingCategories)
        {
            Debug.Assert(outputCallback != null, "Trying to create a logger with no callback!");
            _sendToOutput = outputCallback;

            _isLoggingEnabled = new Dictionary<LoggingCategory, bool>()
            {
                // Default on categories
                { LoggingCategory.StdOut,         true },
                { LoggingCategory.StdErr,         true },
                { LoggingCategory.DebuggerStatus, true },
                { LoggingCategory.DebuggerError,  true },
                { LoggingCategory.Telemetry,      true },
                { LoggingCategory.Exception,      true },
                { LoggingCategory.Module,         true },
                { LoggingCategory.ProcessExit,    true },

                // Default off categories
                { LoggingCategory.EngineLogging,   false },
                { LoggingCategory.AdapterTrace,    false },
                { LoggingCategory.AdapterResponse, false },
            };

            foreach (var category in loggingCategories)
            {
                _isLoggingEnabled[category] = true;
            }

            // If AdapterTrace or AdapterResponse is set at commandline, write trace logging to Console.Error
            if (_isLoggingEnabled[LoggingCategory.AdapterTrace] || _isLoggingEnabled[LoggingCategory.AdapterResponse])
            {
                _traceCallback = (s => Console.Error.Write(s.ToString()));
            }
            else
            {
                _traceCallback = _sendToOutput;
            }
        }

        /// <summary>
        /// Set the logging configuration for a specific category of event.
        /// </summary>
        /// <param name="category">The category of event to modify.</param>
        /// <param name="isEnabled">True if the category should log events, otherwise false to ignore them.</param>
        public void SetLoggingConfiguration(LoggingCategory category, bool isEnabled)
        {
            _isLoggingEnabled[category] = isEnabled;
        }

        /// <summary>
        /// Sends the message line to the logging output callback if the given category has logging enabled.
        /// </summary>
        /// <param name="category">The category of debug event logging.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="data">Logging data to send alongside the message, if any.</param>
        public void WriteLine(LoggingCategory category, string message, Dictionary<string, object> data = null)
        {
            Write(category, message + Environment.NewLine, data);
        }

        /// <summary>
        /// Sends the message to the logging output callback if the given category has logging enabled.
        /// </summary>
        /// <param name="category">The category of debug event logging.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="data">Logging data to send alongside the message, if any.</param>
        public void Write(LoggingCategory category, string message, Dictionary<string, object> data = null)
        {
            // Default to logging the message if we haven't set a status for this category.
            if (_isLoggingEnabled.ContainsKey(category) && !_isLoggingEnabled[category])
            {
                return;
            }

            _sendToOutput.Invoke(CreateOutputEvent(category, message, data));
        }

        public void TraceLogger_EventHandler(object sender, LogEventArgs args)
        {
            string message = args.Message + Environment.NewLine;
            LoggingCategory category = LoggingCategory.DebuggerError;
            if (args.Message.StartsWith("<--  "))
            {
                category = LoggingCategory.AdapterTrace;
            }
            else if (args.Message.StartsWith("--> "))
            {
                category = LoggingCategory.AdapterResponse;
            }

            WriteTrace(category, message);
        }

        private void WriteTrace(LoggingCategory category, string message)
        {
            // Default to logging the message if we haven't set a status for this category.
            if (_isLoggingEnabled.ContainsKey(category) && !_isLoggingEnabled[category])
            {
                return;
            }

            _traceCallback.Invoke(CreateOutputEvent(category, message));
        }

        private OutputEvent CreateOutputEvent(LoggingCategory category, string message, Dictionary<string, object> data = null)
        {
            // By default send debugger messages to the standard console.
            OutputEvent.CategoryValue outputCategory = OutputEvent.CategoryValue.Console;
            switch (category)
            {
                case LoggingCategory.StdOut: outputCategory = OutputEvent.CategoryValue.Stdout; break;
                case LoggingCategory.StdErr: outputCategory = OutputEvent.CategoryValue.Stderr; break;
                case LoggingCategory.DebuggerError: outputCategory = OutputEvent.CategoryValue.Stderr; break;
                case LoggingCategory.Telemetry: outputCategory = OutputEvent.CategoryValue.Telemetry; break;
                default: break;
            }

            return new OutputEvent
            {
                Category = outputCategory,
                Output = message,
                Data = data
            };
        }
    }
}
