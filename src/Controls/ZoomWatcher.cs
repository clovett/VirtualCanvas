//-----------------------------------------------------------------------
// <copyright file="ZoomWatcher.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using VirtualCanvasDemo.Interfaces;

namespace VirtualCanvasDemo.Controls
{
    /// <summary>
    /// This class watches changes to the zoom level and lazily applies updates to visuals that want to know about zoom changes.
    /// </summary>
    internal class ZoomWatcher
    {
        private VirtualCanvas canvas;
        private DispatcherTimer timer;

        public ZoomWatcher(VirtualCanvas canvas)
        {
            this.canvas = canvas;
            canvas.ScaleChanged += new EventHandler(OnScaleChanged);
        }

        private void OnScaleChanged(object sender, EventArgs e)
        {
            if (timer == null)
            {
                timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, new EventHandler(OnTick), canvas.Dispatcher);
            }
            timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            timer.Stop();
            timer = null;
            TickleZoomables(true);
        }

        public void TickleZoomables(bool visibleOnly)
        {
            double scale = canvas.Scale;
            IVisualFactory factory = canvas.VisualFactory;
            factory.BeginRealize();

            // todo: timeslice this work (throttle it)
            IEnumerable<ISpatialItem> query = null;
            if (visibleOnly)
            {
                query = canvas.Items.GetItemsIntersecting(canvas.ActualViewbox);
            }
            else
            {
                query = canvas.Items;
            }
            foreach (ISpatialItem item in query)
            {
                Visual visual = canvas.VisualFromItem(item);
                if (visual == null && !canvas.IsPaused)
                {
                    visual = canvas.RealizeItem(item, false);
                }
                if (visual != null)
                {
                    TickleZoomable(scale, visual);
                    // see if there are any adorners on the visual that also need to be scaled
                    var layer = AdornerLayer.GetAdornerLayer(visual);
                    if (layer != null)
                    {
                        var adorners = layer.GetAdorners(visual as UIElement);
                        if (adorners != null)
                        {
                            foreach (var zoomableAdorner in adorners.OfType<ISemanticZoomable>())
                            {
                                TickleZoomable(scale, zoomableAdorner);
                            }
                        }
                    }
                }
            }

            factory.EndRealize();
        }

        private static void TickleZoomable(double scale, ISemanticZoomable zoomable)
        {
            if (zoomable != null)
            {
                zoomable.OnZoomChange(scale);
            }
        }

        internal static void TickleZoomable(double scale, Visual visual)
        {
            TickleZoomable(scale, visual as ISemanticZoomable);
        }
    }
}
