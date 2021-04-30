//-----------------------------------------------------------------------
// <copyright file="ISpatialItem.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace VirtualCanvasDemo.Interfaces
{
    /// <summary>
    /// An item that can be located within an ISpatialIndex
    /// </summary>
    public interface ISpatialItem
    {
        /// <summary>
        /// The bounds of the item.
        /// </summary>
        Rect Bounds { get; }

        /// <summary>
        /// Return a boolean that indicates whether the bounds is actually computed yet.
        /// </summary>
        bool IsLayoutValid { get; }

        /// <summary>
        /// A value indicating how visually important the element is relative to other items.
        /// Higher priorities will be displayed first.
        /// </summary>
        double Priority { get; }

        /// <summary>
        /// The Z-Index of the visual relative to other items.
        /// Higher ZIndexs will be drawn on top.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ZIndex")]
        int ZIndex { get; }

        /// <summary>
        /// Determines whether the item is visible on the canvas.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Invoked when Visual whose DataContext is this ISpatialItem is measured.
        /// </summary>
        /// <param name="visual">The Visual corresponding to the ISpatialItem</param>
        void OnMeasure(UIElement visual);

        /// <summary>
        /// Return the data item for DataTemplate binding (which can be 'this', but doesn't have to be).
        /// </summary>
        object DataItem { get; }
    }
}
