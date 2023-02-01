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

        public VariableInformation FindChildByName(string name) => Parent.FindChildByName(name);
        public string EvalDependentExpression(string expr) => Parent.EvalDependentExpression(expr);
        public void AsyncEval(IDebugEventCallback2 pExprCallback) => Parent.AsyncEval(pExprCallback);
        public void SyncEval(enum_EVALFLAGS dwFlags, DAPEvalFlags dwDAPFlags) => Parent.SyncEval(dwFlags, dwDAPFlags);
        public virtual string FullName() => Name;
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
            }
        }

        private static Regex s_variableName = new Regex("[a-zA-Z$_][a-zA-Z$_0-9]*");
        private static Regex s_subfieldNameHere = new Regex(@"\G((\.|->)[a-zA-Z$_][a-zA-Z$_0-9]*)+");
        private static Regex s_expression = new Regex(@"^\{[^\}]*\}");
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

        public void Initialize(string fileName)
        {
            InitializeNatvisServices();
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

        /*
         * Handle multiple Natvis files
         */
        public void Initialize(List <string> fileNames)
        {
            InitializeNatvisServices();
            if (fileNames != null && fileNames.Count > 0)
            {
                foreach (var file in fileNames)
                {
                    Initialize(file);
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

        internal (string value, VisualizerId[] uiVisualizers) FormatDisplayString(IVariableInformation variable)
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
                                if (!EvalCondition(display.Condition, variable, visualizer.ScopedNames))
                                {
                                    continue;
                                }
                                return (FormatValue(display.Value, variable, visualizer.ScopedNames), visualizer.GetUIVisualizers());
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

        internal IVariableInformation[] Expand(IVariableInformation variable)
        {
            try
            {
                variable.EnsureChildren();
                if (variable.IsVisualized
                        || ((ShowDisplayStrings == DisplayStringsState.On) && !(variable is VisualizerWrapper)))    // visualize right away if DisplayStringsState.On, but only if not dummy var ([Raw View])
                {
                    return ExpandVisualized(variable);
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

        private IVariableInformation[] ExpandVisualized(IVariableInformation variable)
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
                    if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames))
                    {
                        continue;
                    }
                    IVariableInformation expr = GetExpression(item.Value, variable, visualizer.ScopedNames, item.Name);
                    children.Add(expr);
                }
                else if (i is ArrayItemsType)
                {
                    ArrayItemsType item = (ArrayItemsType)i;
                    if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames))
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
                            string expressionValue = GetExpressionValue(item.Rank, variable, visualizer.ScopedNames);
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
                            string val = GetExpressionValue(substitute, variable, visualizer.ScopedNames);
                            uint tmp = MICore.Debugger.ParseUint(val, throwOnError: true);
                            dimensions[idx] = tmp;
                            totalSize *= tmp;
                        }
                    }
                    else
                    {
                        string val = GetExpressionValue(item.Size, variable, visualizer.ScopedNames);
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
                        if (EvalCondition(vp.Condition, variable, visualizer.ScopedNames))
                        {
                            IVariableInformation ptrExpr = GetExpression("*(" + vp.Value + ")", variable, visualizer.ScopedNames);
                            string typename = ptrExpr.TypeName;
                            if (String.IsNullOrWhiteSpace(typename))
                            {
                                continue;
                            }
                            // Creates an expression: (T[50])*(<ValuePointer> + 50)
                            // This evaluates for 50 elements of type T, starting at <ValuePointer> with an offet of 50 elements.
                            // E.g. This will grab elements 50 - 99 from <ValuePointer>.
                            // Note:
                            //   If requestedSize > 1000, the evaluation will only grab the first 1000 elements.

                            // We want to limit it to at most 50.
                            uint requestedSize = Math.Min(MAX_EXPAND, totalSize - startIndex);

                            StringBuilder arrayBuilder = new StringBuilder();
                            arrayBuilder.Append('(');
                            arrayBuilder.Append(typename);
                            arrayBuilder.Append('[');
                            arrayBuilder.Append(requestedSize);
                            arrayBuilder.Append("])*(");
                            arrayBuilder.Append(vp.Value);
                            arrayBuilder.Append('+');
                            arrayBuilder.Append(startIndex);
                            arrayBuilder.Append(')');
                            string arrayStr = arrayBuilder.ToString();

                            IVariableInformation arrayExpr = GetExpression(arrayStr, variable, visualizer.ScopedNames);
                            arrayExpr.EnsureChildren();
                            if (arrayExpr.CountChildren != 0)
                            {
                                uint offset = startIndex + requestedSize;
                                bool isForward = item.Direction != ArrayDirectionType.Backward;

                                for (uint index = 0; index < requestedSize; ++index)
                                {
                                    string displayName = (startIndex + index).ToString(CultureInfo.InvariantCulture);
                                    if (rank > 1)
                                    {
                                        displayName = GetDisplayNameFromArrayIndex(index, rank, dimensions, isForward);
                                    }

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
                    if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames))
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
                    string val = GetExpressionValue(item.Size, variable, visualizer.ScopedNames);
                    uint size = MICore.Debugger.ParseUint(val, throwOnError: true);
                    IVariableInformation headVal;
                    if (variable is TreeContinueWrapper tcw)
                    {
                        headVal = tcw.ContinueNode.Content;
                    }
                    else
                    {
                        headVal = GetExpression(item.HeadPointer, variable, visualizer.ScopedNames);
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
                        else if (GetExpression(item.ValueNode.Value, headVal, visualizer.ScopedNames) != null)
                        {
                            getValue = (v) => GetExpression(item.ValueNode.Value, v, visualizer.ScopedNames);
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
                    if (String.IsNullOrWhiteSpace(item.Condition))
                    {
                        if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames))
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
                        string val = GetExpressionValue(item.Size, variable, visualizer.ScopedNames);
                        size = MICore.Debugger.ParseUint(val);
                    }
                    IVariableInformation headVal;
                    if (variable is LinkedListContinueWrapper llcw)
                    {
                        headVal = llcw.ContinueNode;
                    }
                    else
                    {
                        headVal = GetExpression(item.HeadPointer, variable, visualizer.ScopedNames);
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
                            var value = GetExpression(item.ValueNode, headVal, visualizer.ScopedNames);
                            if (value != null && !value.Error)
                            {
                                getValue = (v) => GetExpression(item.ValueNode, v, visualizer.ScopedNames);
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
                    if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames))
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
                        if (EvalCondition(s.Condition, variable, visualizer.ScopedNames))
                        {
                            string val = GetExpressionValue(s.Value, variable, visualizer.ScopedNames);
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
                        if (EvalCondition(v.Condition, variable, visualizer.ScopedNames))
                        {
                            string processedExpr = ReplaceNamesInExpression(v.Value, variable, visualizer.ScopedNames);
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
                    if (item.Condition != null)
                    {
                        if (!EvalCondition(item.Condition, variable, visualizer.ScopedNames))
                        {
                            continue;
                        }
                    }
                    if (String.IsNullOrWhiteSpace(item.Value))
                    {
                        continue;
                    }
                    var expand = GetExpression(item.Value, variable, visualizer.ScopedNames);
                    var eChildren = Expand(expand);
                    if (eChildren != null)
                    {
                        children.AddRange(eChildren);
                    }
                }
            }
            if (!(variable is VisualizerWrapper)) // don't stack wrappers
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

        private bool EvalCondition(string condition, IVariableInformation variable, IDictionary<string, string> scopedNames)
        {
            bool res = true;
            if (!String.IsNullOrWhiteSpace(condition))
            {
                string exprValue = GetExpressionValue(condition, variable, scopedNames);

                bool exprBool = false;
                int exprInt = 0;
                res = !String.IsNullOrEmpty(exprValue) &&
                    ((bool.TryParse(exprValue, out exprBool) && exprBool) || (int.TryParse(exprValue, out exprInt) && exprInt > 0));
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
                var visualizer = autoVis.Visualizers.Find((v) => v.ParsedName.Match(name));   // TODO: match on View, version, etc
                if (visualizer != null)
                {
                    _vizCache[variable.TypeName] = new VisualizerInfo(visualizer.Visualizer, name);
                    return _vizCache[variable.TypeName];
                }
            }
            // failed to find a visualizer for the type, try looking for a typedef
            foreach (var autoVis in _typeVisualizers)
            {
                var alias = autoVis.Aliases.Find((v) => v.ParsedName.Match(name));   // TODO: match on View, version, etc
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

        private string FormatValue(string format, IVariableInformation variable, IDictionary<string, string> scopedNames)
        {
            if (String.IsNullOrWhiteSpace(format))
            {
                return String.Empty;
            }
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
                        string exprValue = GetExpressionValue(format.Substring(i + 1, m.Length - 2), variable, scopedNames);
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
                bool found = false;
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

        private string ReplaceNamesInExpression(string expression, IVariableInformation variable, IDictionary<string, string> scopedNames)
        {
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
        private IVariableInformation GetExpression(string expression, IVariableInformation variable, IDictionary<string, string> scopedNames, string displayName = null)
        {
            string processedExpr = ReplaceNamesInExpression(expression, variable, scopedNames);
            IVariableInformation expressionVariable = new VariableInformation(processedExpr, variable, _process.Engine, displayName);
            expressionVariable.SyncEval();
            return expressionVariable;
        }

        private string GetExpressionValue(string expression, IVariableInformation variable, IDictionary<string, string> scopedNames)
        {
            string processedExpr = ReplaceNamesInExpression(expression, variable, scopedNames);
            IVariableInformation expressionVariable = new VariableInformation(processedExpr, variable, _process.Engine, null);
            expressionVariable.SyncEval();
            return FormatDisplayString(expressionVariable).value;
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

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _natvisSettingWatcher?.Dispose();
            _natvisSettingWatcher = null;
        }
    }
}
