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
        bool selected = false;

        public DemoShape Shape { get; set; }

        public bool Selected
        {
            get => selected;
            set
            {
                if (this.selected != value)
                {
                    this.selected = value;
                    this.InvalidateVisual();
                }
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            Pen selectionPen = null;
            Rect selectionBounds;
            if (this.selected)
            {
                selectionBounds = this.Shape.Bounds;
                selectionBounds.Inflate(1.5, 1.5);
                selectionPen = new Pen(Brushes.Yellow, 3 / this.scale);
            }
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

                        if (this.selected)
                        {
                            drawingContext.DrawEllipse(null, selectionPen, new Point(xrad, yrad), xrad, yrad);
                        }

                        drawingContext.DrawEllipse(Shape.Fill, pen, new Point(xrad, yrad), xrad, yrad);
                    }
                    break;
                case ShapeType.Rect:
                    if (this.selected)
                    {
                        drawingContext.DrawRectangle(null, selectionPen, new Rect(-1.5, -1.5, selectionBounds.Width, selectionBounds.Height));
                    }
                    drawingContext.DrawRectangle(Shape.Fill, pen, new Rect(0, 0, Shape.Bounds.Width, Shape.Bounds.Height));
                    break;
                case ShapeType.RoundedRect:
                    {
                        if (this.selected)
                        {
                            var selectionRadiusX = selectionBounds.Width / 20;
                            var selectionRadiusY = selectionBounds.Height / 20;
                            drawingContext.DrawRoundedRectangle(null, selectionPen, new Rect(-1.5, -1.5, selectionBounds.Width, selectionBounds.Height), selectionRadiusX, selectionRadiusY);
                        }
                        var radiusX = Shape.Bounds.Width / 20;
                        var radiusY = Shape.Bounds.Height / 20;
                        drawingContext.DrawRoundedRectangle(Shape.Fill, pen, new Rect(0, 0, Shape.Bounds.Width, Shape.Bounds.Height), radiusX, radiusY);
                    }
                    break;
                case ShapeType.Star:
                    if (this.selected)
                    {
                        drawingContext.DrawGeometry(null, selectionPen, GetSelectionStarGeometry(selectionBounds));
                    }
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
            foreach (var pt in new Point[] {
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


        private Geometry GetSelectionStarGeometry(Rect selectionBounds)
        {
            double a = selectionBounds.Width / 2;
            double b = selectionBounds.Height / 2;

            PathGeometry g = new PathGeometry();
            PathFigure f = new PathFigure() { StartPoint = PointOnEllipse(0, a, b) };
            f.IsClosed = true;
            foreach (var pt in new Point[] {
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
