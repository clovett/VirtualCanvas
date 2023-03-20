using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VirtualCanvasDemo.Controls;
using VirtualCanvasDemo.Helpers;

namespace VirtualCanvasDemo
{
    class DemoDiagram : FrameworkElement, IScrollInfo
    {
        PanGesture panGesture;

        public DemoDiagram()
        {
            this.backdrop = new Border() { Background = Brushes.White };
            this.Canvas = new VirtualCanvas();

            this.Canvas.VisualFactory = new DemoShapeFactory();
            this.Canvas.IsVirtualizing = true;

            this.CoerceValue(OffsetProperty);

            this.Canvas.SetBinding(VirtualCanvas.ScaleProperty, this, ScaleProperty);
            this.Canvas.SetBinding(VirtualCanvas.OffsetProperty, this, OffsetProperty);

            this.AddLogicalChild(this.backdrop);
            this.AddVisualChild(this.backdrop);

            this.AddLogicalChild(this.Canvas);
            this.AddVisualChild(this.Canvas);

            this.Focusable = true;
            this.panGesture = new PanGesture(this);

        }

        public DemoSpatialIndex Index
        {
            get => (DemoSpatialIndex)this.Canvas.Items;
            set
            {
                this.Canvas.Items = value;
                CoerceValue(ScrollExtentProperty);
            }
        }

        bool insideRenderSizeChanged;

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            insideRenderSizeChanged = true;
            try
            {
                base.OnRenderSizeChanged(sizeInfo);
                this.CoerceValue(ScaleProperty);
                this.CoerceValue(OffsetProperty);
                this.CoerceValue(ActualViewboxProperty);
            }
            finally
            {
                insideRenderSizeChanged = false;
            }
        }

        private Border backdrop;

        private VirtualCanvas Canvas { get; }

        private double VerticalScrollIncrement
        {
            get { return ((IScrollInfo)this).ViewportHeight / 20; }
        }

        private double HorizontalScrollIncrement
        {
            get { return ((IScrollInfo)this).ViewportWidth / 20; }
        }

        #region WPF Children Implementation
        protected override int VisualChildrenCount
        {
            get { return 2; }
        }

        protected override Visual GetVisualChild(int index)
        {
            switch (index)
            {
                case 0:
                    return this.backdrop;
                case 1:
                    return this.Canvas;
            }
            return null;
        }
        protected override IEnumerator LogicalChildren
        {
            get
            {
                return this.VisualChildren.GetEnumerator();
            }
        }

        private IEnumerable<FrameworkElement> VisualChildren
        {
            get
            {
                IList<FrameworkElement> children = new List<FrameworkElement>(2);
                children.Add(this.backdrop);
                children.Add(this.Canvas);
                return children;
            }
        }

        /// <summary>
        /// Updates DesiredSize of the UIElement.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <param name="availableSize">Constraint size as an "upper limit" that the return value should not exceed.</param>
        /// <returns>Empty size struct since DemoDiagram does not support size to content.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (UIElement child in this.VisualChildren)
            {
                child.Measure(availableSize);
            }

