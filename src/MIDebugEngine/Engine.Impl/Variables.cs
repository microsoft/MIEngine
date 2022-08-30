// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Debugger.Interop.DAP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
    internal interface IVariableInformation : IDisposable
    {
        string Name { get; }
        string Value { get; }
        string TypeName { get; }
        bool IsParameter { get; }
        VariableInformation[] Children { get; } // children are never synthetic
        AD7Thread Client { get; }
        bool Error { get; }
        uint CountChildren { get; }
        bool IsChild { get; set; }
        enum_DBG_ATTRIB_FLAGS Access { get; }
        string FullName();
        bool IsStringType { get; }
        void EnsureChildren();
        void AsyncEval(IDebugEventCallback2 pExprCallback);
        void AsyncError(IDebugEventCallback2 pExprCallback, IDebugProperty2 error);
        void SyncEval(enum_EVALFLAGS dwFlags = 0, DAPEvalFlags dwDAPFlags = 0);
        ThreadContext ThreadContext { get; }
        VariableInformation FindChildByName(string name);
        string EvalDependentExpression(string expr);
        bool IsVisualized { get; }
        bool IsReadOnly();
        enum_DEBUGPROP_INFO_FLAGS PropertyInfoFlags { get; set; }
        bool IsPreformatted { get; set; }
        string Address();
        uint Size();
    }

    internal class SimpleVariableInformation
    {
        public string Name { get; private set; }
        public string Value { get; private set; }
        public string TypeName { get; private set; }
        public bool IsParameter { get; private set; }

        internal SimpleVariableInformation(string name, bool isParam = false, string value = null, string type = null)
        {
            Name = name;
            Value = value;
            TypeName = type;
            IsParameter = isParam;
        }

        internal async Task<VariableInformation> CreateMIDebuggerVariable(ThreadContext ctx, AD7Engine engine, AD7Thread thread)
        {
            VariableInformation vi = new VariableInformation(Name, Name, ctx, engine, thread, IsParameter);
            await vi.Eval(engine.CurrentRadix());
            return vi;
        }
    }

    internal class ArgumentList : Tuple<int, List<SimpleVariableInformation>>
    {
        public ArgumentList(int level, List<SimpleVariableInformation> args)
            : base(level, args)
        { }
    }

    internal sealed class VariableInformation : IVariableInformation
    {
        public string Name { get; private set; }
        public string Value { get; private set; }
        public string TypeName { get; private set; }
        public bool IsParameter { get; private set; }
        public VariableInformation[] Children { get; private set; }
        public AD7Thread Client { get; private set; }
        public bool Error { get; private set; }
        public uint CountChildren { get; private set; }
        public bool IsChild { get; set; }
        public enum_DBG_ATTRIB_FLAGS Access { get; private set; }
        public bool IsVisualized { get { return _parent == null ? false : _parent.IsVisualized; } }
        public enum_DEBUGPROP_INFO_FLAGS PropertyInfoFlags { get; set; }
        private string DisplayHint { get; set; }
        public bool IsPreformatted { get; set; }

        static readonly Lazy<Regex> s_addressPattern = new Lazy<Regex>(() => new Regex(@"^(0x[0-9a-fA-F]+)\b"));

        public string Address()
        {
            // ask GDB to evaluate "&expression"
            string command = "&("+FullName()+")";
            var result = EvalDependentExpression(command);
            Match m = s_addressPattern.Value.Match(result);
            if (m.Success)
            {
                return m.Captures[0].ToString();
            }
            string errorMessage = String.Format(CultureInfo.InvariantCulture, "Unexpected result {0} from evaluating {1}", result, command);
            throw new UnexpectedMIResultException(_debuggedProcess.MICommandFactory.Name, "-data-evaluate-expression", errorMessage);
        }

        public uint Size()
        {
            // ask GDB to evaluate "sizeof(expression)"
            string command = "sizeof("+FullName()+")";
            return Convert.ToUInt32(EvalDependentExpression(command), CultureInfo.InvariantCulture);
        }

        private static bool IsPointer(string typeName)
        {
            return typeName.Trim().EndsWith("*", StringComparison.Ordinal);
        }

        public string FullName()   // Full expression used to re-compute the value
        {
            if (_fullname == null)
            {
                switch (VariableNodeType)
                {
                    case NodeType.Root:
                    case NodeType.Synthetic:
                        _fullname = _strippedName;
                        break;
                    case NodeType.Field:
                        //Task evalTask = Task.Run(async () =>
                        //{
                        //    m_fullname = await m_engine.DebuggedProcess.MICommandFactory.VarInfoPathExpression(m_internalName);
                        //});
                        //evalTask.Wait();
                        string op = ".";
                        string parentName = _parent.FullName();
                        if (IsPointer(_parent.TypeName))
                        {
                            op = "->";
                            // Underlying debugger sometimes has trouble with long expressions (parent-expression can be arbitrarily long),
                            // so attempt to simplify the expression by using ((parent-type)0xabc)->field instead of (parent-expression)->field 
                            ulong addr;
                            if (_parent.Value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                                    && ulong.TryParse(_parent.Value.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out addr))
                            {
                                parentName = '(' + _parent.TypeName + ')' + _parent.Value;
                            }
                        }
                        _fullname = '(' + parentName + ')' + op + _strippedName;
                        break;
                    case NodeType.Dereference:
                        _fullname = "*(" + _parent.FullName() + ")";
                        break;
                    case NodeType.BaseClass:
                    case NodeType.AccessQualifier:
                        _fullname = _parent.FullName();
                        break;
                    case NodeType.ArrayElement:
                        _fullname = '(' + _parent.FullName() + ')' + Name;
                        break;
                    case NodeType.AnonymousUnion:
                        _fullname = _parent.FullName();
                        break;
                    default:
                        _fullname = String.Empty;
                        break;
                }
            }
            return _fullname;
        }
        public bool IsStringType
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(this.TypeName))
                {
                    for (int i = 0; i < s_stringTypes.Length; ++i)
                    {
                        if (Regex.IsMatch(this.TypeName.Trim(), s_stringTypes[i]))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private VariableInformation(ThreadContext ctx, AD7Engine engine, AD7Thread thread)
        {
            _engine = engine;
            _debuggedProcess = _engine.DebuggedProcess;
            _ctx = ctx;
            Client = thread;

            IsParameter = false;
            IsChild = false;
            _attribsFetched = false;
            _isReadonly = false;
            Access = enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_NONE;
            _fullname = null;

            lock (_debuggedProcess.ActiveVariables)
            {
                _debuggedProcess.ActiveVariables.Add(this);
            }
        }

        //this constructor is used to create root nodes (local/params)
        internal VariableInformation(string displayName, string expr, ThreadContext ctx, AD7Engine engine, AD7Thread thread, bool isParameter = false)
            : this(ctx, engine, thread)
        {
            // strip off formatting string
            _strippedName = ProcessFormatSpecifiers(expr, out _format);
            Name = displayName;
            IsParameter = isParameter;
            _parent = null;
            VariableNodeType = NodeType.Root;
        }

        //this constructor is used to create synthetic child nodes (local/params). These nodes are never in the parent's children list
        internal VariableInformation(string expr, IVariableInformation parent, AD7Engine engine, string displayName)
            : this(parent.ThreadContext, engine, parent.Client)
        {
            // strip off formatting string
            _strippedName = ProcessFormatSpecifiers(expr, out _format);
            Name = displayName ?? expr;
            _parent = parent;
            VariableNodeType = NodeType.Synthetic;
        }

        //this constructor is used to create modified expressions from a parent
        internal VariableInformation(string expr, VariableInformation parent)
            : this(parent._ctx, parent._engine, parent.Client)
        {
            // strip off formatting string
            _strippedName = ProcessFormatSpecifiers(expr, out _format);
            Name = expr;
            VariableNodeType = NodeType.Root;
        }

        //this constructor is private because it should only be used internally to create children
        private VariableInformation(TupleValue results, VariableInformation parent, string name = null)
            : this(parent._ctx, parent._engine, parent.Client)
        {
            TypeName = results.TryFindString("type");
            Value = results.TryFindString("value");
            Name = name ?? results.FindString("exp");
            if (results.Contains("dynamic"))
            {
                CountChildren = results.TryFindUint("has_more").GetValueOrDefault(1);
                IsPreformatted = true;
            }
            else
            {
                CountChildren = results.FindUint("numchild");
            }
            if (results.Contains("displayhint"))
            {
                DisplayHint = results.FindString("displayhint");
            }
            if (results.Contains("attributes"))
            {
                if (results.FindString("attributes") == "noneditable")
                {
                    _isReadonly = true;
                }
                _attribsFetched = true;
            }

            int index;

            if (!results.Contains("value") && (Name == TypeName || Name.Contains("::")))
            {
                // base classes show up with no value and exp==type 
                // (sometimes underlying debugger does not follow this convention, when using typedefs in templated types so look for "::" in the field name too)
                Name = TypeName + " (base)";
                Value = TypeName;
                VariableNodeType = NodeType.BaseClass;
            }
            else if (Int32.TryParse(this.Name, System.Globalization.NumberStyles.Integer, null, out index)) // array element
            {
                Name = '[' + this.Name + ']';
                VariableNodeType = NodeType.ArrayElement;
            }
            else if (this.Name.Length > 2 && this.Name[0] == '[' && this.Name[this.Name.Length - 1] == ']')
            {
                VariableNodeType = NodeType.ArrayElement;
            }
            else if (Name == "<anonymous union>")
            {
                VariableNodeType = NodeType.AnonymousUnion;
            }
            else if (Name.Length > 1 && Name[0] == '*')
            {
                VariableNodeType = NodeType.Dereference;
            }
            else
            {
                _strippedName = Name;
                VariableNodeType = NodeType.Field;
            }

            _internalName = results.FindString("name");
            IsChild = true;
            _format = parent._format; // inherit formatting
            _parent = parent.VariableNodeType == NodeType.AccessQualifier ? parent._parent : parent;
            this.PropertyInfoFlags = parent.PropertyInfoFlags;
        }

        public ThreadContext ThreadContext { get { return _ctx; } }


        public VariableInformation FindChildByName(string name)
        {
            EnsureChildren();
            if (CountChildren == 0)
            {
                return null;
            }
            Debug.Assert(Children != null, "Failed to find children");
            VariableInformation var = Array.Find(Children, (c) => c.Name == name);
            if (var != null)
            {
                return var;
            }
            VariableInformation baseChild = null;
            var = Array.Find(Children, (c) => (c.VariableNodeType == NodeType.BaseClass || c.VariableNodeType == NodeType.AnonymousUnion) && (baseChild = c.FindChildByName(name)) != null);
            return baseChild;
        }

        private string _internalName;  // the MI debugger's private name for this value
        private AD7Engine _engine;
        private DebuggedProcess _debuggedProcess;
        private ThreadContext _ctx;
        private bool _attribsFetched;
        private bool _isReadonly;
        /// <summary>
        /// This callback is used when we need to call into the engine for additional information for
        /// the format specifier.
        ///
        /// <param name="int">threadId</param>
        /// <param name="uint">frameLevel</param>
        /// <returns>The expression to send to the engine</returns>
        /// </summary>
        private Func<int, uint, Task<string>> _deferedFormatExpression;
        private IVariableInformation _parent;
        private string _format;
        private string _strippedName;  // "Name" stripped of format specifiers
        private string _fullname;

        public enum NodeType
        {
            Root,
            Field,
            Dereference,
            ArrayElement,
            BaseClass,
            AccessQualifier,
            Synthetic,
            AnonymousUnion
        };

        public NodeType VariableNodeType { get; private set; }

        private static readonly string[] s_stringTypes = new string[] {
                                 @"^char *\*$",
                                 @"^char *\[[0-9]*\]$",
                                 @"^const +char *\*$",
                                 @"^const +char *\[[0-9]*\]$"
                             };

        private static Regex s_isFunction = new Regex(@".+\(.*\).*");

        private string ProcessFormatSpecifiers(string exp, out string formatSpecifier)
        {
            formatSpecifier = null; // will be used with -var-set-format

            if (EngineUtils.IsConsoleExecCmd(exp, out string _, out string _))
            {
                return exp;
            }

            int lastComma = exp.LastIndexOf(',');
            if (lastComma <= 0)
                return exp;

            // https://docs.microsoft.com/en-us/visualstudio/debugger/format-specifiers-in-cpp
            string expFS = exp.Substring(lastComma + 1);
            string trimmed = expFS.Trim();
            switch (trimmed)
            {
                case "x":
                case "X":
                case "h":
                case "H":
                case "xb":
                case "Xb":
                case "hb":
                case "Hb":
                    // could be improved upon via post-processing with ToUpperInvariant/SubString
                    formatSpecifier = "zero-hexadecimal";
                    goto case "";
                case "o":
                    formatSpecifier = "octal";
                    goto case "";
                case "d":
                    formatSpecifier = "decimal";
                    goto case "";
                case "b":
                case "bb":
                    formatSpecifier = "binary";
                    goto case "";
                case "e":
                case "g":
                    goto case "";
                case "s":
                case "sb":
                case "s8":
                case "s8b":
                    return "(const char*)(" + exp.Substring(0, lastComma) + ")";
                case "su":
                case "sub":
                    return "(const char16_t*)(" + exp.Substring(0, lastComma) + ")";
                case "c":
                    return "(char)(" + exp.Substring(0, lastComma) + ")";
                // just remove and ignore these
                case "en":
                case "na":
                case "nd":
                case "nr":
                case "!":
                case "":
                    return exp.Substring(0, lastComma);
            }

            // array with static size
            var m = Regex.Match(trimmed, @"^\[?(\d+)\]?$");
            if (m.Success)
            {
                string count = m.Groups[1].Value; // (\d+) capture group
                string expr = exp.Substring(0, lastComma);

                if (_engine.DebuggedProcess.MICommandFactory.Mode == MIMode.Gdb)
                {
                    // return *<expression>@<count> which is only supported in GDB
                    return FormattableString.Invariant($"*{expr}@{count}");
                }
                else
                {
                    _deferedFormatExpression = async (int threadId, uint frameLevel) =>
                    {
                        string derefType = await GetDereferencedTypeString(expr, threadId, frameLevel);

                        if (!string.IsNullOrEmpty(derefType))
                        {
                            // Cast 'exp' to a pointer of an array of type 'T' with size 'n' with '*(T(*)[n])(exp)'
                            return FormattableString.Invariant($"*({derefType}(*)[{count}])({expr})");
                        }

                        return string.Empty;
                    };

                    return expr;
                }
            }

            // array with dynamic size
            if (Regex.Match(trimmed, @"^\[([a-zA-Z_][a-zA-Z_\d]*)\]$").Success)
                return exp.Substring(0, lastComma);

            return exp;
        }

        private async Task<string> GetDereferencedTypeString(string expr, int threadId, uint frameLevel)
        {
            // TODO: Should we error if the current type is not a pointer type?

            // Evaluates: *expr
            Results results = await _engine.DebuggedProcess.MICommandFactory.VarCreate($"*({expr})", threadId, frameLevel, 0, ResultClass.None);

            if (results.ResultClass == ResultClass.done)
            {
                string varName = results.TryFindString("name");
                if (!String.IsNullOrWhiteSpace(varName))
                {
                    // Remove the variable we created as we don't track it.
                    await _engine.DebuggedProcess.MICommandFactory.VarDelete(varName);
                }

                return results.TryFindString("type");
            }
            return null;
        }

        public void AsyncEval(IDebugEventCallback2 pExprCallback)
        {
            EngineCallback engineCallback;
            if (pExprCallback != null)
            {
                engineCallback = new EngineCallback(_engine, pExprCallback);
            }
            else
            {
                engineCallback = _engine.Callback;
            }

            uint radix = _engine.CurrentRadix();
            Task evalTask = Task.Run(async () =>
            {
                await Eval(radix);
            });

            Action<Task> onComplete = (Task t) =>
            {
                engineCallback.OnExpressionEvaluationComplete(this);
            };
            evalTask.ContinueWith(onComplete, TaskContinuationOptions.ExecuteSynchronously);
        }

        public static void AsyncErrorImpl(EngineCallback engineCallback, IVariableInformation var, IDebugProperty2 error)
        {
            Task.Run(() =>
             {
                 engineCallback.OnExpressionEvaluationComplete(var, error);
             });
        }

        public void AsyncError(IDebugEventCallback2 pExprCallback, IDebugProperty2 error)
        {
            AsyncErrorImpl(pExprCallback != null ? new EngineCallback(_engine, pExprCallback) : _engine.Callback, this, error);
        }

        public void SyncEval(enum_EVALFLAGS dwFlags = 0, DAPEvalFlags dwDAPFlags = 0)
        {
            uint radix = _engine.CurrentRadix();
            Task eval = Task.Run(async () =>
            {
                await Eval(radix, dwFlags, dwDAPFlags);
            });
            eval.Wait();
        }

        public string EvalDependentExpression(string expr)
        {
            this.VerifyNotDisposed();

            string val = null;
            Task eval = Task.Run(async () =>
            {
                val = await _engine.DebuggedProcess.MICommandFactory.DataEvaluateExpression(expr, Client.GetDebuggedThread().Id, _ctx.Level);
            });
            eval.Wait();
            return val;
        }

        internal async Task Eval(uint radix, enum_EVALFLAGS dwFlags = 0, DAPEvalFlags dwDAPFlags = 0)
        {
            this.VerifyNotDisposed();

            if (radix != 0)
            {
                await _engine.UpdateRadixAsync(radix);    // ensure the radix value is up-to-date
            }

            try
            {
                if (EngineUtils.IsConsoleExecCmd(_strippedName, out string _, out string consoleCommand))
                {
                    // special case for executing raw mi commands. 
                    string consoleResults = null;

                    consoleResults = await MIDebugCommandDispatcher.ExecuteCommand(consoleCommand, _debuggedProcess, ignoreFailures: true);
                    Value = consoleResults;
                    this.TypeName = null;
                }
                else
                {
                    bool canRunClipboardContextCommands = this._debuggedProcess.MICommandFactory.Mode == MIMode.Gdb && dwDAPFlags.HasFlag(DAPEvalFlags.CLIPBOARD_CONTEXT);
                    int numElements = 200;

                    if (canRunClipboardContextCommands)
                    {
                        string showPrintElementsResult = await MIDebugCommandDispatcher.ExecuteCommand("show print elements", _debuggedProcess, ignoreFailures: true);
                        // Possible values for 'numElementsStr'
                        // "Limit on string chars or array elements to print is <number>."
                        // "Limit on string chars or array elements to print is unlimited."
                        string numElementsStr = Regex.Match(showPrintElementsResult, @"\d+").Value;
                        if (!string.IsNullOrEmpty(numElementsStr) && int.TryParse(numElementsStr, out numElements) && numElements != 0)
                        {
                            await MIDebugCommandDispatcher.ExecuteCommand("set print elements 0", _debuggedProcess, ignoreFailures: true);
                        }
                    }

                    int threadId = Client.GetDebuggedThread().Id;
                    uint frameLevel = _ctx.Level;

                    string expression = _strippedName;

                    // If we have a deferred format expression, resolve it.
                    if (_deferedFormatExpression != null)
                    {
                        string deferedExpression = await _deferedFormatExpression(threadId, frameLevel);

                        if (!string.IsNullOrEmpty(deferedExpression))
                        {
                            expression = deferedExpression;
                        }
                        else
                        {
                            Debug.Fail(FormattableString.Invariant($"Failed to resolve deferred expression. Falling back to original: '{expression}'."));
                        }
                    }

                    Results results = await _engine.DebuggedProcess.MICommandFactory.VarCreate(expression, threadId, frameLevel, dwFlags, ResultClass.None);

                    if (results.ResultClass == ResultClass.done)
                    {
                        _internalName = results.FindString("name");
                        TypeName = results.TryFindString("type");
                        if (results.Contains("dynamic"))
                        {
                            IsPreformatted = true;
                        }
                        if (results.Contains("dynamic") && results.Contains("has_more"))
                        {
                            CountChildren = results.FindUint("has_more");
                        }
                        else
                        {
                            CountChildren = results.FindUint("numchild");
                        }
                        if (results.Contains("displayhint"))
                        {
                            DisplayHint = results.FindString("displayhint");
                        }
                        if (results.Contains("attributes"))
                        {
                            if (results.FindString("attributes") == "noneditable")
                            {
                                _isReadonly = true;
                            }
                            _attribsFetched = true;
                        }
                        Value = results.TryFindString("value");
                        if ((string.IsNullOrEmpty(Value) || _format != null) && !string.IsNullOrEmpty(_internalName))
                        {
                            if (_format != null)
                            {
                                await Format();
                            }
                            else
                            {
                                results = await _engine.DebuggedProcess.MICommandFactory.VarEvaluateExpression(_internalName, ResultClass.None);

                                if (results.ResultClass == ResultClass.done)
                                {
                                    Value = results.FindString("value");
                                }
                                else if (results.ResultClass == ResultClass.error)
                                {
                                    SetAsError(results.FindString("msg"));
                                }
                                else
                                {
                                    Debug.Fail("Unexpected format of msg from -var-evaluate-expression");
                                }
                            }
                        }
                    }
                    else if (results.ResultClass == ResultClass.error)
                    {
                        SetAsError(results.FindString("msg"));
                    }
                    else
                    {
                        Debug.Fail("Unexpected format of msg from -var-create");
                    }

                    if (canRunClipboardContextCommands && numElements != 0)
                    {
                        await MIDebugCommandDispatcher.ExecuteCommand(string.Format(CultureInfo.InvariantCulture, "set print elements {0}", numElements), _debuggedProcess, ignoreFailures: true);
                    }
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    e = e.InnerException;

                UnexpectedMIResultException miException = e as UnexpectedMIResultException;
                string message;
                if (miException != null && miException.MIError != null)
                    message = miException.MIError;
                else
                    message = e.Message;

                SetAsError(string.Format(CultureInfo.CurrentCulture, ResourceStrings.Failed_ExecCommandError, message));
            }
        }

        internal async Task Format()
        {
            this.VerifyNotDisposed();

            Debug.Assert(_internalName != null);
            Debug.Assert(_format != null);
            Results results = await _engine.DebuggedProcess.MICommandFactory.VarSetFormat(_internalName, _format, ResultClass.None);
            if (results.ResultClass == ResultClass.done)
            {
                Value = results.FindString("value");
            }
            else if (results.ResultClass == ResultClass.error)
            {
                SetAsError(results.FindString("msg"));
            }
            else
            {
                Debug.Fail("Unexpected format of msg from expression formatting");
            }
        }

        // If we have some children, go get them
        public void EnsureChildren()
        {
            if ((CountChildren != 0) && (Children == null))
            {
                Task task = FetchChildren();
                task.Wait();
            }
        }

        private Task FetchChildren()
        {
            // Note: I am not sure if it is actually useful to run the evaluation code off of the poll thread (will GDB actually handle other commands at the same time)
            // but this seems like one place where we might want to to, so I am allowing it
            return Task.Run((Func<Task>)InternalFetchChildren);
        }

        private async Task InternalFetchChildren()
        {
            this.VerifyNotDisposed();

            Results results = await _engine.DebuggedProcess.MICommandFactory.VarListChildren(_internalName, PropertyInfoFlags, ResultClass.None);

            if (results.ResultClass == ResultClass.done)
            {
                TupleValue[] children = results.Contains("children")
                    ? results.Find<ResultListValue>("children").FindAll<TupleValue>("child")
                    : new TupleValue[0];
                int i = 0;
                bool isArray = IsArrayType();
                if (isArray)
                {
                    CountChildren = results.FindUint("numchild");
                    Children = new VariableInformation[CountChildren];
                    foreach (var c in children)
                    {
                        Children[i] = new VariableInformation(c, this);
                        i++;
                    }
                }
                else if (IsMapType())
                {
                    //
                    // support for gdb's pretty-printing built-in displayHint "map", from the gdb docs:
                    //      'Indicate that the object being printed is “map-like”, and that the 
                    //      children of this value can be assumed to alternate between keys and values.'
                    //
                    List<VariableInformation> listChildren = new List<VariableInformation>();
                    for (int p = 0; (p + 1) < children.Length; p += 2)
                    {
                        if (children[p].TryFindUint("numchild") > 0)
                        {
                            var variable = new VariableInformation("[" + (p / 2).ToString(CultureInfo.InvariantCulture) + "]", this);
                            variable.CountChildren = 2;
                            var first = new VariableInformation(children[p], variable, "first");
                            var second = new VariableInformation(children[p + 1], this, "second");

                            variable.Children = new VariableInformation[] { first, second };
                            variable.TypeName = FormattableString.Invariant($"std::pair<{first.TypeName}, {second.TypeName}>");

                            listChildren.Add(variable);
                        }
                        else
                        {
                            // One Variable is created for each pair returned with the first element (p) being the name of the child
                            // and the second element (p+1) becoming the value.
                            string name = children[p].TryFindString("value");
                            var variable = new VariableInformation(children[p + 1], this, '[' + name + ']');
                            listChildren.Add(variable);
                        }
                    }
                    Children = listChildren.ToArray();
                    CountChildren = (uint)Children.Length;
                }
                else
                {
                    List<VariableInformation> listChildren = new List<VariableInformation>();
                    foreach (var c in children)
                    {
                        var variable = new VariableInformation(c, this);
                        enum_DBG_ATTRIB_FLAGS access = enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_NONE;
                        if (variable.Name == "public")
                        {
                            access = enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_PUBLIC;
                            variable.VariableNodeType = NodeType.AccessQualifier;
                        }
                        else if (variable.Name == "private")
                        {
                            access = enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_PRIVATE;
                            variable.VariableNodeType = NodeType.AccessQualifier;
                        }
                        else if (variable.Name == "protected")
                        {
                            access = enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_PROTECTED;
                            variable.VariableNodeType = NodeType.AccessQualifier;
                        }
                        if (access != enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_NONE)
                        {
                            // Add this child's children
                            await variable.InternalFetchChildren();
                            foreach (var child in variable.Children)
                            {
                                ((VariableInformation)child).Access = access;
                                listChildren.Add(child);
                            }
                        }
                        else
                        {
                            listChildren.Add(variable);
                        }
                    }
                    Children = listChildren.ToArray();
                    CountChildren = (uint)Children.Length;
                }
            }
            else
            {
                Children = new VariableInformation[0];
                CountChildren = 0;
            }
            if (_format != null)
            {
                foreach (var child in Children)
                {
                    await child.Format();
                }
            }
        }

        private void SetAsError(string msg)
        {
            TypeName = "";
            Value = msg;
            CountChildren = 0;
            Error = true;
        }

        private bool IsArrayType()
        {
            if (DisplayHint == "array")
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(TypeName))
            {
                return TypeName[TypeName.Length - 1] == ']';
            }
            return false;
        }

        private bool IsMapType()
        {
            return DisplayHint == "map";
        }

        public bool IsReadOnly()
        {
            if (!_attribsFetched)
            {
                if (string.IsNullOrEmpty(_internalName))
                {
                    return true;
                }

                this.VerifyNotDisposed();

                string attribute = string.Empty;

                _engine.DebuggedProcess.WorkerThread.RunOperation(async () =>
                {
                    attribute = await _engine.DebuggedProcess.MICommandFactory.VarShowAttributes(_internalName);
                });

                _isReadonly = (attribute == "noneditable");
                _attribsFetched = true;
            }

            return _isReadonly;
        }

        public void Assign(string expression)
        {
            this.VerifyNotDisposed();

            _engine.DebuggedProcess.WorkerThread.RunOperation(async () =>
            {
                int threadId = Client.GetDebuggedThread().Id;
                uint frameLevel = _ctx.Level;

                _engine.DebuggedProcess.FlushBreakStateData();
                Value = await _engine.DebuggedProcess.MICommandFactory.VarAssign(_internalName, expression, threadId, frameLevel);
            });
        }

        #region IDisposable Implementation

        private bool _isDisposed = false;

        private void VerifyNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            _isDisposed = true;

            //mi -var-delete deletes all children, so only top level variables should be added to the delete list
            //Additionally, we create variables for anything we try to evaluate. Only succesful evaluations get internal names, 
            //so look for that.
            if (!IsChild && !string.IsNullOrWhiteSpace(_internalName))
            {
                if (!_debuggedProcess.IsClosed)
                {
                    lock (_debuggedProcess.VariablesToDelete)
                    {
                        _debuggedProcess.VariablesToDelete.Add(_internalName);
                    }
                }
            }
        }

        ~VariableInformation()
        {
            this.Dispose(false);
        }

        #endregion
    }
}
