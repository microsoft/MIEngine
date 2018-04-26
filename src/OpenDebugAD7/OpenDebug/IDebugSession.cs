// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using OpenDebugAD7; // AD7Resources
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder;
using System.IO;

// This code is based on https://github.com/Microsoft/vscode-mono-debug/blob/master/src/common/IDebugSession.cs

namespace OpenDebug
{
    public interface IDebugSession
    {
        DebugResult Dispatch(string command, dynamic args);

        Task<DebugResult> Initialize(dynamic arguments);
        Task<DebugResult> Launch(dynamic arguments);
        Task<DebugResult> Attach(dynamic arguments);
        Task<DebugResult> Disconnect();

        Task<DebugResult> SetFunctionBreakpoints(FunctionBreakpoint[] breakpoints);

        // NOTE: This method should never return a failure result, as this causes the launch to be aborted half-way
        // through. Instead, failures should be returned as unverified breakpoints.
        Task<SetBreakpointsResponseBody> SetBreakpoints(Source source, SourceBreakpoint[] lines, bool sourceModified);
        Task<DebugResult> SetExceptionBreakpoints(string[] filter);

        Task<DebugResult> Continue(int threadId);
        Task<DebugResult> Next(int threadId);
        Task<DebugResult> StepIn(int threadId);
        Task<DebugResult> StepOut(int threadId);
        Task<DebugResult> Pause(int threadId);

        Task<DebugResult> Threads();
        Task<DebugResult> StackTrace(int threadId, int startFrame, int levels);
        Task<DebugResult> Scopes(int frameId);
        Task<DebugResult> Variables(int reference);
        Task<DebugResult> SetVariable(int reference, string name, string value);
        Task<DebugResult> Source(int sourceReference);

        Task<DebugResult> Evaluate(string context, int frameId, string expression);
    }

    public abstract class DebugSession : IDebugSession
    {
        protected bool _debuggerLinesStartAt1;
        protected bool _debuggerPathsAreURI;
        protected bool _clientLinesStartAt1 = true;
        protected bool _clientPathsAreURI = true;


        public DebugSession(bool debuggerLinesStartAt1, bool debuggerPathsAreURI = false)
        {
            _debuggerLinesStartAt1 = debuggerLinesStartAt1;
            _debuggerPathsAreURI = debuggerPathsAreURI;
        }

