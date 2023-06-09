using System;
using System.Windows;
using System.Windows.Media;

namespace VirtualCanvasDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Create a ridiculous number of objects in a huge canvas to show off
            // how 2 dimensional virtualization with VirtualCanvas can help.
            double maxX = 100000;
            double maxY = 100000;
            Random r = new Random(Environment.TickCount);

            var index = new DemoSpatialIndex();
            index.Extent = new Rect(0, 0, maxX, maxY);
            for (int i = 0; i < 100000; i++)
            {
                double w = 50 + (r.NextDouble() * 150);
                double h = 50 + (r.NextDouble() * 150);
                double x = r.NextDouble() * maxX - w;
                double y = r.NextDouble() * maxY - h;
                Rect bounds = new Rect(x, y, w, h);
                index.Insert(new DemoShape()
                {
                    Bounds = bounds,
                    IsVisible = true,
                    Fill = GetRandomColor(r),
                    Stroke = GetRandomColor(r),
                    StrokeThickness = 2,
                    Type = (ShapeType)r.Next(4),
                    StarPoints = r.Next(4, 10)
                });
            }
            this.Diagram.Index = index;
            this.Diagram.ScrollExtent = index.Extent;
            this.Diagram.MoveTo(new Point(5000, 5000), true);
            this.WindowState = WindowState.Normal;
        }

        private Brush GetRandomColor(Random r)
        {
            return new SolidColorBrush(Color.FromArgb((byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255)));
        }

    }
}
