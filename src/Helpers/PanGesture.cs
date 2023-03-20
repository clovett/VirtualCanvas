using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace VirtualCanvasDemo.Helpers
{
    internal class PanGesture
    {
        private bool isMouseDown;
        private bool started;
        private Point previousPosition;        
        private DemoDiagram owner;

        /// <summary>
        /// Threashold used to know if we can start the movement of the selection or not. We start the movement when the distance
        /// between the previousPosition and the current mouse position is more than the threshold
        /// </summary>
        public const double Threshold = 2;

        public PanGesture(DemoDiagram owner)
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
                        this.owner.MoveBy(dx, dy);
                        this.previousPosition = position;
                    }
                }
            }
        }

        private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.isMouseDown = true;
            this.started = false;
            this.previousPosition = e.GetPosition(this.owner);
        }

        private void FinishPanning()
        {
            this.isMouseDown = false;
            this.owner.ReleaseMouseCapture();
        }
    }
}
