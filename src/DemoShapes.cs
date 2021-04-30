using System.Windows;
using System.Windows.Media;

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
    class DemoShape 
    {
        public Rect Bounds { get; set; }
        public Brush Fill { get; set; }
        public Brush Stroke { get; set; }
        public double StrokeThickness { get; set; }
        public ShapeType Type { get; set; }
    }
}
