// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using MICore;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Microsoft.MIDebugEngine
{
    internal interface IVariableInformation
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
        void SyncEval();
        ThreadContext ThreadContext { get; }
        VariableInformation FindChildByName(string name);
        string EvalDependentExpression(string expr);
        bool IsVisualized { get; }
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
            VariableInformation vi = new VariableInformation(Name, ctx, engine, thread, IsParameter);
            await vi.Eval();
            return vi;
        }
    }

    internal class ArgumentList : Tuple<int, List<SimpleVariableInformation>>
    {
        public ArgumentList(int level, List<SimpleVariableInformation> args)
            : base(level, args)
        { }
    }

    internal class VariableInformation : IVariableInformation
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
                    case NodeType.BaseClass:
                    case NodeType.AccessQualifier:
                        _fullname = _parent.FullName();
                        break;
                    case NodeType.ArrayElement:
                        _fullname = '(' + _parent.FullName() + ')' + Name;
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

        static VariableInformation()
        {
            s_isFunction = new Regex(@".+\(.*\).*");
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
            Access = enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_NONE;
            _fullname = null;
        }

        //this constructor is used to create root nodes (local/params)
        internal VariableInformation(string expr, ThreadContext ctx, AD7Engine engine, AD7Thread thread, bool isParameter = false)
            : this(ctx, engine, thread)
        {
            // strip off formatting string
            _strippedName = StripFormatSpecifier(expr, out _format);
            Name = expr;
            IsParameter = isParameter;
            _parent = null;
            VariableNodeType = NodeType.Root;
        }

        //this constructor is used to create synthetic child nodes (local/params). These nodes are never in the parent's children list
        internal VariableInformation(string expr, IVariableInformation parent, AD7Engine engine, string displayName)
            : this(parent.ThreadContext, engine, parent.Client)
        {
            // strip off formatting string
            _strippedName = StripFormatSpecifier(expr, out _format);
            Name = displayName ?? expr;
            _parent = parent;
            VariableNodeType = NodeType.Synthetic;
        }

        //this constructor is used to create modified expressions from a parent
        internal VariableInformation(string expr, VariableInformation parent)
            : this(parent._ctx, parent._engine, parent.Client)
        {
            // strip off formatting string
            _strippedName = StripFormatSpecifier(expr, out _format);
            Name = expr;
            VariableNodeType = NodeType.Root;
        }

        //this constructor is private because it should only be used internally to create children
        private VariableInformation(TupleValue results, VariableInformation parent)
            : this(parent._ctx, parent._engine, parent.Client)
        {
            TypeName = results.TryFindString("type");
            Value = results.TryFindString("value");
            Name = results.FindString("exp");
            CountChildren = results.FindUint("numchild");
            int index;

            if (!results.Contains("value") && (Name == TypeName || Name.Contains("::")))
            {
                // base classes show up with no value and exp==type 
                // (sometimes underlying debugger does not follow this convention, when using typedefs in templated types so look for "::" in the field name too)
                Name = "base";
                Value = TypeName;
                VariableNodeType = NodeType.BaseClass;
            }
            else if (Int32.TryParse(this.Name, System.Globalization.NumberStyles.Integer, null, out index)) // array element
            {
                Name = '[' + this.Name + ']';
                VariableNodeType = NodeType.ArrayElement;
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
        }

        private VariableInformation(TupleValue results, VariableInformation parent, bool ro)
            : this(results, parent)
        {
            _attribsFetched = true;  // read-only attribute is passed in at construction
            _isReadonly = ro;
        }

        ~VariableInformation()
        {
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
            var = Array.Find(Children, (c) => c.VariableNodeType == NodeType.BaseClass && (baseChild = c.FindChildByName(name)) != null);
            return baseChild;
        }

        private string _internalName;  // the MI debugger's private name for this value
        private AD7Engine _engine;
        private DebuggedProcess _debuggedProcess;
        private ThreadContext _ctx;
        private bool _attribsFetched;
        private bool _isReadonly;
        private string _format;
        private string _strippedName;  // "Name" stripped of format specifiers
        private IVariableInformation _parent;
        private string _fullname;

        public enum NodeType
        {
            Root,
            Field,
            ArrayElement,
            BaseClass,
            AccessQualifier,
            Synthetic
        };

        public NodeType VariableNodeType { get; private set; }

        private static readonly string[] s_stringTypes = new string[] {
                                 @"^char *\*$",
                                 @"^char *\[[0-9]*\]$",
                                 @"^const +char *\*$",
                                 @"^const +char *\[[0-9]*\]$"
                             };

        private static Regex s_isFunction;

        private string StripFormatSpecifier(string exp, out string formatSpecifier)
        {
            formatSpecifier = null;
            int lastComma = exp.LastIndexOf(',');
            if (lastComma > 0)
            {
                string expFS = exp.Substring(lastComma + 1);
                string trimmed = expFS.Trim();
                if (trimmed == "x" || trimmed == "X" || trimmed == "h" || trimmed == "H")
                {
                    formatSpecifier = "hexadecimal";
                    return exp.Substring(0, lastComma);
                }
                else if (trimmed == "o")
                {
                    formatSpecifier = "octal";
                    return exp.Substring(0, lastComma);
                }
            }
            return exp;
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

            Task evalTask = Task.Run(async () =>
            {
                await Eval();
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

        public void SyncEval()
        {
            Task eval = Task.Run(async () =>
            {
                await Eval();
            });
            eval.Wait();
        }

        public string EvalDependentExpression(string expr)
        {
            string val = null;
            Task eval = Task.Run(async () =>
            {
                val = await _engine.DebuggedProcess.MICommandFactory.DataEvaluateExpression(expr, Client.GetDebuggedThread().Id, _ctx.Level);
            });
            eval.Wait();
            return val;
        }

        internal async Task Eval()
        {
            _engine.CurrentRadix();    // ensure the radix value is up-to-date

            int threadId = Client.GetDebuggedThread().Id;
            uint frameLevel = _ctx.Level;
            Results results = await _engine.DebuggedProcess.MICommandFactory.VarCreate(_strippedName, threadId, frameLevel, ResultClass.None);

            if (results.ResultClass == ResultClass.done)
            {
                _internalName = results.FindString("name");
                TypeName = results.TryFindString("type");
                CountChildren = results.FindUint("numchild");
                Value = results.TryFindString("value");
                if ((Value == String.Empty || _format != null) && !string.IsNullOrEmpty(_internalName))
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
                            Debug.Fail("Weird msg from -var-evaluate-expression");
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
                Debug.Fail("Weird msg from -var-create");
            }
        }

        internal async Task Format()
        {
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
                Debug.Fail("Weird msg from expression formatting");
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
            Results results = await _engine.DebuggedProcess.CmdAsync(string.Format("-var-list-children --simple-values {0}", _internalName), ResultClass.None);

            if (results.ResultClass == ResultClass.done)
            {
                TupleValue[] children = results.Find<ResultListValue>("children").FindAll<TupleValue>("child");
                int i = 0;
                bool isArray = IsArrayType();
                bool elementIsReadonly = false;
                if (isArray)
                {
                    CountChildren = results.FindUint("numchild");
                    Children = new VariableInformation[CountChildren];
                    //if (k.Length > 0)    // perform attrib check on first array child, apply to all elements
                    //{
                    //    MICore.Debugger.Results childResults = MICore.Debugger.DecodeResults(k[0]);
                    //    string name = childResults.Find("name");
                    //    Debug.Assert(!string.IsNullOrEmpty(name));
                    //    string attribute = await m_engine.DebuggedProcess.MICommandFactory.VarShowAttributes(name);
                    //    elementIsReadonly = (attribute != "editable");
                    //}
                    foreach (var c in children)
                    {
                        Children[i] = new VariableInformation(c, this, elementIsReadonly);
                        i++;
                    }
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
            if (!string.IsNullOrWhiteSpace(TypeName))
            {
                return TypeName[TypeName.Length - 1] == ']';
            }
            return false;
        }

        public bool IsReadOnly()
        {
            if (!_attribsFetched)
            {
                if (string.IsNullOrEmpty(_internalName))
                {
                    return true;
                }

                string attribute = string.Empty;

                _engine.DebuggedProcess.WorkerThread.RunOperation(async () =>
                {
                    attribute = await _engine.DebuggedProcess.MICommandFactory.VarShowAttributes(_internalName);
                });

                _isReadonly = (attribute != "editable");
                _attribsFetched = true;
            }
            return _isReadonly;
        }

        public void Assign(string expression)
        {
            _engine.DebuggedProcess.WorkerThread.RunOperation(async () =>
            {
                _engine.DebuggedProcess.FlushBreakStateData();
                Value = await _engine.DebuggedProcess.MICommandFactory.VarAssign(_internalName, expression);
            });
        }
    }
}
