using System;
using System.Windows;
using System.Windows.Media;

namespace VirtualCanvasDemo.Helpers
{
    internal class MouseGestures
    {
        private bool isMouseDown;
        private bool started;
        private bool captured;
        private Point previousPosition;
        private DemoDiagram owner;
        private DemoShapeVisual hit;

        /// <summary>
        /// Threashold used to know if we can start the movement of the selection or not. We start the movement when the distance
        /// between the previousPosition and the current mouse position is more than the threshold
        /// </summary>
        public const double Threshold = 2;

        public MouseGestures(DemoDiagram owner)
        {
            this.owner = owner;
            owner.PreviewMouseDown += OnMouseDown;
            owner.PreviewMouseMove += OnMouseMove;
            owner.PreviewMouseUp += OnMouseUp;
            owner.LostMouseCapture += OnLostMouseCapture;
        }

        private void OnLostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            FinishPanning();
        }

        private void OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FinishPanning();
        }

        private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.owner.Focus();
            this.isMouseDown = true;
            this.started = false;
            this.previousPosition = e.GetPosition(this.owner);
            hit = HitTestVisual(this.previousPosition);
            if (hit != null)
            {
                var bounds = hit.Shape.Bounds;
                bounds.Inflate(3, 3);
                hit.Shape.Bounds = bounds;
            }
            else
            {
                this.owner.Selection = null;
            }
            this.captured = this.owner.CaptureMouse();
        }

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (this.isMouseDown)
            {
                var position = e.GetPosition(this.owner);
                if (!started)
                {
                    Vector deplacement = position - this.previousPosition;
                    if (deplacement.Length > Threshold)
                    {
                        started = true;
                    }
                }
                else
                {
                    // request to move only if needed, in order to save a few CPU cylces.
                    if (position != previousPosition)
                    {
                        double dx = position.X - this.previousPosition.X;
                        double dy = position.Y - this.previousPosition.Y;
                        if (this.hit != null && this.hit.Selected)
                        {
                            // then move the shape
                            var bounds = this.hit.Shape.Bounds;
                            bounds.Offset(dx / owner.Scale, dy / owner.Scale);
                            this.hit.Shape.Bounds = bounds;
                        }
                        else
                        {
                            // then pan the view.
                            this.owner.MoveBy(dx, dy);
                        }
                        this.previousPosition = position;
                    }
                }
            }
        }

        private void FinishPanning()
        {
            if (hit != null)
            {
                var bounds = hit.Shape.Bounds;
                bounds.Inflate(-3, -3);
                hit.Shape.Bounds = bounds;
            }
            if (!started)
            {
                if (hit != null)
                {
                    this.owner.Selection = hit;
                }
            }
            this.isMouseDown = false;
            if (this.captured)
            {
                this.captured = false;
                this.owner.ReleaseMouseCapture();
            }
        }

        public DemoShapeVisual HitTestVisual(Point hitPoint)
        {
            DemoShapeVisual found = null;
            try
            {
                // Hit test radius in screen distance needs to be increased when zoomed out.
                double radius = 5 / Math.Max(owner.Scale, 1);

                // Expand the hit test area by creating a geometry centered on the hit test point.
                var expandedHitTestArea = new GeometryHitTestParameters(new EllipseGeometry(hitPoint, radius, radius));

                // The following callback is called for every visual intersecting with the test area, in reverse z-index order.
                // If hitLinks is true, all links passing through the test area are considered, and the closest to the hitPoint is returned in visual.
                // The hit test search is stopped as soon as the first non-link graph object is encountered.
                var hitTestResultCallback = new HitTestResultCallback(r =>
                {
                    if (r.VisualHit is DemoShapeVisual v)
                    {
                        found = v;
                        return HitTestResultBehavior.Stop;
                    }
                    return HitTestResultBehavior.Continue;
                });

                // start the search
                VisualTreeHelper.HitTest(owner, null, hitTestResultCallback, expandedHitTestArea);
            }
            catch (System.Exception)
            {
                // silently swallow the exception because if we can't find any child node, then we can proceed
                // by just returning null.  The control will probably correct its state on next graph layout.
            }
            return found;
        }

    }
}
