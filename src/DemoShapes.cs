using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using VirtualCanvasDemo.Interfaces;

namespace VirtualCanvasDemo
{
    public enum ShapeType
    {
        Ellipse,
        Rect,
        RoundedRect,
        Star
    }

    /// <summary>
    /// A light weight "data" object that contains the bare minimum info needed to 
    /// create the virtualized visuals in the DemoShapeFactory.
    /// </summary>
    class DemoShape : ISpatialItem, INotifyPropertyChanged
    {
        private Rect bounds;
        private double priority;
        private int zindex;
        private bool visible;
        private bool selected;
        private Brush fill;
        private Brush stroke;
        private double strokeThickness;
        private ShapeType shapeType;

        public event PropertyChangedEventHandler PropertyChanged;

        public DemoShape()
        {
            this.DataItem = this;
        }

        /// <summary>
        /// The bounds of the item.
        /// </summary>
        public Rect Bounds
        {
            get => this.bounds;
            set
            {
                if (this.bounds != value)
                {
                    this.bounds = value;
                    this.OnChanged("Bounds");
                }
            }
        }

        /// <summary>
        /// A value indicating how visually important the element is relative to other items.
        /// Higher priorities will be displayed first.
        /// </summary>
        public double Priority
        {
            get => this.priority;
            set
            {
                if (this.priority != value)
                {
                    this.priority = value;
                    this.OnChanged("Priority");
                }
            }
        }

        /// <summary>
        /// The Z-Index of the visual relative to other items.
        /// Higher ZIndexs will be drawn on top.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ZIndex")]
        public int ZIndex
        {
            get => zindex;
            set
            {
                if (this.zindex != value)
                {
                    this.zindex = value;
                    this.OnChanged("ZIndex");
                }
            }
        }


        /// <summary>
        /// Invoked when Visual whose DataContext is this ISpatialItem is measured.
        /// </summary>
        /// <param name="visual">The Visual corresponding to the ISpatialItem</param>
        public void OnMeasure(UIElement visual)
        {
            // TBD
        }

        /// <summary>
        /// Return a user defined data item associated with this object.
        /// </summary>
        public object DataItem { get; set; }

        /// <summary>
        /// Determines whether the item is visible on the canvas.
        /// </summary>
        public bool IsVisible
        {
            get => this.visible; set
            {
                if (this.visible != value)
                {
                    this.visible = value;
                    this.OnChanged("IsVisible");
                }
            }
        }


        public bool IsSelected
        {
            get => this.selected;
            set
            {
                if (this.selected != value)
                {
                    this.selected = value;
                    this.OnChanged("IsSelected");
                }
            }
        }


        public Brush Fill
        {
            get => this.fill;
            set
            {
                if (this.fill != value)
                {
                    this.fill = value;
                    this.OnChanged("Fill");
                }
            }
        }

        public Brush Stroke
        {
            get => this.stroke;
            set
            {
                if (this.stroke != value)
                {
                    this.stroke = value;
                    this.OnChanged("Stroke");
                }
            }
        }
        public double StrokeThickness
        {
            get => this.strokeThickness;
            set
            {
                if (this.strokeThickness != value)
                {
                    this.strokeThickness = value;
                    this.OnChanged("StrokeThickness");
                }
            }
        }

        public ShapeType Type
        {
            get => this.shapeType;
            set
            {
                if (this.shapeType != value)
                {
                    this.shapeType = value;
                    this.OnChanged("Type");
                }
            }
        }

        private void OnChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
