// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;
using System.Globalization;

namespace MICore
{
    /// <summary>
    /// Prefix from the output indicating the class of result. According to the documentation, these are:
    /// 
    ///    result-class ==> "done" | "running" | "connected" | "error" | "exit" 
    /// </summary>
    public enum ResultClass
    {
        /// <summary>
        /// ResultClass is not set
        /// </summary>
        None,

        done,
        running,
        connected,
        error,
        exit
    }

    public class ResultValue
    {
        public virtual ResultValue Find(string name)
        {
            throw new MIResultFormatException(name, this);
        }

        public virtual bool TryFind(string name, out ResultValue result)
        {
            if (Contains(name))
            {
                result = Find(name);
            }
            else
            {
                result = null;
            }
            return result != null;
        }

        public virtual bool Contains(string name)
        {
            return false;
        }

        public ConstValue FindConst(string name)
        {
            return Find<ConstValue>(name);
        }

        public int FindInt(string name)
        {
            try
            {
                return FindConst(name).ToInt;
            }
            catch (MIResultFormatException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new MIResultFormatException(name, this, e);
            }
        }
        public uint FindUint(string name)
        {
            try
            {
                return FindConst(name).ToUint;
            }
            catch (MIResultFormatException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new MIResultFormatException(name, this, e);
            }
        }

        /// <summary>
        /// Try to find a uint property. Throws if the property can be found but is not a uint.
        /// </summary>
        /// <param name="name">[Required] name of the property to search for</param>
        /// <returns>The value of the property or null if it cannot be found</returns>
        public uint? TryFindUint(string name)
        {
            ConstValue c;
            if (!TryFind(name, out c))
            {
                return null;
            }

            try
            {
                return c.ToUint;
            }
            catch (OverflowException)
            {
                return null;
            }
            catch (MIResultFormatException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new MIResultFormatException(name, this, e);
            }
        }

        public ulong FindAddr(string name)
        {
            try
            {
                return FindConst(name).ToAddr;
            }
            catch (MIResultFormatException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new MIResultFormatException(name, this, e);
            }
        }

        /// <summary>
        /// Try and find an address property. Returns null if there is no property. Will throw if that property exists but it is not an address.
        /// </summary>
        /// <param name="name">[Required] Name of the property to look for</param>
        /// <returns>The value of the address or null if it can't be found</returns>
        public ulong? TryFindAddr(string name)
        {
            ConstValue c;
            if (!TryFind(name, out c))
            {
                return null;
            }

            try
            {
                return c.ToAddr;
            }
            catch (MIResultFormatException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new MIResultFormatException(name, this, e);
            }
        }


        public string FindString(string name)
        {
            return FindConst(name).AsString;
        }

        public string TryFindString(string name)
        {
            ConstValue c;
            if (!TryFind(name, out c))
            {
                return string.Empty;
            }
            return c.AsString;
        }

        public T Find<T>(string name) where T : ResultValue
        {
            var c = Find(name);
            if (c is T)
            {
                return c as T;
            }
            throw new MIResultFormatException(name, this);
        }

        public bool TryFind<T>(string name, out T result) where T : ResultValue
        {
            if (Contains(name))
            {
                result = Find(name) as T;
            }
            else
            {
                result = null;
            }
            return result != null;
        }

        public T TryFind<T>(string name) where T : ResultValue
        {
            T result;
            if (!TryFind(name, out result))
            {
                return null;
            }
            return result;
        }
    }

    public class ConstValue : ResultValue
    {
        public readonly string Content;

        public ConstValue(string str)
        {
            Content = str ?? string.Empty;
        }

        public ulong ToAddr
        {
            get
            {
                return MICore.Debugger.ParseAddr(Content, throwOnError: true);
            }
        }
        public int ToInt
        {
            get
            {
                return int.Parse(Content, CultureInfo.InvariantCulture);
            }
        }
        public uint ToUint
        {
            get
            {
                return MICore.Debugger.ParseUint(Content, throwOnError: true);
            }
        }

