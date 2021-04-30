//-----------------------------------------------------------------------
// <copyright file="ISpatialIndex.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;

namespace VirtualCanvasDemo.Interfaces
{
    /// <summary>
    /// Interface for implementing different types of QuadTree type data structures for fast
    /// lookup of items by their rectangular bounds.  The interface provides one batch event to
    /// communicate when items have changed.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public interface ISpatialIndex : IEnumerable<ISpatialItem>
    {
        /// <summary>
        /// This event is raised whenever the collection is changed or the index is updated
        /// </summary>
        event EventHandler Changed;

        /// <summary>
        /// Get the current outer bounds of all the items in the index or Rect.Empty if none.
        /// </summary>
        Rect Extent { get; }

        /// <summary>
        /// Find out if there are any items intersecting the given bounds.
        /// </summary>
        /// <returns>
        /// true if there are any items intersecting the given bounds.
        /// </returns>
        bool HasItemsIntersecting(Rect bounds);

        /// <summary>
        /// Find the items that intersect a given bounds
        /// where items with lower priority values are returned first.
        /// </summary>
        /// <returns>
        /// An iterator over the items.
        /// </returns>
        IEnumerable<ISpatialItem> GetItemsIntersecting(Rect bounds);

        /// <summary>
        /// Find out if there are any items inside a given bounds.
        /// </summary>
        /// <returns>
        /// true if there are any items are contained inside the given bounds.
        /// </returns>
        bool HasItemsInside(Rect bounds);

        /// <summary>
        /// Find the items that are inside a given bounds
        /// where items with lower priority values are returned first.
        /// </summary>
        /// <returns>
        /// An iterator over the items.
        /// </returns>
        IEnumerable<ISpatialItem> GetItemsInside(Rect bounds);
    }
}
