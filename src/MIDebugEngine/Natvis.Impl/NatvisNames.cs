// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MICore;
using System.Globalization;
using Microsoft.DebugEngineHost;

namespace Microsoft.MIDebugEngine.Natvis
{
    internal class TypeName
    {
        public string FullyQualifiedName { get; private set; }
        public List<TypeName> Qualifiers { get; private set; }
        public string BaseName { get; set; }
        public List<TypeName> Args { get; private set; }    // template arguements
        public List<TypeName> Parameters { get; private set; }    // parameter types

        public bool IsWildcard { get; private set; }
        public bool IsArray { get; private set; }
        public bool IsFunction { get; private set; }
        public int[] Dimensions { get; private set; }
        public bool IsConst { get; private set; }

        private void SetArraySize(int[] Dims)
        {
            IsArray = true;
            Dimensions = Dims;
        }

        private static Regex s_identifier = new Regex("^[a-zA-Z$_][a-zA-Z$_0-9]*");
        private static Regex s_numeric = new Regex("^[-]?[0-9]+(u|l|ul)*");        // only decimal constants
        private static Regex s_simpleType = new Regex(
                    @"^(signed\s+char|unsigned\s+char|char16_t|char32_t|wchar_t|char|"
                    + @"signed\s+short\s+int|signed\s+short|unsigned\s+short\s+int|unsigned\s+short|short\s+int|short|"
                    + @"signed\s+int|unsigned\s+int|int|"
                    + @"signed\s+long\s+int|unsigned\s+long\s+int|"
                    + @"signed\s+long\s+long\s+int|long\s+long\s+int|long\s+int|unsigned\s+long\s+long\s+int|long\s+long"
                    + @"float|double|"
                    + @"long\s+double|long|bool|void)\b" // matches prefixes ending in '$' (e.g. void$Foo), these are checked for below
                );
        private static readonly TypeName s_any = new TypeName()
        {
            IsWildcard = true
        };

        private TypeName()
        {
            Args = new List<TypeName>();
            Qualifiers = new List<TypeName>();
            Parameters = null;
            IsWildcard = false;
            IsArray = false;
        }

        private TypeName(string name) : this()
        {
            BaseName = name;
        }