        public string AsString
        {
            get
            {
                return Content;
            }
        }

        public override string ToString()
        {
            return Content;
        }
    }

    [DebuggerDisplay("{DisplayValue,nq}", Name = "{Name,nq}")]
    [DebuggerTypeProxy(typeof(NamedResultValueTypeProxy))]
    public class NamedResultValue
    {
        internal class NamedResultValueTypeProxy
        {
            private ResultValue _value;
            public NamedResultValueTypeProxy(NamedResultValue namedResultValue)
            {
                _value = namedResultValue.Value;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public NamedResultValue[] Content
            {
                get
                {
                    List<NamedResultValue> values = null;

                    if (_value is ValueListValue)
                    {
                        var valueListValue = (ValueListValue)_value;
                        values = new List<NamedResultValue>(valueListValue.Length);
                        for (int i = 0; i < valueListValue.Length; i++)
                        {
                            string name = string.Format(CultureInfo.InvariantCulture, "[{0}]", i); // fake the [0], [1], [2], etc...
                            values.Add(new NamedResultValue(name, valueListValue.Content[i]));
                        }
                    }
                    else if (_value is ResultListValue)
                    {
                        var resultListValue = (ResultListValue)_value;
                        var namedResultValues = resultListValue.Content.Select(value => new NamedResultValue(value));
                        values = new List<NamedResultValue>(namedResultValues);
                    }
                    else if (_value is TupleValue)
                    {
                        var tupleValue = (TupleValue)_value;
                        values = new List<NamedResultValue>(tupleValue.Content.Count);
                        tupleValue.Content.ForEach((namedResultValue) =>
                        {
                            values.Add(new NamedResultValue(namedResultValue.Name, namedResultValue.Value));
                        });
                    }

                    return values?.ToArray();
                }
            }
        }

        public string Name { get; private set; }
        public ResultValue Value { get; private set; }

        public NamedResultValue(string name, ResultValue value)
        {
            this.Name = name;
            this.Value = value;
        }

        public NamedResultValue(NamedResultValue namedResultValue) : this(namedResultValue.Name, namedResultValue.Value)
        {
        }

        internal string DisplayValue
        {
            get
            {
                if (this.Value is ConstValue)
                {
                    return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", ((ConstValue)this.Value).AsString);
                }
                else if (this.Value is TupleValue)
                {
                    return string.Format(CultureInfo.InvariantCulture, "{{...}}");
                }
                else if (this.Value is ListValue)
                {
                    return string.Format(CultureInfo.InvariantCulture, "[...] count = {0}", ((ListValue)this.Value).Length);
                }

                return "<Unkonwn ResultValue Type>";
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Name);
            builder.Append("=");
            builder.Append(Value.ToString());
            return builder.ToString();
        }
    }

    public class TupleValue : ResultValue
    {
        public List<NamedResultValue> Content { get; private set; }

        public TupleValue(List<NamedResultValue> list)
        {
            Content = list;
        }
        public override ResultValue Find(string name)
        {
            var item = Content.Find((c) => c.Name == name);
            if (item == null)
            {
                throw new MIResultFormatException(name, this);
            }
            return item.Value;
        }

        public override bool Contains(string name)
        {
            var item = Content.Find((c) => c.Name == name);
            return item != null;
        }

        public override string ToString()
        {
            StringBuilder outTuple = new StringBuilder();
            outTuple.Append("{");
            for (int i = 0; i < Content.Count; ++i)
            {
                if (i != 0)
                {
                    outTuple.Append(",");
                }
                outTuple.Append(Content[i].ToString());
            }
            outTuple.Append("}");
            return outTuple.ToString();
        }
        public ResultValue[] FindAll(string name)
        {
            return Content.FindAll((c) => c.Name == name).Select((c) => c.Value).ToArray();
        }