        public virtual DebugResult Dispatch(string command, dynamic args)
        {
            int thread;

            switch (command)
            {
                case "initialize":
                    return Initialize(args).Result;

                case "launch":
                    return Launch(args).Result;

                case "attach":
                    return Attach(args).Result;

                case "disconnect":
                    return Disconnect().Result;

                case "configurationDone":
                    return ConfigurationDone().Result;

                case "next":
                    thread = GetInt(args, "threadId", 0);
                    return Next(thread).Result;

                case "continue":
                    thread = GetInt(args, "threadId", 0);
                    return Continue(thread).Result;

                case "stepIn":
                    thread = GetInt(args, "threadId", 0);
                    return StepIn(thread).Result;

                case "stepOut":
                    thread = GetInt(args, "threadId", 0);
                    return StepOut(thread).Result;

                case "pause":
                    thread = GetInt(args, "threadId", 0);
                    return Pause(thread).Result;

                case "stackTrace":
                    int levels = GetInt(args, "levels", 0);
                    int startFrame = GetInt(args, "startFrame", 0);
                    thread = GetInt(args, "threadId", 0);
                    return StackTrace(thread, startFrame, levels).Result;

                case "scopes":
                    int frameId0 = GetInt(args, "frameId", 0);
                    return Scopes(frameId0).Result;

                case "variables":
                    int varRef = GetInt(args, "variablesReference", -1);
                    if (varRef == -1)
                    {
                        return Utilities.CreateErrorResult(1009, AD7Resources.Error_PropertyMissing, "variables", "variablesReference");
                    }
                    return Variables(varRef).Result;

                case "setVariable":
                    string setVarValue = GetString(args, "value");
                    if (string.IsNullOrEmpty(setVarValue))
                    {
                        // Just exit out of editing if we're given an empty expression.
                        return new DebugResult();
                    }
                    int setVarRef = GetInt(args, "variablesReference", -1);
                    if (setVarRef == -1)
                    {
                        return Utilities.CreateErrorResult(1106, AD7Resources.Error_PropertyMissing, "setVariable", "variablesReference");
                    }
                    string setVarName = GetString(args, "name");
                    if (string.IsNullOrEmpty(setVarName))
                    {
                        return Utilities.CreateErrorResult(1106, AD7Resources.Error_PropertyMissing, "setVariable", "name");
                    }
                    return SetVariable(setVarRef, setVarName, setVarValue).Result;

                case "source":
                    int sourceRef = GetInt(args, "sourceReference", -1);
                    if (sourceRef == -1)
                    {
                        return Utilities.CreateErrorResult(1010, AD7Resources.Error_PropertyMissing, "source", "sourceReference");
                    }
                    return Source(sourceRef).Result;

                case "threads":
                    return Threads().Result;

                case "setBreakpoints":
                    string path = null;
                    string name = null;

                    dynamic source = args.source;
                    if (source != null)
                    {
                        string p = (string)source.path;
                        if (p != null && p.Trim().Length > 0)
                        {
                            path = p;
                        }
                        string nm = (string)source.name;
                        if (nm != null && nm.Trim().Length > 0)
                        {
                            name = nm;
                        }
                    }

                    var src2 = new Source(name, path, 0);
                    var bps = args.breakpoints.ToObject<SourceBreakpoint[]>();
                    SetBreakpointsResponseBody body = SetBreakpoints(src2, bps, ((bool?)args.sourceModified) ?? false).Result;
                    return new DebugResult(body);

                case "setExceptionBreakpoints":
                    string[] filters = null;
                    if (args.filters != null)
                    {
                        filters = args.filters.ToObject<string[]>();
                    }
                    else
                    {
                        filters = new string[0];
                    }
                    return SetExceptionBreakpoints(filters).Result;

                case "setFunctionBreakpoints":
                    if (args.breakpoints != null)
                    {
                        FunctionBreakpoint[] breakpoints = args.breakpoints.ToObject<FunctionBreakpoint[]>();
                        return SetFunctionBreakpoints(breakpoints).Result;
                    }
                    return Utilities.CreateErrorResult(1012, AD7Resources.Error_PropertyMissing, "setFunctionBreakpoints", "breakpoints");

                case "evaluate":
                    var context = GetString(args, "context");
                    int frameId = GetInt(args, "frameId", -1);
                    var expression = GetString(args, "expression");
                    if (expression == null)
                    {
                        return Utilities.CreateErrorResult(1013, AD7Resources.Error_PropertyMissing, "evaluate", "expression");
                    }
                    return Evaluate(context, frameId, expression).Result;

                default:
                    return new DebugResult(1014, "unrecognized request: {_request}", new { _request = command });
            }
        }

        public virtual Task<DebugResult> Initialize(dynamic args)
        {
            if (args.linesStartAt1 != null)
            {
                _clientLinesStartAt1 = (bool)args.linesStartAt1;
            }

            var pathFormat = (string)args.pathFormat;
            if (pathFormat != null)
            {
                switch (pathFormat)
                {
                    case "uri":
                        _clientPathsAreURI = true;
                        break;
                    case "path":
                        _clientPathsAreURI = false;
                        break;
                    default:
                        return Task.FromResult(new DebugResult(1015, "initialize: bad value '{_format}' for pathFormat", new { _format = pathFormat }));
                }
            }

            return Task.FromResult(new DebugResult(new InitializeResponseBody(this.Capabilities)));
        }

        public abstract Task<DebugResult> Launch(dynamic arguments);

        public virtual Task<DebugResult> Attach(dynamic arguments)
        {
            return Task.FromResult(new DebugResult(1016, "Attach not supported"));
        }

        public virtual Task<DebugResult> Disconnect()
        {
            return Task.FromResult(new DebugResult(new TerminatedEvent()));
        }

        public abstract Task<DebugResult> ConfigurationDone();

        public virtual Task<DebugResult> SetExceptionBreakpoints(string[] filter)
        {
            return Task.FromResult(new DebugResult());
        }

        public abstract Task<SetBreakpointsResponseBody> SetBreakpoints(Source source, SourceBreakpoint[] breakpoints, bool sourceModified);

