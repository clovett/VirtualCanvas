using System;
using System.Windows;
using VirtualCanvasDemo.Interfaces;
using VirtualCanvasDemo.QuadTree;

namespace VirtualCanvasDemo
{
    /// <summary>
    /// This provides the ISpatialIndex needed by VirtualCanvas, implemented using our
    /// handy PriorityQuadTree helper class.
    /// </summary>
    class DemoSpatialIndex : PriorityQuadTree<ISpatialItem>, ISpatialIndex
    {
        public event EventHandler Changed;

        public void Insert(DemoShape item)
        {
            this.Insert(new DemoItem(item), item.Bounds, 0);
            if (Changed != null)
            {
                Changed(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// This wrapper implements ISpatialItem on behalf of our DemoShape object
        /// </summary>
        class DemoItem : ISpatialItem
        {
            DemoShape shape;

            public DemoItem(DemoShape shape)
            {
                this.shape = shape;
                this.IsVisible = true;
            }

            public bool IsLayoutValid { get; set; }
            public double Priority { get; set; }
            public int ZIndex { get; set; }
            public bool IsVisible { get; set; }

            public void OnMeasure(UIElement visual)
            {
            }

            public object DataItem => shape;

            public Rect Bounds => shape.Bounds;
        }
    }
}