        public T[] FindAll<T>(string name) where T : class
        {
            return FindAll(name).OfType<T>().ToArray();
        }

        /// <summary>
        /// Creates a new TupleValue with a subset of values from this TupleValue.
        /// </summary>
        /// <param name="requiredNames">The list of names that must be added to the TupleValue.</param>
        /// <param name="optionalNames">The list of names that will be added to the TupleValue if they exist in this TupleValue.</param>
        public TupleValue Subset(IEnumerable<string> requiredNames, IEnumerable<string> optionalNames = null)
        {
            List<NamedResultValue> values = new List<NamedResultValue>();

            // Iterate the required list and add the values.
            // Will throw if a name cannot be found.
            foreach (string name in requiredNames)
            {
                values.Add(new NamedResultValue(name, this.Find(name)));
            }

            // Iterate the optional list and add the values of the name exists.
            if (null != optionalNames)
            {
                foreach (string name in optionalNames)
                {
                    ResultValue value;
                    if (this.TryFind(name, out value))
                    {
                        values.Add(new NamedResultValue(name, value));
                    }
                }
            }

            return new TupleValue(values);
        }
    }

    public abstract class ListValue : ResultValue
    {
        public abstract int Length { get; }
        public bool IsEmpty()
        {
            return this.Length == 0;
        }
    }

    public class ValueListValue : ListValue
    {
        public ResultValue[] Content { get; private set; }

        public override int Length { get { return Content.Length; } }

        public ValueListValue(List<ResultValue> list)
        {
            Content = list.ToArray();
        }
        public T[] AsArray<T>() where T : ResultValue
        {
            return Content.Cast<T>().ToArray();
        }

        public string[] AsStrings
        {
            get { return Content.Cast<ConstValue>().Select(c => c.AsString).ToArray(); }
        }

        public override string ToString()
        {
            StringBuilder outList = new StringBuilder();
            outList.Append("[");
            for (int i = 0; i < Content.Length; ++i)
            {
                if (i != 0)
                {
                    outList.Append(",");
                }
                outList.Append(Content[i].ToString());
            }
            outList.Append("]");
            return outList.ToString();
        }
    }

    public class ResultListValue : ListValue
    {
        public NamedResultValue[] Content { get; private set; }

        public override int Length { get { return Content.Length; } }

        public ResultListValue(List<NamedResultValue> list)
        {
            Content = list.ToArray();
        }
        public override ResultValue Find(string name)
        {
            var item = Array.Find(Content, (c) => c.Name == name);
            if (item == null)
            {
                throw new MIResultFormatException(name, this);
            }
            return item.Value;
        }

        public override bool Contains(string name)
        {
            var item = Array.Find(Content, (c) => c.Name == name);
            return item != null;
        }

        public ResultValue[] FindAll(string name)
        {
            return Array.FindAll(Content, (c) => c.Name == name).Select((c) => c.Value).ToArray();
        }

        public T[] FindAll<T>(string name) where T : class
        {
            return FindAll(name).OfType<T>().ToArray();
        }

        public string[] FindAllStrings(string name)
        {
            return FindAll<ConstValue>(name).Select((c) => c.AsString).ToArray();
        }

        public int CountOf(string name)
        {
            return Content.Count(c => c.Name == name);
        }

        public override string ToString()
        {
            StringBuilder outList = new StringBuilder();
            outList.Append("[");
            for (int i = 0; i < Content.Length; ++i)
            {
                if (i != 0)
                {
                    outList.Append(",");
                }
                outList.Append(Content[i].Name);
                outList.Append("=");
                outList.Append(Content[i].Value.ToString());
            }
            outList.Append("]");
            return outList.ToString();
        }
    }

    [DebuggerTypeProxy(typeof(ResultsTypeProxy))]
    [DebuggerDisplay("{ResultClass}, Length={Length}")]
    public class Results : ResultListValue
    {
        internal class ResultsTypeProxy
        {
            public ResultsTypeProxy(Results results)
            {
                this.Content = results.Content;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public NamedResultValue[] Content { get; private set; }
        }

