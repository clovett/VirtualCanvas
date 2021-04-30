//-----------------------------------------------------------------------
// <copyright file="SortedCollection.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace VirtualCanvasDemo.Helpers
{
    /// <summary>
    /// This class guarantees that items will be sorted in priority order and provides efficient array indexed access to the sorted list.
    /// If priorities are the same, then it also preserves the order in which the items were added.
    /// </summary>
    internal class SortedCollection<T> : IEnumerable<T>
    {
        /// <summary>
        /// number assigned to an item on insertion
        /// </summary>
        private int addIndex;

        /// <summary>
        /// Stores items in sorted order
        /// </summary>
        private SortedDictionary<ItemKey, T> sortedItems = new SortedDictionary<ItemKey, T>(new ItemKeyComparer());

        /// <summary>
        /// So we can quickly look up the previous key of an item we added already.
        /// </summary>
        private Dictionary<T, ItemKey> indexes = new Dictionary<T, ItemKey>();

        /// <summary>
        /// Cached list of items in sorted order.
        /// </summary>
        private List<T> cachedList;

        /// <summary>
        /// Adds an item according in order of Priority
        /// </summary>
        /// <remarks>
        /// Duplicate items can be added.
        /// </remarks>
        internal void Add(T item, int priority)
        {
            if (addIndex == int.MaxValue - 1)
            {
                // we've done so many add/removes on this list that the addIndex is about to wrap around, so now we need to force a full re-index.
                var saved = sortedItems;
                sortedItems = new SortedDictionary<ItemKey, T>();
                Clear();
                foreach (var pair in sortedItems)
                {
                    ItemKey key = pair.Key;
                    T item2 = pair.Value;
                    InternalAdd(item, key.Priority);
                }
            }

            InternalAdd(item, priority);
        }

        private void InternalAdd(T item, int priority)
        {
            int index = addIndex++;
            var key = new ItemKey(priority, index);
            indexes[item] = key;
            this.sortedItems[key] = item;
            cachedList = null;
        }

        /// <summary>
        /// Updates priority of an already added item
        /// </summary>
        /// <remarks>
        /// Results in a no-op if no item found.
        /// </remarks>
        /// <returns>
        /// true if priority was updated.
        /// false if priority was not updated. (this happens if the item priority is equal to the priority parameter)
        /// </returns>
        internal bool Update(T item, int priority)
        {
            ItemKey oldKey = null;
            if (!indexes.TryGetValue(item, out oldKey))
            {
                Debug.Fail("Update called but item is not in the list");
                return false;
            }

            if (oldKey.Priority == priority)
            {
                return false;
            }
            else
            {
                this.sortedItems.Remove(oldKey);
                // Since we are changing the priority, we also have to give it a new index since we cannot
                // guarentee that the new priority + old index is not already used.
                InternalAdd(item, priority);
                return true;
            }
        }

        /// <summary>
        /// All items that are equal to this parameter will be removed, irrespective of the priority.
        /// During commit, all remove calls are executed after all insert calls(if any) are processed.
        /// Attempting to remove an item that is not inserted results in a no-op.
        /// </summary>
        internal void Remove(T item)
        {
            if (this.sortedItems.Count > 0)
            {
                ItemKey oldKey = null;
                if (!indexes.TryGetValue(item, out oldKey))
                {
                    Debug.Fail("Remove called but item is not in the list");
                    return;
                }
                cachedList = null;
                indexes.Remove(item);
                this.sortedItems.Remove(oldKey);
            }
        }

        /// <summary>
        /// Count of items in the Collection.
        /// If duplicate items are added, they will be counted as many times.
        /// </summary>
        internal int Count
        {
            get
            {
                return this.sortedItems.Count;
            }
        }

        /// <summary>
        /// Returns the item at that index, sorted by (priority, insertion order)
        /// </summary>
        internal T this[int index]
        {
            get
            {
                if (cachedList == null)
                {
                    cachedList = new List<T>(this.sortedItems.Values);
                }
                return cachedList[index];
            }
        }

        /// <summary>
        /// Enumerator to go over items
        /// </summary>
        /// <returns>Enumerator over items</returns>
        /// <remarks>
        /// Call Commit first, to get items in sorted order.
        /// </remarks>
        public IEnumerator<T> GetEnumerator()
        {
            return this.sortedItems.Values.GetEnumerator();
        }

        /// <summary>
        /// Enumerator to go over items
        /// </summary>
        /// <returns>Enumerator over sorted items</returns>
        /// <remarks>
        /// Call Commit first, to get items in sorted order.
        /// </remarks>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #region Item Order Comparer
        /// <summary>
        /// Comparer for items
        /// </summary>
        private class ItemKeyComparer : IComparer<ItemKey>
        {
            public int Compare(ItemKey x, ItemKey y)
            {
                // Avoid subtracting. This will avoid underflow/overflow issues.
                if (x.Priority < y.Priority)
                {
                    // Comparing Priority
                    return -1;
                }
                else if (x.Priority > y.Priority)
                {
                    // Comparing Priority
                    return 1;
                }
                else
                {
                    // Priorities are the same. Compare the unique AddIndexes.
                    // AddIndex is guaranteed to be positive. Subracting will not underflow/overflow.
                    return x.AddIndex - y.AddIndex;
                }
            }
        }

        internal class ItemKey
        {
            internal ItemKey(int priority, int addIndex)
            {
                this.Priority = priority;
                this.AddIndex = addIndex;
            }

            internal int Priority { get; set; }
            internal int AddIndex { get; set; }
        }
        #endregion

        internal void Clear()
        {
            this.sortedItems.Clear();
            this.indexes.Clear();
            this.cachedList = null;
            this.addIndex = 0;
        }
    }
}
