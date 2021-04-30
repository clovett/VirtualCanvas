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
                Debug.WriteLine("Creating " + d.Type);
                switch (d.Type)
                {
                    case ShapeType.Ellipse:
                        return new Ellipse()
                        {
                            Width = d.Bounds.Width,
                            Height = d.Bounds.Height,
                            Fill = d.Fill,
                            Stroke = d.Stroke,
                            StrokeThickness = d.StrokeThickness
                        };
                    case ShapeType.Rect:
                        return new Rectangle()
                        {
                            Width = d.Bounds.Width,
                            Height = d.Bounds.Height,
                            Fill = d.Fill,
                            Stroke = d.Stroke,
                            StrokeThickness = d.StrokeThickness
                        };
                    case ShapeType.RoundedRect:
                        return new Rectangle()
                        {
                            RadiusX = d.Bounds.Width / 20,
                            RadiusY = d.Bounds.Height / 20,
                            Width = d.Bounds.Width,
                            Height = d.Bounds.Height,
                            Fill = d.Fill,
                            Stroke = d.Stroke,
                            StrokeThickness = d.StrokeThickness
                        };
                    case ShapeType.Star:
                        double a = d.Bounds.Width / 2;
                        double b = d.Bounds.Height / 2;

                        return new Polygon()
                        {
                            Width = d.Bounds.Width,
                            Height = d.Bounds.Height,
                            Fill = d.Fill,
                            Stroke = d.Stroke,
                            StrokeThickness = d.StrokeThickness,
                            Points = new PointCollection(new Point[]
                            {
                                PointOnEllipse(0, a, b),
                                PointOnEllipse(72 * 2, a, b),
                                PointOnEllipse(72 * 4, a, b),
                                PointOnEllipse(72, a, b),
                                PointOnEllipse(72 * 3, a, b)
                            })
                        };
                }
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
            if (visual.GetValue(VirtualCanvas.RealizedItemProperty) is ISpatialItem s && s.DataItem is DemoShape d)
            {
                Debug.WriteLine("Virtualizing " + d.Type);
            }
            return true;
        }

    }
}