        public readonly ResultClass ResultClass;

        public Results(ResultClass resultsClass, List<NamedResultValue> list = null)
            : base(list ?? new List<NamedResultValue>())
        {
            ResultClass = resultsClass;
        }

        public Results Add(string name, ResultValue value)
        {
            var l = Content.ToList();
            l.Add(new NamedResultValue(name, value));
            return new Results(ResultClass, l);
        }

        public override string ToString()
        {
            StringBuilder outList = new StringBuilder();
            outList.Append("result-class: " + ResultClass.ToString());
            for (int i = 0; i < Content.Length; ++i)
            {
                outList.Append("\r\n");
                outList.Append(Content[i].Name);
                outList.Append(": ");
                outList.Append(Content[i].Value.ToString());
            }
            outList.Append("\r\n");
            return outList.ToString();
        }
    };

    public class MIResults
    {
        private Logger Logger { get; set; }

        public MIResults(Logger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// result-record ==> result-class ( "," result )* 
        /// </summary>
        /// <param name="output"></param>
        public Results ParseCommandOutput(string output)
        {
            output = output.Trim();
            int comma = output.IndexOf(',');
            Results results;
            ResultClass resultClass = ResultClass.None;
            if (comma < 0)
            {
                // no comma, so entire string should be the result class
                results = new Results(ParseResultClass(output), new List<NamedResultValue>());
            }
            else
            {
                resultClass = ParseResultClass(output.Substring(0, comma));
                results = ParseResultList(output.Substring(comma + 1), resultClass);
            }
            return results;
        }

        public Results ParseResultList(string listStr, ResultClass resultClass = ResultClass.None)
        {
            listStr = listStr.Trim();
            string rest;
            var list = ParseResultList((string s, ref int i) =>
                {
                    return true;
                }, (string s, ref int i) =>
                    {
                        return i == s.Length;
                    }, listStr, out rest);
            if (rest != String.Empty)
            {
                ParseError("trailing chars", rest);
                return null;
            }
            return new Results(resultClass, list);
        }

        public string ParseCString(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            else if (input == String.Empty)
            {
                return string.Empty;
            }

            string cstr = input.Trim();
            if (cstr[0] != '\"')   // not a Cstring, just return the string
            {
                return input;
            }
            string rest;
            var s = ParseCString(cstr, out rest);
            return s == null ? string.Empty : s.AsString;
        }

        /// <summary>
        /// value ==>const | tuple | list
        /// </summary>
        /// <returns></returns>
        private ResultValue ParseValue(string resultStr, out string rest)
        {
            ResultValue value = null;
            rest = String.Empty;
            if (string.IsNullOrEmpty(resultStr))
            {
                return null;
            }
            switch (resultStr[0])
            {
                case '\"':
                    value = ParseCString(resultStr, out rest);
                    break;
                case '{':
                    value = ParseTuple(resultStr, out rest);
                    break;
                case '[':
                    value = ParseList(resultStr, out rest);
                    break;
                default:
                    ParseError("unexpected char", resultStr);
                    break;
            }
            return value;
        }

        /// <summary>
        /// GDB (on x86) sometimes returns a tuple list in a context requiring a tuple (&lt;MULTIPLE&gt; breakpoints). 
        /// The grammer does not allow this, but we recognize it and accept it only in the special case when it is contained
        /// in a result value.
        ///     tuplelist --  tuple ("," tuple)*
        ///     value -- const | tuple | tuplelist | list
        /// </summary>
        /// <returns></returns>
        private ResultValue ParseResultValue(string resultStr, out string rest)
        {
            ResultValue value = null;
            rest = String.Empty;
            if (string.IsNullOrEmpty(resultStr))
            {
                return null;
            }
            switch (resultStr[0])
            {
                case '\"':
                    value = ParseCString(resultStr, out rest);
                    break;
                case '{':
                    value = ParseResultTuple(resultStr, out rest);
                    break;
                case '[':
                    value = ParseList(resultStr, out rest);
                    break;
                default:
                    ParseError("unexpected char", resultStr);
                    break;
            }
            return value;
        }

        /// <summary>
        /// IsValueChar - true is the char is a start-char for a value
        /// </summary>
        private static bool IsValueChar(char c)
        {
            return c == '\"' || c == '{' || c == '[';
        }

        /// <summary>
        /// result ==> variable "=" value
        /// </summary>
        /// <param name="resultStr">trimmed input string</param>
        /// <param name="rest">trimmed remainder after result</param>
        private NamedResultValue ParseResult(string resultStr, out string rest)
        {
            rest = String.Empty;
            int equals = resultStr.IndexOf('=');
            if (equals < 1)
            {
                ParseError("variable not found", resultStr);
                return null;
            }
            string name = resultStr.Substring(0, equals);
            ResultValue value = ParseResultValue(resultStr.Substring(equals + 1), out rest);
            if (value == null)
            {
                return null;
            }
            return new NamedResultValue(name, value);
        }

        private static ResultClass ParseResultClass(string resultClass)
        {
            switch (resultClass)
            {
                case "done": return ResultClass.done;
                case "running": return ResultClass.running;
                case "connected": return ResultClass.connected;
                case "error": return ResultClass.error;
                case "exit": return ResultClass.exit;
                default:
                    {
                        Debug.Fail("unexpected result class");
                        return ResultClass.None;
                    }
            }
        }

        private ConstValue ParseCString(string input, out string rest)
        {
            rest = input;
            StringBuilder output = new StringBuilder(input.Length);
            if (String.IsNullOrEmpty(input) || input[0] != '\"')
            {
                ParseError("Cstring expected", input);
                return null;
            }
            int i = 1;
            bool endFound = false;
            for (; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '\"')
                {
                    // closing quote, so we are (probably) done
                    i++;
                    if ((i < input.Length) && (input[i] == c))
                    {
                        // double quotes mean we emit a single quote, and carry on
                        ;
                    }
                    else
                    {
                        endFound = true;
                        break;
                    }
                }
                else if (c == '\\')
                {
                    // escaped character
                    c = input[++i];
                    switch (c)
                    {
                        case 'n': c = '\n'; break;
                        case 'r': c = '\r'; break;
                        case 't': c = '\t'; break;
                        default: break;
                    }
                }
                output.Append(c);
            }
            if (!endFound)
            {
                ParseError("CString not terminated", input);
                return null;
            }
            rest = input.Substring(i);
            return new ConstValue(output.ToString());
        }