        public abstract Task<DebugResult> SetFunctionBreakpoints(FunctionBreakpoint[] breakpoints);

        public abstract Task<DebugResult> Continue(int thread);

        public abstract Task<DebugResult> Next(int thread);

        public virtual Task<DebugResult> StepIn(int thread)
        {
            return Task.FromResult(new DebugResult(1017, "StepIn not supported"));
        }

        public virtual Task<DebugResult> StepOut(int thread)
        {
            return Task.FromResult(new DebugResult(1018, "StepOut not supported"));
        }

        public virtual Task<DebugResult> Pause(int thread)
        {
            return Task.FromResult(new DebugResult(1019, "Pause not supported"));
        }

        public abstract Task<DebugResult> StackTrace(int thread, int startFrame, int levels);

        public abstract Task<DebugResult> Scopes(int frameId);

        public abstract Task<DebugResult> Variables(int reference);

        public abstract Task<DebugResult> SetVariable(int reference, string name, string value);

        public virtual Task<DebugResult> Source(int sourceId)
        {
            return Task.FromResult(new DebugResult(1020, "Source not supported"));
        }

        public virtual Task<DebugResult> Threads()
        {
            return Task.FromResult(new DebugResult(new ThreadsResponseBody()));
        }

        public virtual Task<DebugResult> Evaluate(string context, int frameId, string expression)
        {
            return Task.FromResult(new DebugResult(1021, "Evaluate not supported"));
        }

        public abstract Capabilities Capabilities { get; }

        // protected

        protected int ConvertDebuggerLineToClient(int line)
        {
            if (_debuggerLinesStartAt1)
            {
                return _clientLinesStartAt1 ? line : line - 1;
            }
            else
            {
                return _clientLinesStartAt1 ? line + 1 : line;
            }
        }

        protected int ConvertClientLineToDebugger(int line)
        {
            if (_debuggerLinesStartAt1)
            {
                return _clientLinesStartAt1 ? line : line + 1;
            }
            else
            {
                return _clientLinesStartAt1 ? line - 1 : line;
            }
        }

        protected int ConvertDebuggerColumnToClient(int column)
        {
            // TODO@AW same as line
            return column;
        }

        protected string ConvertDebuggerPathToClient(string path)
        {
            if (_debuggerPathsAreURI)
            {
                if (_clientPathsAreURI)
                {
                    return path;
                }
                else
                {
                    Uri uri = new Uri(path);
                    return uri.LocalPath;
                }
            }
            else
            {
                if (_clientPathsAreURI)
                {
                    try
                    {
                        var uri = new System.Uri(path);
                        return uri.AbsoluteUri;
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    return path;
                }
            }
        }

        protected string ConvertClientPathToDebugger(string clientPath)
        {
            if (clientPath == null)
            {
                return null;
            }

            if (_debuggerPathsAreURI)
            {
                if (_clientPathsAreURI)
                {
                    return clientPath;
                }
                else
                {
                    var uri = new System.Uri(clientPath);
                    return uri.AbsoluteUri;
                }
            }
            else
            {
                if (_clientPathsAreURI)
                {
                    if (Uri.IsWellFormedUriString(clientPath, UriKind.Absolute))
                    {
                        Uri uri = new Uri(clientPath);
                        return uri.LocalPath;
                    }
                    Console.Error.WriteLine("path not well formed: '{0}'", clientPath);
                    return null;
                }
                else
                {
                    return clientPath;
                }
            }
        }

        // private

        private static bool GetBool(dynamic args, string property)
        {
            try
            {
                return (bool)args[property];
            }
            catch (RuntimeBinderException)
            {
            }
            return false;
        }

        private static int GetInt(dynamic args, string property, int dflt)
        {
            try
            {
                return (int)args[property];
            }
            catch (RuntimeBinderException)
            {
                // ignore and return default value
            }
            return dflt;
        }

        private static string GetString(dynamic args, string property, string dflt = null)
        {
            var s = (string)args[property];
            if (s == null)
            {
                return dflt;
            }
            s = s.Trim();
            if (s.Length == 0)
            {
                return dflt;
            }
            return s;
        }
    }
}
