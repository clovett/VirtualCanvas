//-----------------------------------------------------------------------
// <copyright file="PriorityQuadTree.QuadNode.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
    /// Original class written by Chris Lovett.  Prioritization and lazy enumeration added by Kael Rowan.
    /// </remarks>
    public partial class PriorityQuadTree<T> : IEnumerable<T>
    {
        // Each node stored in the tree has a position, width & height.
        private class QuadNode
        {
            private Rect quadBounds;
            private QuadNode next; // linked in a circular list.
            private T node; // the actual visual object being stored here.
            private double priority; // the priority of the object being stored here.

            /// <summary>
            /// Construct new QuadNode to wrap the given node with given bounds
            /// </summary>
            /// <param name="node">The node with generic type T</param>
            /// <param name="bounds">The bounds of that node</param>
            /// <param name="priority">The priority of that node</param>
            public QuadNode(T node, Rect bounds, double priority)
            {
                this.node = node;
                this.quadBounds = bounds;
                this.priority = priority;
            }

            /// <summary>
            /// The wrapped node
            /// </summary>
            public T Node
            {
                get { return this.node; }
                set { this.node = value; }
            }

            /// <summary>
            /// The Rect bounds of the node
            /// </summary>
            public Rect Bounds
            {
                get { return this.quadBounds; }
            }

            /// <summary>
            /// The priority of the node
            /// </summary>
            public double Priority
            {
                get { return this.priority; }
            }

            /// <summary>
            /// QuadNodes form a linked list in the Quadrant.
            /// </summary>
            public QuadNode Next
            {
                get { return this.next; }
                set { this.next = value; }
            }

            /// <summary>
            /// Inserts this QuadNode into an existing list and returns the new tail of the list.
            /// </summary>
            /// <param name="tail">The tail of an existing circular linked list of QuadNodes, or <c>null</c> if this is the first.</param>
            /// <returns>The (possibly new) tail of the circular linked list after inserting this QuadNode into it.</returns>
            public QuadNode InsertInto(QuadNode tail)
            {
                if (tail == null)
                {
                    Next = this;
                    tail = this;
                }
                else
                {
                    // link up in circular link list.
                    if (Priority < tail.Priority)
                    {
                        Next = tail.Next;
                        tail.Next = this;
                        tail = this;
                    }
                    else
                    {
                        QuadNode x;
                        for (x = tail; x.Next != tail && Priority < x.Next.Priority; x = x.Next)
                        {
                        }

                        Next = x.Next;
                        x.Next = this;
                    }
                }
                return tail;
            }

            /// <summary>
            /// Walk the linked list of QuadNodes and check them against the given bounds.
            /// </summary>
            /// <param name="bounds">The bounds to test against each node.</param>
            /// <returns>A lazy list of nodes along with the priority of the next node.</returns>
            public IEnumerable<Tuple<QuadNode, double>> GetIntersectingNodes(Rect bounds)
            {
                QuadNode n = this;
                do
                {
                    n = n.Next; // first node.
                    if (bounds.Intersects(n.Bounds) || bounds == InfiniteBounds)
                    {
                        yield return Tuple.Create(n, n != this ? n.Next.Priority : double.NaN);
                    }
                }
                while (n != this);
            }

            /// <summary>
            /// Walk the linked list and test each node against the given bounds.
            /// </summary>
            /// <param name="bounds">Bounds to test.</param>
            /// <returns>Return true if a node in the list intersects the bounds.</returns>
            public bool HasIntersectingNodes(Rect bounds)
            {
                QuadNode n = this;
                do
                {
                    n = n.Next; // first node.
                    if (bounds.Intersects(n.Bounds) || bounds == InfiniteBounds)
                    {
                        return true;
                    }
                }
                while (n != this);

                return false;
            }

             /// <summary>
            /// Walk the linked list and test each node against the given bounds.
            /// </summary>
            /// <param name="bounds">Bounds to test.</param>
            /// <returns>Return true if a node in the list is inside the bounds.</returns>
            public bool HasNodesInside(Rect bounds)
            {
                QuadNode n = this;
                do
                {
                    n = n.Next; // first node.
                    if (bounds.Contains(n.Bounds) || bounds == InfiniteBounds)
                    {
                        return true;
                    }
                }
                while (n != this);

                return false;
            }
        }
    }
}
