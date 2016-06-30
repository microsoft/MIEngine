using System;
using System.Collections.Generic;
using MICore;
using Microsoft.DebugEngineHost;
using System.Diagnostics;
using System.Linq;
using System.Globalization;

namespace Microsoft.MIDebugEngine
{
    internal class EngineTelemetry
    {
        private const string Event_DebuggerAborted = @"VS/Diagnostics/Debugger/MIEngine/DebuggerAborted";
        private const string Property_DebuggerName = @"VS.Diagnostics.Debugger.MIEngine.DebuggerName";
        private const string Property_LastSentCommandName = @"VS.Diagnostics.Debugger.MIEngine.LastSentCommandName";
        private const string Property_DebuggerExitCode = @"VS.Diagnostics.Debugger.MIEngine.DebuggerExitCode";
        private const string Windows_Runtime_Environment = @"VS/Diagnostics/Debugger/MIEngine/WindowsRuntime";
        private const string Property_Windows_Runtime_Environment = @"VS.Diagnostics.Debugger.MIEngine.WindowsRuntime";
        private const string Value_Windows_Runtime_Environment_Cygwin = "Cygwin";
        private const string Value_Windows_Runtime_Environment_MinGW = "MinGW";


        private KeyValuePair<string, object>[] _clrdbgProcessCreateProperties;

        public bool DecodeTelemetryEvent(Results results, out string eventName, out KeyValuePair<string, object>[] properties)
        {
            properties = null;

            // NOTE: the message event is an MI Extension from clrdbg, though we could use in it the future for other debuggers
            eventName = results.TryFindString("event-name");
            if (string.IsNullOrEmpty(eventName) || !char.IsLetter(eventName[0]) || !eventName.Contains('/'))
            {
                Debug.Fail("Bogus telemetry event. 'Event-name' property is missing or invalid.");
                return false;
            }

            TupleValue tuple;
            if (!results.TryFind("properties", out tuple))
            {
                Debug.Fail("Bogus message event, missing 'properties' property");
                return false;
            }

            List<KeyValuePair<string, object>> propertyList = new List<KeyValuePair<string, object>>(tuple.Content.Count);
            foreach (NamedResultValue pair in tuple.Content)
            {
                ConstValue resultValue = pair.Value as ConstValue;
                if (resultValue == null)
                    continue;

                string content = resultValue.Content;
                if (string.IsNullOrEmpty(content))
                    continue;

                object value = content;
                int numericValue;
                if (content.Length >= 3 && content.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && int.TryParse(content.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out numericValue))
                {
                    value = numericValue;
                }
                else if (int.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out numericValue))
                {
                    value = numericValue;
                }

                if (value != null)
                {
                    propertyList.Add(new KeyValuePair<string, object>(pair.Name, value));
                }
            }

            properties = propertyList.ToArray();

            // If we are processing a clrdbg ProcessCreate event, save the event properties so we can use them to send other events
            if (eventName == "VS/Diagnostics/Debugger/clrdbg/ProcessCreate")
            {
                _clrdbgProcessCreateProperties = properties;
            }

            return true;
        }

        public void SendDebuggerAborted(MICommandFactory commandFactory, string lastSentCommandName, /*OPTIONAL*/ string debuggerExitCode)
        {
            List<KeyValuePair<string, object>> eventProperties = new List<KeyValuePair<string, object>>();
            eventProperties.Add(new KeyValuePair<string, object>(Property_DebuggerName, commandFactory.Name));
            eventProperties.Add(new KeyValuePair<string, object>(Property_LastSentCommandName, lastSentCommandName));
            if (!string.IsNullOrEmpty(debuggerExitCode))
            {
                eventProperties.Add(new KeyValuePair<string, object>(Property_DebuggerExitCode, debuggerExitCode));
            }

            if (_clrdbgProcessCreateProperties != null)
            {
                eventProperties.AddRange(_clrdbgProcessCreateProperties);
            }

            HostTelemetry.SendEvent(Event_DebuggerAborted, eventProperties.ToArray());
        }

        public enum WindowsRuntimeEnvironment
        {
            Cygwin,
            MinGW
        }
        public void SendWindowsRuntimeEnvironment(WindowsRuntimeEnvironment environment)
        {
            string envValue;
            if (environment == WindowsRuntimeEnvironment.Cygwin)
            {
                envValue = Value_Windows_Runtime_Environment_Cygwin;
            }
            else
            {
                envValue = Value_Windows_Runtime_Environment_MinGW;
            }

            HostTelemetry.SendEvent(
                           Windows_Runtime_Environment,
                           new KeyValuePair<string, object>(Property_Windows_Runtime_Environment,
                           envValue));
        }
    }
}