        private delegate bool EdgeCondition(string s, ref int i);

        private List<NamedResultValue> ParseResultList(EdgeCondition begin, EdgeCondition end, string input, out string rest)
        {
            rest = string.Empty;
            List<NamedResultValue> list = new List<NamedResultValue>();
            int i = 0;
            if (!begin(input, ref i))
            {
                ParseError("Unexpected opening character", input);
                return null;
            }
            if (end(input, ref i))    // tuple is empty
            {
                rest = input.Substring(i);  // eat through the closing brace
                return list;
            }
            input = input.Substring(i);
            var item = ParseResult(input, out rest);
            if (item == null)
            {
                ParseError("Result expected", input);
                return null;
            }
            list.Add(item);
            input = rest;
            while (!string.IsNullOrEmpty(input) && input[0] == ',')
            {
                item = ParseResult(input.Substring(1), out rest);
                if (item == null)
                {
                    ParseError("Result expected", input);
                    return null;
                }
                list.Add(item);
                input = rest;
            }

            i = 0;
            if (!end(input, ref i))    // tuple is not closed
            {
                ParseError("Unexpected list termination", input);
                rest = string.Empty;
                return null;
            }
            rest = input.Substring(i);
            return list;
        }

        private List<NamedResultValue> ParseResultList(char begin, char end, string input, out string rest)
        {
            return ParseResultList((string s, ref int i) =>
            {
                if (s[i] == begin)
                {
                    i++;
                    return true;
                }
                return false;
            }, (string s, ref int i) =>
            {
                if (i < s.Length && s[i] == end)
                {
                    i++;
                    return true;
                }
                return false;
            }, input, out rest);
        }

