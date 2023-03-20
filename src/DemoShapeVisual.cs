using System;
using System.Windows;
using System.Windows.Media;
using VirtualCanvasDemo.Interfaces;

namespace VirtualCanvasDemo
{
    /// <summary>
    /// This is an even more light weight way to create the visuals by using drawingContext
    /// </summary>
    class DemoShapeVisual : FrameworkElement, ISemanticZoomable
    {
        double scale = 1.0;

        public DemoShape Shape { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            Pen pen = null;
            if (Shape.Stroke != null)
            {
                pen = new Pen(Shape.Stroke, Shape.StrokeThickness / this.scale);
            }

            switch (Shape.Type)
            {
                case ShapeType.Ellipse: 
                    {
                        double xrad = Shape.Bounds.Width / 2;
                        double yrad = Shape.Bounds.Height / 2;
                        drawingContext.DrawEllipse(Shape.Fill, pen, new Point(Shape.Bounds.Left + xrad, Shape.Bounds.Top + yrad), xrad, yrad);
                    }
                    break;
                case ShapeType.Rect:
                    drawingContext.DrawRectangle(Shape.Fill, pen, new Rect(0,0,Shape.Bounds.Width, Shape.Bounds.Height));
                    break;
                case ShapeType.RoundedRect:
                    {
                        var radiusX = Shape.Bounds.Width / 20;
                        var radiusY = Shape.Bounds.Height / 20;
                        drawingContext.DrawRoundedRectangle(Shape.Fill, pen, new Rect(0, 0, Shape.Bounds.Width, Shape.Bounds.Height), radiusX, radiusY);
                    }
                    break;
                case ShapeType.Star:
                    drawingContext.DrawGeometry(Shape.Fill, pen, GetStarGeometry());
                    break;
            }
        }

        private Geometry GetStarGeometry()
        {
            double a = Shape.Bounds.Width / 2;
            double b = Shape.Bounds.Height / 2;

            PathGeometry g = new PathGeometry();
            PathFigure f = new PathFigure() { StartPoint = PointOnEllipse(0, a, b) };
            f.IsClosed = true;
            foreach(var pt in new Point[] {
                                PointOnEllipse(0, a, b),
                                PointOnEllipse(72 * 2, a, b),
                                PointOnEllipse(72 * 4, a, b),
                                PointOnEllipse(72, a, b),
                                PointOnEllipse(72 * 3, a, b)})
            {
                f.Segments.Add(new LineSegment(pt, true));
            }
            g.Figures.Add(f);
            return g;
        }

        private Point PointOnEllipse(double angle, double a, double b)
        {
            double d = angle * Math.PI / 180;
            return new Point(a * Math.Cos(d), b * Math.Sin(d));
        }

        public void OnZoomChange(double newZoomLevel)
        {
            this.scale = newZoomLevel == 0 ? 0.000001 : newZoomLevel;
            if (Shape.Stroke != null)
            {
                this.InvalidateVisual();
            }
        }

    }
}
