using System;
using System.ComponentModel;
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
            item.PropertyChanged -= OnItemChanged; // make sure we never add this handler twice
            item.PropertyChanged += OnItemChanged;

            this.Insert(item, item.Bounds, 0);

            if (Changed != null)
            {
                Changed(this, EventArgs.Empty);
            }
        }

        private void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is DemoShape shape)
            {
                if (e.PropertyName == "Bounds")
                {
                    // needs to be reindexed.
                    this.Remove(shape);
                    this.Insert(shape);
                }
            }
        }
    }
}
