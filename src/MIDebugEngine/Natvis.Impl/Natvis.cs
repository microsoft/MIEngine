// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using Microsoft.DebugEngineHost;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Debugger.Interop.DAP;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.MIDebugEngine.Natvis
{
    internal class SimpleWrapper : IVariableInformation
    {
        public SimpleWrapper(string name, AD7Engine engine, IVariableInformation underlyingVariable)
        {
            Parent = underlyingVariable;
            Name = name;
            _engine = engine;
        }

        public readonly IVariableInformation Parent;
        private AD7Engine _engine;

        public string Name { get; private set; }
        public string Value { get { return Parent.Value; } }
        public virtual string TypeName { get { return Parent.TypeName; } }
        public bool IsParameter { get { return Parent.IsParameter; } }
        public VariableInformation[] Children { get { return Parent.Children; } }
        public AD7Thread Client { get { return Parent.Client; } }
        public bool Error { get { return Parent.Error; } }
        public uint CountChildren { get { return Parent.CountChildren; } }
        public bool IsChild { get { return Parent.IsChild; } set { Parent.IsChild = value; } }
        public enum_DBG_ATTRIB_FLAGS Access { get { return Parent.Access; } }
        public bool IsStringType { get { return Parent.IsStringType; } }
        public ThreadContext ThreadContext { get { return Parent.ThreadContext; } }
        public virtual bool IsVisualized { get { return Parent.IsVisualized; } }
        public virtual enum_DEBUGPROP_INFO_FLAGS PropertyInfoFlags { get; set; }
        public virtual bool IsReadOnly() => Parent.IsReadOnly();
        public bool IsNullPointer() => Parent.IsNullPointer();

        public VariableInformation FindChildByName(string name) => Parent.FindChildByName(name);
        public string EvalDependentExpression(string expr) => Parent.EvalDependentExpression(expr);
        public void AsyncEval(IDebugEventCallback2 pExprCallback) => Parent.AsyncEval(pExprCallback);
        public void SyncEval(enum_EVALFLAGS dwFlags, DAPEvalFlags dwDAPFlags) => Parent.SyncEval(dwFlags, dwDAPFlags);
        public virtual string FullName() => Parent.FullName();
        public void EnsureChildren() => Parent.EnsureChildren();
        public void AsyncError(IDebugEventCallback2 pExprCallback, IDebugProperty2 error)
        {
            VariableInformation.AsyncErrorImpl(pExprCallback != null ? new EngineCallback(_engine, pExprCallback) : _engine.Callback, this, error);
        }

        public void Dispose()
        {
        }
        public bool IsPreformatted { get { return Parent.IsPreformatted; } set { } }

        public string Address()
        {
            return Parent.Address();
        }
        public uint Size()
        {
            return Parent.Size();
        }
    }

    internal class VisualizerWrapper : SimpleWrapper
    {
        public readonly Natvis.VisualizerInfo Visualizer;
        private readonly bool _isVisualizerView;

        public VisualizerWrapper(string name, AD7Engine engine, IVariableInformation underlyingVariable, Natvis.VisualizerInfo viz, bool isVisualizerView)
            : base(name, engine, underlyingVariable)
        {
            Visualizer = viz;
            _isVisualizerView = isVisualizerView;
        }
        public override bool IsVisualized { get { return _isVisualizerView; } }
        public override string TypeName { get { return String.Empty; } }
        public override string FullName()
        {
            return _isVisualizerView ? Parent.Name + ",viz" : Name;
        }
    }
    /// <summary>
    /// Represents a VisualizedWrapper that starts at an offset.
    /// </summary>
    internal class PaginatedVisualizerWrapper : VisualizerWrapper
    {
        public readonly uint StartIndex;

        public PaginatedVisualizerWrapper(string name, AD7Engine engine, IVariableInformation underlyingVariable, Natvis.VisualizerInfo viz, bool isVisualizerView, uint startIndex=0)
            : base(name, engine, underlyingVariable, viz, isVisualizerView)
        {
            StartIndex = startIndex;
        }
    }
    /// <summary>
    /// Represents the continuation of a LinkedListItemsType.
    /// </summary>
    internal sealed class LinkedListContinueWrapper : PaginatedVisualizerWrapper
    {
        public readonly IVariableInformation ContinueNode;
        public LinkedListContinueWrapper(string name, AD7Engine engine, IVariableInformation underlyingVariable, Natvis.VisualizerInfo viz, bool isVisualizerView, IVariableInformation continueNode, uint startIndex)
            : base(name, engine, underlyingVariable, viz, isVisualizerView, startIndex)
        {
            ContinueNode = continueNode;
        }
    }

    internal class Node
    {
        public enum ScanState
        {
            left, value, right
        }
        public ScanState State { get; set; }
        public IVariableInformation Content { get; private set; }
        public Node(IVariableInformation v)
        {
            Content = v;
            State = ScanState.left;
        }
    }
    /// <summary>
    /// Represents the continuation of a TreeItemsType.
    /// </summary>
    internal sealed class TreeContinueWrapper : PaginatedVisualizerWrapper
    {
        public readonly Node ContinueNode;
        public readonly Stack<Node> Nodes;
        public TreeContinueWrapper(string name, AD7Engine engine, IVariableInformation underlyingVariable, Natvis.VisualizerInfo viz, bool isVisualizerView, Node continueNode, Stack<Node> nodes, uint startIndex)
            : base (name, engine, underlyingVariable, viz, isVisualizerView, startIndex)
        {
            ContinueNode = continueNode;
            Nodes = nodes;
        }
    }

    internal struct VisualizerId
    {
        public string Name { get; }
        public int Id { get; }

        public VisualizerId(string name,int id)
        {
            this.Name = name;
            this.Id = id;
        }
    };

    public class Natvis : IDisposable
    {
        private class AliasInfo
        {
            public TypeName ParsedName { get; private set; }
            public AliasType Alias { get; private set; }
            public AliasInfo(TypeName name, AliasType alias)
            {
                ParsedName = name;
                Alias = alias;
            }
        }

        private class TypeInfo
        {
            public TypeName ParsedName { get; private set; }
            public VisualizerType Visualizer { get; private set; }

            public TypeInfo(TypeName name, VisualizerType visualizer)
            {
                ParsedName = name;
                Visualizer = visualizer;
            }
        }
        private class FileInfo
        {
            public List<TypeInfo> Visualizers { get; private set; }
            public List<AliasInfo> Aliases { get; private set; }
            public List<UIVisualizerType> UIVisualizers { get; set; } = null;
            public readonly AutoVisualizer Environment;

            public FileInfo(AutoVisualizer env)
            {
                Environment = env;
                Visualizers = new List<TypeInfo>();
                Aliases = new List<AliasInfo>();
            }
        }

        internal class VisualizerInfo
        {
            public VisualizerType Visualizer { get; private set; }
            public Dictionary<string, string> ScopedNames { get; private set; }

            /// <summary>
            /// Intrinsics defined in this type block, keyed by name.
            /// Stored as IntrinsicType so that Parameter[] is available at call time
            /// for argument substitution in parametrized intrinsics.
            /// </summary>
            public Dictionary<string, IntrinsicType> Intrinsics { get; }

            public VisualizerId[] GetUIVisualizers()
            {
                return this.Visualizer.Items.Where((i) => i is UIVisualizerItemType).Select(i =>
                  {
                      var visualizer = (UIVisualizerItemType)i;
                      return new VisualizerId(visualizer.ServiceId, visualizer.Id);
                  }).ToArray();
            }

            public VisualizerInfo(VisualizerType viz, TypeName name)
            {
                Visualizer = viz;
                // add the template parameter macro values
                ScopedNames = new Dictionary<string, string>();
                for (int i = 0; i < name.Args.Count; ++i)
                {
                    ScopedNames["$T" + (i + 1).ToString(CultureInfo.InvariantCulture)] = name.Args[i].FullyQualifiedName;
                }
                // collect intrinsics defined in this type block
                Intrinsics = new Dictionary<string, IntrinsicType>();
                if (viz.Items != null)
                {
                    foreach (var item in viz.Items)
                    {
                        if (item is IntrinsicType intrinsic && !string.IsNullOrEmpty(intrinsic.Name))
                        {
                            Intrinsics[intrinsic.Name] = intrinsic;
                        }
                    }
                }
            }
        }

        private static Regex s_variableName = new Regex("[a-zA-Z$_][a-zA-Z$_0-9]*");
        private static Regex s_subfieldNameHere = new Regex(@"\G((\.|->)[a-zA-Z$_][a-zA-Z$_0-9]*)+");
        private static Regex s_expression = new Regex(@"^\{[^\}]*\}");
        private static readonly Regex s_moduleQualifiedPrefix = new Regex(@"\w+(?:\.\w+)*\.(?:dll|exe)!", RegexOptions.IgnoreCase);
        private static readonly Regex s_intrinsicCallPattern = new Regex(@"\b(\w+)\s*\(");
        // Matches the leading "0x<hex> " address that GDB/LLDB prepends when displaying a string pointer value.
        private static readonly Regex s_addressPrefix = new Regex(@"^0x[0-9a-fA-F]+\s+");
        // Matches "varName = rhs" in a CustomListItems <Exec> expression to detect local-variable assignments.
        // The negative look-ahead (?!=) prevents matching "==" comparison operators.
        private static readonly Regex s_execAssignment = new Regex(@"^\s*(\w+)\s*=(?!=)\s*(.+)$", RegexOptions.Singleline | RegexOptions.Compiled);
        // Matches the bare "$i" token (word boundary) in a CustomListItems Name template.
        // The {$i} form is matched first with a plain Replace; this regex handles bare "$i"
        // with a word-boundary guard so that e.g. "$item" is not corrupted.
        private static readonly Regex s_dollarI = new Regex(@"\$i\b", RegexOptions.Compiled);
        // Matches increment/decrement shorthand in <Exec>: ++i, i++, --i, i--
        // Groups: (1) prefix-op  (2) prefix-varname | (3) postfix-varname  (4) postfix-op
        private static readonly Regex s_execIncrDecr = new Regex(@"^\s*(?:(\+\+|--)(\w+)|(\w+)(\+\+|--))\s*$", RegexOptions.Compiled);
        private List<FileInfo> _typeVisualizers;
        private DebuggedProcess _process;
        private HostConfigurationStore _configStore;
        private Dictionary<string, VisualizerInfo> _vizCache;
        private uint _depth;
        public HostWaitDialog WaitDialog { get; private set; }

        public VisualizationCache Cache { get; private set; }

        private const uint MAX_EXPAND = 50;
        private const int MAX_FORMAT_DEPTH = 10;
        private const int MAX_ALIAS_CHAIN = 10;

        private IDisposable _natvisSettingWatcher;

        public enum DisplayStringsState
        {
            On,
            Off,
            ForVisualizedItems
        }
        public DisplayStringsState ShowDisplayStrings { get; set; }

        internal Natvis(DebuggedProcess process, bool showDisplayString, HostConfigurationStore configStore)
        {
            _typeVisualizers = new List<FileInfo>();
            _process = process;
            _vizCache = new Dictionary<string, VisualizerInfo>();
            WaitDialog = new HostWaitDialog(ResourceStrings.VisualizingExpressionMessage, ResourceStrings.VisualizingExpressionCaption);
            ShowDisplayStrings = showDisplayString ? DisplayStringsState.On : DisplayStringsState.ForVisualizedItems;  // don't compute display strings unless explicitly requested
            _depth = 0;
            Cache = new VisualizationCache();
            _configStore = configStore;
        }

        private void InitializeNatvisServices()
        {
            try
            {
                _natvisSettingWatcher = HostNatvisProject.WatchNatvisOptionSetting(_configStore, _process.Logger.NatvisLogger);
                HostNatvisProject.FindNatvis((s) => LoadFile(s));
            }
            catch (FileNotFoundException)
            {
                // failed to find the VS Service
            }
        }

        /*
         * Handle multiple Natvis files
         */
        public void Initialize(List<string> fileNames)
        {
            InitializeNatvisServices();
            if (fileNames != null && fileNames.Count > 0)
            {
                foreach (var fileName in fileNames)
                {
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        if (!Path.IsPathRooted(fileName))
                        {
                            string globalVisualizersDirectory = _process.Engine.GetMetric("GlobalVisualizersDirectory") as string;
                            string globalNatVisPath = null;
                            if (!string.IsNullOrEmpty(globalVisualizersDirectory) && !string.IsNullOrEmpty(fileName))
                            {
                                globalNatVisPath = Path.Combine(globalVisualizersDirectory, fileName);
                            }

                            // For local launch, try and load natvis next to the target exe if it exists and if 
                            // the exe is rooted. If the file doesn't exist, and also doesn't exist in the global folder fail.
                            if (_process.LaunchOptions is LocalLaunchOptions)
                            {
                                string exePath = (_process.LaunchOptions as LocalLaunchOptions).ExePath;
                                if (Path.IsPathRooted(exePath))
                                {
                                    string localNatvisPath = Path.Combine(Path.GetDirectoryName(exePath), fileName);

                                    if (File.Exists(localNatvisPath))
                                    {
                                        LoadFile(localNatvisPath);
                                        return;
                                    }
                                    else if (globalNatVisPath == null || !File.Exists(globalNatVisPath))
                                    {
                                        // Neither local or global path exists, report an error.
                                        _process.WriteOutput(String.Format(CultureInfo.CurrentCulture, ResourceStrings.FileNotFound, localNatvisPath));
                                        return;
                                    }
                                }
                            }

                            // Local wasn't supported or the file didn't exist. Try and load from globally registered visualizer directory if local didn't work 
                            // or wasn't supported by the launch options
                            if (!string.IsNullOrEmpty(globalNatVisPath))
                            {
                                LoadFile(globalNatVisPath);
                            }
                        }
                        else
                        {
                            // Full path to the natvis file.. Just try the load
                            LoadFile(fileName);
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security.Xml", "CA3053: UseSecureXmlResolver.",
            Justification = "Usage is secure -- XmlResolver property is set to 'null' in desktop CLR, and is always null in CoreCLR. But CodeAnalysis cannot understand the invocation since it happens through reflection.")]
        private bool LoadFile(string path)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AutoVisualizer));
                if (!File.Exists(path))
                {
                    _process.Logger.NatvisLogger?.WriteLine(LogLevel.Error, ResourceStrings.FileNotFound, path);
                    return false;
                }
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreComments = true;
                settings.IgnoreProcessingInstructions = true;
                settings.IgnoreWhitespace = true;

                // set XmlResolver via reflection, if it exists. This is required for desktop CLR, as otherwise the XML reader may
                // attempt to hit untrusted external resources.
                var xmlResolverProperty = settings.GetType().GetProperty("XmlResolver", BindingFlags.Public | BindingFlags.Instance);
                xmlResolverProperty?.SetValue(settings, null);

                using (var stream = new System.IO.FileStream(path, FileMode.Open, FileAccess.Read))
                using (var reader = XmlReader.Create(stream, settings))
                {
                    AutoVisualizer autoVis = null;
                    autoVis = serializer.Deserialize(reader) as AutoVisualizer;
                    if (autoVis != null)
                    {
                        FileInfo f = new FileInfo(autoVis);
                        if (autoVis.Items == null)
                        {
                            return false;
                        }
                        foreach (var o in autoVis.Items)
                        {
                            if (o is VisualizerType)
                            {
                                VisualizerType v = (VisualizerType)o;
                                TypeName t = TypeName.Parse(v.Name, _process.Logger.NatvisLogger);
                                if (t != null)
                                {
                                    lock (_typeVisualizers)
                                    {
                                        f.Visualizers.Add(new TypeInfo(t, v));
                                    }
                                }
                                // add an entry for each alternative name too
                                if (v.AlternativeType != null)
                                {
                                    foreach (var a in v.AlternativeType)
                                    {
                                        t = TypeName.Parse(a.Name, _process.Logger.NatvisLogger);
                                        if (t != null)
                                        {
                                            lock (_typeVisualizers)
                                            {
                                                f.Visualizers.Add(new TypeInfo(t, v));
                                            }
                                        }
                                    }
                                }
                            }
                            else if (o is AliasType)
                            {
                                AliasType a = (AliasType)o;
                                TypeName t = TypeName.Parse(a.Name, _process.Logger.NatvisLogger);
                                if (t != null)
                                {
                                    lock (_typeVisualizers)
                                    {
                                        f.Aliases.Add(new AliasInfo(t, a));
                                    }
                                }
                            }
                        }

                        if (autoVis.UIVisualizer != null)
                        {
                            f.UIVisualizers = autoVis.UIVisualizer.ToList();
                        }

                        _typeVisualizers.Add(f);
                    }
                    return autoVis != null;
                }
            }
            catch (Exception exception)
            {
                // don't allow natvis failures to stop debugging
                _process.Logger.NatvisLogger?.WriteLine(LogLevel.Error, ResourceStrings.ErrorReadingFile, exception.Message, path);
                return false;
            }
        }

        internal (string value, VisualizerId[] uiVisualizers) FormatDisplayString(IVariableInformation variable, string currentView = null)
        {
            VisualizerInfo visualizer = null;
            try
            {
                _depth++;
                if (_depth < MAX_FORMAT_DEPTH)
                {
                    if (!(variable is VisualizerWrapper) && //no displaystring for dummy vars ([Raw View])
                        (ShowDisplayStrings == DisplayStringsState.On
                        || (ShowDisplayStrings == DisplayStringsState.ForVisualizedItems && variable.IsVisualized)) &&
                        !variable.IsPreformatted)
                    {
                        visualizer = FindType(variable);
                        if (visualizer == null)
                        {
                            return (variable.Value, null);
                        }

                        Cache.Add(variable);    // vizualized value has been displayed
                        foreach (var item in visualizer.Visualizer.Items)
                        {
                            if (item is DisplayStringType)
                            {
                                DisplayStringType display = item as DisplayStringType;
                                // e.g. <DisplayString>{{ size={_Mypair._Myval2._Mylast - _Mypair._Myval2._Myfirst} }}</DisplayString>

                                // IncludeView: only use this DisplayString when the named view is active.
                                if (!IsIncludeViewMatch(display.IncludeView, currentView))
                                    continue;

                                // ExcludeView: skip this DisplayString when the current view is in the excluded list.
                                if (IsExcludeViewMatch(display.ExcludeView, currentView))
                                    continue;

                                if (!EvalCondition(display.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                                {
                                    continue;
                                }
                                return (FormatValue(display.Value, variable, visualizer.ScopedNames, visualizer.Intrinsics), visualizer.GetUIVisualizers());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // don't allow natvis to mess up debugging
                // eat any exceptions and return the variable's value
                _process.Logger.NatvisLogger?.WriteLine(LogLevel.Error, "FormatDisplayString: " + e.Message);
            }
            finally
            {
                _depth--;
            }
            return (variable.Value, visualizer?.GetUIVisualizers());
        }

        private IVariableInformation GetVisualizationWrapper(IVariableInformation variable)
        {
            if (variable.IsPreformatted)
            {
                return null;
            }
            VisualizerInfo visualizer = FindType(variable);
            if (visualizer == null || variable is VisualizerWrapper)    // don't stack wrappers
            {
                return null;
            }
            ExpandType1 expandType = (ExpandType1)Array.Find(visualizer.Visualizer.Items, (o) => { return o is ExpandType1; });
            if (expandType == null)
            {
                return null;
            }
            // return expansion with [Visualizer View] child as first element
            return new VisualizerWrapper(ResourceStrings.VisualizedView, _process.Engine, variable, visualizer, isVisualizerView: true);
        }

        internal IVariableInformation[] Expand(IVariableInformation variable, string currentView = null)
        {
            try
            {
                variable.EnsureChildren();
                if (variable.IsVisualized
                        || ((ShowDisplayStrings == DisplayStringsState.On) && !(variable is VisualizerWrapper)))    // visualize right away if DisplayStringsState.On, but only if not dummy var ([Raw View])
                {
                    return ExpandVisualized(variable, currentView);
                }
                IVariableInformation visView = GetVisualizationWrapper(variable);
                if (visView == null)
                {
                    return variable.Children;
                }
                List<IVariableInformation> children = new List<IVariableInformation>();
                children.Add(visView);
                children.AddRange(variable.Children);
                return children.ToArray();
            }
            catch (Exception e)
            {
                _process.Logger.WriteLine(LogLevel.Error, "natvis Expand: " + e.Message);    // TODO: add telemetry
                return variable.Children;
            }
        }

        internal IVariableInformation GetVariable(string expr, AD7StackFrame frame)
        {
            IVariableInformation variable;
            if (!EngineUtils.IsConsoleExecCmd(expr, out string _, out string _)
                && expr.EndsWith(",viz", StringComparison.Ordinal))
            {
                expr = expr.Substring(0, expr.Length - 4);
                variable = new VariableInformation(expr, expr, frame.ThreadContext, frame.Engine, frame.Thread);
                variable.SyncEval();
                if (!variable.Error)
                {
                    variable = GetVisualizationWrapper(variable) ?? variable;
                }
            }
            else
            {
                variable = new VariableInformation(expr, expr, frame.ThreadContext, frame.Engine, frame.Thread);
            }
            return variable;
        }

        internal string GetUIVisualizerName(string serviceId, int id)
        {
            string result = string.Empty;
            this._typeVisualizers.ForEach((f)=>
            {
                UIVisualizerType uiViz;
                if ((uiViz = f.UIVisualizers?.FirstOrDefault((u) => u.ServiceId == serviceId && u.Id == id)) != null)
                {
                    result = uiViz.MenuName;
                }
            });

            return result;
        }

        private delegate IVariableInformation Traverse(IVariableInformation node);

        private IVariableInformation[] ExpandVisualized(IVariableInformation variable, string currentView = null)
        {
            VisualizerInfo visualizer = FindType(variable);
            if (visualizer == null)
            {
                return variable.Children;
            }
            List<IVariableInformation> children = new List<IVariableInformation>();
            ExpandType1 expandType = (ExpandType1)Array.Find(visualizer.Visualizer.Items, (o) => { return o is ExpandType1; });
            if (expandType == null)
            {
                return variable.Children;
            }
            foreach (var i in expandType.Items)
            {
                if (i is ItemType && !(variable is PaginatedVisualizerWrapper)) // we do not want to repeatedly display other ItemTypes when expanding the "[More...]" node
                {
                    ItemType item = (ItemType)i;
                    if (!IsIncludeViewMatch(item.IncludeView, currentView))
                        continue;
                    if (IsExcludeViewMatch(item.ExcludeView, currentView))
                        continue;
                    if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                    {
                        continue;
                    }
                    IVariableInformation expr = GetExpression(item.Value, variable, visualizer.ScopedNames, item.Name, visualizer.Intrinsics);
                    children.Add(expr);
                }
                else if (i is ArrayItemsType)
                {
                    ArrayItemsType item = (ArrayItemsType)i;
                    if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                    {
                        continue;
                    }

                    uint totalSize = 0;
                    int rank = 0;
                    uint[] dimensions = null;

                    if (!string.IsNullOrEmpty(item.Rank))
                    {
                        totalSize = 1;
                        if (!int.TryParse(item.Rank, NumberStyles.None, CultureInfo.InvariantCulture, out rank))
                        {
                            string expressionValue = GetExpressionValue(item.Rank, variable, visualizer.ScopedNames, visualizer.Intrinsics);
                            rank = Int32.Parse(expressionValue, CultureInfo.InvariantCulture);
                        }
                        if (rank <= 0)
                        {
                            throw new Exception("Invalid rank value");
                        }
                        dimensions = new uint[rank];
                        for (int idx = 0; idx < rank; idx++)
                        {
                            // replace $i with Item.Rank here before passing it into GetExpressionValue
                            string substitute = item.Size.Replace("$i", idx.ToString(CultureInfo.InvariantCulture));
                            string val = GetExpressionValue(substitute, variable, visualizer.ScopedNames, visualizer.Intrinsics);
                            uint tmp = MICore.Debugger.ParseUint(val, throwOnError: true);
                            dimensions[idx] = tmp;
                            totalSize *= tmp;
                        }
                    }
                    else
                    {
                        string val = GetExpressionValue(item.Size, variable, visualizer.ScopedNames, visualizer.Intrinsics);
                        totalSize = MICore.Debugger.ParseUint(val, throwOnError: true);
                    }

                    uint startIndex = 0;
                    if (variable is PaginatedVisualizerWrapper pvwVariable)
                    {
                        startIndex = pvwVariable.StartIndex;
                    }

                    ValuePointerType[] vptrs = item.ValuePointer;
                    foreach (var vp in vptrs)
                    {
                        if (EvalCondition(vp.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                        {
                            IVariableInformation ptrExpr = GetExpression("*(" + vp.Value + ")", variable, visualizer.ScopedNames, intrinsics: visualizer.Intrinsics);
                            string typename = ptrExpr.TypeName;
                            if (String.IsNullOrWhiteSpace(typename))
                            {
                                continue;
                            }

                            // Creates a dereferenced pointer-to-array expression: (*(T(*)[50])(ValuePointer + 50))
                            // This evaluates for 50 elements of type T, starting at <ValuePointer> with an offset of 50 elements.
                            // E.g. This will grab elements 50 - 99 from <ValuePointer>.
                            // Note:
                            //   If requestedSize > 1000, the evaluation will only grab the first 1000 elements.

                            // We want to limit it to at most 50.
                            uint requestedSize = Math.Min(MAX_EXPAND, totalSize - startIndex);

                            StringBuilder arrayBuilder = new StringBuilder();
                            arrayBuilder.Append("(*(");
                            arrayBuilder.Append(typename);
                            arrayBuilder.Append("(*)[");
                            arrayBuilder.Append(requestedSize);
                            arrayBuilder.Append("])(");
                            arrayBuilder.Append(vp.Value);
                            arrayBuilder.Append('+');
                            arrayBuilder.Append(startIndex);
                            arrayBuilder.Append("))");
                            string arrayStr = arrayBuilder.ToString();

                            IVariableInformation arrayExpr = GetExpression(arrayStr, variable, visualizer.ScopedNames, intrinsics: visualizer.Intrinsics);
                            arrayExpr.EnsureChildren();
                            if (arrayExpr.CountChildren != 0)
                            {
                                uint offset = startIndex + requestedSize;
                                bool isForward = item.Direction != ArrayDirectionType.Backward;

                                for (uint index = 0; index < requestedSize; ++index)
                                {
                                    uint currentOffsetIndex = startIndex + index;
                                    string displayName = rank > 1 ? GetDisplayNameFromArrayIndex(currentOffsetIndex, rank, dimensions, isForward) : currentOffsetIndex.ToString(CultureInfo.InvariantCulture);
                                    children.Add(new SimpleWrapper("[" + displayName + "]", _process.Engine, arrayExpr.Children[index]));
                                }

                                if (totalSize > offset)
                                {
                                    IVariableInformation moreVariable = new PaginatedVisualizerWrapper(ResourceStrings.MoreView, _process.Engine, variable, FindType(variable), isVisualizerView: true, offset);
                                    children.Add(moreVariable);
                                }
                            }
                            break;
                        }
                    }
                }
                else if (i is TreeItemsType)
                {
                    TreeItemsType item = (TreeItemsType)i;
                    if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                    {
                        continue;
                    }
                    if (String.IsNullOrWhiteSpace(item.Size) || String.IsNullOrWhiteSpace(item.HeadPointer) || String.IsNullOrWhiteSpace(item.LeftPointer) ||
                        String.IsNullOrWhiteSpace(item.RightPointer))
                    {
                        continue;
                    }
                    if (item.ValueNode == null || String.IsNullOrWhiteSpace(item.ValueNode.Value))
                    {
                        continue;
                    }
                    string val = GetExpressionValue(item.Size, variable, visualizer.ScopedNames, visualizer.Intrinsics);
                    uint size = MICore.Debugger.ParseUint(val, throwOnError: true);
                    IVariableInformation headVal;
                    if (variable is TreeContinueWrapper tcw)
                    {
                        headVal = tcw.ContinueNode.Content;
                    }
                    else
                    {
                        headVal = GetExpression(item.HeadPointer, variable, visualizer.ScopedNames, intrinsics: visualizer.Intrinsics);
                    }
                    ulong head = MICore.Debugger.ParseAddr(headVal.Value);
                    var content = new List<IVariableInformation>();
                    if (head != 0 && size != 0)
                    {
                        headVal.EnsureChildren();
                        Traverse goLeft = GetTraverse(item.LeftPointer, headVal);
                        Traverse goRight = GetTraverse(item.RightPointer, headVal);
                        Traverse getValue = null;
                        if (item.ValueNode.Value == "this") // TODO: handle condition
                        {
                            getValue = (v) => v;
                        }
                        else if (headVal.FindChildByName(item.ValueNode.Value) != null)
                        {
                            getValue = (v) => v.FindChildByName(item.ValueNode.Value);
                        }
                        else if (GetExpression(item.ValueNode.Value, headVal, visualizer.ScopedNames, intrinsics: visualizer.Intrinsics) != null)
                        {
                            getValue = (v) => GetExpression(item.ValueNode.Value, v, visualizer.ScopedNames, intrinsics: visualizer.Intrinsics);
                        }
                        if (goLeft == null || goRight == null || getValue == null)
                        {
                            continue;
                        }
                        uint startIndex = 0;
                        IVariableInformation parent = variable;
                        if (variable is PaginatedVisualizerWrapper visualizerWrapper)
                        {
                            startIndex = visualizerWrapper.StartIndex;
                            parent = visualizerWrapper.Parent;
                        }
                        else if (variable is SimpleWrapper simpleWrapper)
                        {
                            parent = simpleWrapper.Parent;
                        }
                        TraverseTree(headVal, goLeft, goRight, getValue, children, size, variable, parent, startIndex);
                    }
                }
                else if (i is LinkedListItemsType)
                {
                    // example:
                    //    <LinkedListItems>
                    //      <Size>m_nElements</Size>    -- optional, will go until NextPoint is 0 or == HeadPointer
                    //      <HeadPointer>m_pHead</HeadPointer>
                    //      <NextPointer>m_pNext</NextPointer>
                    //      <ValueNode>m_element</ValueNode>
                    //    </LinkedListItems>
                    LinkedListItemsType item = (LinkedListItemsType)i;
                    if (!String.IsNullOrWhiteSpace(item.Condition))
                    {
                        if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                            continue;
                    }
                    if (String.IsNullOrWhiteSpace(item.HeadPointer) || String.IsNullOrWhiteSpace(item.NextPointer))
                    {
                        continue;
                    }
                    if (String.IsNullOrWhiteSpace(item.ValueNode))
                    {
                        continue;
                    }
                    uint size = MAX_EXPAND;
                    if (!String.IsNullOrWhiteSpace(item.Size))
                    {
                        string val = GetExpressionValue(item.Size, variable, visualizer.ScopedNames, visualizer.Intrinsics);
                        size = MICore.Debugger.ParseUint(val);
                    }
                    IVariableInformation headVal;
                    if (variable is LinkedListContinueWrapper llcw)
                    {
                        headVal = llcw.ContinueNode;
                    }
                    else
                    {
                        headVal = GetExpression(item.HeadPointer, variable, visualizer.ScopedNames, intrinsics: visualizer.Intrinsics);
                    }
                    ulong head = MICore.Debugger.ParseAddr(headVal.Value);
                    var content = new List<IVariableInformation>();
                    if (head != 0 && size != 0)
                    {
                        headVal.EnsureChildren();
                        Traverse goNext = GetTraverse(item.NextPointer, headVal);
                        Traverse getValue = null;
                        if (item.ValueNode == "this")
                        {
                            getValue = (v) => v;
                        }
                        else if (headVal.FindChildByName(item.ValueNode) != null)
                        {
                            getValue = (v) => v.FindChildByName(item.ValueNode);
                        }
                        else
                        {
                            var value = GetExpression(item.ValueNode, headVal, visualizer.ScopedNames, intrinsics: visualizer.Intrinsics);
                            if (value != null && !value.Error)
                            {
                                getValue = (v) => GetExpression(item.ValueNode, v, visualizer.ScopedNames, intrinsics: visualizer.Intrinsics);
                            }
                        }
                        if (goNext == null || getValue == null)
                        {
                            continue;
                        }
                        uint startIndex = 0;
                        IVariableInformation parent = variable;
                        if (variable is PaginatedVisualizerWrapper visualizerWrapper)
                        {
                            startIndex = visualizerWrapper.StartIndex;
                            parent = visualizerWrapper.Parent;
                        }
                        else if (variable is SimpleWrapper simpleWrapper)
                        {
                            parent = simpleWrapper.Parent;
                        }
                        TraverseList(headVal, goNext, getValue, children, size, item.NoValueHeadPointer, parent, startIndex);
                    }
                }
                else if (i is IndexListItemsType)
                {
                    // example:
                    //     <IndexListItems>
                    //      <Size>_M_vector._M_index</Size>
                    //      <ValueNode>*(_M_vector._M_array[$i])</ValueNode>
                    //    </IndexListItems>
                    IndexListItemsType item = (IndexListItemsType)i;
                    if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                    {
                        continue;
                    }
                    var sizes = item.Size;
                    uint size = 0;
                    if (sizes == null)
                    {
                        continue;
                    }
                    foreach (var s in sizes)
                    {
                        if (string.IsNullOrWhiteSpace(s.Value))
                            continue;
                        if (EvalCondition(s.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                        {
                            string val = GetExpressionValue(s.Value, variable, visualizer.ScopedNames, visualizer.Intrinsics);
                            size = MICore.Debugger.ParseUint(val);
                            break;
                        }
                    }
                    var values = item.ValueNode;
                    if (values == null)
                    {
                        continue;
                    }
                    foreach (var v in values)
                    {
                        if (string.IsNullOrWhiteSpace(v.Value))
                            continue;
                        if (EvalCondition(v.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                        {
                            string processedExpr = ReplaceNamesInExpression(v.Value, variable, visualizer.ScopedNames, visualizer.Intrinsics);
                            Dictionary<string, string> indexDic = new Dictionary<string, string>();
                            uint currentIndex = 0;
                            if (variable is PaginatedVisualizerWrapper pvwVariable)
                            {
                                currentIndex = pvwVariable.StartIndex;
                            }
                            uint maxIndex = currentIndex + MAX_EXPAND > size ? size : currentIndex + MAX_EXPAND;
                            for (uint index = currentIndex; index < maxIndex; ++index) // limit expansion to first 50 elements
                            {
                                indexDic["$i"] = index.ToString(CultureInfo.InvariantCulture);
                                string finalExpr = ReplaceNamesInExpression(processedExpr, null, indexDic);
                                IVariableInformation expressionVariable = new VariableInformation(finalExpr, variable, _process.Engine, "[" + indexDic["$i"] + "]");
                                expressionVariable.SyncEval();
                                children.Add(expressionVariable);
                            }

                            currentIndex += MAX_EXPAND;
                            if (size > currentIndex)
                            {
                                IVariableInformation moreVariable = new PaginatedVisualizerWrapper(ResourceStrings.MoreView, _process.Engine, variable, visualizer, isVisualizerView: true, currentIndex);
                                children.Add(moreVariable);
                            }

                            break;
                        }
                    }
                }
                else if (i is ExpandedItemType)
                {
                    ExpandedItemType item = (ExpandedItemType)i;
                    // example:
                    // <Type Name="std::auto_ptr&lt;*&gt;">
                    //   <DisplayString>auto_ptr {*_Myptr}</DisplayString>
                    //   <Expand>
                    //     <ExpandedItem>_Myptr</ExpandedItem>
                    //   </Expand>
                    // </Type>

                    // IncludeView/ExcludeView: skip this ExpandedItem if the current view doesn't match.
                    if (!IsIncludeViewMatch(item.IncludeView, currentView))
                        continue;
                    if (IsExcludeViewMatch(item.ExcludeView, currentView))
                        continue;

                    if (item.Condition != null)
                    {
                        if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                        {
                            continue;
                        }
                    }
                    if (String.IsNullOrWhiteSpace(item.Value))
                    {
                        continue;
                    }

                    // A view() specifier on the ExpandedItem expression (e.g. "inner(),view(myview)")
                    // means: expand the result but show its children in the named view.
                    // Strip the specifier before evaluating the expression, then pass the
                    // view name into the recursive Expand call so that IncludeView guards
                    // on the target's Expand elements (including CustomListItems) match.
                    string rawExpr = item.Value.Trim();
                    string spec = ExtractFormatSpecifier(rawExpr);
                    string viewName = ExtractViewName(spec);
                    string exprToEval = viewName != null ? StripFormatSpecifier(rawExpr) : rawExpr;
                    string childView = viewName ?? currentView;

                    var expand = GetExpression(exprToEval, variable, visualizer.ScopedNames, intrinsics: visualizer.Intrinsics);
                    var eChildren = Expand(expand, childView);
                    if (eChildren != null)
                    {
                        children.AddRange(eChildren);
                    }
                }
                else if (i is CustomListItemsType)
                {
                    CustomListItemsType customList = (CustomListItemsType)i;
                    if (!IsIncludeViewMatch(customList.IncludeView, currentView)) continue;
                    if (IsExcludeViewMatch(customList.ExcludeView, currentView)) continue;
                    if (!EvalCondition(customList.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics)) continue;
                    if (customList.Loop == null || customList.Loop.Length == 0) continue;

                    // Build the natvis local-variable table from <Variable Name="x" InitialValue="expr"/> elements.
                    // Each entry maps the declared name to its current expression string; expressions are
                    // substituted in-place whenever the name appears in subsequent loop-body expressions.
                    var localVars = new Dictionary<string, string>(StringComparer.Ordinal);
                    if (customList.Items != null)
                    {
                        foreach (var v in customList.Items)
                        {
                            if (string.IsNullOrEmpty(v.Name)) continue;
                            string initVal = v.InitialValue ?? "0";
                            // Resolve field names, template parameters and intrinsics in the initial value.
                            localVars[v.Name] = ReplaceNamesInExpression(initVal, variable, visualizer.ScopedNames, visualizer.Intrinsics);
                        }
                    }

                    // Optional <Size> element provides an upper bound for children (and drives pagination).
                    uint totalSize = uint.MaxValue;
                    if (customList.Items1 != null)
                    {
                        foreach (var sz in customList.Items1)
                        {
                            if (!EvalCondition(sz.Condition, variable, visualizer.ScopedNames, visualizer.Intrinsics)) continue;
                            try
                            {
                                string szExpr = SubstituteLocalVars(sz.Value?.Trim() ?? "0", localVars);
                                string szVal = GetExpressionValue(szExpr, variable, visualizer.ScopedNames, visualizer.Intrinsics);
                                totalSize = MICore.Debugger.ParseUint(szVal, throwOnError: false);
                            }
                            catch (Exception) { /* leave totalSize as MaxValue so Break drives termination */ }
                            break;
                        }
                    }

                    uint startIndex = 0;
                    if (variable is PaginatedVisualizerWrapper pvwCLI)
                        startIndex = pvwCLI.StartIndex;

                    var ctx = new CustomListLoopContext(startIndex, totalSize);

                    foreach (var loop in customList.Loop)
                    {
                        if (ctx.Done) break;
                        DriveLoop(loop, ctx, variable, visualizer, localVars, children);
                    }
                }
            }
            if (!(variable is VisualizerWrapper) && !expandType.HideRawView) // don't stack wrappers, and respect HideRawView
            {
                // add the [Raw View] field
                IVariableInformation rawView = new VisualizerWrapper(ResourceStrings.RawView, _process.Engine, variable, visualizer, isVisualizerView: false);
                children.Add(rawView);
            }
            return children.ToArray();
        }

        private Traverse GetTraverse(string direction, IVariableInformation node)
        {
            Traverse go;
            var val = node.FindChildByName(direction);
            if (val == null)
            {
                return null;
            }
            if (val.TypeName == node.TypeName)
            {
                go = (v) => v.FindChildByName(direction);
            }
            else
            {
                go = (v) =>
                {
                    ulong addr = MICore.Debugger.ParseAddr(v.Value);
                    if (addr != 0)
                    {
                        var next = v.FindChildByName(direction);
                        next = new VariableInformation("(" + v.TypeName + ")" + next.Value, next, _process.Engine, "");
                        next.SyncEval();
                        return next;
                    }
                    return null;
                };
            }
            return go;
        }

        /// <summary>
        /// Traverse tree based on specified startIndex.
        /// Then add wrappers for Natvis tree visualizations.
        /// </summary>
        /// <param name="root">Root of the tree</param>
        /// <param name="goLeft">Traverse callback to retrieve left child of root</param>
        /// <param name="goRight">Traverse callback to retrieve right child of root</param>
        /// <param name="getValue">Callback to retrieve value of root</param>
        /// <param name="content">List of variables to display given current variable</param>
        /// <param name="size">Number of nodes in tree</param>
        /// <param name="variable">Tree to traverse if size <= 50. Otherwise, expandable continue wrapper.</param>
        /// <param name="parent">The tree to traverse</param>
        /// <param name="startIndex">Index to start traversing from</param>
        /// <returns></returns>
        private void TraverseTree(IVariableInformation root, Traverse goLeft, Traverse goRight, Traverse getValue, List<IVariableInformation> content, uint size, IVariableInformation variable, IVariableInformation parent, uint startIndex)
        {
            uint i = startIndex;
            var nodes = new Stack<Node>();
            if (variable is TreeContinueWrapper tcwVariable)
            {
                nodes = tcwVariable.Nodes;
            }
            else
            {
                nodes.Push(new Node(root));
            }

            uint maxIndex = i + MAX_EXPAND > size ? size : i + MAX_EXPAND;
            while (nodes.Count > 0 && i < maxIndex)
            {
                switch (nodes.Peek().State)
                {
                    case Node.ScanState.left:
                        nodes.Peek().State = Node.ScanState.value;
                        var leftVal = goLeft(nodes.Peek().Content);
                        if (leftVal != null)
                        {
                            ulong left = MICore.Debugger.ParseAddr(leftVal.Value);
                            if (left != 0)
                            {
                                nodes.Push(new Node(leftVal));
                            }
                        }
                        break;
                    case Node.ScanState.value:
                        nodes.Peek().State = Node.ScanState.right;
                        IVariableInformation value = getValue(nodes.Peek().Content);
                        if (value != null)
                        {
                            content.Add(new SimpleWrapper("[" + i.ToString(CultureInfo.InvariantCulture) + "]", _process.Engine, value));
                            i++;
                        }
                        break;
                    case Node.ScanState.right:
                        Node n = nodes.Pop();
                        var rightVal = goRight(n.Content);
                        if (rightVal != null)
                        {
                            ulong right = MICore.Debugger.ParseAddr(rightVal.Value);
                            if (right != 0)
                            {
                                nodes.Push(new Node(rightVal));
                            }
                        }
                        break;
                }
            }
            if (size > i)
            {
                IVariableInformation tcw = new TreeContinueWrapper(ResourceStrings.MoreView, _process.Engine, parent, FindType(parent), isVisualizerView: true, nodes.Peek(), nodes, i);
                content.Add(tcw);
            }
        }

        private void TraverseList(IVariableInformation root, Traverse goNext, Traverse getValue, List<IVariableInformation> content, uint size, bool noValueInRoot, IVariableInformation parent, uint startIndex)
        {
            uint i = startIndex;
            IVariableInformation node = root;
            ulong rootAddr = MICore.Debugger.ParseAddr(node.Value);
            ulong nextAddr = rootAddr;

            uint maxIndex = i + MAX_EXPAND > size ? size : i + MAX_EXPAND;
            while (node != null && nextAddr != 0 && i < maxIndex)
            {
                if (!noValueInRoot || nextAddr != rootAddr)
                {
                    IVariableInformation value = getValue(node);
                    if (value != null)
                    {
                        content.Add(new SimpleWrapper("[" + i.ToString(CultureInfo.InvariantCulture) + "]", _process.Engine, value));
                        i++;
                    }
                }
                if (i < maxIndex)
                {
                    node = goNext(node);
                }
                nextAddr = MICore.Debugger.ParseAddr(node.Value);
                if (nextAddr == rootAddr)
                {
                    // circular link, exit the loop
                    break;
                }
            }
            if (size > i)
            {
                IVariableInformation llcw = new LinkedListContinueWrapper(ResourceStrings.MoreView, _process.Engine, parent, FindType(parent), isVisualizerView: true, goNext(node), i);
                content.Add(llcw);
            }
        }

        private static string BaseName(string type)
        {
            type = type.TrimEnd();
            if (type[type.Length - 1] == '*')
            {
                type = type.Substring(0, type.Length - 1);
            }
            return type;
        }

        private bool EvalCondition(string condition, IVariableInformation variable, IDictionary<string, string> scopedNames, IDictionary<string, IntrinsicType> intrinsics = null)
        {
            bool res = true;
            if (!String.IsNullOrWhiteSpace(condition))
            {
                try
                {
                    string exprValue = GetExpressionValue(condition, variable, scopedNames, intrinsics);

                    bool exprBool = false;
                    int exprInt = 0;
                    res = !String.IsNullOrEmpty(exprValue) &&
                        ((bool.TryParse(exprValue, out exprBool) && exprBool) || (int.TryParse(exprValue, out exprInt) && exprInt > 0));
                }
                catch (MICore.MIException e)
                {
                    // Expected failure path: the debugger rejected the expression
                    // (e.g. expression too long, unknown symbol).
                    // Treat as false so the next DisplayString is tried as a fallback.
                    _process.Logger.NatvisLogger?.WriteLine(LogLevel.Verbose, "EvalCondition failed: " + e.Message);
                    res = false;
                }
                catch (Exception e)
                {
                    // Unexpected failure (e.g. NullReferenceException in the evaluation path).
                    // Still return false to avoid surfacing natvis errors as debug session failures,
                    // but log at Warning so unexpected exceptions are not silently swallowed.
                    _process.Logger.NatvisLogger?.WriteLine(LogLevel.Warning, "EvalCondition unexpected exception: " + e.Message);
                    res = false;
                }
            }
            return res;
        }

        private IVariableInformation FindBaseClass(IVariableInformation variable)
        {
            variable.EnsureChildren();
            if (variable.Children != null)
            {
                return Array.Find(variable.Children, (c) => c.VariableNodeType == VariableInformation.NodeType.BaseClass);
            }
            return null;
        }

        private VisualizerInfo Scan(TypeName name, IVariableInformation variable)
        {
            int aliasChain = 0;
        tryAgain:
            foreach (var autoVis in _typeVisualizers)
            {
                var visualizer = FindBestMatch(autoVis.Visualizers, name, v => v.ParsedName);
                if (visualizer != null)
                {
                    _vizCache[variable.TypeName] = new VisualizerInfo(visualizer.Visualizer, name);
                    return _vizCache[variable.TypeName];
                }
            }
            // failed to find a visualizer for the type, try looking for a typedef
            foreach (var autoVis in _typeVisualizers)
            {
                var alias = FindBestMatch(autoVis.Aliases, name, a => a.ParsedName);
                if (alias != null)
                {
                    // add the template parameter macro values
                    var scopedNames = new Dictionary<string, string>();
                    int t = 1;
                    for (int i = 0; i < name.Qualifiers.Count; ++i)
                    {
                        for (int j = 0; j < name.Qualifiers[i].Args.Count; ++j)
                        {
                            scopedNames["$T" + t.ToString(CultureInfo.InvariantCulture)] = name.Qualifiers[i].Args[j].FullyQualifiedName;
                            t++;
                        }
                    }

                    string newName = ReplaceNamesInExpression(alias.Alias.Value, null, scopedNames);
                    name = TypeName.Parse(newName, _process.Logger.NatvisLogger);
                    aliasChain++;
                    if (aliasChain > MAX_ALIAS_CHAIN)
                    {
                        break;
                    }
                    goto tryAgain;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the best matching candidate from a list based on type name matching.
        /// </summary>
        /// <typeparam name="T">Either TypeInfo or AliasInfo</typeparam>
        /// <param name="candidates">The list of candidate objects to consider for a match.</param>
        /// <param name="name">The type name to match against the candidate patterns.</param>
        /// <param name="getParsedName">A function that returns the parsed <see cref="TypeName"/> for a given candidate.</param>
        /// <returns>The best matching candidate, or <c>null</c> if no candidate matches.</returns>
        internal static T FindBestMatch<T>(List<T> candidates, TypeName name, Func<T, TypeName> getParsedName) where T : class
        {
            T best = null;
            int bestArgCount = 0;
            int bestConcreteCount = 0;
            foreach (var candidate in candidates)
            {
                TypeName parsedName = getParsedName(candidate);
                if (parsedName.Match(name)) // TODO: match on View, version, etc
                {
                    int concreteCount = 0;
                    foreach (var arg in parsedName.Args)
                    {
                        if (!arg.IsWildcard)
                        {
                            concreteCount++;
                        }
                    }

                    if (best == null
                        || parsedName.Args.Count > bestArgCount
                        || (parsedName.Args.Count == bestArgCount && concreteCount > bestConcreteCount))
                    {
                        best = candidate;
                        bestArgCount = parsedName.Args.Count;
                        bestConcreteCount = concreteCount;
                    }
                }
            }
            return best;
        }

        private VisualizerInfo FindType(IVariableInformation variable)
        {
            if (variable is VisualizerWrapper)
            {
                return ((VisualizerWrapper)variable).Visualizer;
            }
            if (_vizCache.ContainsKey(variable.TypeName))
            {
                return _vizCache[variable.TypeName];
            }
            TypeName parsedName = TypeName.Parse(variable.TypeName, _process.Logger.NatvisLogger);
            IVariableInformation var = variable;
            while (parsedName != null)
            {
                var visualizer = Scan(parsedName, variable);
                if (visualizer == null && (parsedName.BaseName.EndsWith("*", StringComparison.Ordinal) || parsedName.BaseName.EndsWith("&", StringComparison.Ordinal)))
                {
                    parsedName.BaseName = parsedName.BaseName.Substring(0, parsedName.BaseName.Length - 1);
                    visualizer = Scan(parsedName, variable);
                }
                if (visualizer != null)
                {
                    return visualizer;
                }
                var = FindBaseClass(var);   // TODO: handle more than one base class?
                if (var == null)
                {
                    break;
                }
                parsedName = TypeName.Parse(var.TypeName, _process.Logger.NatvisLogger);
            }
            return null;
        }

        private string FormatValue(string format, IVariableInformation variable, IDictionary<string, string> scopedNames, IDictionary<string, IntrinsicType> intrinsics = null)
        {
            if (String.IsNullOrWhiteSpace(format))
            {
                return String.Empty;
            }
            format = format.Trim();
            StringBuilder value = new StringBuilder();
            for (int i = 0; i < format.Length; ++i)
            {
                if (format[i] == '{')
                {
                    if (i + 1 < format.Length && format[i + 1] == '{')
                    {
                        value.Append('{');
                        i++;
                        continue;
                    }
                    // start of expression
                    Match m = s_expression.Match(format.Substring(i));
                    if (m.Success)
                    {
                        // Trim whitespace (including newlines from multi-line XML blocks) so that
                        // the expression never starts with \n, which would break LLDB MI's line-based protocol.
                        string rawExpr = format.Substring(i + 1, m.Length - 2).Trim();
                        string spec = ExtractFormatSpecifier(rawExpr);
                        string exprValue;
                        string viewName = ExtractViewName(spec);
                        if (viewName != null)
                        {
                            // {expr,view(name)} -- format expr using the named view's DisplayString.
                            // Any other specifiers combined with view() (e.g. "na", "sub", "sb") are
                            // intentionally ignored: specifiers like sub/sb exist to post-process raw
                            // debugger output (stripping address prefixes and quotes), but view() already
                            // produces fully-formatted text via FormatDisplayString — applying those
                            // post-processors on top would corrupt the result.
                            string strippedExpr = StripFormatSpecifier(rawExpr);
                            exprValue = GetExpressionValue(strippedExpr, variable, scopedNames, intrinsics, viewName);
                        }
                        else
                        {
                            exprValue = GetExpressionValue(rawExpr, variable, scopedNames, intrinsics);
                            if (spec == "sub" || spec == "su")
                                exprValue = CleanUtf16StringValue(exprValue);
                            else if (spec == "sb")
                                exprValue = CleanAsciiStringValue(exprValue);
                        }
                        value.Append(exprValue);
                        i += m.Length - 1;
                    }
                }
                else if (format[i] == '}')
                {
                    if (i + 1 < format.Length && format[i + 1] == '}')
                    {
                        value.Append('}');
                        i++;
                        continue;
                    }
                    // error, unmatched closing brace
                    return variable.Value;  // TODO: return an error indication
                }
                else
                {
                    value.Append(format[i]);
                }
            }
            return value.ToString();
        }

        private delegate string Substitute(Match name);

        private string ProcessNamesInString(string expression, Substitute[] processors)
        {
            StringBuilder result = new StringBuilder();
            int pos = 0;
            do
            {
                Match m = s_variableName.Match(expression, pos);
                if (!m.Success)
                {
                    break;    // failed to match a name
                }
                result.Append(expression.Substring(pos, m.Index - pos));
                pos = m.Index;

                // Check if this identifier is preceded by '->', '.', or '::', indicating it is
                // a member access or scope-qualified name rather than a root-level variable reference.
                // In that case, skip substitution and emit the name as-is.
                bool isMemberAccess = IsPrecededByMemberAccessOperator(expression, m.Index);

                bool found = false;
                if (!isMemberAccess)
                {
                    foreach (var p in processors)
                    {
                        string repl = p(m);
                        if (repl != null)
                        {
                            result.Append(repl);
                            found = true;
                            break;  // found a substitute
                        }
                    }
                }
                if (!found)
                {
                    result.Append(m.Value); // no name replacement to perform
                }
                pos = m.Index + m.Length;
                Match sub = s_subfieldNameHere.Match(expression, pos);  // span the subfields
                if (sub.Success)
                {
                    result.Append(expression.Substring(pos, sub.Length));
                    pos = pos + sub.Length;
                }
            } while (pos < expression.Length);
            if (pos < expression.Length)
            {
                result.Append(expression.Substring(pos, expression.Length - pos));
            }
            return result.ToString();
        }

        /// <summary>
        /// Returns true if the character(s) immediately before <paramref name="index"/>
        /// form a member-access or scope-resolution operator:
        ///   '->'  (pointer member access)
        ///   '.'   (direct member access)
        ///   '::'  (C++ scope resolution)
        /// Identifiers that follow these operators are part of a sub-expression
        /// and must not be replaced with a root-level child lookup.
        /// </summary>
        /// <param name="expression">The full expression string in which the identifier is located.</param>
        /// <param name="index">The zero-based index in <paramref name="expression"/> where the identifier starts.</param>
        /// <returns>
        /// <c>true</c> if the identifier at <paramref name="index"/> is immediately preceded (optionally via whitespace)
        /// by a member-access or scope-resolution operator (<c>.</c>, <c>-&gt;</c>, or <c>::</c>); otherwise, <c>false</c>.
        /// </returns>
        internal static bool IsPrecededByMemberAccessOperator(string expression, int index)
        {
            // Validate index bounds
            if (string.IsNullOrEmpty(expression) || index < 0 || index > expression.Length)
            {
                return false;
            }

            // Skip any whitespace between the operator and the identifier
            int i = index - 1;
            while (i >= 0 && char.IsWhiteSpace(expression[i]))
            {
                i--;
            }

            if (i >= 0 && expression[i] == '.')
            {
                return true;    // preceded by '.'
            }

            if (i >= 1)
            {
                char prev1 = expression[i];
                char prev2 = expression[i - 1];

                if (prev2 == '-' && prev1 == '>')
                {
                    return true;    // preceded by '->'
                }

                if (prev2 == ':' && prev1 == ':')
                {
                    return true;    // preceded by '::'
                }
            }

            return false;
        }

        /// <summary>
        /// Find the index of the closing parenthesis that matches the opening paren at <paramref name="openPos"/>.
        /// Returns -1 if not found.
        /// </summary>
        internal static int FindMatchingParen(string s, int openPos)
        {
            int depth = 0;
            for (int i = openPos; i < s.Length; i++)
            {
                if (s[i] == '(') depth++;
                else if (s[i] == ')')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Split a comma-separated argument list (the text inside the parentheses)
        /// at depth-zero commas only, so nested calls like f(a,b) are kept intact.
        /// Only parentheses and square brackets are treated as nesting — angle brackets
        /// are intentionally excluded because '&gt;' is also a comparison operator and
        /// NatVis intrinsic arguments are never C++ template types.
        /// </summary>
        internal static List<string> SplitArguments(string argsText)
        {
            var result = new List<string>();
            int depth = 0;
            int start = 0;
            for (int i = 0; i < argsText.Length; i++)
            {
                char c = argsText[i];
                if (c == '(' || c == '[') depth++;
                else if (c == ')' || c == ']') depth--;
                else if (c == ',' && depth == 0)
                {
                    result.Add(argsText.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            string last = argsText.Substring(start).Trim();
            if (last.Length > 0 || result.Count > 0)
                result.Add(last);
            return result;
        }

        /// <summary>
        /// Returns the index of the last top-level comma in <paramref name="expression"/>,
        /// i.e. a comma not nested inside any parentheses or square brackets.
        /// Returns -1 when no such comma exists.
        /// </summary>
        private static int FindLastTopLevelComma(string expression)
        {
            int depth = 0;
            int lastTopLevelComma = -1;
            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];
                if (c == '(' || c == '[') depth++;
                else if (c == ')' || c == ']') depth--;
                else if (c == ',' && depth == 0)
                    lastTopLevelComma = i;
            }
            return lastTopLevelComma;
        }

        /// <summary>
        /// Strips a NatVis format specifier (e.g. ",sub", ",d", ",view(name)na") from the end of
        /// an expression, returning the bare expression.  The specifier boundary is the last
        /// top-level comma (not nested inside any parentheses or square brackets).
        /// </summary>
        internal static string StripFormatSpecifier(string expression)
        {
            int commaPos = FindLastTopLevelComma(expression);
            return commaPos >= 0
                ? expression.Substring(0, commaPos).TrimEnd()
                : expression;
        }

        /// <summary>
        /// Returns true when <paramref name="currentView"/> is listed in the semicolon-separated
        /// IncludeView attribute, i.e. the DisplayString should only be shown in one of those views.
        /// An empty or null includeView means "show in all views" (returns true for any currentView).
        /// Both IncludeView and ExcludeView are defined as semicolon-delimited lists in the natvis XSD.
        /// </summary>
        internal static bool IsIncludeViewMatch(string includeView, string currentView)
        {
            if (string.IsNullOrEmpty(includeView)) return true;
            if (currentView == null) return false;
            return includeView.Split(';').Any(v => string.Equals(v.Trim(), currentView, StringComparison.Ordinal));
        }

        /// <summary>
        /// Returns true when <paramref name="currentView"/> is listed in the semicolon-separated
        /// ExcludeView attribute, i.e. the DisplayString should be skipped in this view.
        /// An empty/null excludeView or a null currentView never excludes.
        /// </summary>
        internal static bool IsExcludeViewMatch(string excludeView, string currentView)
        {
            if (string.IsNullOrEmpty(excludeView) || currentView == null) return false;
            return excludeView.Split(';').Any(v => string.Equals(v.Trim(), currentView, StringComparison.Ordinal));
        }

        /// <summary>
        /// If <paramref name="spec"/> is a view specifier of the form "view(name)" or
        /// "view(name)na", returns the view name.  Otherwise returns null.
        /// </summary>
        internal static string ExtractViewName(string spec)
        {
            if (spec == null) return null;
            if (!spec.StartsWith("view(", StringComparison.Ordinal)) return null;
            int closeParen = spec.IndexOf(')');
            if (closeParen < 0) return null;
            string name = spec.Substring(5, closeParen - 5);
            // view() with an empty name is not a valid specifier; treat as absent.
            return name.Length > 0 ? name : null;
        }

        /// <summary>
        /// Returns the format specifier from a NatVis expression (the part after the last
        /// top-level comma), normalized the same way as
        /// <see cref="VariableInformation.ProcessFormatSpecifiers"/>: modifiers "nvo", "na",
        /// "nr", "nd" are stripped before returning.  Returns null when no specifier is present.
        /// </summary>
        internal static string ExtractFormatSpecifier(string expression)
        {
            int commaPos = FindLastTopLevelComma(expression);
            if (commaPos < 0) return null;
            return expression.Substring(commaPos + 1).Trim()
                .Replace("nvo", "").Replace("na", "").Replace("nr", "").Replace("nd", "");
        }

        /// <summary>
        /// Cleans up the raw value that GDB/LLDB returns for a <c>const char16_t*</c>
        /// expression (i.e. one evaluated with the <c>,sub</c> / <c>,su</c> format specifier).
        /// GDB and LLDB both prefix the string with the pointer address, e.g.
        ///   <c>0x00007fff5fbff6c0 u"Hello"</c>
        /// This method strips the address and the surrounding <c>u"…"</c> quotes so that
        /// the NatVis DisplayString shows just the string content.
        /// </summary>
        internal static string CleanUtf16StringValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            // Strip leading "0x<hex> " address prefix emitted by GDB/LLDB.
            value = s_addressPrefix.Replace(value, "");
            // Strip surrounding u"..." or U"..." quotes.
            if (value.Length >= 3 &&
                (value.StartsWith("u\"", StringComparison.Ordinal) || value.StartsWith("U\"", StringComparison.Ordinal)))
            {
                value = value.EndsWith("\"", StringComparison.Ordinal)
                    ? value.Substring(2, value.Length - 3)
                    : value.Substring(2);
            }
            return value;
        }

        /// <summary>
        /// Cleans up the raw value that GDB/LLDB returns for a <c>char*</c> expression
        /// (i.e. one evaluated with the <c>,sb</c> format specifier).
        /// GDB and LLDB prefix the string with the pointer address, e.g.
        ///   <c>0x00007fff5fbff6c0 "Hello"</c>
        /// This method strips the address and the surrounding <c>"…"</c> quotes so that
        /// the NatVis DisplayString shows just the string content (matching VS behaviour,
        /// where <c>{ptr,sb}</c> evaluates to bare text without quotes).
        /// </summary>
        internal static string CleanAsciiStringValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            // Strip leading "0x<hex> " address prefix emitted by GDB/LLDB.
            value = s_addressPrefix.Replace(value, "");
            // Strip surrounding "..." quotes.
            if (value.Length >= 2 && value.StartsWith("\"", StringComparison.Ordinal))
            {
                value = value.EndsWith("\"", StringComparison.Ordinal)
                    ? value.Substring(1, value.Length - 2)
                    : value.Substring(1);
            }
            return value;
        }

        /// <summary>
        /// Substitute named parameters in an intrinsic expression with the supplied argument
        /// values.  Each parameter name is replaced as a whole word so that e.g. "val" inside
        /// "interval" is not touched.
        /// </summary>
        internal static string SubstituteIntrinsicParameters(string body, IntrinsicParameterType[] parameters, List<string> args)
        {
            if (parameters == null || parameters.Length == 0)
                return body;

            string result = body;
            for (int i = 0; i < parameters.Length && i < args.Count; i++)
            {
                string paramName = parameters[i].Name;
                if (string.IsNullOrEmpty(paramName)) continue;
                // whole-word replacement
                result = Regex.Replace(result, @"\b" + Regex.Escape(paramName) + @"\b", args[i]);
            }
            return result;
        }

        /// <summary>
        /// Expand intrinsic calls in <paramref name="expression"/> into their C++ equivalents.
        /// For example, given an intrinsic  day() = "jd - 2440588"  the call  day() + 1
        /// becomes  (jd - 2440588) + 1.
        /// Recurses up to <paramref name="maxDepth"/> times to handle chained calls.
        /// </summary>
        internal static string ResolveIntrinsicCalls(string expression, IDictionary<string, IntrinsicType> intrinsics, int maxDepth = 20)
        {
            if (string.IsNullOrEmpty(expression) || intrinsics == null || intrinsics.Count == 0 || maxDepth <= 0)
                return expression;

            bool anyReplaced = false;
            string result = expression;

            // We scan left-to-right and build the output string incrementally.
            // Using a loop rather than Regex.Replace because we need to consume the
            // matched argument list (which the regex does not capture fully).
            // s_intrinsicCallPattern matches a word immediately followed by '(';
            // \b on the right side is intentionally absent — the '(' is the boundary.
            int pos = 0;
            var sb = new StringBuilder();

            while (pos < result.Length)
            {
                Match m = s_intrinsicCallPattern.Match(result, pos);
                if (!m.Success) break;

                string name = m.Groups[1].Value;

                // Skip if the identifier is a member or scope access (.name, ->name, ::name).
                // \b matches after '.' / '>' / ':' because those are non-word characters, so
                // we must guard here to avoid re-expanding e.g. _q_value.value() when "value"
                // is also an intrinsic name.
                if (IsPrecededByMemberAccessOperator(result, m.Index))
                {
                    sb.Append(result, pos, m.Index - pos + name.Length);
                    pos = m.Index + name.Length;
                    continue;
                }

                if (!intrinsics.TryGetValue(name, out IntrinsicType intrinsic))
                {
                    // Not one of our intrinsics — skip past the identifier and keep going
                    sb.Append(result, pos, m.Index - pos + name.Length);
                    pos = m.Index + name.Length;
                    continue;
                }

                // Found an intrinsic call.  Locate the matching close paren.
                int openParen = m.Index + m.Length - 1; // position of '('
                int closeParen = FindMatchingParen(result, openParen);
                if (closeParen < 0)
                {
                    // Malformed — leave as-is
                    sb.Append(result, pos, m.Index - pos + name.Length);
                    pos = m.Index + name.Length;
                    continue;
                }

                // Append everything before the call
                sb.Append(result, pos, m.Index - pos);

                // Extract and split arguments
                string argsText = result.Substring(openParen + 1, closeParen - openParen - 1);
                List<string> args = string.IsNullOrWhiteSpace(argsText)
                    ? new List<string>()
                    : SplitArguments(argsText);

                // Expand: substitute parameters into the intrinsic expression body
                string body = intrinsic.Expression ?? string.Empty;
                body = SubstituteIntrinsicParameters(body, intrinsic.Parameter, args);

                // Wrap in parens to preserve operator precedence
                sb.Append('(');
                sb.Append(body);
                sb.Append(')');

                pos = closeParen + 1;
                anyReplaced = true;
            }

            // Append any trailing text after the last match
            sb.Append(result, pos, result.Length - pos);
            result = sb.ToString();

            // Recurse if we expanded anything (handles chained intrinsics)
            if (anyReplaced)
                result = ResolveIntrinsicCalls(result, intrinsics, maxDepth - 1);

            return result;
        }

        private string ReplaceNamesInExpression(string expression, IVariableInformation variable, IDictionary<string, string> scopedNames, IDictionary<string, IntrinsicType> intrinsics = null)
        {
            // Expand intrinsic calls FIRST so that dll!-qualified type names that appear
            // inside intrinsic bodies (e.g. "(Foo.dll!MyType*)ptr") are also stripped
            // in the next step.
            expression = ResolveIntrinsicCalls(expression, intrinsics);

            // Strip Windows dll!-qualified type prefixes (e.g. Qt6Cored.dll!)
            // for GDB/LLDB compatibility — meaningless outside Windows.
            // Must run AFTER intrinsic expansion so intrinsic-body dll! references are caught.
            expression = s_moduleQualifiedPrefix.Replace(expression, "");

            return ProcessNamesInString(expression, new Substitute[] {
                (m)=>
                    {
                        if (variable == null)
                            return null;

                        // replace explicit this references
                        if (m.Value == "this")
                            return (variable.TypeName.EndsWith("*", StringComparison.Ordinal) ? "(" : "(&") + variable.FullName() + ")";

                        // finds children of this structure and sub's in the fullname of the child
                        IVariableInformation child = variable.FindChildByName(m.Value);
                        if (child != null)
                            return "(" + child.FullName() + ")";

                        return null;
                    },
                (m)=>
                    {   // replaces the '$Tx' with actual template parameter 
                        string res;
                        if (scopedNames != null && scopedNames.TryGetValue(m.Value, out res))
                        {
                            return res;
                        }
                        return null;
                    }});
        }

        /// <summary>
        /// Replace child field names in the expression with the childs full expression.
        /// Then evaluate the new expression.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="variable"></param>
        /// <returns></returns>
        private IVariableInformation GetExpression(string expression, IVariableInformation variable, IDictionary<string, string> scopedNames, string displayName = null, IDictionary<string, IntrinsicType> intrinsics = null)
        {
            string processedExpr = ReplaceNamesInExpression(expression, variable, scopedNames, intrinsics);
            IVariableInformation expressionVariable = new VariableInformation(processedExpr, variable, _process.Engine, displayName);
            expressionVariable.SyncEval();
            return expressionVariable;
        }

        private string GetExpressionValue(string expression, IVariableInformation variable, IDictionary<string, string> scopedNames, IDictionary<string, IntrinsicType> intrinsics = null, string view = null)
        {
            // Strip any format specifier (e.g. ",d", ",x") BEFORE name/intrinsic substitution.
            // If we don't do this, an identifier that happens to appear in the specifier — most
            // commonly the "d" in ",d" — will be matched by ProcessNamesInString and replaced
            // with the full expression for the child variable of that name (e.g. a member "d"),
            // turning  "1234,d"  into  "1234,(obj.d)"  which the debugger evaluates as a C
            // comma-operator expression returning the struct field instead of the integer.
            string spec = ExtractFormatSpecifier(expression);
            string exprNoSpec = spec != null ? StripFormatSpecifier(expression) : expression;

            string processedExpr = ReplaceNamesInExpression(exprNoSpec, variable, scopedNames, intrinsics);

            // Re-attach the format specifier so that VariableInformation.ProcessFormatSpecifiers
            // can apply the correct display format (decimal, hex, etc.) via -var-set-format.
            if (spec != null)
                processedExpr = processedExpr + "," + spec;

            IVariableInformation expressionVariable = new VariableInformation(processedExpr, variable, _process.Engine, null);
            expressionVariable.SyncEval();

            // Avoid recursive natvis formatting when expression is 'this' and no view is requested.
            // With a view, {this,view(name)} must go through FormatDisplayString to select the right
            // IncludeView DisplayString for the named view.
            if (expression.Trim() == "this" && view == null)
            {
                return expressionVariable.Value;
            }

            return FormatDisplayString(expressionVariable, view).value;
        }

        private string GetDisplayNameFromArrayIndex(uint arrayIndex, int rank, uint[] dimensions, bool isForward)
        {
            StringBuilder displayName = new StringBuilder();
            uint index = arrayIndex;

            int i = rank - 1;
            int inc = -1;
            int endLoop = -1;

            if (!isForward)
            {
                i = 0;
                inc = 1;
                endLoop = rank;
            }

            uint[] indices = new uint[rank];

            while (i != endLoop)
            {
                uint dimensionSize = dimensions[i];
                uint divResult = index / dimensionSize;
                uint modResult = index % dimensionSize;

                indices[i] = modResult;
                index = divResult;

                i += inc;
            }

            string format = _process.Engine.CurrentRadix() == 16 ? "0x{0:X}" : "{0:D}";
            displayName.AppendFormat(CultureInfo.InvariantCulture, format, indices[0]);
            for (i = 1; i < rank; i++)
            {
                displayName.Append(',');
                displayName.AppendFormat(CultureInfo.InvariantCulture, format, indices[i]);
            }

            return displayName.ToString();
        }

        // ---- CustomListItems execution helpers ----------------------------------

        /// <summary>
        /// Mutable state shared across one invocation of the CustomListItems loop engine.
        /// </summary>
        private sealed class CustomListLoopContext
        {
            /// <summary>Total children emitted across all iterations ($i counter).</summary>
            public uint GlobalIndex;
            /// <summary>Children added to the current page.</summary>
            public uint Emitted;
            /// <summary>Pagination start (0 for the first page).</summary>
            public readonly uint StartIndex;
            /// <summary>Maximum total children expected (from &lt;Size&gt;, or uint.MaxValue).</summary>
            public readonly uint TotalSize;
            /// <summary>Set to true when a &lt;Break&gt; fires or the page limit is reached.</summary>
            public bool Done;

            public CustomListLoopContext(uint startIndex, uint totalSize)
            {
                StartIndex = startIndex;
                TotalSize = totalSize;
            }
        }

        /// <summary>
        /// Drives a single &lt;Loop&gt; element: runs its body once per iteration until a &lt;Break&gt;
        /// fires (ctx.Done), the optional while-Condition becomes false, the size limit is reached,
        /// a pass makes no progress, or the iteration cap is hit. Shared by the top-level loop and
        /// nested &lt;Loop&gt; elements. Returns true if any pass made progress.
        /// </summary>
        private bool DriveLoop(
            LoopType loop,
            CustomListLoopContext ctx,
            IVariableInformation variable,
            VisualizerInfo visualizer,
            Dictionary<string, string> localVars,
            List<IVariableInformation> children)
        {
            bool progress = false;
            if (loop?.Items == null)
                return progress;

            // Cap iterations to cover fast-forwarding through StartIndex items plus one page of
            // MAX_EXPAND, with a hard ceiling of 10 000 guarding against malformed natvis where no
            // <Size>/<Break> bounds the loop.
            long maxIter = Math.Min((long)ctx.StartIndex + MAX_EXPAND + 1, 10000);
            for (long iter = 0; !ctx.Done && ctx.GlobalIndex < ctx.TotalSize && iter < maxIter; iter++)
            {
                // While-guard: stop if the loop's Condition evaluates to false.
                if (!string.IsNullOrEmpty(loop.Condition))
                {
                    string loopCond = SubstituteLocalVars(loop.Condition, localVars);
                    if (!EvalCondition(loopCond, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                        break;
                }
                bool passProgress = ExecuteCustomListBody(loop.Items, loop.ItemsElementName, ctx, variable, visualizer, localVars, children);
                if (!passProgress && !ctx.Done)
                    break; // no items emitted and no break — avoid an infinite loop
                progress = true;
            }
            return progress;
        }

        /// <summary>
        /// Executes one pass through a loop-body element sequence (Break / Item / Exec / If / Else).
        /// Returns true if at least one Item or nested body was processed, false if the pass produced
        /// no observable effect (used to detect infinite-loop conditions).
        /// </summary>
        private bool ExecuteCustomListBody(
            object[] body,
            ItemsChoiceType[] choices,
            CustomListLoopContext ctx,
            IVariableInformation variable,
            VisualizerInfo visualizer,
            Dictionary<string, string> localVars,
            List<IVariableInformation> children)
        {
            bool progress = false;

            // <If> and <Elseif> both deserialize to IfType; the parallel choice array records the
            // original element name so the two can be told apart (defaults to If if unavailable).
            ItemsChoiceType ChoiceAt(int i) =>
                (choices != null && i < choices.Length) ? choices[i] : ItemsChoiceType.If;

            for (int idx = 0; idx < body.Length && !ctx.Done; idx++)
            {
                var elem = body[idx];

                if (elem is BreakType br)
                {
                    // <Break Condition="expr"/>: stop the loop when the condition holds.
                    if (string.IsNullOrEmpty(br.Condition))
                    {
                        ctx.Done = true;
                        break;
                    }
                    string condExpr = SubstituteLocalVars(br.Condition, localVars);
                    if (EvalCondition(condExpr, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                        ctx.Done = true;
                }
                else if (elem is CustomListItemType li)
                {
                    // <Item Name="..." Condition="...">expr</Item>: emit a child variable.
                    if (li.Condition != null)
                    {
                        string condExpr = SubstituteLocalVars(li.Condition, localVars);
                        if (!EvalCondition(condExpr, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                            continue;
                    }

                    if (ctx.GlobalIndex >= ctx.StartIndex && ctx.Emitted < MAX_EXPAND)
                    {
                        string rawExpr = SubstituteLocalVars(li.Value?.Trim() ?? "", localVars);
                        string processedExpr = ReplaceNamesInExpression(rawExpr, variable, visualizer.ScopedNames, visualizer.Intrinsics);
                        string name = FormatCustomListItemName(li.Name, ctx.GlobalIndex, localVars);
                        var childVar = new VariableInformation(processedExpr, variable, _process.Engine, name);
                        childVar.SyncEval();
                        children.Add(childVar);
                        ctx.Emitted++;
                    }
                    ctx.GlobalIndex++;
                    progress = true;

                    // Check whether the page is now full.
                    if (ctx.Emitted >= MAX_EXPAND && ctx.GlobalIndex < ctx.TotalSize)
                    {
                        children.Add(new PaginatedVisualizerWrapper(
                            ResourceStrings.MoreView, _process.Engine, variable,
                            visualizer, isVisualizerView: true, ctx.StartIndex + MAX_EXPAND));
                        ctx.Done = true;
                    }
                }
                else if (elem is ExecType exec)
                {
                    // <Exec Condition="...">var = expr</Exec> (or "++var", or several
                    // comma-separated assignments): update one or more local variables.
                    if (exec.Condition != null)
                    {
                        string condExpr = SubstituteLocalVars(exec.Condition, localVars);
                        if (!EvalCondition(condExpr, variable, visualizer.ScopedNames, visualizer.Intrinsics))
                            continue;
                    }
                    // Pass the raw <Exec> text: ApplyExecToLocalVars detects the assigned variable
                    // name(s) and substitutes local vars on the right-hand side itself. A single
                    // <Exec> may update several variables, comma-separated (e.g. "++idx, ++statptr").
                    var updatedVars = ApplyExecToLocalVars(exec.Value?.Trim() ?? "", localVars, out List<string> unhandledExec);
                    foreach (string seg in unhandledExec)
                    {
                        // A segment we could not apply (unsupported form, or an undeclared
                        // left-hand side) would otherwise silently do nothing; log it so the
                        // omission is visible rather than producing a wrong/stuck traversal.
                        _process.Logger.NatvisLogger?.WriteLine(LogLevel.Warning, "CustomListItems <Exec> segment not applied (unsupported expression or undeclared variable): " + seg);
                    }
                    foreach (string updatedVar in updatedVars)
                    {
                        // Normalise each updated variable to prevent unbounded growth across
                        // iterations: after each i++ the expression would otherwise grow as
                        // "(((0)+1)+1)+1...". Evaluate it and store the scalar result so each
                        // iteration starts from a compact literal. Skip pointer values ("0x...")
                        // — those must remain as expressions, not substituted as address literals.
                        try
                        {
                            string normalized = GetExpressionValue(localVars[updatedVar], variable, visualizer.ScopedNames, visualizer.Intrinsics);
                            if (!string.IsNullOrEmpty(normalized) &&
                                !normalized.TrimStart().StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                            {
                                localVars[updatedVar] = normalized;
                            }
                        }
                        catch (Exception e)
                        {
                            // The normalization evaluates a variable we just updated. If that throws,
                            // the variable can't be evaluated -- and since the loop body and its
                            // conditions use the same variable, the iteration can't continue meaningfully.
                            // Log and stop the loop rather than emit error items or run to the cap.
                            _process.Logger.NatvisLogger?.WriteLine(LogLevel.Warning, "CustomListItems <Exec> normalization failed; stopping loop: " + e.Message);
                            ctx.Done = true;
                            break;
                        }
                    }
                    progress = true; // Exec advances loop state (e.g. iSpan++) even when no Item is emitted
                }
                else if (elem is IfType ifElem)
                {
                    // <If> optionally followed by any number of <Elseif>s and a final <Else>.
                    // Execute the first branch whose condition holds; consume all siblings.
                    // <Elseif> shares IfType with <If>, so they are distinguished via the choice
                    // array. A standalone <Elseif> (no preceding <If>) is malformed: warn and skip.
                    if (ChoiceAt(idx) == ItemsChoiceType.Elseif)
                    {
                        _process.Logger.NatvisLogger?.WriteLine(LogLevel.Warning, "CustomListItems: <Elseif> without a preceding <If>.");
                        continue;
                    }

                    string condExpr = SubstituteLocalVars(ifElem.Condition ?? "", localVars);
                    bool taken = !string.IsNullOrEmpty(condExpr) &&
                                 EvalCondition(condExpr, variable, visualizer.ScopedNames, visualizer.Intrinsics);

                    if (taken && ifElem.Items != null)
                        progress |= ExecuteCustomListBody(ifElem.Items, ifElem.ItemsElementName, ctx, variable, visualizer, localVars, children);

                    // Consume any immediately following <Elseif> elements (IfType with choice == Elseif).
                    while (idx + 1 < body.Length && body[idx + 1] is IfType elseIfElem && ChoiceAt(idx + 1) == ItemsChoiceType.Elseif)
                    {
                        idx++;
                        if (!taken)
                        {
                            string eiCond = SubstituteLocalVars(elseIfElem.Condition ?? "", localVars);
                            bool eiTaken = !string.IsNullOrEmpty(eiCond) &&
                                           EvalCondition(eiCond, variable, visualizer.ScopedNames, visualizer.Intrinsics);
                            if (eiTaken && elseIfElem.Items != null)
                            {
                                progress |= ExecuteCustomListBody(elseIfElem.Items, elseIfElem.ItemsElementName, ctx, variable, visualizer, localVars, children);
                                taken = true;
                            }
                        }
                    }

                    // Consume an immediately following <Else> element.
                    if (idx + 1 < body.Length && body[idx + 1] is ElseType elseElem)
                    {
                        idx++;
                        if (!taken && elseElem.Items != null)
                            progress |= ExecuteCustomListBody(elseElem.Items, elseElem.ItemsElementName, ctx, variable, visualizer, localVars, children);
                    }
                }
                else if (elem is LoopType nestedLoop)
                {
                    // Nested <Loop [Condition="..."]>: drive it exactly like the top-level loop.
                    progress |= DriveLoop(nestedLoop, ctx, variable, visualizer, localVars, children);
                }
                else
                {
                    // Unrecognised loop-body element. This should not occur for natvis that
                    // validates against the schema (a stray <Else> without a preceding <If>
                    // also lands here); log so unsupported/malformed elements are visible.
                    _process.Logger.NatvisLogger?.WriteLine(LogLevel.Warning, "CustomListItems loop body contains an unsupported element: " + elem?.GetType().Name);
                }
            }

            return progress;
        }

        /// <summary>
        /// Substitutes natvis local variable names in <paramref name="expression"/> with their
        /// current expression strings, using word-boundary matching to avoid partial replacements.
        /// Each substituted value is wrapped in parentheses to preserve operator precedence.
        /// </summary>
        internal static string SubstituteLocalVars(string expression, Dictionary<string, string> localVars)
        {
            if (string.IsNullOrEmpty(expression) || localVars == null || localVars.Count == 0)
                return expression;
            foreach (var kv in localVars)
            {
                expression = Regex.Replace(
                    expression,
                    @"\b" + Regex.Escape(kv.Key) + @"\b",
                    "(" + kv.Value + ")");
            }
            return expression;
        }

        /// <summary>
        /// Attempts to parse <paramref name="execExpr"/> as "varName = rhs" and, when the left-hand
        /// side is a declared natvis local variable, updates its entry to the substituted RHS.
        /// Expressions that do not match this pattern are silently ignored.
        /// </summary>
        /// <returns>
        /// The name of the local variable that was updated, or <c>null</c> if nothing changed.
        /// The caller can use this to normalise the stored expression (evaluate it and replace
        /// with the scalar result) so that repeated increments do not cause unbounded growth.
        /// </returns>
        internal static List<string> ApplyExecToLocalVars(string execExpr, Dictionary<string, string> localVars)
            => ApplyExecToLocalVars(execExpr, localVars, out _);

        internal static List<string> ApplyExecToLocalVars(string execExpr, Dictionary<string, string> localVars, out List<string> unhandled)
        {
            var updated = new List<string>();
            unhandled = new List<string>();
            if (string.IsNullOrEmpty(execExpr)) return updated;

            // A single <Exec> may update several variables, comma-separated, e.g.
            // "++idx, ++statptr" (common in the .natvis files shipped with VS). Split on
            // top-level commas (not those inside parentheses/brackets) and apply each in order.
            // Non-empty segments we cannot apply (unsupported form, or an undeclared left-hand
            // side) are collected so the caller can log a warning rather than drop them silently.
            foreach (string part in SplitTopLevelCommas(execExpr))
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;
                string varName = ApplySingleExec(part, localVars);
                if (varName != null)
                    updated.Add(varName);
                else
                    unhandled.Add(part.Trim());
            }
            return updated;
        }

        /// <summary>
        /// Applies one assignment from an &lt;Exec&gt; block to the local-variable table:
        /// "varName = rhs", or the "++"/"--" shorthand. Returns the updated variable name,
        /// or null when the segment is empty or its left-hand side is not a declared local.
        /// </summary>
        private static string ApplySingleExec(string execExpr, Dictionary<string, string> localVars)
        {
            if (string.IsNullOrWhiteSpace(execExpr)) return null;

            // Check for increment/decrement shorthand: ++i, i++, --i, i--
            var mIncr = s_execIncrDecr.Match(execExpr);
            if (mIncr.Success)
            {
                // prefix form: groups 1 (op) + 2 (varname); postfix form: groups 3 (varname) + 4 (op)
                string varName = mIncr.Groups[2].Success ? mIncr.Groups[2].Value : mIncr.Groups[3].Value;
                string op      = mIncr.Groups[1].Success ? mIncr.Groups[1].Value : mIncr.Groups[4].Value;
                if (localVars.ContainsKey(varName))
                {
                    localVars[varName] = SubstituteLocalVars(varName, localVars) + (op == "++" ? " + 1" : " - 1");
                    return varName;
                }
                return null;
            }

            // Check for simple assignment: varName = rhs
            var m = s_execAssignment.Match(execExpr);
            if (m.Success && localVars.ContainsKey(m.Groups[1].Value))
            {
                string varName = m.Groups[1].Value;
                string rhs = m.Groups[2].Value.Trim();
                localVars[varName] = SubstituteLocalVars(rhs, localVars);
                return varName;
            }
            return null;
        }

        /// <summary>
        /// Splits an expression on top-level commas — commas not nested inside parentheses
        /// or square brackets — so a multi-assignment &lt;Exec&gt; like "++idx, ++statptr" is
        /// separated while a comma inside a call such as "f(a, b)" is left intact.
        /// </summary>
        internal static IEnumerable<string> SplitTopLevelCommas(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                yield break;

            int depth = 0;
            int start = 0;
            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];
                if (c == '(' || c == '[')
                    depth++;
                else if (c == ')' || c == ']')
                {
                    if (depth > 0) depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    yield return expression.Substring(start, i - start);
                    start = i + 1;
                }
            }
            yield return expression.Substring(start);
        }

        /// <summary>
        /// Formats the display name for a CustomListItems child.  Replaces <c>{$i}</c> and
        /// bare <c>$i</c> with <paramref name="index"/>, then substitutes any local variable
        /// names.  Falls back to <c>[index]</c> when <paramref name="nameTemplate"/> is null.
        /// </summary>
        /// <param name="index">
        /// The condition-passing item counter (<c>ctx.GlobalIndex</c>), which starts at 0 and
        /// increments for every Item whose Condition passes, across all pages.  This matches
        /// the Visual Studio behaviour where <c>$i</c> is the absolute loop-item index, not a
        /// page-relative offset.
        /// </param>
        internal static string FormatCustomListItemName(string nameTemplate, uint index, Dictionary<string, string> localVars)
        {
            if (string.IsNullOrEmpty(nameTemplate))
                return "[" + index.ToString(CultureInfo.InvariantCulture) + "]";

            string indexStr = index.ToString(CultureInfo.InvariantCulture);
            // Replace the {$i} token first (complete braced form), then bare $i with a
            // word-boundary guard so that e.g. "$item" in a Name template is not corrupted.
            string name = s_dollarI.Replace(
                nameTemplate.Replace("{$i}", indexStr),
                indexStr);
            name = SubstituteLocalVars(name, localVars);

            // If {expr} tokens remain after substitution they would require evaluating against
            // the debugger, which is not supported here.  Fall back to [index] to avoid surfacing
            // a failed expression evaluation string as the child name.
            if (name.Contains('{'))
                return "[" + index.ToString(CultureInfo.InvariantCulture) + "]";

            return name;
        }

        // ---- End CustomListItems execution helpers ------------------------------

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _natvisSettingWatcher?.Dispose();
            _natvisSettingWatcher = null;
        }
    }
}
