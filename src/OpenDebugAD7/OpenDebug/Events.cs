// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.




using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace OpenDebug
{
    public class DebugEvent
    {
        public string type { get; set; }

        public DebugEvent(string typ)
        {
            type = typ;
        }
    }

    public class DebugProtocolCallbacks
    {
        public Action<DebugEvent> Send { get; set; }
        public Action<DebugEvent> SendRaw { get; set; }
        public Action<DebugEvent> SendLater { get; set; }

        public Action<Action<string>> SetTraceLogger { get; set; }
        public Action<Action<string>> SetResponseLogger { get; set; }
        public Action<Action<string>> SetEngineLogger { get; set; }
    }

    public class InitializedEvent : DebugEvent
    {
        public InitializedEvent() : base("initialized")
        {
        }
    }

    public class StoppedEvent : DebugEvent
    {
        public int threadId { get; set; }
        public string reason { get; set; }
        public Source source { get; set; }
        public int line { get; set; }
        public int column { get; set; }
        public string text { get; set; }
        public bool allThreadsStopped { get; set; }

        public StoppedEvent(string reasn, Source src, int ln, int col = 0, string txt = null, int tid = 0) : base("stopped")
        {
            reason = reasn;
            source = src;
            line = ln;
            column = col;
            text = txt;
            threadId = tid;
            allThreadsStopped = true;
        }
    }

    public class ExitedEvent : DebugEvent
    {
        public int exitCode { get; set; }

        public ExitedEvent(int exCode) : base("exited")
        {
            exitCode = exCode;
        }
    }

    public class TerminatedEvent : DebugEvent
    {
        public TerminatedEvent() : base("terminated")
        {
        }
    }

    public class ThreadEvent : DebugEvent
    {
        public string reason { get; set; }
        public int threadId { get; set; }

        public ThreadEvent(string reasn, int tid) : base("thread")
        {
            reason = reasn;
            threadId = tid;
        }
    }

    public class OutputEvent : DebugEvent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Category
        {
            console,
            stdout,
            stderr,
            telemetry
        }

        public Category category { get; protected set; }
        public string output { get; protected set; }

        /// <summary>
        /// Optional data to report. For the 'telemetry' category the data will be sent to telemetry, for the other categories the data is shown in JSON format.
        /// </summary>
        public Dictionary<string, object> data { get; protected set; }

        public OutputEvent(Category category, string output, Dictionary<string, object> data = null) : base("output")
        {
            this.category = category;
            this.output = output;
            this.data = data;
        }
    }

    public class BreakpointEvent : DebugEvent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Reason
        {
            changed,
            @new
        }

        public Reason reason { get; set; }
        public Breakpoint breakpoint { get; set; }

        public BreakpointEvent(Reason reason, Breakpoint breakpoint) : base("breakpoint")
        {
            this.reason = reason;
            this.breakpoint = breakpoint;
        }
    }
}
