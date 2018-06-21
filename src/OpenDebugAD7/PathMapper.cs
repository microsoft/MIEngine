// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDebugAD7
{
    internal class PathMapper
    {
        // List of entries from PDB path -> local path
        private readonly List<KeyValuePair<string, string>> _entries = new List<KeyValuePair<string, string>>();

        public PathMapper(IReadOnlyDictionary<string, string> entries)
        {
            if (entries != null)
            {
                foreach (KeyValuePair<string, string> pair in entries)
                {
                    ValidatePath(pair.Key);
                    ValidatePath(pair.Value);
                    string symbolPath = EncodeKey(pair.Key);
                    string localPath = ExpandPath(pair.Value);

                    _entries.Add(new KeyValuePair<string, string>(symbolPath, localPath));
                }
            }
        }

        /// <summary>
        /// Looks to see if we have a rule redirecting the specified symbol path, and if so return the mapped path. Otherwise return the original.
        /// </summary>
        /// <param name="symbolPath">The incoming path from the engine</param>
        /// <returns>The path to return to the UI</returns>
        public string ResolveSymbolPath(string symbolPath)
        {
            string encodedSymbolPath = EncodeKey(symbolPath);

            foreach (KeyValuePair<string, string> entry in _entries)
            {
                if (encodedSymbolPath.StartsWith(entry.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (entry.Key.Length == encodedSymbolPath.Length) // Full file name match
                        return entry.Value;

                    string remainder = symbolPath.Substring(entry.Key.Length);

                    // Partial match, ensure the match doesn't have partial directory name (i.e. c:\\foo doesn't match c:\\foo-jam entry)
                    if (entry.Key.EndsWith("\\", StringComparison.OrdinalIgnoreCase) ||
                        remainder.StartsWith("\\", StringComparison.OrdinalIgnoreCase) ||
                        remainder.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                    {
                        return Path.Combine(entry.Value, remainder.TrimStart('\\', '/'));
                    }
                }
            }

            return symbolPath;
        }

        private string ExpandPath(string path)
        {
            // Handle home directory
            if (path.StartsWith("~", StringComparison.OrdinalIgnoreCase))
            {
                string homeDirectory = Environment.GetEnvironmentVariable("HOME");

                if (!string.IsNullOrEmpty(homeDirectory))
                {
                    path = homeDirectory + path.Substring(1);
                }
            }

            return path;
        }

        private string EncodeKey(string key)
        {
            // To ensure consistent mappings even if we get the slashes changed to back slashes by the engine, we will swap all the slashes to back slashes
            return key.Replace('/', '\\');
        }

        private void ValidatePath(string symbolPath)
        {
            if (string.IsNullOrEmpty(symbolPath))
                throw new AD7Exception(AD7Resources.Error_SourceFileMapEntryNull);

            if (symbolPath.IndexOf('/') < 0 && symbolPath.IndexOf('\\') < 0 && symbolPath != "~")
            {
                throw new AD7Exception(string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_SourceFileMapEntryInvalid, symbolPath));
            }
        }
    }
}
