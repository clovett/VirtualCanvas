//-----------------------------------------------------------------------
// <copyright file="PriorityQuadTree.Quadrant.cs" company="Microsoft">
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
    /// Original class written by Chris Lovett.  Prioritization and lazy enumeration added by Kael Rowan.
    /// </remarks>
    public partial class PriorityQuadTree<T> : IEnumerable<T>
    {
        /// <summary>
        /// The canvas is split up into four Quadrants and objects are stored in the quadrant that contains them
        /// and each quadrant is split up into four child Quadrants recurrsively.  Objects that overlap more than
        /// one quadrant are stored in the this.nodes list for this Quadrant.
        /// </summary>
        private class Quadrant : IEnumerable<QuadNode>
        {
            private Rect quadrantBounds; // quadrant bounds.
            private double maxDescendantPriority = Double.NegativeInfinity; // the maximum priority of all nodes within this quadrant.
            private int count;

            private QuadNode nodes; // nodes that overlap the sub quadrant boundaries.

            // The quadrant is subdivided when nodes are inserted that are
            // completely contained within those subdivisions.
            private Quadrant topLeftQuadrant;
            private Quadrant topRightQuadrant;
            private Quadrant bottomLeftQuadrant;
            private Quadrant bottomRightQuadrant;

            /// <summary>
            /// Construct new Quadrant with a given bounds all nodes stored inside this quadrant
            /// will fit inside this bounds.
            /// </summary>
            /// <param name="bounds">The bounds of this quadrant</param>
            public Quadrant(Rect bounds)
            {
                this.quadrantBounds = bounds;
            }

            /// <summary>
            /// Insert the given node.
            /// </summary>
            /// <param name="node">The wrapped node.</param>
            /// <param name="bounds">The bounds of that node.</param>
            /// <param name="priority">The priority of that node.</param>
            /// <returns>The quadrant that ultimately holds the node.</returns>
            internal Quadrant Insert(T node, Rect bounds, double priority, int quadrantDepth)
            {
                this.maxDescendantPriority = Math.Max(this.maxDescendantPriority, priority);
                this.count++;

                Quadrant child = null;

                // Only drill down the tree for positive sized bounds, otherwise we could drill forever.
                // Todo: We can remove this restriction if we choose to only split quads when "full".
                if (quadrantDepth <= MaxTreeDepth && (bounds.Width > 0 || bounds.Height > 0))
                {
                    double w = this.quadrantBounds.Width / 2;
                    double h = this.quadrantBounds.Height / 2;

                    // assumption that the Rect struct is almost as fast as doing the operations
                    // manually since Rect is a value type.
                    Rect topLeft = new Rect(this.quadrantBounds.Left, this.quadrantBounds.Top, w, h);
                    Rect topRight = new Rect(this.quadrantBounds.Left + w, this.quadrantBounds.Top, w, h);
                    Rect bottomLeft = new Rect(this.quadrantBounds.Left, this.quadrantBounds.Top + h, w, h);
                    Rect bottomRight = new Rect(this.quadrantBounds.Left + w, this.quadrantBounds.Top + h, w, h);

                    // See if any child quadrants completely contain this node.
                    if (topLeft.Contains(bounds) || bounds == InfiniteBounds)
                    {
                        if (this.topLeftQuadrant == null)
                        {
                            this.topLeftQuadrant = new Quadrant(topLeft);
                        }
                        child = this.topLeftQuadrant;
                    }
                    else if (topRight.Contains(bounds) || bounds == InfiniteBounds)
                    {
                        if (this.topRightQuadrant == null)
                        {
                            this.topRightQuadrant = new Quadrant(topRight);
                        }
                        child = this.topRightQuadrant;
                    }
                    else if (bottomLeft.Contains(bounds) || bounds == InfiniteBounds)
                    {
                        if (this.bottomLeftQuadrant == null)
                        {
                            this.bottomLeftQuadrant = new Quadrant(bottomLeft);
                        }
                        child = this.bottomLeftQuadrant;
                    }
                    else if (bottomRight.Contains(bounds) || bounds == InfiniteBounds)
                    {
                        if (this.bottomRightQuadrant == null)
                        {
                            this.bottomRightQuadrant = new Quadrant(bottomRight);
                        }
                        child = this.bottomRightQuadrant;
                    }
                }

                if (child != null)
                {
                    return child.Insert(node, bounds, priority, quadrantDepth + 1);
                }

                QuadNode n = new QuadNode(node, bounds, priority);
                this.nodes = n.InsertInto(this.nodes);
                return this;
            }

            /// <summary>
            /// Removes the first occurance of the given node from this quadrant or any child quadrants within the search bounds.
            /// </summary>
            /// <param name="node">The node to remove.</param>
            /// <param name="bounds">The bounds to search within.</param>
            /// <returns><c>true</c> if the node was found and removed; otherwise, <c>false</c>.</returns>
            internal bool Remove(T node, Rect bounds)
            {
                bool nodeRemoved = false;
                if (RemoveNode(node))
                {
                    nodeRemoved = true;
                }
                else
                {
                    double w = this.quadrantBounds.Width / 2;
                    double h = this.quadrantBounds.Height / 2;

                    // assumption that the Rect struct is almost as fast as doing the operations
                    // manually since Rect is a value type.
                    Rect topLeft = new Rect(this.quadrantBounds.Left, this.quadrantBounds.Top, w, h);
                    Rect topRight = new Rect(this.quadrantBounds.Left + w, this.quadrantBounds.Top, w, h);
                    Rect bottomLeft = new Rect(this.quadrantBounds.Left, this.quadrantBounds.Top + h, w, h);
                    Rect bottomRight = new Rect(this.quadrantBounds.Left + w, this.quadrantBounds.Top + h, w, h);

                    if (this.topLeftQuadrant != null && (topLeft.Intersects(bounds) || bounds == InfiniteBounds) && this.topLeftQuadrant.Remove(node, bounds))
                    {
                        if (this.topLeftQuadrant.count == 0)
                        {
                            this.topLeftQuadrant = null;
                        }

                        nodeRemoved = true;
                    }
                    else if (this.topRightQuadrant != null && (topRight.Intersects(bounds) || bounds == InfiniteBounds) && this.topRightQuadrant.Remove(node, bounds))
                    {
                        if (this.topRightQuadrant.count == 0)
                        {
                            this.topRightQuadrant = null;
                        }

                        nodeRemoved = true;
                    }
                    else if (this.bottomLeftQuadrant != null && (bottomLeft.Intersects(bounds) || bounds == InfiniteBounds) && this.bottomLeftQuadrant.Remove(node, bounds))
                    {
                        if (this.bottomLeftQuadrant.count == 0)
                        {
                            this.bottomLeftQuadrant = null;
                        }

                        nodeRemoved = true;
                    }
                    else if (this.bottomRightQuadrant != null && (bottomRight.Intersects(bounds) || bounds == InfiniteBounds) && this.bottomRightQuadrant.Remove(node, bounds))
                    {
                        if (this.bottomRightQuadrant.count == 0)
                        {
                            this.bottomRightQuadrant = null;
                        }

                        nodeRemoved = true;
                    }
                }

                if (nodeRemoved)
                {
                    this.count--;
                    this.maxDescendantPriority = this.CalculateQuadrantPriority();
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Returns all nodes in this quadrant that intersect the given bounds.
            /// The nodes are returned in pretty much random order as far as the caller is concerned.
            /// </summary>
            /// <param name="bounds">The bounds that contains the nodes you want returned, pass InfiniteBounds to get all items in this quadrant.</param>
            /// <returns>A lazy list of nodes along with the new potential of this quadrant</returns>
            internal IEnumerable<Tuple<QuadNode, double>> GetIntersectingNodes(Rect bounds)
            {
                double w = this.quadrantBounds.Width / 2;
                double h = this.quadrantBounds.Height / 2;

                // assumption that the Rect struct is almost as fast as doing the operations
                // manually since Rect is a value type.
                Rect topLeft = new Rect(this.quadrantBounds.Left, this.quadrantBounds.Top, w, h);
                Rect topRight = new Rect(this.quadrantBounds.Left + w, this.quadrantBounds.Top, w, h);
                Rect bottomLeft = new Rect(this.quadrantBounds.Left, this.quadrantBounds.Top + h, w, h);
                Rect bottomRight = new Rect(this.quadrantBounds.Left + w, this.quadrantBounds.Top + h, w, h);

                // Create a priority queue based on the potential of our nodes and our quads.
                var queue = new PriorityQueue<IEnumerator<Tuple<QuadNode, double>>, double>(true);

                if (this.nodes != null)
                {
                    queue.Enqueue(this.nodes.GetIntersectingNodes(bounds).GetEnumerator(), this.nodes.Next.Priority);
                }

                if (this.topLeftQuadrant != null && (topLeft.Intersects(bounds) || bounds == InfiniteBounds))
                {
                    queue.Enqueue(this.topLeftQuadrant.GetIntersectingNodes(bounds).GetEnumerator(), this.topLeftQuadrant.maxDescendantPriority);
                }

                if (this.topRightQuadrant != null && (topRight.Intersects(bounds) || bounds == InfiniteBounds))
                {
                    queue.Enqueue(this.topRightQuadrant.GetIntersectingNodes(bounds).GetEnumerator(), this.topRightQuadrant.maxDescendantPriority);
                }

                if (this.bottomLeftQuadrant != null && (bottomLeft.Intersects(bounds) || bounds == InfiniteBounds))
                {
                    queue.Enqueue(this.bottomLeftQuadrant.GetIntersectingNodes(bounds).GetEnumerator(), this.bottomLeftQuadrant.maxDescendantPriority);
                }

                if (this.bottomRightQuadrant != null && (bottomRight.Intersects(bounds) || bounds == InfiniteBounds))
                {
                    queue.Enqueue(this.bottomRightQuadrant.GetIntersectingNodes(bounds).GetEnumerator(), this.bottomRightQuadrant.maxDescendantPriority);
                }

                // Then just loop through the queue.
                while (queue.Count > 0)
                {
                    // Grab the enumerator with the highest potential.
                    var enumerator = queue.Dequeue().Key;
                    if (enumerator.MoveNext())
                    {
                        // Get the current node and its new potential from the enumerator.
                        var current = enumerator.Current;
                        var node = current.Item1;
                        var potential = current.Item2;

                        // Determine our new potential.
                        var newPotential = queue.Count > 0 ? !Double.IsNaN(potential) ? Math.Max(potential, queue.Peek().Value) : queue.Peek().Value : potential;

                        // It might be the case that the actual intersecting node has less priority than our remaining potential.
                        if (newPotential > node.Priority)
                        {
                            // Store it for later in a container containing only it with no further potential.
                            var store = Enumerable.Repeat(Tuple.Create(node, double.NaN), 1).GetEnumerator();

                            // Enqueue the container at the correct position.
                            queue.Enqueue(store, node.Priority);
                        }
                        else
                        {
                            // Return it to our parent along with our new potential.
                            yield return Tuple.Create(node, newPotential);
                        }

                        // If this enumerator has some more potential then re-enqueue it.
                        if (!Double.IsNaN(potential))
                        {
                            queue.Enqueue(enumerator, potential);
                        }
                    }
                }
            }

            /// <summary>
            /// Return true if there are any nodes in this Quadrant are inside the given bounds.
            /// </summary>
            /// <param name="bounds">The bounds to test</param>
            /// <returns>True if : has nodes inside. false otherwise.</returns>
            internal bool HasNodesInside(Rect bounds)
            {
                double w = this.quadrantBounds.Width / 2;
                double h = this.quadrantBounds.Height / 2;

                // assumption that the Rect struct is almost as fast as doing the operations
                // manually since Rect is a value type.
                Rect topLeft = new Rect(this.quadrantBounds.Left, this.quadrantBounds.Top, w, h);
                Rect topRight = new Rect(this.quadrantBounds.Left + w, this.quadrantBounds.Top, w, h);
                Rect bottomLeft = new Rect(this.quadrantBounds.Left, this.quadrantBounds.Top + h, w, h);
                Rect bottomRight = new Rect(this.quadrantBounds.Left + w, this.quadrantBounds.Top + h, w, h);

                if (this.nodes != null && this.nodes.HasNodesInside(bounds))
                {
                    return true;
                }

                if (this.topLeftQuadrant != null && (topLeft.Contains(bounds) || bounds == InfiniteBounds) && this.topLeftQuadrant.HasNodesInside(bounds))
                {
                    return true;
                }

                if (this.topRightQuadrant != null && (topRight.Contains(bounds) || bounds == InfiniteBounds) && this.topRightQuadrant.HasNodesInside(bounds))
                {
                    return true;
                }

                if (this.bottomLeftQuadrant != null && (bottomLeft.Contains(bounds) || bounds == InfiniteBounds) && this.bottomLeftQuadrant.HasNodesInside(bounds))
                {
                    return true;
                }

                if (this.bottomRightQuadrant != null && (bottomRight.Contains(bounds) || bounds == InfiniteBounds) && this.bottomRightQuadrant.HasNodesInside(bounds))
                {
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Return true if there are any nodes in this Quadrant that intersect the given bounds.
            /// </summary>
            /// <param name="bounds">The bounds to test</param>
            /// <returns>True if : has intersecting nodes. false otherwise.</returns>
            internal bool HasIntersectingNodes(Rect bounds)
            {
                double w = this.quadrantBounds.Width / 2;
                double h = this.quadrantBounds.Height / 2;

                // assumption that the Rect struct is almost as fast as doing the operations
                // manually since Rect is a value type.
                Rect topLeft = new Rect(this.quadrantBounds.Left, this.quadrantBounds.Top, w, h);
                Rect topRight = new Rect(this.quadrantBounds.Left + w, this.quadrantBounds.Top, w, h);
                Rect bottomLeft = new Rect(this.quadrantBounds.Left, this.quadrantBounds.Top + h, w, h);
                Rect bottomRight = new Rect(this.quadrantBounds.Left + w, this.quadrantBounds.Top + h, w, h);

                if (this.nodes != null && this.nodes.HasIntersectingNodes(bounds))
                {
                    return true;
                }

                if (this.topLeftQuadrant != null && (topLeft.Intersects(bounds) || bounds == InfiniteBounds) && this.topLeftQuadrant.HasIntersectingNodes(bounds))
                {
                    return true;
                }

                if (this.topRightQuadrant != null && (topRight.Intersects(bounds) || bounds == InfiniteBounds) && this.topRightQuadrant.HasIntersectingNodes(bounds))
                {
                    return true;
                }

                if (this.bottomLeftQuadrant != null && (bottomLeft.Intersects(bounds) || bounds == InfiniteBounds) && this.bottomLeftQuadrant.HasIntersectingNodes(bounds))
                {
                    return true;
                }

                if (this.bottomRightQuadrant != null && (bottomRight.Intersects(bounds) || bounds == InfiniteBounds) && this.bottomRightQuadrant.HasIntersectingNodes(bounds))
                {
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Remove the given node from this Quadrant.(non-recursive)
            /// </summary>
            /// <param name="node">The node to remove</param>
            /// <returns>Returns true if the node was found and removed.</returns>
            private bool RemoveNode(T node)
            {
                bool rc = false;
                if (this.nodes != null)
                {
                    QuadNode p = this.nodes;
                    while (!Object.Equals(p.Next.Node, node) && p.Next != this.nodes)
                    {
                        p = p.Next;
                    }
                    if (Object.Equals(p.Next.Node, node))
                    {
                        rc = true;
                        QuadNode n = p.Next;
                        if (p == n)
                        {
                            // list goes to empty
                            this.nodes = null;
                        }
                        else
                        {
                            if (this.nodes == n)
                            {
                                this.nodes = p;
                            }

                            p.Next = n.Next;
                        }
                    }
                }
                return rc;
            }

            /// <summary>
            /// This call assumes that the priority is correctly set on the subquadrants
            /// It returns the priority for the quadrant based on Math.Max(this.nodes.Priority, subQuads' Priorities)
            /// </summary>
            /// <returns>the priority for the quadrant</returns>
            private double CalculateQuadrantPriority()
            {
                double quadPriority = Double.NegativeInfinity;
                if (this.nodes != null)
                {
                    quadPriority = this.nodes.Next.Priority;
                }

                if (this.topLeftQuadrant != null)
                {
                    quadPriority = Math.Max(quadPriority, this.topLeftQuadrant.maxDescendantPriority);
                }

                if (this.topRightQuadrant != null)
                {
                    quadPriority = Math.Max(quadPriority, this.topRightQuadrant.maxDescendantPriority);
                }

                if (this.bottomLeftQuadrant != null)
                {
                    quadPriority = Math.Max(quadPriority, this.bottomLeftQuadrant.maxDescendantPriority);
                }

                if (this.bottomRightQuadrant != null)
                {
                    quadPriority = Math.Max(quadPriority, this.bottomRightQuadrant.maxDescendantPriority);
                }

                return quadPriority;
            }

            /// <summary>
            /// Enumerates over all nodes within this quadrant in random order.
            /// </summary>
            /// <returns>
            /// Enumerator that enumerates over all its nodes.
            /// </returns>
            public IEnumerator<QuadNode> GetEnumerator()
            {
                var queue = new Queue<Quadrant>();
                queue.Enqueue(this);

                while (queue.Count > 0)
                {
                    var quadrant = queue.Dequeue();
                    if (quadrant.nodes != null)
                    {
                        var start = quadrant.nodes;
                        var n = quadrant.nodes;
                        do
                        {
                            n = n.Next;
                            yield return n;
                        }
                        while (n != start);
                    }

                    if (quadrant.topLeftQuadrant != null)
                    {
                        queue.Enqueue(quadrant.topLeftQuadrant);
                    }

                    if (quadrant.topRightQuadrant != null)
                    {
                        queue.Enqueue(quadrant.topRightQuadrant);
                    }

                    if (quadrant.bottomLeftQuadrant != null)
                    {
                        queue.Enqueue(quadrant.bottomLeftQuadrant);
                    }

                    if (quadrant.bottomRightQuadrant != null)
                    {
                        queue.Enqueue(quadrant.bottomRightQuadrant);
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
