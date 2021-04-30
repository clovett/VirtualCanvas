//-----------------------------------------------------------------------
// <copyright file="PriorityQuadTree.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VirtualCanvasDemo.Helpers;

namespace VirtualCanvasDemo.QuadTree
{
    /// <summary>
    /// This class efficiently stores and lazily retrieves arbitrarily sized and positioned objects in a prioritized order in a quad-tree data structure.
    /// This can be used to do efficient hit detection or visiblility checks on objects in a two dimensional space.
    /// The object does not need to implement any special interface because the Rect Bounds of those objects is handled as a separate argument to Insert.
    /// </summary>
    /// <remarks>
    /// Original class written by Chris Lovett.
    /// Prioritization and lazy enumeration added by Kael Rowan.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "By design")]
    public partial class PriorityQuadTree<T> : IEnumerable<T>
    {
        private Rect publicExtent = Rect.Empty;
        private Rect realExtent = new Rect(0.0, 0.0, 0.0, 0.0);
        private Quadrant root;
        private HashSet<T> emptyItems = new HashSet<T>();
        private Rect emptyBounds = new Rect(0, 0, 0, 0);

        // The MaxTreeDepth limis is required since recursion calls in this call
        // can go that deep if item bounds (height or width) are very small compared to Extent (height or width).
        // almost like a point on the extent.
        // The max depth is put to prevent stack overflow exception in some of the recursive calls we make.
        // This recursive implementation will be changed to a non-recursive version to prevent having to deal with stack overflow
        // issues all-together
        // With a value of 50 the item bounds can be 2^-50 times the extent before the tree stops growing in height.
        // (this is with the assumption that there no previous calls in the stacktrace.)
        private const int MaxTreeDepth = 50;

        /// <summary>
        /// This determines the overall quad-tree indexing strategy.
        /// Changing this bounds is expensive since it has to re-divide the entire thing - like a re-hash operation.
        /// </summary>
        public Rect Extent
        {
            get
            {
                return this.publicExtent;
            }
            set
            {
                if (!(value.Top >= double.MinValue &&
                      value.Top <= double.MaxValue &&
                      value.Left >= double.MinValue &&
                      value.Left <= double.MaxValue &&
                      value.Width <= double.MaxValue &&
                      value.Height <= double.MaxValue) &&
                      value != Rect.Empty)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this.publicExtent = value;

                // Don't recalculate extent if the value has shrunk, unless it has shrunk significantly
                if (value != Rect.Empty && (!this.realExtent.Contains(value) || (value.Width * value.Height * 2) < (this.realExtent.Width * this.realExtent.Height)))
                {
                    this.realExtent = value;
                    ReIndex();
                }
            }
        }

        /// <summary>
        /// Insert an item with given bounds and priority into this QuadTree.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        /// <param name="bounds">The bounds of this item.</param>
        /// <param name="priority">The priority to return this item before others in query results.</param>
        public void Insert(T item, Rect bounds, double priority)
        {
            if (!bounds.IsDefined())
            {
                throw new ArgumentOutOfRangeException("bounds");
            }
            if (bounds == emptyBounds)
            {
                emptyItems.Add(item);
                return;
            }

            if (this.root == null)
            {
                this.root = new Quadrant(this.realExtent);
            }

            if (Double.IsNaN(priority))
            {
                priority = Double.NegativeInfinity;
            }

            this.root.Insert(item, bounds, priority, 1);
        }

        /// <summary>
        /// Gets whether any items are fully inside the given bounds.
        /// </summary>
        /// <param name="bounds">The bounds to test.</param>
        /// <returns><c>true</c> if any items are inside the given bounds; otherwise, <c>false</c>.</returns>
        public bool HasItemsInside(Rect bounds)
        {
            if (!bounds.IsDefined())
            {
                throw new ArgumentOutOfRangeException("bounds");
            }
            if (bounds == emptyBounds && emptyItems.Count > 0)
            {
                return true;
            }

            if (this.root != null)
            {
                return this.root.HasNodesInside(bounds);
            }
            return false;
        }

        /// <summary>
        /// Get a list of the items that are fully inside the given bounds.
        /// </summary>
        /// <param name="bounds">The bounds to test.</param>
        /// <returns>
        /// List of zero or more items that are inside the given bounds, returned in the order given by the priority assigned during Insert.
        /// </returns>
        public IEnumerable<T> GetItemsInside(Rect bounds)
        {
            if (!bounds.IsDefined())
            {
                throw new ArgumentOutOfRangeException();
            }
            if ((bounds.Contains(emptyBounds) || bounds == InfiniteBounds) && emptyItems.Count > 0)
            {
                foreach (T item in emptyItems)
                {
                    yield return item;
                }
            }

            if (this.root != null)
            {
                foreach (var node in this.root.GetIntersectingNodes(bounds))
                {
                    if (bounds.Contains(node.Item1.Bounds) || bounds == InfiniteBounds)
                    {
                        yield return node.Item1.Node;
                    }
                }
            }
        }

