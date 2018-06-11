// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OpenDebug
{
    /*
     * This monomorphic class is used to return results from a debugger request or to return errors.
     * In addition events can be attached that are fired after the request results have been returned to the caller.
     */
    public sealed class DebugResult
    {
        public bool Success { get; } // boolean indicating success
        public ResponseBody Body { get; }   // depending on value of success either the result or an error
        public List<DebugEvent> Events { get; private set; } // send this event after returning the result

        /*
         * A success result without additional data.
         */
        public DebugResult()
        {
            Success = true;
        }

        /*
         * A success result with as additional event.
         */
        public DebugResult(DebugEvent ev)
        {
            Success = true;
            Add(ev);
        }

        /*
         * A result with a response body. If body is a ErrorResponseBody then Success will be set to false.
         */
        public DebugResult(ResponseBody body)
        {
            Success = true;
            Body = body;
            if (body is ErrorResponseBody)
            {
                Success = false;
            }
        }

        /*
         * A failure result with a full error message.
         */
        public DebugResult(int id, string format, dynamic arguments = null)
        {
            Success = false;
            Body = new ErrorResponseBody(new Message(id, format, arguments));
        }

        /*
         * Add a DebugEvent to this request result.
         * Events are fired after the result is returned to the caller of the request.
         */
        public void Add(DebugEvent ev)
        {
            if (ev != null)
            {
                if (Events == null)
                {
                    Events = new List<DebugEvent>();
                }
                Events.Add(ev);
            }
        }
    }

    /*
     * subclasses of ResponseBody are serialized as the response body.
     * Don't change their instance variables since that will break the OpenDebug protocol.
     */
    public class ResponseBody
    {
        // empty
    }

    public class InitializeResponseBody : ResponseBody
    {
        public Capabilities body { get; private set; }

        public InitializeResponseBody(Capabilities capabilities)
        {
            body = capabilities;
        }
    }

    public class ErrorResponseBody : ResponseBody
    {
        public Message error { get; }

        public ErrorResponseBody(Message m)
        {
            error = m;
        }
    }

    public class StackTraceResponseBody : ResponseBody
    {
        public StackFrame[] stackFrames { get; }

        public int totalFrames { get; }

        public StackTraceResponseBody(List<StackFrame> frames = null, int total = 0)
        {
            if (frames == null)
                stackFrames = new StackFrame[0];
            else
                stackFrames = frames.ToArray<StackFrame>();

            totalFrames = total;
        }
    }

    public class ScopesResponseBody : ResponseBody
    {
        public Scope[] scopes { get; }

        public ScopesResponseBody(List<Scope> scps = null)
        {
            if (scps == null)
                scopes = new Scope[0];
            else
                scopes = scps.ToArray<Scope>();
        }
    }

    public class VariablesResponseBody : ResponseBody
    {
        public Variable[] variables { get; }

        public VariablesResponseBody(List<Variable> vars = null)
        {
            if (vars == null)
                variables = new Variable[0];
            else
                variables = vars.ToArray<Variable>();
        }
    }

    public class SetVariablesResponseBody : ResponseBody
    {
        public string value { get; }

        public SetVariablesResponseBody(string val)
        {
            value = val;
        }
    }

    public class SourceResponseBody : ResponseBody
    {
        public string content { get; }

        public SourceResponseBody(string cont)
        {
            content = cont;
        }
    }

    public class ThreadsResponseBody : ResponseBody
    {
        public Thread[] threads { get; }

        public ThreadsResponseBody(List<Thread> vars = null)
        {
            if (vars == null)
                threads = new Thread[0];
            else
                threads = vars.ToArray<Thread>();
        }
    }

    public class EvaluateResponseBody : ResponseBody
    {
        public string result { get; }
        public int variablesReference { get; }
        public string type { get; }

        public EvaluateResponseBody(string value, int reff, string type)
        {
            this.result = value;
            this.variablesReference = reff;
            this.type = type;
        }
    }

    public class SetBreakpointsResponseBody : ResponseBody
    {
        public Breakpoint[] breakpoints { get; }

        public SetBreakpointsResponseBody(List<Breakpoint> bpts = null)
        {
            if (bpts == null)
                breakpoints = new Breakpoint[0];
            else
                breakpoints = bpts.ToArray<Breakpoint>();
        }
    }
}
