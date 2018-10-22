// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Newtonsoft.Json;

namespace OpenDebug.CustomProtocolObjects
{
    public class OpenDebugStoppedEvent : DebugEvent
    {
        // Summary:
        //     Protocol type for this event.
        public const string EventType = "stopped";
                
        // Summary:
        //     Creates a new instance of the Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StoppedEvent
        //     class.
        public OpenDebugStoppedEvent() : base(EventType)
        {
            // All threads are always stopped. 
            this.AllThreadsStopped = true;
        }

        // Summary:
        //     The reason for the event. For backward compatibility this string is shown in
        //     the UI if the 'description' attribute is missing (but it must not be translated).
        [JsonProperty("reason")]
        public StoppedEvent.ReasonValue Reason { get; set; }
        
        // Summary:
        //     The full reason for the event, e.g. 'Paused on exception'. This string is shown
        //     in the UI as is.
        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Description { get; set; }
        
        // Summary:
        //     The thread which was stopped.
        [JsonProperty("threadId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? ThreadId { get; set; }
        
        // Summary:
        //     A value of true hints to the frontend that this event should not change the focus.
        [JsonProperty("preserveFocusHint", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? PreserveFocusHint { get; set; }
        
        // Summary:
        //     Additional information. E.g. if reason is 'exception', text contains the exception
        //     name. This string is shown in the UI.
        [JsonProperty("text", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Text { get; set; }
        
        // Summary:
        //     If allThreadsStopped is true, a debug adapter can announce that all threads have
        //     stopped. * The client should use this information to enable that all threads
        //     can be expanded to access their stacktraces. * If the attribute is missing or
        //     false, only the thread with the given threadId can be expanded.
        [JsonProperty("allThreadsStopped", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? AllThreadsStopped { get; set; }

        // Custom fields for Testing
        [JsonProperty("source", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Source Source { get; set; }
        [JsonProperty("line", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Line { get; set; }
        [JsonProperty("column", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Column { get; set; }
    }

    public class OpenDebugThread : Thread
    {
        public OpenDebugThread(int id, string name) : base()
        {
            base.Id = id;
            if(string.IsNullOrEmpty(name))
            {
                base.Name = string.Format(CultureInfo.CurrentCulture, "Thread #{0}", id);
            }
            else
            {
                base.Name = name;
            }
        }
    }
}