            return new Size();
        }

        /// <summary>
        /// Arranges the child to take up the entire size.
        /// </summary>
        /// <param name="finalSize">Size the element will assume.</param>
        /// <returns>The size required by this element.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in this.VisualChildren)
            {
                child.Arrange(new Rect(finalSize));
            }

            return finalSize;
        }
        #endregion 

        #region Scroll Animation Properties

        private double scrollAnimationDuration = 0.1;
        public double ScrollAnimationDuration
        {
            get
            {
                return scrollAnimationDuration;
            }
            set
            {
                scrollAnimationDuration = value;
            }
        }

        private double scrollAccelerationRatio = 0; // off by default as it makes the canvas feel sluggish.
        public double ScrollAccelerationRatio
        {
            get
            {
                return scrollAccelerationRatio;
            }
            set
            {
                scrollAccelerationRatio = value;
            }
        }

        private const double BounceBackDecelerationRatio = 0.5;

        private double bounceBackAnimationDuration = 0.5;
        public double BounceBackAnimationDuration
        {
            get
            {
                return bounceBackAnimationDuration;
            }
            set
            {
                bounceBackAnimationDuration = value;
            }
        }

        private double scrollDecelerationRatio = 0; // off by default as it makes the canvas feel sluggish.

        public double ScrollDecelerationRatio
        {
            get
            {
                return scrollDecelerationRatio;
            }
            set
            {
                scrollDecelerationRatio = value;
            }
        }
        #endregion

        #region ScrollExtentProperty

        public static readonly DependencyProperty ScrollExtentProperty = DependencyProperty.Register("ScrollExtent", typeof(Rect), typeof(DemoDiagram), new FrameworkPropertyMetadata(Rect.Empty, OnScrollExtentChanged, CoerceScrollExtent), IsScrollExtentValid);

        private static bool IsScrollExtentValid(object value)
        {
            var scrollExtent = (Rect)value;
            return scrollExtent.IsEmpty ||
                (scrollExtent.X.IsBetween(double.MinValue, double.MaxValue)
                && scrollExtent.Y.IsBetween(double.MinValue, double.MaxValue)
                && scrollExtent.Height.IsBetween(0, double.MaxValue)
                && scrollExtent.Width.IsBetween(0, double.MaxValue));
        }

        private static object CoerceScrollExtent(DependencyObject d, object value)
        {
            var extent = (Rect)value;

            var control = d as DemoDiagram;
            if (control != null)
            {
                if (extent.IsEmpty)
                {
                    var index = control.Canvas.Items;
                    if (index != null)
                    {
                        extent = index.Extent;
                    }
                }
            }

            return extent;
        }

        private static void OnScrollExtentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DemoDiagram;
            if (control != null)
            {
                control.OnScrollExtentChanged();
            }
        }

        private void OnScrollExtentChanged()
        {
            InvalidateScrollInfo();
            if (ScrollExtentChanged != null)
            {
                ScrollExtentChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler ScrollExtentChanged;

        public Rect ScrollExtent
        {
            get { return (Rect)GetValue(ScrollExtentProperty); }
            set { SetValue(ScrollExtentProperty, value); }
        }

        #endregion

        #region OffsetProperty

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register("Offset", typeof(Point), typeof(DemoDiagram), new FrameworkPropertyMetadata(new Point(0.0, 0.0), OnOffsetChanged, CoerceOffset), IsOffsetValid);

        private static bool IsOffsetValid(object value)
        {
            var point = (Point)value;
            return point.X.IsBetween(double.MinValue, double.MaxValue)
                && point.Y.IsBetween(double.MinValue, double.MaxValue);
        }

        private static object CoerceOffset(DependencyObject d, object value)
        {
            var control = d as DemoDiagram;
            if (control != null)
            {
                var viewbox = control.DesiredViewbox;
                if (!viewbox.IsEmpty)
                {
                    var scale = control.Scale;
                    var renderSize = control.GetLayoutSize();
                    Point center = viewbox.Center();
                    value = new Point((center.X * scale) - (renderSize.Width / 2), (center.Y * scale) - (renderSize.Height / 2));
                }
            }

            return value;
        }

        private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ActualViewboxProperty);
        }

        // Made it virtual for testing purpose
        public virtual Point Offset
        {
            get
            {
                return (Point)GetValue(OffsetProperty);
            }
            set
            {
                // Set Value before removing any animation object.
                // Removing the Animation changes the Offset value as well. Hence its best to set the final value first.
                SetValue(OffsetProperty, value);
                BeginAnimation(OffsetProperty, null);
            }
        }

        #endregion

        #region ViewboxProperty

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Viewbox")]
        public static readonly DependencyProperty DesiredViewboxProperty = DependencyProperty.Register("DesiredViewbox", typeof(Rect), typeof(DemoDiagram), new FrameworkPropertyMetadata(Rect.Empty, OnDesiredViewboxChanged));

        private static void OnDesiredViewboxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ScaleProperty);
            d.CoerceValue(OffsetProperty);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Viewbox")]
        public Rect DesiredViewbox
        {
            get { return (Rect)GetValue(DesiredViewboxProperty); }
            set { SetValue(DesiredViewboxProperty, value); }
        }

        #endregion

        #region ActualViewboxProperty

        /// <summary>
        /// Gets the layout size of the diagram - from actual rendered size, if possible, or from underlying WPF properties.
        /// This method is declared virtual, to allow test classes to override it if necessary.
        /// </summary>
        protected virtual Size GetLayoutSize()
        {
            // If we are being drawing offscreen then the RenderSize is not accurate.
            if (!this.IsVisible)
            {
                if (!double.IsNaN(this.Width) && !double.IsNaN(this.Height))
                {
                    return new Size(this.Width, this.Height);
                }
                if (!double.IsNaN(this.ActualWidth) && !double.IsNaN(this.ActualHeight))
                {
                    return new Size(this.ActualWidth, this.ActualHeight);
                }
            }
            return this.RenderSize;
        }

        public static readonly DependencyProperty ActualViewboxProperty = DependencyProperty.RegisterReadOnly("ActualViewbox", typeof(Rect), typeof(DemoDiagram), new FrameworkPropertyMetadata(Rect.Empty, OnActualViewboxChanged, CoerceActualViewbox)).DependencyProperty;

        private static object CoerceActualViewbox(DependencyObject d, object value)
        {
            var control = d as DemoDiagram;
            if (control != null)
            {
                var offset = control.Offset;
                var scale = control.Scale;
                var renderSize = control.GetLayoutSize();
                value = new Rect(offset.X / scale, offset.Y / scale, renderSize.Width / scale, renderSize.Height / scale);
            }

            return value;
        }

        private static void OnActualViewboxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DemoDiagram;
            if (control != null)
            {
                control.InvalidateScrollInfo();
            }
        }

        // Made it virtual for testing purpose
        public virtual Rect ActualViewbox
        {
            get { return (Rect)GetValue(ActualViewboxProperty); }
        }

        private void InvalidateScrollInfo()
        {
            var owner = ((IScrollInfo)this).ScrollOwner;
            if (owner != null && !insideRenderSizeChanged)
            {
                owner.InvalidateScrollInfo();
            }
        }
        #endregion

        #region ScaleProperty

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.RegisterAttached("Scale", typeof(double), typeof(DemoDiagram), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.Inherits, OnScaleChanged, CoerceScale), IsScaleValid);

        // Note: Any zoom scale that smaller than MinValidZoomScale will cause crash.
        private const int MinSignificantDigits = 4;
        private const double MinValidZoomScale = 1E-4;
        private const double MaxValidZoomScale = 1E+4;

        private static bool IsScaleValid(object value)
        {
            // WPF control (ZoomCombox) sometimes will passing in slightly smaller number than the input value (0.0001) can cause this to return false, and wpf throw invalidoperation message.
            // Round this value up to avoid this from happenning.
            double scale = (double)value;
            scale = Math.Round(scale, MinSignificantDigits);

            return scale.IsBetween(MinValidZoomScale, MaxValidZoomScale);
        }

        private static object CoerceScale(DependencyObject d, object value)
        {
            var scale = (double)value;

            var control = d as DemoDiagram;
            if (control != null)
            {
                var viewbox = control.DesiredViewbox;
                if (!viewbox.IsEmpty)
                {
                    var renderSize = control.GetLayoutSize();
                    scale = Math.Min(renderSize.Width / viewbox.Width, renderSize.Height / viewbox.Height);
                }
            }

            return scale;
        }

        private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ActualViewboxProperty);
            d.CoerceValue(OffsetProperty);
        }

        public static double GetScale(DependencyObject element)
        {
            return (double)element.GetValue(ScaleProperty);
        }

        public static void SetScale(DependencyObject element, double scale)
        {
            element.SetValue(ScaleProperty, scale);
        }

        // Made it virtual for testing purpose
        public virtual double Scale
        {
            get
            {
                return (double)GetValue(ScaleProperty);
            }
            set
            {
                SetValue(ScaleProperty, value);
                BeginAnimation(ScaleProperty, null);
            }
        }

        // Property that specifies whether the scale factor is user-set (true) or auto-calculated (false).
        // The user-set scale factor is not modified by auto-calculation, until the user explicitly unsets this flag.
        public bool UserSetScale { get; set; }

        #endregion

        #region MinScaleProperty

        public static readonly DependencyProperty MinScaleProperty = DependencyProperty.RegisterAttached("MinScale", typeof(double), typeof(DemoDiagram), new FrameworkPropertyMetadata(MinValidZoomScale, FrameworkPropertyMetadataOptions.Inherits, OnMinScaleChanged), IsScaleValid);

        private static void OnMinScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ScaleProperty);
        }

        public static double GetMinScale(DependencyObject element)
        {
            return (double)element.GetValue(MinScaleProperty);
        }

        public static void SetMinScale(DependencyObject element, double scale)
        {
            element.SetValue(MinScaleProperty, scale);
        }

        public double MinScale
        {
            get { return (double)GetValue(MinScaleProperty); }
            set { SetValue(MinScaleProperty, value); }
        }

        #endregion

        #region MaxScaleProperty

        public static readonly DependencyProperty MaxScaleProperty = DependencyProperty.RegisterAttached("MaxScale", typeof(double), typeof(DemoDiagram), new FrameworkPropertyMetadata(MaxValidZoomScale, FrameworkPropertyMetadataOptions.Inherits, OnMaxScaleChanged), IsScaleValid);

        private static void OnMaxScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ScaleProperty);
        }

        public static double GetMaxScale(DependencyObject element)
        {
            return (double)element.GetValue(MaxScaleProperty);
        }

        public static void SetMaxScale(DependencyObject element, double scale)
        {
            element.SetValue(MaxScaleProperty, scale);
        }

        public double MaxScale
        {
            get { return (double)GetValue(MaxScaleProperty); }
            set { SetValue(MaxScaleProperty, value); }
        }

        #endregion


        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            this.Focus();
        }

        public const int MouseWheelDeltaForOneLine = 120;
        public const double ZoomSensitivityCalibration = 1.8;
        public const double ZoomWheelSensitivity = 1.0;

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);

            double adjusted = ((double)e.Delta * SystemParameters.WheelScrollLines) / (double)Mouse.MouseWheelDeltaForOneLine;
            if (adjusted > 0)
            {
                adjusted = Math.Min(400, Math.Max(20, adjusted)); // pin it so we get a reasonable spread
            }
            else
            {
                adjusted = Math.Max(-400, Math.Min(-20, adjusted)); // pin it so we get a reasonable spread
            }
            double factor = Math.Pow(ZoomSensitivityCalibration * ZoomWheelSensitivity, adjusted / 100);

            this.ZoomBy(factor, e.GetPosition(this));
            e.Handled = true;

        }

        public void ZoomBy(double factor, Point origin)
        {
            var scale = DestinationScale ?? Scale;
            ZoomTo(scale * factor, origin);
        }

        public void ZoomTo(double scale, Point origin)
        {
            Vector fixedScreenPoint;
            if (!this.ScrollExtent.IsEmpty)
            {
                Rect scrollExtent = TransformFromCanvas().TransformBounds(this.ScrollExtent);
                fixedScreenPoint = new Vector(
                    origin.X.AtLeast(scrollExtent.Left).AtMost(scrollExtent.Right),
                    origin.Y.AtLeast(scrollExtent.Top).AtMost(scrollExtent.Bottom));
            }
            else
            {
                fixedScreenPoint = (Vector)origin;
            }

            var fixedWorldPoint = ((Vector)this.Offset + fixedScreenPoint) / this.Scale;

            scale = scale.AtLeast(this.MinScale).AtMost(this.MaxScale);
            origin = (Point)((fixedWorldPoint * scale) - fixedScreenPoint);

            ZoomTo(scale);
            MoveTo(origin, true);
        }

        public bool IsZoomAnimating
        {
            get;
            set;
        }
        public Point? DestinationOffset
        {
            get;
            set;
        }

        public double? DestinationScale
        {
            get;
            set;
        }

        private double zoomToFitScale;

        // Made it virtual for testing purpose
        public virtual void ZoomTo(double scale)
        {
            if (scale == Scale)
            {
                return;
            }

            DestinationScale = scale;

            var animation = new DoubleAnimation();
            // Don't specify .From or the animation is jerky when the graph is centered
            animation.To = scale;
            animation.Duration = TimeSpan.FromSeconds(scrollAnimationDuration);
            if (scrollAccelerationRatio != 0)
            {
                animation.AccelerationRatio = scrollAccelerationRatio;
            }
            if (scrollDecelerationRatio != 0)
            {
                animation.DecelerationRatio = scrollDecelerationRatio;
            }
            IsZoomAnimating = scrollAnimationDuration > 0;
            animation.Completed += delegate
            {
                IsZoomAnimating = false;
                if (Scale == DestinationScale)
                {
                    Scale = DestinationScale.Value;
                    BeginAnimation(ScaleProperty, null);
                    DestinationScale = null;
                }
            };

            animation.Freeze();

            if (zoomToFitScale == -1)
            {
                zoomToFitScale = 0;
            }
            BeginAnimation(ScaleProperty, animation, HandoffBehavior.Compose);
        }

        public GeneralTransform TransformFromCanvas()
        {
            TransformGroup g = new TransformGroup();
            g.Children.Add(new ScaleTransform(this.Scale, this.Scale));
            g.Children.Add(new TranslateTransform(-this.Offset.X, -this.Offset.Y));
            return g;
        }

        #region IScrollInfo
        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }

        double IScrollInfo.ViewportHeight
        {
            get
            {
                return Math.Ceiling(RenderSize.Height);
            }
        }

        double IScrollInfo.ViewportWidth
        {
            get
            {
                return Math.Ceiling(RenderSize.Width);
            }
        }

        double IScrollInfo.ExtentHeight
        {
            get
            {
                return Math.Max(Math.Max(this.ActualViewbox.Bottom, this.ScrollExtent.Bottom) - Math.Min(this.ActualViewbox.Top, this.ScrollExtent.Top), 0.0) * Scale;
            }
        }

        double IScrollInfo.ExtentWidth
        {
            get
            {
                return Math.Max(Math.Max(this.ActualViewbox.Right, this.ScrollExtent.Right) - Math.Min(this.ActualViewbox.Left, this.ScrollExtent.Left), 0.0) * Scale;
            }
        }

        double IScrollInfo.HorizontalOffset
        {
            get
            {
                return Math.Max(this.ActualViewbox.X - this.ScrollExtent.X, 0.0) * this.Scale;
            }
        }

        double IScrollInfo.VerticalOffset
        {
            get
            {
                return Math.Max(this.ActualViewbox.Y - this.ScrollExtent.Y, 0.0) * this.Scale;
            }
        }

        public ScrollViewer ScrollOwner { get; set; }


        public void LineDown()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset + VerticalScrollIncrement);
        }

        public void LineLeft()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset - HorizontalScrollIncrement);
        }

        public void LineRight()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset + HorizontalScrollIncrement);
        }

        public void LineUp()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset - VerticalScrollIncrement);
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            if (visual == this)
            {
                return this.ScrollExtent;
            }
            return rectangle;
        }

        public void MouseWheelDown()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset + SystemParameters.WheelScrollLines);
        }

        public void MouseWheelLeft()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset - HorizontalScrollIncrement);
        }

        public void MouseWheelRight()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset + HorizontalScrollIncrement);
        }

        public void MouseWheelUp()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset - SystemParameters.WheelScrollLines);
        }

        public void PageDown()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset + ((IScrollInfo)this).ViewportHeight);
        }

        public void PageLeft()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset - ((IScrollInfo)this).ViewportWidth);
        }

        public void PageRight()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset + ((IScrollInfo)this).ViewportWidth);
        }

        public void PageUp()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset - ((IScrollInfo)this).ViewportHeight);
        }

        public void SetHorizontalOffset(double offset)
        {
            if (offset == Double.NegativeInfinity)
            {
                offset = 0;
            }
            if (offset == Double.PositiveInfinity)
            {
                offset = ((IScrollInfo)this).ExtentWidth - ((IScrollInfo)this).ViewportWidth;
            }
            var x = ((IScrollInfo)this).HorizontalOffset;
            var y = ((IScrollInfo)this).HorizontalOffset;
            MoveTo(new Point(Offset.X + offset - ((IScrollInfo)this).HorizontalOffset, Offset.Y), false);
        }

        public void SetVerticalOffset(double offset)
        {
            if (offset == Double.NegativeInfinity)
            {
                offset = 0;
            }
            if (offset == Double.PositiveInfinity)
            {
                offset = ((IScrollInfo)this).ExtentHeight - ((IScrollInfo)this).ViewportHeight;
            }
            MoveTo(new Point(Offset.X, Offset.Y + offset - ((IScrollInfo)this).VerticalOffset), false);
        }

        public bool IsOffsetAnimating
        {
            get;
            set;
        }

        internal void MoveBy(double dx, double dy)
        {
            var offset = DestinationOffset ?? Offset;
            MoveTo(new Point(offset.X - dx, offset.Y - dy), false);
        }


        public virtual void MoveTo(Point offset, bool animate)
        {
            if (offset == Offset)
            {
                return;
            }

            double animationDuration = 0;
            if (animate)
            {
                // only want a non-zero animation duration if we are applying zoom-to to a graph that has already been zoomed
                animationDuration = scrollAnimationDuration;
            }

            var animation = new PointAnimation();
            // Don't specify .From or the animation is jerky when the graph is centered
            animation.To = offset;
            animation.Duration = TimeSpan.FromSeconds(animationDuration);
            if (scrollAccelerationRatio != 0)
            {
                animation.AccelerationRatio = scrollAccelerationRatio;
            }
            if (scrollDecelerationRatio != 0)
            {
                animation.DecelerationRatio = scrollDecelerationRatio;
            }
            IsOffsetAnimating = animation.Duration.TimeSpan.Ticks > 0;
            bool bounce = true;
            Vector direction = offset - this.Offset;

            this.DestinationOffset = offset;
            animation.Completed += delegate
            {
                IsOffsetAnimating = false;
                if (Offset == DestinationOffset)
                {
                    // call the Offset setter to ensure correct side effects
                    Offset = Offset;
                }
                if (bounce)
                {
                    // Bounce back, but not if we're zooming!
                    BounceBack(direction, animate);
                }
            };
            animation.Freeze();
            BeginAnimation(OffsetProperty, animation, HandoffBehavior.Compose);
        }

        /// <summary>
        /// If the offset is beyond the bounds of the graph, then this method deos a nice little bounce back to where we should be
        /// so that the diagram remains visible within the current viewport.
        /// </summary>
        private void BounceBack(Vector direction, bool animate)
        {
            Point offset = this.Offset;
            double scale = this.Scale;
            double pageWidth = ((IScrollInfo)this).ViewportWidth;
            double pageHeight = ((IScrollInfo)this).ViewportHeight;

            Rect scrollExtent = new ScaleTransform(scale, scale).TransformBounds(this.ScrollExtent);

            if (direction.X != 0)
            {
                if (scrollExtent.Width < pageWidth)
                {
                    // then at least keep it visible in the viewport.
                    if (offset.X < 0 && -offset.X + scrollExtent.Right > pageWidth)
                    {
                        offset.X = scrollExtent.Right - pageWidth;
                    }
                    if (offset.X > 0 && offset.X > scrollExtent.Left)
                    {
                        offset.X = scrollExtent.Left;
                    }
                }
                else
                {
                    double right = scrollExtent.Right - pageWidth;
                    if (offset.X > right)
                    {
                        offset.X = right;
                    }
                    if (offset.X < scrollExtent.Left)
                    {
                        offset.X = scrollExtent.Left;
                    }
                }
            }
            if (direction.Y != 0)
            {
                if (scrollExtent.Height < pageHeight)
                {
                    // then at least keep it visible in the viewport.
                    if (offset.Y < 0 && -offset.Y + scrollExtent.Bottom > pageHeight)
                    {
                        offset.Y = scrollExtent.Bottom - pageHeight;
                    }
                    if (offset.Y > 0 && offset.Y > scrollExtent.Top)
                    {
                        offset.Y = scrollExtent.Top;
                    }
                }
                else
                {
                    double bottom = scrollExtent.Bottom - pageHeight;
                    if (offset.Y > bottom)
                    {
                        offset.Y = bottom;
                    }
                    if (offset.Y < scrollExtent.Top)
                    {
                        offset.Y = scrollExtent.Top;
                    }
                }
            }
            if (offset != this.Offset)
            {
                double saved = ScrollAnimationDuration;
                double saved2 = ScrollDecelerationRatio;
                ScrollAnimationDuration = BounceBackAnimationDuration;
                ScrollDecelerationRatio = BounceBackDecelerationRatio;
                MoveTo(offset, animate);
                ScrollAnimationDuration = saved;
                ScrollDecelerationRatio = saved2;
            }
        }

        #endregion
    }
}