        /// <summary>
        /// Gets whether any items intersect the given bounds.
        /// </summary>
        /// <param name="bounds">The bounds to test.</param>
        /// <returns><c>true</c> if any items intersect the given bounds; otherwise, <c>false</c>.</returns>
        public bool HasItemsIntersecting(Rect bounds)
        {
            if (!bounds.IsDefined())
            {
                throw new ArgumentOutOfRangeException("bounds");
            }

            if ((bounds.Contains(emptyBounds) || bounds == InfiniteBounds) && emptyItems.Count > 0)
            {
                return true;
            }

            if (this.root != null)
            {
                return this.root.HasIntersectingNodes(bounds);
            }
            return false;
        }

        /// <summary>
        /// Get list of nodes that intersect the given bounds.
        /// </summary>
        /// <param name="bounds">The bounds to test.</param>
        /// <returns>The list of nodes intersecting the given bounds.</returns>
        public IEnumerable<T> GetItemsIntersecting(Rect bounds)
        {
            if (!bounds.IsDefined())
            {
                throw new ArgumentOutOfRangeException();
            }

            if ((bounds.IntersectsWith(emptyBounds) || bounds == InfiniteBounds) && emptyItems.Count > 0)
            {
                foreach (T item in emptyItems)
                {
                    yield return item;
                }
            }

            if (this.root != null)
            {
                foreach (var node in this.root.GetIntersectingNodes(bounds))
                {
                    yield return node.Item1.Node;
                }
            }
        }

        /// <summary>
        /// Removes the first instance of the given item from the tree (if it exists) by searching through the entire tree for the item.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <remarks>
        /// This overload does a full search through the entire tree for the item.
        /// Clients should instead call the overload that takes a <see cref="Rect"/> if the bounds of the item are known.
        /// </remarks>
        /// <returns>
        /// True, if removed.
        /// False, otherwise.
        /// </returns>
        public virtual bool Remove(T item)
        {
            return Remove(item, InfiniteBounds);
        }

        /// <summary>
        /// This bounds is guarenteed to contain every item in the quad tree.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        internal static readonly Rect InfiniteBounds = new Rect(Double.NegativeInfinity, Double.NegativeInfinity, Double.PositiveInfinity, Double.PositiveInfinity);

        /// <summary>
        /// Removes the first instance of the given item that intersects the given bounds from the tree (if it exists).
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <param name="bounds">The bounds within to search for the item.</param>
        /// <returns>
        /// True, if removed.
        /// False, otherwise.
        /// </returns>
        public bool Remove(T item, Rect bounds)
        {
            if (!bounds.IsDefined())
            {
                throw new ArgumentOutOfRangeException("bounds");
            }
            if (emptyItems.Contains(item))
            {
                emptyItems.Remove(item);
                return true;
            }
            else
            {
                if (this.root != null)
                {
                    return this.root.Remove(item, bounds);
                }
            }
            return false;
        }

        /// <summary>
        /// Removes all nodes from the tree.
        /// </summary>
        public virtual void Clear()
        {
            this.root = null;
            this.emptyItems.Clear();
        }

        /// <summary>
        /// Rebuild all the Quadrants according to the current QuadTree Bounds.
        /// </summary>
        private void ReIndex()
        {
            Quadrant oldRoot = this.root;
            this.root = new Quadrant(this.realExtent);
            if (oldRoot != null)
            {
                foreach (var node in oldRoot.GetIntersectingNodes(this.realExtent))
                {
                    // Todo: It would be more efficient if we added a code path that allowed reuse of the QuadNode wrappers.
                    Insert(node.Item1.Node, node.Item1.Bounds, node.Item1.Priority);
                }
            }
        }

        /// <summary>
        /// Returns all items in the tree in unspecified order.
        /// </summary>
        /// <returns>An enumerator over all items in the tree in random order.</returns>
        /// <remarks>To get all items in the tree in prioritized-order then simply call <see cref="GetItemsInside"/> with an infinitely large rectangle.</remarks>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var node in emptyItems)
            {
                yield return node;
            }
            if (this.root != null)
            {
                foreach (var node in this.root)
                {
                    yield return node.Node;
                }
            }
        }

        /// <summary>
        /// Returns all items in the tree in unspecified order.
        /// </summary>
        /// <returns>An enumerator over all items in the tree in random order.</returns>
        /// <remarks>To get all items in the tree in prioritized-order then simply call <see cref="GetItemsInside"/> with an infinitely large rectangle.</remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