        /// <summary>
        /// tuple ==> "{}" | "{" result ( "," result )* "}" 
        /// </summary>
        /// <returns>if one tuple found a TupleValue, otherwise a ValueListValue of TupleValues</returns>
        private ResultValue ParseResultTuple(string input, out string rest)
        {
            var list = ParseResultList('{', '}', input, out rest);
            if (list == null)
            {
                return null;
            }
            var tlist = new List<ResultValue>();
            TupleValue v;
            while (rest.StartsWith(",{", StringComparison.Ordinal))
            {
                // a tuple list
                v = new TupleValue(list);
                tlist.Add(v);
                list = ParseResultList('{', '}', rest.Substring(1), out rest);
            }
            v = new TupleValue(list);
            if (tlist.Count != 0)
            {
                tlist.Add(v);
                return new ValueListValue(tlist);
            }
            return v;
        }

        /// <summary>
        /// tuple ==> "{}" | "{" result ( "," result )* "}" 
        /// </summary>
        private TupleValue ParseTuple(string input, out string rest)
        {
            var list = ParseResultList('{', '}', input, out rest);
            if (list == null)
            {
                return null;
            }
            return new TupleValue(list);
        }

        /// <summary>
        /// list ==> "[]" | "[" value ( "," value )* "]" | "[" result ( "," result )* "]"  
        /// </summary>
        private ResultValue ParseList(string input, out string rest)
        {
            rest = string.Empty;
            if (input[0] != '[')
            {
                ParseError("List expected", input);
                return null;
            }
            if (input[1] == ']')    // list is empty
            {
                rest = input.Substring(2);  // eat through the closing brace
                return new ValueListValue(new List<ResultValue>());
            }
            if (IsValueChar(input[1]))
            {
                return ParseValueList(input, out rest);
            }
            else
            {
                return ParseResultList(input, out rest);
            }
        }

        /// <summary>
        /// list ==>  "[" value ( "," value )* "]"   
        /// </summary>
        private ValueListValue ParseValueList(string input, out string rest)
        {
            rest = string.Empty;
            List<ResultValue> list = new List<ResultValue>();
            if (input[0] != '[')
            {
                ParseError("List expected", input);
                return null;
            }
            input = input.Substring(1);
            var item = ParseValue(input, out rest);
            if (item == null)
            {
                ParseError("Value expected", input);
                return null;
            }
            list.Add(item);
            input = rest;
            while (!string.IsNullOrEmpty(input) && input[0] == ',')
            {
                item = ParseValue(input.Substring(1), out rest);
                if (item == null)
                {
                    ParseError("Value expected", input);
                    return null;
                }
                list.Add(item);
                input = rest;
            }

            if (input.Length == 0 || input[0] != ']')    // list is not closed
            {
                ParseError("List not terminated", input);
                rest = string.Empty;
                return null;
            }
            rest = input.Substring(1);
            return new ValueListValue(list);
        }

        /// <summary>
        /// list ==>  "[" result ( "," result )* "]"  
        /// </summary>
        private ResultListValue ParseResultList(string input, out string rest)
        {
            var list = ParseResultList('[', ']', input, out rest);
            if (list == null)
            {
                return null;
            }
            return new ResultListValue(list);
        }

        private void ParseError(string message, string input)
        {
            Debug.Fail(message + ": " + input);
#if DEBUG
            Logger?.WriteLine(String.Format(CultureInfo.CurrentCulture, "MI parsing error: {0}: \"{1}\"", message, input));
#endif
        }
    }
}
