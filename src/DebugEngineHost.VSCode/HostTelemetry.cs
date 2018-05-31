// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Static class providing telemetry reporting services to debug engines. Telemetry 
    /// reports go to Microsoft, and so in general this functionality should not be used 
    /// by non-Microsoft implemented debug engines.
    /// </summary>
    public static class HostTelemetry
    {
        // NOTE: These are from Microsoft.VisualStudio.Debugger.Engine (src\debugger\concord\Dispatcher\Managed\WatsonErrorReport.cs)
        private const string TelemetryNonFatalWatsonEventName = @"VS/Diagnostics/Debugger/NonFatalError";
        private const string TelemetryNonFatalErrorImplementationName = @"VS.Diagnostics.Debugger.NonFatalError.ImplementationName";
        private const string TelemetryNonFatalErrorExceptionTypeName = @"VS.Diagnostics.Debugger.NonFatalError.ExceptionType";
        private const string TelemetryNonFatalErrorExceptionStackName = @"VS.Diagnostics.Debugger.NonFatalError.ExceptionStack";
        private const string TelemetryNonFatalErrorExceptionHResult = @"VS.Diagnostics.Debugger.NonFatalError.HResult";

        // Common telemetry constants
        private const string TelemetryImplementationName = @"VS.Diagnostics.Debugger.ImplementationName";
        private const string TelemetryEngineVersion = @"VS.Diagnostics.Debugger.EngineVersion";
        private const string TelemetryHostVersion = @"VS.Diagnostics.Debugger.HostVersion";
        private const string TelemetryAdapterId = @"VS.Diagnostics.Debugger.AdapterId";

        private static Action<string, KeyValuePair<string, object>[]> s_telemetryCallback;
        private static string s_engineName;
        private static string s_engineVersion;
        private static string s_hostVersion;
        private static string s_adapterId;

        public static void InitializeTelemetry(Action<string, KeyValuePair<string, object>[]> telemetryCallback, TypeInfo engineType, string adapterId)
        {
            Debug.Assert(telemetryCallback != null && engineType != null, "Bogus arguments to InitializeTelemetry");
            s_telemetryCallback = telemetryCallback;
            s_engineName = engineType.Namespace;
            s_engineVersion = GetVersionAttributeValue(engineType);
            s_hostVersion = GetVersionAttributeValue(typeof(HostTelemetry).GetTypeInfo());
            s_adapterId = adapterId;
        }

        /// <summary>
        /// Reports a telemetry event to Microsoft.
        /// </summary>
        /// <param name="eventName">Name of the event. This should generally start with the 
        /// prefix 'VS/Diagnostics/Debugger/'</param>
        /// <param name="eventProperties">0 or more properties of the event. Property names 
        /// should generally start with the prefix 'VS.Diagnostics.Debugger.'</param>
        [Conditional("LAB")]
        public static void SendEvent(string eventName, params KeyValuePair<string, object>[] eventProperties)
        {
#if LAB
            if (s_telemetryCallback != null)
            {
                s_telemetryCallback(eventName, eventProperties);
            }
#endif
        }


        /// <summary>
        /// Reports the current exception to Microsoft's telemetry service. 
        /// </summary>
        /// <param name="currentException">Exception object to report.</param>
        /// <param name="engineName">[Optional] Name of the engine reporting the exception. Ex:Microsoft.MIEngine.
        /// In OpenDebugAD7, this is optional.</param>
        public static void ReportCurrentException(Exception currentException, string engineName)
        {
            try
            {
                // Report the inner-most exception
                while (currentException.InnerException != null)
                {
                    currentException = currentException.InnerException;
                }

                if (s_hostVersion == null)
                {
                    // InitializeTelemetry not called yet
                    return;
                }

                if (engineName == null)
                {
                    engineName = s_engineName;
                }

                SendEvent(TelemetryNonFatalWatsonEventName,
                    new KeyValuePair<string, object>(TelemetryNonFatalErrorImplementationName, engineName),
                    new KeyValuePair<string, object>(TelemetryNonFatalErrorExceptionTypeName, currentException.GetType().FullName),
                    new KeyValuePair<string, object>(TelemetryNonFatalErrorExceptionStackName, currentException.StackTrace),
                    new KeyValuePair<string, object>(TelemetryNonFatalErrorExceptionHResult, currentException.HResult),
                    new KeyValuePair<string, object>(TelemetryEngineVersion, s_engineVersion),
                    new KeyValuePair<string, object>(TelemetryAdapterId, s_adapterId),
                    new KeyValuePair<string, object>(TelemetryHostVersion, s_hostVersion)
                    );
            }
            catch
            {
                // We are already reporting an exception. If something goes wrong, nothing to be done.
            }
        }

        private static string GetVersionAttributeValue(TypeInfo engineType)
        {
            var attribute = engineType.Assembly.GetCustomAttribute(typeof(System.Reflection.AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
            if (attribute == null)
                return string.Empty;

            return attribute.Version;
        }
    }
}
