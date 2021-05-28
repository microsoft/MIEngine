// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        protected IEnumerable<T> OrderBasedOnDependencies(IEnumerable<T> itemsEnumerable)
        {
            List<T> items = new List<T>(itemsEnumerable);

            // Keep track of times we are stuck in a loop
            int stallCount = 0;

            int i = 0;
            while (i < items.Count)
            {
                T currentItem = items[i];
                IEnumerable<int> dependencyIndexes = GetDependencyIndexes(items, currentItem);
                if (dependencyIndexes == null)
                    continue;

                if (dependencyIndexes.Any(x => x < 0))
                {
                    // Remove item if dependency cannot be resolved in this set of items
                    Debug.WriteLine("ERROR: Cannot find dependency for '{0}'.".FormatWithArgs(GetItemName(currentItem)));

                    // Remove the item and start over
                    items.RemoveAt(i);
                    stallCount = 0;
                    i = 0;
                    continue;
                }

                // Move the current test after any required dependencies.
                // Verify we aren't stuck in an infinite loop
                int lastDependencyIndex = dependencyIndexes.Any() ? dependencyIndexes.Max() : -1;
                if (lastDependencyIndex > i)
                {
                    MoveAfter(items, i, lastDependencyIndex);
                    stallCount++;
                    if (stallCount > (items.Count - i))
                    {
                        Debug.WriteLine("ERROR: Circular test dependency found.");
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
