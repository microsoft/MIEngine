// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace OpenDebug
{
    public class DispatcherMessage
    {
        public int seq;
        public string type;

        public DispatcherMessage(string typ)
        {
            type = typ;
        }
    }

    public class DispatcherRequest : DispatcherMessage
    {
        public string command;
        public dynamic arguments;

        public DispatcherRequest() : base("request")
        {
        }

        public DispatcherRequest(int id, string cmd, dynamic arg) : base("request")
        {
            seq = id;
            command = cmd;
            arguments = arg;
        }
    }

    public class DispatcherResponse : DispatcherMessage
    {
        public bool success;
        public string message;
        public int request_seq;
        public string command;
        public dynamic body;
        public bool running;
        public dynamic refs;

        public DispatcherResponse() : base("response")
        {
        }

        public DispatcherResponse(string msg) : base("response")
        {
            success = false;
            message = msg;
        }

        public DispatcherResponse(bool succ, string msg) : base("response")
        {
            success = succ;
            message = msg;
        }

        public DispatcherResponse(dynamic m) : base("response")
        {
            seq = m.seq;
            success = m.success;
            message = m.message;
            request_seq = m.request_seq;
            command = m.command;
            body = m.body;
            running = m.running;
            refs = m.refs;
        }

        public DispatcherResponse(int rseq, string cmd) : base("response")
        {
            request_seq = rseq;
            command = cmd;
        }
    }

    public class DispatcherEvent : DispatcherMessage
    {
        [JsonProperty(PropertyName = "event")]
        public string eventType;
        public dynamic body;

        public DispatcherEvent() : base("event")
        {
        }

        public DispatcherEvent(dynamic m) : base("event")
        {
            seq = m.seq;
            eventType = m["event"];
            body = m.body;
        }

        public DispatcherEvent(string type, dynamic bdy = null) : base("event")
        {
            eventType = type;
            body = bdy;
        }
    }
}
