// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using OpenDebug;

namespace OpenDebugAD7
{
    internal sealed class DebuggerTelemetry
    {
        private static DebuggerTelemetry s_telemetryInstance;
        private Action<DebugEvent> _callback;

        // Telemetry Prefixes
        private const string EventNamePrefix = @"VS/Diagnostics/Debugger/";
        private const string PropertyNamePrefix = @"VS.Diagnostics.Debugger.";

        // Debug Completed Telemetry event constants
        public const string TelemetryDebugCompletedEventName = "DebugCompleted";
        public const string TelemetryBreakCounter = TelemetryDebugCompletedEventName + ".BreakCounter";

        // Debug Event Names
        public const string TelemetryLaunchEventName = "Launch";
        public const string TelemetryAttachEventName = "Attach";
        public const string TelemetryEvaluateEventName = "Evaluate";
        public const string TelemetryPauseEventName = "Pause";
        public const string TelemetryTracepointEventName = "Tracepoint";

        // Common telemetry properties
        public const string TelemetryErrorType = "ErrorType";
        public const string TelemetryErrorCode = "ErrorCode";
        public const string TelemetryMessage = "Message";
        public const string TelemetryDuration = "Duration";
        public const string TelemetryIsError = "IsError";

        // Common property names and values
        private const string TelemetryImplementationName = "ImplementationName";
        private const string TelemetryEngineVersion = "EngineVersion";
        private const string TelemetryHostVersion = "HostVersion";
        private const string TelemetryAdapterId = "AdapterId";
        private string _engineName;
        private string _engineVersion;
        private string _hostVersion;
        private string _adapterId;

        // Specific telemetry properties
        public const string TelemetryIsCoreDump = TelemetryLaunchEventName + ".IsCoreDump";
        public const string TelemetryIsNoDebug = TelemetryLaunchEventName + ".IsNoDebug";
        public const string TelemetryUsesDebugServer = TelemetryLaunchEventName + ".UsesDebugServer";
        public const string TelemetryExecuteInConsole = TelemetryEvaluateEventName + ".ExecuteInConsole";
        public const string TelemetryVisualizerFileUsed = "VisualizerFileUsed";
        public const string TelemetrySourceFileMappings = "SourceFileMappings";
        public const string TelemetryMIMode = "MIMode";
        public const string TelemetryFrameworkVersion = "FrameworkVersion";
        public const string TelemetryStackFrameId = TelemetryExecuteInConsole + ".StackFrameId";

        private DebuggerTelemetry(Action<DebugEvent> callback, TypeInfo engineType, TypeInfo hostType, string adapterId)
        {
            Debug.Assert(_engineName == null && _engineVersion == null, "InitializeTelemetry called more than once?");

            _callback = callback;
            _engineName = engineType.Namespace;
            _engineVersion = GetVersionAttributeValue(engineType);
            _hostVersion = GetVersionAttributeValue(hostType);
            _adapterId = adapterId;
        }

        #region Public Static Methods

        public static void InitializeTelemetry(Action<DebugEvent> telemetryCallback, TypeInfo engineType, TypeInfo hostType, string adapterId)
        {
            Debug.Assert(telemetryCallback != null, "InitializeTelemetry called with incorrect values.");

            s_telemetryInstance = new DebuggerTelemetry(telemetryCallback, engineType, hostType, adapterId);
        }

        /// <summary>
        /// Report an error with a message.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="message">Error message</param>
        /// <param name="eventProperties">[Optional] Other event properties</param>
        public static void ReportError(string eventName, string message, Dictionary<string, object> eventProperties = null)
        {
            DebuggerTelemetry.s_telemetryInstance.ReportErrorInternal(eventName, null, message, eventProperties);
        }

        /// <summary>
        /// Report an error with error code
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="errorCode">Error code</param>
        /// <param name="eventProperties">[Optional] Other event properties</param>
        public static void ReportError(string eventName, int errorCode, Dictionary<string, object> eventProperties = null)
        {
            DebuggerTelemetry.s_telemetryInstance?.ReportErrorInternal(eventName, errorCode, String.Empty, eventProperties);
        }

        /// <summary>
        /// Report an error with error code and message.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="errorCode">Error code</param>
        /// <param name="message">Error message</param>
        /// <param name="eventProperties">[Optional] Other event properties</param>
        public static void ReportError(string eventName, int errorCode, string message, Dictionary<string, object> eventProperties = null)
        {
            DebuggerTelemetry.s_telemetryInstance?.ReportErrorInternal(eventName, errorCode, message, eventProperties);
        }

        /// <summary>Reports expression evaluation duration</summary>
        /// <param name="isError">Whether evaluation resulted in an error result</param>
        /// <param name="duration">Time it took to evaluate</param>
        public static void ReportEvaluation(bool isError, TimeSpan duration, Dictionary<string, object> eventProperties = null)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties[String.Concat(TelemetryEvaluateEventName, ".", TelemetryDuration)] = Math.Round(duration.TotalMilliseconds, MidpointRounding.AwayFromZero);
            properties[String.Concat(TelemetryEvaluateEventName, ".", TelemetryIsError)] = isError;

