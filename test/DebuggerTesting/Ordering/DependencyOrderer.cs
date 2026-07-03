// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DebuggerTesting.Ordering
{
    /// <summary>
    /// Base class that provides generic way to order items that declare their
    /// dependency. This removes items that can't fullfil their dependencies as
    /// well as items with circular dependencies.
    /// </summary>
    public abstract class DependencyOrderer<T, TKey>
    {
        /// <summary>
        /// Reports a diagnostic message produced while ordering items. The default
        /// implementation discards the message; derived classes should override to
        /// route the message to whatever logging facility is available in their
        /// hosting environment (e.g. xUnit's <c>IMessageSink</c>).
        /// </summary>
        protected virtual void LogDiagnostic(string message)
        {
        }

        protected IEnumerable<T> OrderBasedOnDependencies(IEnumerable<T> itemsEnumerable)
        {
            List<T> items = new List<T>(itemsEnumerable);

            // Keep track of times we are stuck in a loop
            int stallCount = 0;

            int i = 0;
            while (i < items.Count)
            {
                T currentItem = items[i];
                // Materialize once: GetDependencyIndexes returns a lazy Select(...),
                // and we both filter it and count it below. Treat null (no dependency
                // metadata) the same as an empty list so the loop always advances —
                // otherwise `continue` would leave `i` unchanged and loop forever.
                IList<int> dependencyIndexes = (IList<int>)GetDependencyIndexes(items, currentItem)?.ToList() ?? Array.Empty<int>();

                // Ignore dependencies that aren't present in the current item set
                // (for example, when the user filtered the run with `dotnet test --filter`
                // or via VS Test Explorer). The orderer's job is to order items, not to
                // drop them — silently removing the dependent test makes filtered runs
                // appear to match nothing. If the dependency is genuinely required at
                // runtime, the test will fail with an actionable error rather than
                // vanishing from the run.
                IList<int> presentDependencyIndexes = dependencyIndexes.Where(x => x >= 0).ToList();
                if (presentDependencyIndexes.Count < dependencyIndexes.Count)
                {
                    LogDiagnostic("WARNING: Missing dependency for '{0}'; running anyway.".FormatWithArgs(GetItemName(currentItem)));
                }

                // Move the current test after any required dependencies.
                // Verify we aren't stuck in an infinite loop
                int lastDependencyIndex = presentDependencyIndexes.Count > 0 ? presentDependencyIndexes.Max() : -1;
                if (lastDependencyIndex > i)
                {
                    MoveAfter(items, i, lastDependencyIndex);
                    stallCount++;
                    if (stallCount > (items.Count - i))
                    {
                        LogDiagnostic("ERROR: Circular test dependency found.");
                        // Based on the stall count, the items at the end of the list
                        // have a circular reference. Remove them all.
                        items.RemoveRange(i, items.Count - i);
                        break;
                    }
                }
                else
                {
                    stallCount = 0;
                    i++;
                }
            }

            return items;
        }

        private static void MoveAfter(IList list, int itemIndex, int afterIndex)
        {
            while (itemIndex < afterIndex)
            {
                SwapItems(list, itemIndex, itemIndex + 1);
                itemIndex += 1;
            }
        }

        private static void SwapItems(IList list, int indexA, int indexB)
        {
            object item = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = item;
        }

        #region Dependency Helpers

        private IEnumerable<int> GetDependencyIndexes(IList<T> items, T currentItem)
        {
            IEnumerable<TKey> dependencies = GetDependencies(currentItem);
            return dependencies?.Select(x => GetIndexOfDependency(items, x));
        }

        protected abstract IEnumerable<TKey> GetDependencies(T currentItem);

        protected abstract int GetIndexOfDependency(IList<T> items, TKey dependency);

        protected virtual string GetItemName(T item)
        {
            return item.ToString();
        }

        #endregion
    }
}
