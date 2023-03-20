using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using VirtualCanvasDemo.Controls;
using VirtualCanvasDemo.Interfaces;

namespace VirtualCanvasDemo
{
    class DemoShapeFactory : IVisualFactory
    {
        public void BeginRealize()
        {
        }

        public void EndRealize()
        {
        }

        public Visual Realize(object item, bool force)
        {
            if (item is DemoShape d)
            {
                return new DemoShapeVisual()
                {
                    Shape = d
                };                
            }
            return null;
        }


        private Point PointOnEllipse(double angle, double a, double b)
        {
            double d = angle * Math.PI / 180;
            return new Point(a * Math.Cos(d), b * Math.Sin(d));
        }

        public bool Virtualize(Visual visual)
        {
            return true;
        }

    }
}