            properties.Merge(eventProperties);

            DebuggerTelemetry.s_telemetryInstance?.ReportEventInternal(TelemetryEvaluateEventName, properties);
        }

        /// <summary>
        /// Report an event
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventProperties">[Optional] Other event properties</param>
        public static void ReportEvent(string eventName, Dictionary<string, object> eventProperties = null)
        {
            DebuggerTelemetry.s_telemetryInstance?.ReportEventInternal(eventName, String.Empty, eventProperties);
        }

        /// <summary>
        /// Report an event with a message
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="message">Message</param>
        /// <param name="eventProperties">[Optional] Other event properties</param>
        public static void ReportEvent(string eventName, string message, Dictionary<string, object> eventProperties = null)
        {
            DebuggerTelemetry.s_telemetryInstance?.ReportEventInternal(eventName, message, eventProperties);
        }

        /// <summary>Reports a time measurement to Microsoft's telemetry service</summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="duration">Duration of the event</param>
        public static void ReportTimedEvent(string eventName, TimeSpan duration, Dictionary<string, object> eventProperties = null)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties[String.Concat(eventName, ".", TelemetryDuration)] = (uint)duration.TotalMilliseconds;

            properties.Merge(eventProperties);

            DebuggerTelemetry.s_telemetryInstance?.ReportEventInternal(eventName, properties);
        }

        #endregion

        private void ReportErrorInternal(string eventName, int? errorCode, string message, Dictionary<string, object> eventProperties)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            if (errorCode.HasValue)
                properties[String.Concat(eventName, ".", TelemetryErrorCode)] = errorCode.Value;

            if (!String.IsNullOrWhiteSpace(message))
                properties[String.Concat(eventName, ".", TelemetryMessage)] = message;
            properties[String.Concat(eventName, ".", TelemetryIsError)] = true;

            properties.Merge(eventProperties);

            this.SendTelemetryEvent(eventName, properties);
        }

        private void ReportEventInternal(string eventName, Dictionary<string, object> eventProperties = null)
        {
            this.ReportEventInternal(eventName, String.Empty, eventProperties);
        }

        private void ReportEventInternal(string eventName, string message, Dictionary<string, object> eventProperties = null)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            if (!String.IsNullOrWhiteSpace(message))
            {
                properties[String.Concat(eventName, ".", TelemetryMessage)] = message;
            }

            properties.Merge(eventProperties);

            this.SendTelemetryEvent(eventName, properties);
        }

        /// <summary>
        /// Send the telemetry event. This method will ensure the proper prefixes are in place and apply a default set of properties to the event
        /// </summary>
        /// <param name="eventName">Name of the Event</param>
        /// <param name="eventProperties">Properties related to the event</param>
        public void SendTelemetryEvent(string eventName, Dictionary<string, object> eventProperties)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            // Add base telemetry properties
            properties[TelemetryImplementationName] = _engineName;
            properties[TelemetryEngineVersion] = _engineVersion;
            properties[TelemetryHostVersion] = _hostVersion;
            properties[TelemetryAdapterId] = _adapterId;

            properties.Merge(eventProperties);

            this.SendTelemetryEventBase(TelemetryHelper.EnsureEventNamePrefix(EventNamePrefix, eventName),
                TelemetryHelper.EnsurePropertyPrefix(PropertyNamePrefix, properties));
        }

        private void SendTelemetryEventBase(string eventName, params KeyValuePair<string, object>[] eventProperties)
        {
            Dictionary<string, object> properties = null;
            if (eventProperties != null)
            {
                properties = new Dictionary<string, object>();
                foreach (var item in eventProperties)
                {
                    if (properties.ContainsKey(item.Key))
                        continue;
                    properties.Add(item.Key, item.Value);
                }
            }
            this.SendTelemetryEventBase(eventName, properties);
        }

        /// <summary>
        /// Only log telemetry if it is a LAB Build. LAB builds will be the official shipped builds. This prevents logging of telemetry for private builds
        /// </summary>
        [Conditional("LAB")]
        private void SendTelemetryEventBase(string eventName, Dictionary<string, object> properties)
        {
#if LAB
            this._callback?.Invoke(new OutputEvent()
            {
                Category = OutputEvent.CategoryValue.Telemetry,
                Output = eventName,
                Data = properties
            });
#endif
        }

#region private Static Methods

        private static string GetVersionAttributeValue(TypeInfo engineType)
        {
            var attribute = engineType.Assembly.GetCustomAttribute(typeof(System.Reflection.AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
            if (attribute == null)
                return string.Empty;

            return attribute.Version;
        }

#endregion
    }
}