        /// <summary>
        /// Match this typeName to a candidate typeName. This type support wildcard matching against the candidate
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool Match(TypeName t)
        {
            if (IsWildcard)
            {
                return true;
            }
            if (Qualifiers.Count != t.Qualifiers.Count)
            {
                return false;
            }
            if (BaseName != t.BaseName)
            {
                return false;
            }
            for (int i = 0; i < Qualifiers.Count; ++i)
            {
                if (!Qualifiers[i].Match(t.Qualifiers[i]))
                {
                    return false;
                }
            }
            // args must match one-for one, 
            // or if last arg is a wildcard it will match any number of additional args
            if (Args.Count > t.Args.Count || (Args.Count == 0 && t.Args.Count > 0) || (Args.Count < t.Args.Count && !Args[Args.Count - 1].IsWildcard))
            {
                return false;
            }
            for (int arg = 0; arg < Args.Count; ++arg)
            {
                if (!Args[arg].Match(t.Args[arg]))
                {
                    return false;
                }
            }
            if (IsArray != t.IsArray)
            {
                return false;
            }
            if (IsArray)
            {
                if (Dimensions.Length != t.Dimensions.Length)
                {
                    return false;
                }
                for (int i = 0; i < Dimensions.Length; i++)
                {
                    if (Dimensions[i] != t.Dimensions[i])
                    {
                        if (Dimensions[i] != -1)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Return a parsed type name
        /// Acceptable name format:
        ///     typeName = ["const "] ([unqualifiedName "::"]* unqualifiedName | simpleTypeName) ['*'] [Array] | funtionDef
        ///     unqualifiedName = identifier | identifier '<' templateList '>'
        ///     templatelist = listElem  | listElem ',' templateList 
        ///     listElem = typeName | numericConstant | '*'
        ///     Array = '[]' | '[' [numericConstant ',']* numericConstant ']'
        ///     functionDef = typeName (parameterList)
        ///   
        /// </summary>
        /// <param name="fullyQualifiedName"></param>
        /// <returns></returns>
        public static TypeName Parse(string fullyQualifiedName, ILogChannel logger)
        {
            if (String.IsNullOrEmpty(fullyQualifiedName))
                return null;
            string rest = null;
            TypeName t = MatchTypeName(fullyQualifiedName.Trim(), out rest);
            if (!String.IsNullOrWhiteSpace(rest))
            {
                logger.WriteLine(LogLevel.Error, "Natvis failed to parse typename: {0}", fullyQualifiedName);
                return null;
            }
            return t;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Trimmed string containing a type name</param>
        /// <param name="rest">Trimmed remainder of string after name match</param>
        /// <returns></returns>
        private static TypeName MatchTypeName(string name, out string rest)
        {
            string original = name;
            if (name.StartsWith("const ", StringComparison.Ordinal))
            {
                name = name.Substring(6).Trim();    // TODO: we just ignore const
            }
            TypeName t = MatchSimpleTypeName(name, out rest);
            if (t == null)
            {
                List<TypeName> qualifiers = new List<TypeName>();
                t = MatchUnqualifiedName(name, out rest);
                while (t != null && rest.Length > 2 && rest.StartsWith("::", StringComparison.Ordinal))
                {
                    // process qualifiers
                    qualifiers.Add(t);
                    t = MatchUnqualifiedName(rest.Substring(2).Trim(), out rest);
                }
                if (t == null)
                {
                    return null;
                }
                t.Qualifiers = qualifiers;  // add qualifiers to the type
            }
            if (rest.StartsWith("const", StringComparison.Ordinal))
            {
                rest = rest.Substring(5).Trim();
            }
            while (rest.StartsWith("*", StringComparison.Ordinal) || rest.StartsWith("&", StringComparison.Ordinal))
            {
                t.BaseName += rest[0];
                rest = rest.Substring(1).Trim();
                if (rest.StartsWith("const", StringComparison.Ordinal))
                {
                    rest = rest.Substring(5).Trim();
                }
            }
            MatchArray(t, rest, out rest); // add array or pointer
            if (rest.StartsWith("(", StringComparison.Ordinal))
            {
                t.IsFunction = true;
                t.Parameters = new List<TypeName>();
                if (!MatchParameterList(rest.Substring(1).Trim(), out rest, t.Parameters))
                {
                    return null;
                }
                if (rest.Length > 0 && rest[0] == ')')
                {
                    rest = rest.Substring(1).Trim();
                }
                else
                {
                    return null;
                }
            }
            // complete the full name of the type
            t.FullyQualifiedName = original.Substring(0, original.Length - rest.Length);
            return t;
        }

        private static TypeName MatchSimpleTypeName(string name, out string rest)
        {
            rest = String.Empty;
            var m = s_simpleType.Match(name);
            if (m.Success)
            {
                // The simpleType regular expression will succeed for strings that look like
                // simple types, but are terminated by '$'. 
                // Since the $ is a valid C++ identifier character we check it here to make 
                // sure we haven't accidentally matched a prefix, e.g. int$Foo
                string r = name.Substring(m.Length);
                if (r.Length > 0 && r[0] == '$')
                {
                    return null;
                }
                rest = r.Trim();
                return new TypeName(m.Value);
            }
            return null;
        }

        private static TypeName MatchUnqualifiedName(string name, out string rest)
        {
            string basename = MatchIdentifier(name, out rest);
            if (String.IsNullOrEmpty(basename))
            {
                return null;
            }
            TypeName t = new TypeName(basename);
            if (rest.Length > 0 && rest[0] == '<')
            {
                if (!MatchTemplateList(rest.Substring(1).Trim(), out rest, t.Args) || rest.Length < 1 || rest[0] != '>')
                {
                    return null;
                }
                rest = rest.Substring(1).Trim();
            }
            return t;
        }

        private static void MatchArray(TypeName t, string name, out string rest)
        {
            if (name.StartsWith("[]", StringComparison.Ordinal))
            {
                t.SetArraySize(new int[] { -1 });
                rest = name.Substring(2).Trim();
            }
            else if (name.StartsWith("[", StringComparison.Ordinal))  // TODO: handle multiple dimensions
            {
                string num = MatchConstant(name.Substring(1).Trim(), out rest);
                if (rest.StartsWith("]", StringComparison.Ordinal))
                {
                    t.SetArraySize(new int[] { Int32.Parse(num, CultureInfo.InvariantCulture) });
                }
                rest = rest.Substring(1).Trim();
            }
            else
            {
                rest = name;
            }
        }

        private static string MatchIdentifier(string name, out string rest)
        {
            rest = String.Empty;
            var m = s_identifier.Match(name);
            if (m.Success)
            {
                rest = name.Substring(m.Length).Trim();
            }
            return m.Value;
        }

        private static string MatchConstant(string name, out string rest)
        {
            rest = String.Empty;
            var m = s_numeric.Match(name);
            if (m.Success)
            {
                rest = name.Substring(m.Length).Trim();
            }
            return m.Value;
        }

        private static bool MatchTemplateList(string templist, out string rest, List<TypeName> args)
        {
            TypeName t;
            string arg = MatchConstant(templist, out rest); // no constants allowed in parameter lists
            if (!String.IsNullOrEmpty(arg))
            {
                var constantArg = new TypeName(arg);
                constantArg.FullyQualifiedName = arg;
                args.Add(constantArg);
            }
            else if (templist.StartsWith("*", StringComparison.Ordinal))
            {
                rest = templist.Substring(1).Trim();
                args.Add(TypeName.s_any);
            }
            else if ((t = MatchTypeName(templist, out rest)) != null)
            {
                args.Add(t);
            }
            else
            {
                return false;
            }
            if (rest.Length > 1 && rest[0] == ',')
            {
                return MatchTemplateList(rest.Substring(1).Trim(), out rest, args);
            }
            return true;
        }
        private static bool MatchParameterList(string plist, out string rest, List<TypeName> args)
        {
            rest = plist;
            while (rest.Length > 0 && rest[0] != ')')
            {
                TypeName t;
                if ((t = MatchTypeName(rest, out rest)) == null)
                {
                    return false;
                }
                args.Add(t);
                if (t != null && rest.Length > 1 && rest[0] == ',')
                {
                    rest = rest.Substring(1).Trim();
                }
            }
            return true;
        }
    }
}
