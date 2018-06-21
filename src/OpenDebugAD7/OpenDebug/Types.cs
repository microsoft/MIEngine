// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using Microsoft.DebugEngineHost.VSCode;

namespace OpenDebug
{
    public class Message
    {
        public int id { get; set; }
        public string format { get; set; }
        public dynamic variables { get; set; }

        public Message(int id, string format, dynamic variables = null)
        {
            this.id = id;
            this.format = format;
            this.variables = variables;
        }
    }

    public class StackFrame
    {
        public int id { get; set; }
        public Source source { get; set; }
        public int line { get; set; }
        public int column { get; set; }
        public string name { get; set; }

        public StackFrame(int i, string nm, Source src, int ln, int col)
        {
            id = i;
            source = src;
            line = ln;
            column = col;
            name = nm;
        }
    }

    public class Scope
    {
        public string name { get; set; }
        public int variablesReference { get; set; }
        public bool expensive { get; set; }

        public Scope(string nm, int rf, bool exp = false)
        {
            name = nm;
            variablesReference = rf;
            expensive = exp;
        }
    }

    public class Variable
    {
        public string name { get; set; }
        public string value { get; set; }
        public string type { get; set; }
        public int variablesReference { get; set; }
        public string evaluateName { get; set; }

        public Variable(string nm, string val, string typ, int rf, string evaluateName)
        {
            name = nm;
            value = val;
            type = typ;
            variablesReference = rf;
            this.evaluateName = evaluateName;
        }
    }

    public class Thread
    {
        public int id { get; set; }
        public string name { get; set; }

        public Thread(int i, string nm)
        {
            id = i;
            if (nm == null || nm.Length == 0)
            {
                name = string.Format(CultureInfo.CurrentCulture, "Thread #{0}", id);
            }
            else
            {
                name = nm;
            }
        }
    }

    public class Source
    {
        public string name { get; set; }
        public string path { get; set; }
        public int sourceReference { get; set; }

        public Source(string nm, string pth, int rf = 0)
        {
            name = nm;
            path = pth;
            sourceReference = rf;
        }

        public Source(string pth, int rf = 0)
        {
            name = Path.GetFileName(pth);
            path = pth;
            sourceReference = rf;
        }
    }

    public class Breakpoint
    {
        // TODO: add 'column', 

        public uint id { get; set; }
        public bool verified { get; set; }
        public int line { get; set; }
        public string message { get; set; }

        public Breakpoint(uint id, bool verified, int line, string message = null)
        {
            this.id = id;
            this.verified = verified;
            this.line = line;
            this.message = message;
        }
    }

    public class SourceBreakpoint
    {
        public int line { get; set; }
        public string condition { get; set; }

        public SourceBreakpoint(int line, string condition)
        {
            this.line = line;
            this.condition = condition;
        }
    }

    public class FunctionBreakpoint
    {
        public string name { get; set; }

        public FunctionBreakpoint(string n)
        {
            name = n;
        }
    }

    public class Capabilities
    {
        public bool supportsConfigurationDoneRequest { get; set; }
        public bool supportsFunctionBreakpoints { get; set; }
        public bool supportsSetVariable { get; set; }
        public bool supportsEvaluateForHovers { get; set; }
        public bool supportsConditionalBreakpoints { get; set; }
        public IList<ExceptionBreakpointFilter> exceptionBreakpointFilters { get; set; }
    }
}
