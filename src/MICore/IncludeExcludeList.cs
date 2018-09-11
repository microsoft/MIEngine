// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MICore.SymbolLocator
{
    public class IncludeExcludeList
    {
        static readonly char[] WildCardCharacters = new char[] { '*', '?' };
        Lazy<List<Regex>> _wildcardEntries;
        Lazy<HashSet<string>> _qualifiedEntries;

        public bool IsEmpty
        {
            get
            {
                return !_wildcardEntries.IsValueCreated && !_qualifiedEntries.IsValueCreated;
            }
        }


        public IncludeExcludeList()
        {
            Clear();
        }

        public void Add(string entry)
        {
            if (string.IsNullOrEmpty(entry))
                return;

            if (entry.IndexOfAny(WildCardCharacters) >= 0)
            {
                // Entry is a wild card. Convert to a Regex.
                StringBuilder regexStringBuilder = new StringBuilder();
                regexStringBuilder.Append('^');
                regexStringBuilder.Append(Regex.Escape(entry));
                regexStringBuilder.Append('$');
                regexStringBuilder.Replace("\\*", ".*");
                regexStringBuilder.Replace("\\?", ".");

                _wildcardEntries.Value.Add(new Regex(regexStringBuilder.ToString(), RegexOptions.CultureInvariant));
            }
            else
            {
                _qualifiedEntries.Value.Add(entry);
            }

        }

        public bool Contains(string moduleName)
        {
            if (!_qualifiedEntries.IsValueCreated && !_wildcardEntries.IsValueCreated)
            {
                // the list is empty
                return false;
            }

            if (_qualifiedEntries.IsValueCreated)
            {
                if (_qualifiedEntries.Value.Contains(moduleName))
                {
                    return true;
                }

                // To handle entries without extension, try removing the extension from the module name and matching that
                string moduleNameWithoutExtension = Path.GetFileNameWithoutExtension(moduleName);
                if (_qualifiedEntries.Value.Contains(moduleNameWithoutExtension))
                {
                    return true;
                }
            }

            if (_wildcardEntries.IsValueCreated)
            {
                foreach (Regex regex in _wildcardEntries.Value)
                {
                    if (regex.IsMatch(moduleName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Clear()
        {
            if (_wildcardEntries == null || _wildcardEntries.IsValueCreated)
            {
                _wildcardEntries = new Lazy<List<Regex>>(() => new List<Regex>());
            }

            if (_qualifiedEntries == null || _qualifiedEntries.IsValueCreated)
            {
                _qualifiedEntries = new Lazy<HashSet<string>>(() => new HashSet<string>(StringComparer.Ordinal));
            }
        }

        public void SetTo(IEnumerable<string> items)
        {
            Clear();
            foreach (string item in items)
            {
                Add(item);
            }
        }
    }
}