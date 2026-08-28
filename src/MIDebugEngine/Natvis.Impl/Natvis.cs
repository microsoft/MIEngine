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
using System.Threading.Tasks;
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
        // Restricts debugger type-name queries to plain C++ type names (identifiers,
        // scope operators, template arguments, cv/ref/pointer decorations) so that no
        // other console syntax can be smuggled into the query command. Literal spaces,
        // not \s: a newline inside a name could terminate the MI console line early.
        private static readonly Regex s_safeTypeNameForQuery = new Regex(@"^[A-Za-z_$][\w$]*(::[\w$]+)*( *<[\w :<>,\*&$]*>)?( *[\*&])*$", RegexOptions.Compiled);
        private List<FileInfo> _typeVisualizers;
        private DebuggedProcess _process;
        private HostConfigurationStore _configStore;
        private Dictionary<string, VisualizerInfo> _vizCache;
        // Debugger type-name resolution results (typedef target / first base class),
        // keyed by the reported type name. Null values cache negative results so each
        // unmatched name costs at most one round of debugger queries per session.
        private Dictionary<string, string> _typeNameQueryCache = new Dictionary<string, string>();
        // Single-token built-in type names, never worth a debugger query (natvis rules
        // cannot target primitives). Multi-word forms ("unsigned long") are already
        // rejected by s_safeTypeNameForQuery.
        private static readonly HashSet<string> s_primitiveTypeNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "void", "bool", "char", "short", "int", "long", "float", "double",
            "signed", "unsigned", "wchar_t", "char8_t", "char16_t", "char32_t"
        };
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
                    var expand = GetExpression(item.Value, variable, visualizer.ScopedNames, intrinsics: visualizer.Intrinsics);
                    var eChildren = Expand(expand);
                    if (eChildren != null)
                    {
                        children.AddRange(eChildren);
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
                string exprValue = GetExpressionValue(condition, variable, scopedNames, intrinsics);

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

            // Name-based fallback: the debugger reports a variable's type under its
            // declared name. A typedef alias or a subclass of a visualized type
            // therefore misses the wildcard rule of its underlying type, and the
            // debugger may expose no BaseClass child for FindBaseClass to walk.
            // Ask the debugger to resolve the name (typedef target or first base class)
            // and retry the rule lookup with the resolved name.
            if (_typeVisualizers.Count == 0)
            {
                return null;    // no natvis loaded — a resolved name could not match anything
            }
            string currentName = StripTypeDecorations(variable.TypeName);
            for (int chain = 0; chain < MAX_ALIAS_CHAIN && !String.IsNullOrEmpty(currentName); chain++)
            {
                string resolvedName = ResolveTypeNameViaDebugger(currentName);
                if (String.IsNullOrEmpty(resolvedName) || resolvedName == currentName)
                {
                    break;
                }
                _process.Logger.NatvisLogger?.WriteLine(LogLevel.Verbose, "FindType: '{0}' resolved to '{1}'", currentName, resolvedName);
                TypeName resolvedParsed = TypeName.Parse(resolvedName, _process.Logger.NatvisLogger);
                if (resolvedParsed == null)
                {
                    break;
                }
                // On success Scan caches the visualizer under variable.TypeName, so the
                // next variable reported under this alias hits _vizCache directly.
                var visualizer = Scan(resolvedParsed, variable);
                if (visualizer != null)
                {
                    return visualizer;
                }
                currentName = resolvedName; // alias of an alias / base of a base
            }
            return null;
        }

        /// <summary>
        /// Strips cv-qualifiers and pointer/reference decorations from a reported type
        /// name so it can be used in a debugger type query, e.g.
        /// "const TextItemList &amp;" -> "TextItemList".
        /// </summary>
        internal static string StripTypeDecorations(string typeName)
        {
            if (String.IsNullOrEmpty(typeName))
            {
                return typeName;
            }
            string result = typeName.Trim();
            while (result.EndsWith("*", StringComparison.Ordinal) || result.EndsWith("&", StringComparison.Ordinal))
            {
                result = result.Substring(0, result.Length - 1).TrimEnd();
            }
            foreach (string qualifier in new[] { "const ", "volatile " })
            {
                if (result.StartsWith(qualifier, StringComparison.Ordinal))
                {
                    result = result.Substring(qualifier.Length).TrimStart();
                }
            }
            return result;
        }

        /// <summary>
        /// Extracts the underlying type from a debugger type-description output:
        /// the typedef target, the head type name (when the debugger already expanded
        /// a typedef), or the first base class. Returns null when the output describes
        /// only <paramref name="queriedName"/> itself.
        /// Handles GDB "whatis"/"ptype" ("type = ..." prefix) and LLDB "type lookup".
        /// </summary>
        internal static string ExtractResolvedTypeName(string output, string queriedName)
        {
            if (String.IsNullOrWhiteSpace(output))
            {
                return null;
            }
            string line = null;
            foreach (string candidate in output.Split('\n'))
            {
                string trimmed = candidate.Trim();
                if (trimmed.Length > 0)
                {
                    line = trimmed;
                    break;
                }
            }
            if (line == null)
            {
                return null;
            }

            // Debugger error prose ("error: use of undeclared identifier ...") has a
            // top-level colon, which would make "error" parse as a head name.
            if (line.StartsWith("error:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("warning:", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (line.StartsWith("type = ", StringComparison.Ordinal))
            {
                line = line.Substring("type = ".Length).Trim();
            }

            if (line.StartsWith("typedef ", StringComparison.Ordinal))
            {
                // "typedef <target>" (lldb) or "typedef <target> <alias>" (C style).
                // Strip the trailing alias only when it is a whole whitespace-separated
                // token, so a target that merely ends with the alias name (ns::Alias)
                // is kept intact.
                string target = line.Substring("typedef ".Length).Trim().TrimEnd(';').Trim();
                if (target.Length > queriedName.Length &&
                    target.EndsWith(queriedName, StringComparison.Ordinal) &&
                    Char.IsWhiteSpace(target[target.Length - queriedName.Length - 1]))
                {
                    target = target.Substring(0, target.Length - queriedName.Length).TrimEnd();
                }
                return (target != queriedName) ? ValidatedTypeName(target) : null;
            }

            foreach (string keyword in new[] { "class ", "struct " })
            {
                if (line.StartsWith(keyword, StringComparison.Ordinal))
                {
                    line = line.Substring(keyword.Length).Trim();
                    break;
                }
            }

            // Strip a "template<...> " prefix (lldb prints template specializations
            // as "template<> class Foo<Bar> : ..."). Only when '<' follows the keyword —
            // a type genuinely named "template_something" must not be eaten.
            if (line.StartsWith("template", StringComparison.Ordinal) &&
                line.Substring("template".Length).TrimStart().StartsWith("<", StringComparison.Ordinal))
            {
                int d = 0;
                int i = "template".Length;
                for (; i < line.Length; i++)
                {
                    if (line[i] == '<') { d++; }
                    else if (line[i] == '>') { d--; if (d == 0) { i++; break; } }
                }
                line = line.Substring(i).TrimStart();
                foreach (string keyword in new[] { "class ", "struct " })
                {
                    if (line.StartsWith(keyword, StringComparison.Ordinal))
                    {
                        line = line.Substring(keyword.Length).Trim();
                        break;
                    }
                }
            }

            // GDB prints template types with a substitution clause after the head name:
            //   "ItemStack<int> [with T = int] : public ItemList<T> {"
            // Capture the parameter values and strip the clause; the base clause is
            // printed with UNsubstituted parameters and needs them re-applied.
            Dictionary<string, string> templateParams = null;
            int withPos = line.IndexOf("[with ", StringComparison.Ordinal);
            if (withPos >= 0)
            {
                int closePos = line.IndexOf(']', withPos);
                if (closePos > withPos)
                {
                    string clause = line.Substring(withPos + "[with ".Length, closePos - withPos - "[with ".Length);
                    templateParams = new Dictionary<string, string>(StringComparer.Ordinal);
                    int partStart = 0;
                    int d2 = 0;
                    for (int i = 0; i <= clause.Length; i++)
                    {
                        char c = i < clause.Length ? clause[i] : ',';
                        if (c == '<' || c == '(') { d2++; }
                        else if (c == '>' || c == ')') { d2--; }
                        else if (c == ',' && d2 == 0)
                        {
                            string part = clause.Substring(partStart, i - partStart);
                            int eq = part.IndexOf('=');
                            if (eq > 0)
                            {
                                string key = part.Substring(0, eq).Trim();
                                string value = part.Substring(eq + 1).Trim();
                                if (key.Length > 0 && value.Length > 0)
                                {
                                    templateParams[key] = value;
                                }
                            }
                            partStart = i + 1;
                        }
                    }
                    line = (line.Substring(0, withPos) + line.Substring(closePos + 1)).Trim();
                }
            }

            // line is now e.g. "TextItemList : public ItemList<TextItem> {" or
            // "ItemList<ItemValue> {" (typedef already expanded by the debugger).
            // Find the head-name end: the base-clause ':' (template depth 0,
            // skipping "::") or the opening '{'.
            int colonPos = -1;
            int headEnd = line.Length;
            int depth = 0;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '<') { depth++; }
                else if (c == '>') { depth--; }
                else if (c == '{') { headEnd = i; break; }
                else if (c == ':' && depth == 0)
                {
                    if (i + 1 < line.Length && line[i + 1] == ':')
                    {
                        i++; // scope operator
                        continue;
                    }
                    colonPos = i;
                    headEnd = i;
                    break;
                }
            }

            // If the debugger already resolved the queried name to another type
            // (alias expansion), the head name is the answer. Even when a base
            // clause follows, that is the base of the RESOLVED type, not of the
            // queried one — returning the base would skip past the type whose
            // rule we are looking for.
            string headName = line.Substring(0, headEnd).Trim();
            if (headName.Length > 0 && headName != queriedName)
            {
                return ValidatedTypeName(headName);
            }

            if (colonPos >= 0)
            {
                // First base class: cut at '{' and at the first top-level ','.
                string bases = line.Substring(colonPos + 1);
                int end = bases.Length;
                depth = 0;
                for (int i = 0; i < bases.Length; i++)
                {
                    char c = bases[i];
                    if (c == '<') { depth++; }
                    else if (c == '>') { depth--; }
                    else if ((c == ',' || c == '{') && depth == 0)
                    {
                        end = i;
                        break;
                    }
                }
                string firstBase = bases.Substring(0, end).Trim();
                // Strip access/virtual keywords to fixpoint: gdb prints
                // "public virtual Base", lldb prints "virtual public Base".
                bool strippedKeyword = true;
                while (strippedKeyword)
                {
                    strippedKeyword = false;
                    foreach (string keyword in new[] { "virtual", "public", "protected", "private" })
                    {
                        if (firstBase.StartsWith(keyword + " ", StringComparison.Ordinal))
                        {
                            firstBase = firstBase.Substring(keyword.Length + 1).TrimStart();
                            strippedKeyword = true;
                        }
                    }
                }
                // Re-apply gdb's "[with ...]" template-parameter values: the base
                // clause is printed with the bare parameter names (ItemList<T>).
                if (templateParams != null)
                {
                    foreach (KeyValuePair<string, string> param in templateParams)
                    {
                        firstBase = Regex.Replace(firstBase, @"\b" + Regex.Escape(param.Key) + @"\b", param.Value);
                    }
                }
                return ValidatedTypeName(firstBase);
            }

            // The output names only the queried type itself, with no base clause.
            return null;
        }

        /// <summary>
        /// Returns <paramref name="name"/> when it has the shape of a plain type name,
        /// null otherwise. Keeps debugger error prose (e.g. "no type was found ...")
        /// from being handed back as a resolution or entering the cache and the log.
        /// </summary>
        private static string ValidatedTypeName(string name)
        {
            return (!String.IsNullOrEmpty(name) && s_safeTypeNameForQuery.IsMatch(name)) ? name : null;
        }

        /// <summary>
        /// Asks the debugger what a reported type name is (typedef target or first base
        /// class) and returns the resolved name, or null. Results, including negative
        /// resolutions, are cached for the session; transient query errors are not.
        /// </summary>
        private string ResolveTypeNameViaDebugger(string typeName)
        {
            if (String.IsNullOrEmpty(typeName) ||
                s_primitiveTypeNames.Contains(typeName) ||
                !s_safeTypeNameForQuery.IsMatch(typeName))
            {
                return null;
            }
            if (_typeNameQueryCache.TryGetValue(typeName, out string cached))
            {
                return cached;
            }

            string resolved = null;
            try
            {
                switch (_process.MICommandFactory.Mode)
                {
                    case MIMode.Gdb:
                        // "whatis" is one line and resolves one typedef level. For a
                        // class it echoes the name back, so fall through to "ptype",
                        // whose first line carries the base-class clause.
                        resolved = ExtractResolvedTypeName(ConsoleCommandSync("whatis " + typeName), typeName);
                        if (resolved == null)
                        {
                            resolved = ExtractResolvedTypeName(ConsoleCommandSync("ptype " + typeName), typeName);
                        }
                        break;
                    case MIMode.Lldb:
                        resolved = ExtractResolvedTypeName(ConsoleCommandSync("type lookup " + typeName), typeName);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                // Do not cache: the failure may be transient (e.g. the process was not
                // stopped) rather than a genuine "no resolution" for this name.
                // Task.Wait() wraps the real failure in an AggregateException — unwrap
                // it so the log carries the actual message.
                string message = (e as AggregateException)?.Flatten().InnerException?.Message ?? e.Message;
                _process.Logger.NatvisLogger?.WriteLine(LogLevel.Verbose, "FindType: type name query for '{0}' failed: {1}", typeName, message);
                return null;
            }

            _typeNameQueryCache[typeName] = resolved;
            return resolved;
        }

        private string ConsoleCommandSync(string cmd)
        {
            string output = null;
            Task task = Task.Run(async () =>
            {
                output = await _process.ConsoleCmdAsync(cmd, allowWhileRunning: false, ignoreFailures: true);
            });
            task.Wait();
            return output;
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
                        string rawExpr = format.Substring(i + 1, m.Length - 2);
                        string spec = ExtractFormatSpecifier(rawExpr);
                        string exprValue = GetExpressionValue(rawExpr, variable, scopedNames, intrinsics);
                        if (spec == "sub" || spec == "su")
                            exprValue = CleanUtf16StringValue(exprValue);
                        else if (spec == "sb")
                            exprValue = CleanAsciiStringValue(exprValue);
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
            // Strip Windows dll!-qualified type prefixes (e.g. Qt6Cored.dll!)
            // for GDB/LLDB compatibility — meaningless outside Windows
            expression = s_moduleQualifiedPrefix.Replace(expression, "");

            // Expand intrinsic calls (e.g. day(), memberOffset(3)) into plain C++ expressions
            expression = ResolveIntrinsicCalls(expression, intrinsics);

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

        private string GetExpressionValue(string expression, IVariableInformation variable, IDictionary<string, string> scopedNames, IDictionary<string, IntrinsicType> intrinsics = null)
        {
            string processedExpr = ReplaceNamesInExpression(expression, variable, scopedNames, intrinsics);
            IVariableInformation expressionVariable = new VariableInformation(processedExpr, variable, _process.Engine, null);
            expressionVariable.SyncEval();

            // Avoid recursive natvis formatting when expression is 'this'
            if (expression.Trim() == "this")
            {
                return expressionVariable.Value;
            }

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
