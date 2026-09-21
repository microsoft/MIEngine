// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine.Natvis
{
    public class VisualizationCache
    {
        private struct VisualizerKey : IEquatable<VisualizerKey>, IComparable<VisualizerKey>
        {
            private string _name;
            private int _threadId;
            private int _level;

            public VisualizerKey(IVariableInformation variable)
            {
                _name = variable.FullName();
                _threadId = variable.Client.Id;
                // Variables only ever come from real (level-bearing) frames; synthetic frames
                // from a Python frame filter expose no locals to visualize. Fall back to 0 to
                // keep the key total in the null case rather than throwing.
                Debug.Assert(variable.ThreadContext.Level.HasValue, "How are we getting a variable from a synthetic thread context?");
                _level = (int)(variable.ThreadContext.Level ?? 0);
            }

            public VisualizerKey(string name, int threadId, int level)
            {
                _name = name;
                _threadId = threadId;
                _level = level;
            }

            public int CompareTo(VisualizerKey other)
            {
                int res = string.Compare(_name, other._name, StringComparison.Ordinal);
                if (res == 0)
                {
                    res = _threadId - other._threadId;
                    if (res == 0)
                    {
                        res = _level - other._level;
                    }
                }
                return res;
            }

            public bool Equals(VisualizerKey other)
            {
                return CompareTo(other) == 0;
            }

            public override bool Equals(object obj)
            {
                return Equals((VisualizerKey)obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        private Dictionary<VisualizerKey, VisualizerWrapper> _cache;

        internal VisualizationCache()
        {
            _cache = new Dictionary<VisualizerKey, VisualizerWrapper>();
        }

        internal void Add(IVariableInformation var)
        {
            if (var is VisualizerWrapper)
            {
                lock (_cache)
                {
                    VisualizerKey key = new VisualizerKey(var);
                    if (!_cache.ContainsKey(key))
                        _cache.Add(key, (var as VisualizerWrapper));
                }
            }
        }

        internal IVariableInformation Lookup(IVariableInformation var)
        {
            if (var is VisualizerWrapper)
            {
                lock (_cache)
                {
                    VisualizerWrapper result = null;
                    _cache.TryGetValue(new VisualizerKey(var), out result);
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Check how this variable should be visualized on refresh
        /// </summary>
        /// <param name="var">the variable to display</param>
        /// <returns>
        ///     1. var if var is not a VisualizationWrapper
        ///     2. null if a VisualizedView that is not in the cache already
        ///     2. Lookup(var) if in the cache
        /// </returns>
        internal IVariableInformation VisualizeOnRefresh(IVariableInformation var)
        {
            if (var is VisualizerWrapper)
            {
                lock (_cache)
                {
                    VisualizerWrapper result = null;
                    if (_cache.TryGetValue(new VisualizerKey(var), out result))
                    {
                        return result;
                    }
                    return null;
                }
            }
            return var;
        }



        internal void Flush()
        {
            lock (_cache)
            {
                _cache.Clear();
            }
        }
    }
}
