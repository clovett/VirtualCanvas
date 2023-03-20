//-----------------------------------------------------------------------
// <copyright file="VirtualCanvas.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using VirtualCanvasDemo.Helpers;
using VirtualCanvasDemo.Interfaces;

namespace VirtualCanvasDemo.Controls
{
    /// <summary>
    /// Defines an area within which you can explicitly position an infinite number of child elements by using coordinates that are relative to the <see cref="VirtualCanvas"/> area.
    /// </summary>
    /// <remarks>
    /// This class is the same as ZoomableCanvas, except for the following:
    /// * Changed base class from Panel to FrameworkElement.
    /// * Virtualization logic has been copied from VirtualPanel into this class.
    /// * DataTemplate application has been copied from ItemsControl into this class.
    /// * Support for VirtualizationMode (recycling) has been removed.
    /// * Support for UpdateReality has been removed.
    /// * Support for RealizationLimit has been removed.
    /// * Support for ConstrainToBounds has been removed.
    /// * Support for ItemsControl has been removed.
    /// * Support for Panel has been removed.
    /// * Support for ContentPresenters has been removed.
    /// * Support for DataTemplateSelector has been removed.
    /// * Support for ItemsSource has been removed.
    /// * Support for direct visual children has been removed.
    /// * Support for Canvas.Top, Canvas.Left, and Panel.ZIndex attached properties have been removed.
    /// * Support for ViewBox, Stretch, StretchDirection, MinScale, MaxScale has been moved to DiagramControl.
    /// * Support for GetCanvasPoint, GetVisualPoint, MousePosition has been moved to DiagramControl.
    /// * Support for IScrollInfo has been moved to DiagramControl.
    /// * The data source must implement ISpatialIndex; a default spatial index is no longer provided.
    /// </remarks>
    public partial class VirtualCanvas : FrameworkElement, IScopedChanges
    {
        #region ItemsProperty

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(ISpatialIndex), typeof(VirtualCanvas), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, OnItemsChanged));

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = d as VirtualCanvas;
            if (canvas != null)
            {
                canvas.OnItemsChanged((ISpatialIndex)e.OldValue, (ISpatialIndex)e.NewValue);
            }
        }

        public ISpatialIndex Items
        {
            get { return (ISpatialIndex)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        #endregion

        #region IsVirtualizingProperty

        public static readonly DependencyProperty IsVirtualizingProperty = VirtualizingStackPanel.IsVirtualizingProperty.AddOwner(typeof(VirtualCanvas), new FrameworkPropertyMetadata(VirtualizingStackPanel.IsVirtualizingProperty.DefaultMetadata.DefaultValue, OnIsVirtualizingChanged));

        private static void OnIsVirtualizingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = d as VirtualCanvas;
            if (canvas != null)
            {
                canvas.InvalidateReality();
            }
        }

        public bool IsVirtualizing
        {
            get { return (bool)GetValue(IsVirtualizingProperty); }
            set { SetValue(IsVirtualizingProperty, value); }
        }

        #endregion

        #region UseRenderTransformProperty

        public static readonly DependencyProperty UseRenderTransformProperty = DependencyProperty.Register("UseRenderTransform", typeof(bool), typeof(VirtualCanvas), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange, OnUseRenderTransformChanged));

        /// <summary>
        /// Returns a transform applying the <see cref="Scale"/> and <see cref="Offset"/> when <see cref="UseRenderTransform"/> is set to <c>true</c>.
        /// </summary>
        /// <param name="d">Dependency object whos transform is being coerced.</param>
        /// <param name="value">The original uncoerced value.</param>
        /// <returns>A new transform if <see cref="UseRenderTransform"/> is set to <c>true</c>; otherwise, <paramref name="value"/>.</returns>
        private static object CoerceRenderTransform(DependencyObject d, object value)
        {
            var canvas = d as VirtualCanvas;
            if (canvas != null && canvas.UseRenderTransform)
            {
                var transform = new TransformGroup();
                transform.Children.Add(new ScaleTransform());
                transform.Children.Add(new TranslateTransform());
                return transform;
            }

            return value;
        }

        private static void OnUseRenderTransformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(RenderTransformProperty);
        }

        public bool UseRenderTransform
        {
            get { return (bool)GetValue(UseRenderTransformProperty); }
            set { SetValue(UseRenderTransformProperty, value); }
        }

        #endregion

        #region ActualViewboxProperty

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static readonly DependencyProperty ActualViewboxProperty = DependencyProperty.RegisterReadOnly("ActualViewbox", typeof(Rect), typeof(VirtualCanvas), new FrameworkPropertyMetadata(Rect.Empty, OnActualViewboxChanged, CoerceActualViewbox)).DependencyProperty;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        private static object CoerceActualViewbox(DependencyObject d, object value)
        {
            var canvas = d as VirtualCanvas;
            if (canvas != null)
            {
                var offset = canvas.Offset;
                var scale = canvas.Scale;
                var renderSize = canvas.RenderSize;
                value = new Rect(offset.X / scale, offset.Y / scale, renderSize.Width / scale, renderSize.Height / scale);
            }

            return value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        private static void OnActualViewboxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = d as VirtualCanvas;
            if (canvas != null)
            {
                canvas.InvalidateReality();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        // Made it virtual for testing purpose
        public virtual Rect ActualViewbox
        {
            get { return (Rect)GetValue(ActualViewboxProperty); }
        }

        #endregion

        #region OffsetProperty

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register("Offset", typeof(Point), typeof(VirtualCanvas), new FrameworkPropertyMetadata(new Point(0.0, 0.0), OnOffsetChanged), IsOffsetValid);

        private static bool IsOffsetValid(object value)
        {
            var point = (Point)value;
            return point.X.IsBetween(double.MinValue, double.MaxValue)
                && point.Y.IsBetween(double.MinValue, double.MaxValue);
        }

        private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ActualViewboxProperty);

            var canvas = d as VirtualCanvas;
            if (canvas != null)
            {
                canvas.OffsetOverride((Point)e.NewValue);
                canvas.OnOffsetChanged();
            }
        }

        protected virtual void OnOffsetChanged()
        {
            InvalidateReality();
            if (OffsetChanged != null)
            {
                OffsetChanged(this, null);
            }
        }

        public Point Offset
        {
            get { return (Point)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public event EventHandler OffsetChanged;


        #endregion

        #region ScaleProperty

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.RegisterAttached("Scale", typeof(double), typeof(VirtualCanvas), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.Inherits, OnScaleChanged), IsScaleValid);

        private static bool IsScaleValid(object value)
        {
            return ((double)value).IsBetween(double.Epsilon, double.MaxValue);
        }

        private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ActualViewboxProperty);

            var canvas = d as VirtualCanvas;
            if (canvas != null)
            {
                canvas.ScaleOverride((double)e.NewValue);
                canvas.OnScaleChanged();
            }
        }

        protected virtual void OnScaleChanged()
        {
            if (ScaleChanged != null)
            {
                ScaleChanged(this, null);
            }
        }

        internal void InternalScaleChanged()
        {
            OnScaleChanged();
        }

        public static double GetScale(DependencyObject element)
        {
            return (double)element.GetValue(ScaleProperty);
        }

        public static void SetScale(DependencyObject element, double scale)
        {
            element.SetValue(ScaleProperty, scale);
        }

        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        public event EventHandler ScaleChanged;

        #endregion

        #region Initialization

        /// <summary>
        /// Performs a one-time initialization of <see cref="VirtualCanvas"/>-related metadata.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static VirtualCanvas()
        {
            FrameworkElement.RenderTransformProperty.OverrideMetadata(typeof(VirtualCanvas), new FrameworkPropertyMetadata(null, CoerceRenderTransform));
        }

        /// <summary>
        /// Ensures coersion routines are invoked with their default values.
        /// </summary>
        public VirtualCanvas()
        {
            UIDispatcher.Initialize(this.Dispatcher);
            CoerceValue(ActualViewboxProperty);
            CoerceValue(RenderTransformProperty);
            this.ComputeOutlineGeometry = true;
            this.defaultFactory = new DefaultVisualFactory(this.AddVisualChild);
            this.VisualFactory = this.defaultFactory;
            this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(VirtualCanvas_IsVisibleChanged);
            this.zoomWatcher = new ZoomWatcher(this);
        }

        private ZoomWatcher zoomWatcher;
        private bool isVisible;

        private void VirtualCanvas_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                if (!isVisible)
                {
                    isVisible = true;
                    EndUpdate();
                }
            }
            else if (isVisible)
            {
                isVisible = false;
                BeginUpdate();
            }
        }

        #endregion

        #region Virtualization

        private DefaultVisualFactory defaultFactory;

        /// <summary>
        /// Handles the event that occurs when the <see cref="Items"/> property is set on this element.
        /// </summary>
        /// <param name="oldItems">The old value of <see cref="Items"/>.</param>
        /// <param name="newItems">The new value of <see cref="Items"/>.</param>
        protected virtual void OnItemsChanged(ISpatialIndex oldItems, ISpatialIndex newItems)
        {
            if (oldItems != null)
            {
                oldItems.Changed -= Items_Changed;
            }
            if (newItems != null)
            {
                newItems.Changed += Items_Changed;
            }
            Items_Changed(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handles the event that occurs when the <see cref="ISpatialIndex.Changed"/> event is raised on the <see cref="Items"/> index.
        /// </summary>
        private void Items_Changed(object sender, EventArgs e)
        {
            if (!doingLayout)
            {
                // If the items were all deleted, immediately clear the canvas.
                if (!this.Items.Any())
                {
                    this.visualChildren.Clear();
                    foreach (KeyValuePair<ISpatialItem, Visual> item in this.visualMap.ToList())
                    {
                        this.ForceVirtualizeItem(item.Key, item.Value);
                    }
                }

                InvalidateReality();
                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Update the layout of the contents of this canvas, but don't invalidate the arrangement yet.
        /// </summary>
        public void RefreshLayout()
        {
            doingLayout = true;
            try
            {
                this.UpdateLayout();
            }
            finally
            {
                doingLayout = false;
            }
        }

        private IVisualFactory factory;

        /// <summary>
        /// Get or Set the visual factory used for creating visuals from the ISpatialItems.
        /// Never returns null, if you set null, then you will get back the default factory.
        /// </summary>
        public IVisualFactory VisualFactory
        {
            get
            {
                return this.factory;
            }
            set
            {
                if (this.factory != value)
                {
                    this.factory = value;
                }
                if (this.factory == null)
                {
                    this.factory = this.defaultFactory;
                }
            }
        }

        /// <summary>
        /// Realizes and virtualizes items based on the current viewbox.
        /// </summary>
        /// <returns>An enumerator which allows this method to continue realization where it left off.</returns>
        private IEnumerator RealizeOverride()
        {
            IVisualFactory f = VisualFactory ?? this.defaultFactory;
            f.BeginRealize();
            IEnumerator<ISpatialItem> itemEnumerator = null;
            HashSet<ISpatialItem> realizedItems = new HashSet<ISpatialItem>();

            // RealizeItems
            if (this.Items != null)
            {
                IEnumerable<ISpatialItem> itemsToRealize = null;
                if (this.IsVirtualizing)
                {
                    // Only realize the items within our viewbox.
                    double scale = Scale;
                    Rect viewbox = ActualViewbox;

                    // Buffer the viewbox.
                    // This just seems to make things worse, especially when zoomed out a long way.
                    // A smarter algorithm would predict the direction we are moving and only prefetch those.
                    // viewbox.Inflate(viewbox.Width / 2, viewbox.Height / 2);

                    // Query the index for all items that intersect our viewbox.
                    // use ToList() because we can't leave the query to be lazy.
                    // by the time RealizeItems is called below the contents of the query
                    // may have mutated.
                    itemsToRealize = Items.GetItemsIntersecting(viewbox).ToList();
                }
                else
                {
                    // Get all items.
                    itemsToRealize = Items;
                }

                itemEnumerator = itemsToRealize.GetEnumerator();
                QuantizedWorkHandler realizeHandler = delegate (int realizationQuantum)
                {
                    return this.RealizeItems(itemEnumerator, realizedItems, realizationQuantum);
                };

                while (this.SelfThrottlingWorker(ref this.realizationQuantum, realizeHandler))
                {
                    yield return true;
                }

                // Raise VisualChildrenChanged only if new
                if (realizedItems.Count > 0)
                {
                    // Raise the VisualChildrenChanged event since all items are visible.
                    if (this.VisualChildrenChanged != null)
                    {
                        this.VisualChildrenChanged(this, EventArgs.Empty);
                    }
                }
            }

            // VirtualizeItems
            // Build a list of items to virtualize.
            IList<ISpatialItem> itemsToVirtualize = new List<ISpatialItem>(this.visualMap.Count);

            // Get any items that are no longer part of our result set.
            foreach (ISpatialItem item in this.visualMap.Keys)
            {
                if (!realizedItems.Contains(item))
                {
                    if (this.ShouldVirtualize(item))
                    {
                        itemsToVirtualize.Add(item);
                    }
                }
            }

            itemEnumerator = itemsToVirtualize.GetEnumerator();
            QuantizedWorkHandler virtualizingHandler = delegate (int virtualizationQuantum)
            {
                return this.VirtualizeItems(itemEnumerator, virtualizationQuantum);
            };

            while (this.SelfThrottlingWorker(ref this.virtualizationQuantum, virtualizingHandler))
            {
                yield return true;
            }

            f.EndRealize();
        }

        /// <summary>
        /// This method synchronously realizes all items in the given list if they are not already realized.
        /// </summary>
        /// <param name="itemsToRealize">The items to realize</param>
        public void RealizeItems(IEnumerable<ISpatialItem> itemsToRealize)
        {
            if (itemsToRealize != null)
            {
                foreach (ISpatialItem item in itemsToRealize)
                {
                    Visual v = this.VisualFromItem(item);
                    if (v == null)
                    {
                        v = this.RealizeItem(item, false);
                    }
                }
            }
        }


        /// <summary>
        /// RealizeItems from the itemsToRealize enumerable upto the maxItemsToRealize limit.
        /// </summary>
        /// <param name="itemsToRealize">items to realize</param>
        /// <param name="realizedItems">Set of Realized Items</param>
        /// <param name="maxItemsToRealize">Max limit of items to realize</param>
        /// <returns>count of items realized.</returns>
        private int RealizeItems(IEnumerator<ISpatialItem> itemsToRealize, HashSet<ISpatialItem> realizedItems, int maxItemsToRealize)
        {
            int itemsRealized = 0;

            // This has to happen again because of the lazy throttling that can happen after RealizeOverride has returned.
            IVisualFactory f = VisualFactory ?? this.defaultFactory;
            f.BeginRealize();

            // Realize n items where n <= this.realizationQuantum.
            while (itemsRealized < maxItemsToRealize && itemsToRealize.MoveNext())
            {
                ISpatialItem item = itemsToRealize.Current;
                Visual realizedVisual = RealizeItem(item, false);

                if (realizedVisual != null)
                {
                    itemsRealized++;
                    realizedItems.Add(item);
                }
            }

            f.EndRealize();

            return itemsRealized;
        }

        /// <summary>
        /// Virtualizes Items
        /// </summary>
        /// <param name="itemsToVirtualize">List of items to virtualize</param>
        /// <param name="maxItemsToVirtualize">Max limit of items to virtualize</param>
        /// <returns>count of virtualized items</returns>
        private int VirtualizeItems(IEnumerator<ISpatialItem> itemsToVirtualize, int maxItemsToVirtualize)
        {
            int itemsVirtualized = 0;

            // Remove them.
            while (itemsVirtualized < maxItemsToVirtualize && itemsToVirtualize.MoveNext())
            {
                ++itemsVirtualized;
                VirtualizeItem(itemsToVirtualize.Current);
            }

            return itemsVirtualized;
        }

        internal void Clear()
        {
            this.visualChildren.Clear();
            foreach (KeyValuePair<ISpatialItem, Visual> item in this.visualMap.ToList())
            {
                this.ForceVirtualizeItem(item.Key, item.Value);
            }
            this.visualMap.Clear();
        }

        #endregion

        #region Virtualization Management
        /// <summary>
        /// Stores a private copy of the realized item related to a visual.
        /// </summary>
        private static readonly DependencyPropertyKey RealizedItemPropertyKey = DependencyProperty.RegisterReadOnly("RealizedItem", typeof(object), typeof(VirtualCanvas), new PropertyMetadata());

        /// <summary>
        /// Identifies the <see cref="VirtualCanvas.RealizedItem"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RealizedItemProperty = RealizedItemPropertyKey.DependencyProperty;

        /// <summary>
        /// Maps realized items to their corresponding <see cref="Visual"/>.
        /// </summary>
        private readonly Dictionary<ISpatialItem, Visual> visualMap = new Dictionary<ISpatialItem, Visual>();

        /// <summary>
        /// Holds the deferred dispatcher operation to do the realization.
        /// </summary>
        private DispatcherOperation realizeOperation;

        private bool realizeCoreWithThrottlingPending;
        private int paused;

        public bool IsPaused { get { return paused > 0; } }

        public event EventHandler BeginChanges;
        public event EventHandler EndChanges;

        /// <summary>
        /// Call this method to start batch calls to InvalidateReality.
        /// You must call EndUpdate when you are done.
        /// </summary>
        /// <returns>A disposable object that will automatically call EndUpdate for you</returns>
        public IDisposable BeginUpdate()
        {
            CancelRealization();
            if (paused++ == 0 && BeginChanges != null)
            {
                BeginChanges(this, EventArgs.Empty);
            }
            return new DisposableAction(this.EndUpdate);
        }

        /// <summary>
        /// Call this method to end batch updating and start realizing items.
        /// </summary>
        public void EndUpdate()
        {
            if (paused > 0)
            {
                paused--;
                if (paused == 0)
                {
                    if (realizeCoreWithThrottlingPending)
                    {
                        Debug.Assert(this.realizeOperation == null, "Not expecting any realize operation at this point");
                        this.RealizeCoreWithThrottling();
                    }
                    if (EndChanges != null)
                    {
                        EndChanges(this, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Invalidates the realization state of all items being hosted by this panel. After the invalidation, the panel will have its reality updated, which will occur asynchronously unless subsequently forced by <see cref="UpdateReality"/>.
        /// </summary>
        public void InvalidateReality()
        {
            CancelRealization();
            realizeCoreWithThrottlingPending = true;
            if (paused == 0)
            {
                this.RealizeCoreWithThrottling();
            }
        }

        /// <summary>
        /// Stop realizing items
        /// </summary>
        private void CancelRealization()
        {
            if (this.realizeOperation != null)
            {
                this.realizeOperation.Abort();
                this.realizeOperation = null;
                realizeCoreWithThrottlingPending = true;
            }
        }

        /// <summary>
        /// Creates a <see cref="Visual"/> for an item and adds it as a visual child.
        /// </summary>
        /// <param name="item">The item to create a visual for.</param>
        /// <returns>The <see cref="Visual"/> created for the item.</returns>
        public Visual RealizeItem(ISpatialItem item)
        {
            return RealizeItem(item, true);
        }

        /// <summary>
        /// Creates a <see cref="Visual"/> for an item and adds it as a visual child.
        /// </summary>
        /// <param name="item">The item to create a visual for.</param>
        /// <param name="force">Whether to tell the factory to force construction of this shape</param>
        /// <returns>The <see cref="Visual"/> created for the item.</returns>
        internal Visual RealizeItem(ISpatialItem item, bool force)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (!ShouldRealize(item))
            {
                return null;
            }

            Visual visual;
            if (!this.visualMap.TryGetValue(item, out visual))
            {
                var f = VisualFactory;
                object dataItem = item.DataItem;
                visual = f.Realize(dataItem, force);
                if (visual != null)
                {
                    visual.SetValue(RealizedItemPropertyKey, item);
                    this.visualChildren.Add(visual, item.ZIndex);
                    AddVisualChild(visual);
                    // Set the data context after visual has been added to the tree so that the
                    // visual DataContextChanged event handler can find resources and other things
                    // that are inherited up the UI element hierarchy.
                    var element = visual as FrameworkElement;
                    if (element != null)
                    {
                        element.DataContext = dataItem;
                    }
                    this.visualMap.Add(item, visual);
                    itemsAdded++;
                    InvalidateMeasure();
                    InvalidateArrange();
                    if (visual is FrameworkElement e)
                    {
                        e.InvalidateVisual();
                    }
                }
            }
            else
            {
                bool updated = this.visualChildren.Update(visual, item.ZIndex);
                if (updated)
                {
                    itemsChanged++;
                    // this is the only way we can find to force WPF to redraw everything in the right order.
                    // Somehow, InvalidateVisual is not enough.
                    SetChangingZIndex(visual, true);
                    try
                    {
                        this.RemoveVisualChild(visual);
                        this.AddVisualChild(visual);
                    }
                    finally
                    {
                        visual.ClearValue(ChangingZIndexProperty);
                    }
                }
            }
            return visual;
        }



        /// <summary>
        /// Destroys and/or recycles a visual from an item and removes it as a visual child.
        /// </summary>
        /// <param name="item">The item to remove the visual for.</param>
        private void VirtualizeItem(ISpatialItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            Visual visual;
            if (this.visualMap.TryGetValue(item, out visual))
            {
                if (this.ShouldVirtualize(item))
                {
                    this.ForceVirtualizeItem(item, visual);
                }
            }
        }

        /// <summary>
        /// Forces the item to be virtualized.
        /// </summary>
        public void ForceVirtualizeItem(ISpatialItem item)
        {
            Visual visual = VisualFromItem(item);
            if (visual != null)
            {
                ForceVirtualizeItem(item, visual);
            }
        }

        private void ForceVirtualizeItem(ISpatialItem item, Visual visual)
        {
            this.visualChildren.Remove(visual);
            RemoveVisualChild(visual);
            this.visualMap.Remove(item);
            visual.ClearValue(RealizedItemPropertyKey);

            // This kills performance of scrolling and zooming because it adds a ton of extra cleanup work.
            // But we don't know why it was added yet, so we'll see what breaks when we remove it.
            // visual.ClearValue(FrameworkElement.DataContextProperty);
            itemsRemoved++;
        }

        /// <summary>
        /// Determines if the item should be virtualized.
        /// </summary>
        /// <returns>True if the item should be virtualized.</returns>
        private bool ShouldVirtualize(ISpatialItem item)
        {
            if (this.Items == null || !item.IsVisible)
            {
                return true;
            }

            UIElement visual = VisualFromItem(item) as UIElement;
            if (visual == null)
            {
                return true;
            }
            else if (visual.IsMouseCaptureWithin || visual.IsKeyboardFocusWithin || visual.IsFocused)
            {
                return false;
            }

            return VisualFactory == null || VisualFactory.Virtualize(visual);
        }

        /// <summary>
        /// Determines if the item should be realized.
        /// </summary>
        /// <returns>True if the item should be realized.</returns>
        internal static bool ShouldRealize(ISpatialItem item)
        {
            return item.IsVisible;
        }

        /// <summary>
        /// Gets the realized <see cref="Visual"/> associated with an item if the <see cref="Visual"/> has been realized already.
        /// </summary>
        /// <param name="item">The item associated with the returned <see cref="Visual"/>.</param>
        /// <returns>The <see cref="Visual"/> created in response to <see cref="RealizeItem"/> with the given item, or null if none has been created.</returns>
        public Visual VisualFromItem(ISpatialItem item)
        {
            Visual visual = null;
            this.visualMap.TryGetValue(item, out visual);
            return visual;
        }

        /// <summary>
        /// Gets the item associated with a realized <see cref="Visual"/>.
        /// </summary>
        /// <param name="visual">The <see cref="Visual"/> associated with the returned item.</param>
        /// <returns>The item that was responsible for the creation of the <see cref="Visual"/> in response to <see cref="RealizeItem"/>.
        /// It is possible to get null here if the item was virtualized after you got hold of the visual.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public ISpatialItem ItemFromVisual(Visual visual)
        {
            return visual != null ? (ISpatialItem)visual.GetValue(RealizedItemProperty) : null;
        }
        #endregion

        #region Children Management

        /// <summary>
        /// This event is raised once all the Visuals have been created for the current view.
        /// </summary>
        public event EventHandler VisualChildrenChanged;

        /// <summary>
        /// Holds the visual children of this element.
        /// </summary>
        private readonly SortedCollection<Visual> visualChildren = new SortedCollection<Visual>();

        /// <summary>
        /// Returns visual children of this element.
        /// </summary>
        public IEnumerable<Visual> VisualChildren
        {
            get
            {
                return this.visualChildren;
            }
        }

        /// <summary>
        /// Gets the number of visual children of this element.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                return this.visualChildren.Count;
            }
        }

        /// <summary>
        /// Gets a visual child of this element at the given index which is based on its z-index.
        /// </summary>
        /// <param name="index">The sequential index of the child in this element.</param>
        /// <returns>The visual for the specified index.</returns>
        protected override Visual GetVisualChild(int index)
        {
            return this.visualChildren[index];
        }
        #endregion

        #region Measure/Arrange Logic

        private bool doingLayout;

        /// <summary>
        /// Determines whether OutlineGeometries will be computed by Canvas.
        /// </summary>
        public bool ComputeOutlineGeometry
        {
            get;
            set;
        }

        /// <summary>
        /// Updates the calculated <see cref="ActualViewbox"/> and the <see cref="Scale"/> and <see cref="Offset"/> when the size changes.
        /// </summary>
        /// <param name="sizeInfo">Size information about the render size.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            CoerceValue(ActualViewboxProperty);

            base.OnRenderSizeChanged(sizeInfo);
        }

        /// <summary>
        /// Gets the applied scale transform if <see cref="UseRenderTransform"/> is set to <c>true</c>.
        /// </summary>
        private ScaleTransform AppliedScaleTransform
        {
            get
            {
                if (UseRenderTransform)
                {
                    return (ScaleTransform)((TransformGroup)RenderTransform).Children[0];
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the applied translate transform if <see cref="UseRenderTransform"/> is set to <c>true</c>.
        /// </summary>
        private TranslateTransform AppliedTranslateTransform
        {
            get
            {
                if (UseRenderTransform)
                {
                    return (TranslateTransform)((TransformGroup)RenderTransform).Children[1];
                }
                return null;
            }
        }

        /// <summary>
        /// Scales the child elements of a <see cref="VirtualCanvas"/> by applying a transform if <see cref="UseRenderTransform"/> is <c>true</c>, or invalidating Measure/Arrange otherwise.
        /// </summary>
        /// <param name="scale">The new scale of the canvas.</param>
        protected virtual void ScaleOverride(double scale)
        {
            var appliedTransform = AppliedScaleTransform;
            if (appliedTransform != null)
            {
                appliedTransform.ScaleX = scale;
                appliedTransform.ScaleY = scale;
            }
            else
            {
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Offsets the child elements of a <see cref="VirtualCanvas"/> by applying a transform if <see cref="UseRenderTransform"/> is <c>true</c>, or invalidating Arrange otherwise.
        /// </summary>
        /// <param name="offset">The new offset of the canvas.</param>
        protected virtual void OffsetOverride(Point offset)
        {
            var appliedTransform = AppliedTranslateTransform;
            if (appliedTransform != null)
            {
                appliedTransform.X = -offset.X;
                appliedTransform.Y = -offset.Y;
            }
            else
            {
                InvalidateArrange();
            }
        }

        public event EventHandler Measuring;
        public event EventHandler Measured;

        /// <summary>
        /// Measures the child elements of a <see cref="VirtualCanvas"/> in anticipation of arranging them during the <see cref="ArrangeOverride"/> pass.
        /// </summary>
        /// <param name="availableSize">An upper limit <see cref="Size"/> that should not be exceeded.</param>
        /// <returns>A <see cref="Size"/> that represents the size that is required to arrange child content.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size finalSize = new Size();

            doingLayout = true;
            try
            {
                if (Measuring != null)
                {
                    Measuring(this, EventArgs.Empty);
                }

                // Currently we are only enumerating actual visuals to get the extent that SizeToContent sizes to.
                // We may want to think about changing the definition of SizeToContent to account for virtualized items too.
                foreach (Visual visual in this.VisualChildren)
                {
                    UIElement child = visual as UIElement;
                    if (child != null)
                    {
                        // Initialize child constraint to infinity.  We need to get a "natural" size for the child in absence of constraint.
                        Size infiniteConstraint = new Size(Double.PositiveInfinity, Double.PositiveInfinity);
                        child.Measure(infiniteConstraint);

                        ISpatialItem spatial = ItemFromVisual(child) as ISpatialItem;
                        if (spatial != null && this.ComputeOutlineGeometry)
                        {
                            spatial.OnMeasure(child);
                        }
                    }
                }
                if (Measured != null)
                {
                    Measured(this, EventArgs.Empty);
                }
            }
            finally
            {
                doingLayout = false;
            }
            return finalSize;
        }

        /// <summary>
        /// Arranges the content of a <see cref="VirtualCanvas"/> element.
        /// </summary>
        /// <param name="finalSize">The size that this <see cref="VirtualCanvas"/> element should use to arrange its child elements.</param>
        /// <returns>A <see cref="Size"/> that represents the arranged size of this <see cref="VirtualCanvas"/> element and its descendants.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            using (this.BeginUpdate())
            {
                try
                {
                    if (Measuring != null)
                    {
                        Measuring(this, EventArgs.Empty);
                    }
                    doingLayout = true;

                    foreach (Visual visual in this.VisualChildren)
                    {
                        UIElement child = visual as UIElement;
                        if (child != null)
                        {
                            ISpatialItem spatialItem = (ISpatialItem)ItemFromVisual(child);
                            Rect desiredBounds = new Rect(spatialItem.Bounds.TopLeft, child.DesiredSize);
                            desiredBounds.X = desiredBounds.X.AtLeast(Single.MinValue / 2);
                            desiredBounds.Y = desiredBounds.Y.AtLeast(Single.MinValue / 2);
                            desiredBounds.Width = desiredBounds.Width.AtMost(Single.MaxValue);
                            desiredBounds.Height = desiredBounds.Height.AtMost(Single.MaxValue);
                            child.Arrange(desiredBounds);
                            child.RaiseEvent(new RoutedEventArgs(FrameworkElement.SizeChangedEvent));

                            ZoomWatcher.TickleZoomable(this.Scale, child);
                        }
                    }

                    if (Measured != null)
                    {
                        Measured(this, EventArgs.Empty);
                    }
                }
                finally
                {
                    doingLayout = false;
                }
                return finalSize;
            }
        }

        /// <summary>
        /// Returns a clipping geometry that indicates the area that will be clipped if the <see cref="ClipToBounds"/> property is set to <c>true</c>.
        /// </summary>
        /// <param name="layoutSlotSize">The available size of the element.</param>
        /// <returns>A <see cref="Geometry"/> that represents the area that is clipped if <see cref="ClipToBounds"/> is <c>true</c>.</returns>
        protected override Geometry GetLayoutClip(Size layoutSlotSize)
        {
            // VirtualCanvas only clips to bounds if ClipToBounds is set, no automatic clipping.
            return ClipToBounds ? new RectangleGeometry(new Rect(RenderSize)) : null;
        }

        #endregion

        #region Trace Switch
        /// <summary>
        /// TraceSwitch to log trace messages.
        /// </summary>
        private static TraceSwitch viewTraceSwitch = new TraceSwitch("TuvaluViewSwitch", "Tuvalu View Trace Switch");

        /// <summary>
        /// Get the Trace Switch for View dll.
        /// </summary>
        internal static TraceSwitch ViewTraceSwitch
        {
            get
            {
                return VirtualCanvas.viewTraceSwitch;
            }
        }
        #endregion


    }
}
