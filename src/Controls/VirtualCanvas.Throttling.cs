//-----------------------------------------------------------------------
// <copyright file="VirtualCanvas.Throttling.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace VirtualCanvasDemo.Controls
{
    /// <summary>
    /// Throttling related code for Virtual Canvas
    /// </summary>
    public partial class VirtualCanvas
    {
        private int itemsAdded; // since the last event
        private int itemsRemoved;
        private int itemsChanged;
        private int minimumQuantum = 10;
        private int throttlingLimit = 0;
        private long idealDuration = 10; // in milliseconds
        private int realizationQuantum = 10;
        private int virtualizationQuantum = 50;
        private delegate int QuantizedWorkHandler(int quantum);
        private Stopwatch throttlingWorkerWatch = new Stopwatch();



        public static bool GetChangingZIndex(DependencyObject obj)
        {
            return (bool)obj.GetValue(ChangingZIndexProperty);
        }

        public static void SetChangingZIndex(DependencyObject obj, bool value)
        {
            obj.SetValue(ChangingZIndexProperty, value);
        }

        // Using a DependencyProperty as the backing store for ChangingZIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChangingZIndexProperty =
            DependencyProperty.RegisterAttached("ChangingZIndex", typeof(bool), typeof(VirtualCanvas), new UIPropertyMetadata(false));



        /// <summary>
        /// Determines the number of items that will be realized/virtualized at a time.
        /// Set to a value less than 1 for auto-throttling.
        /// </summary>
        public int ThrottlingLimit
        {
            get
            {
                return this.throttlingLimit;
            }
            set
            {
                this.throttlingLimit = value;
            }
        }

        private void RealizeCoreWithThrottling()
        {
           if (viewTraceSwitch.TraceInfo)
           {
                Trace.WriteLine("Virtual Canvas Realization Invoked.");
           }

            realizeCoreWithThrottlingPending = false;

            // enumerator keeps track of where we are in terms of work completed.
            // work is complete when enumerator.MoveNext() returns false.
            IEnumerator enumerator = null;
            Action action = null;
            action = delegate
            {
                enumerator = RealizeCore(enumerator);
                if (enumerator != null && this.realizeOperation != null)
                {
                    this.realizeOperation = Dispatcher.BeginInvoke(action, DispatcherPriority.Input);
                }
                else
                {
                    this.realizeOperation = null;
                    if (viewTraceSwitch.TraceInfo)
                    {
                        Trace.WriteLine("Virtual Canvas Realization Completed.");
                    }
                }
            };

            this.realizeOperation = Dispatcher.BeginInvoke(action, DispatcherPriority.Input);
        }

        /// <summary>
        /// Performs throttling of <see cref="RealizeOverride"/> based on how long <see cref="RealizeOverride"/> took during the previous call.
        /// </summary>
        /// <param name="enumerator">The previous return value of this method. Helps pick up the RealizeCore task from where it was last left off.</param>
        /// <returns>A non-<c>null</c> value if further realization is required; otherwise, <c>null</c>.</returns>
        private IEnumerator RealizeCore(IEnumerator enumerator)
        {
            var realizationEnumerator = enumerator ?? RealizeOverride();
            if (realizationEnumerator.MoveNext())
            {
                return realizationEnumerator;
            }
            else
            {
                this.realizeOperation = null;
                this.OnRealizationComplete();
                return null;
            }
        }

        /// <summary>
        /// Helper method that chunks up the work done by the handler.
        /// The method returns true while there is more work to be done by the handler.
        /// quantum parameter determines the size of one chunck of work to be done by the handler.
        /// </summary>
        /// <param name="quantum">The current quantum allocation. Each call updates so that the handler will more likely hit the ideal time it takes for each chunck of work</param>
        /// <param name="handler">The handler to call that does the work being throttled</param>
        /// <returns>Returns true if there is more work to be done by the handler. false otherwise.</returns>
        private bool SelfThrottlingWorker(ref int quantum, QuantizedWorkHandler handler)
        {
            int work;
            int workDone;
            bool workRemaining = true;

            if (this.throttlingLimit > 0)
            {
                work = this.throttlingLimit;
                workDone = handler(work);
            }
            else
            {
                work = quantum;
                this.throttlingWorkerWatch.Restart();
                workDone = handler(work);
                this.throttlingWorkerWatch.Stop();
                long duration = this.throttlingWorkerWatch.ElapsedMilliseconds;
                if (workDone > 0 && duration > 0)
                {
                    long adjustedQuantum = (workDone * this.idealDuration) / duration;
                    quantum = Math.Max(this.minimumQuantum, (int)Math.Min(adjustedQuantum, int.MaxValue));
                }
            }

            if (workDone < work)
            {
                workRemaining = false;
            }

            return workRemaining;
        }


        #region Realization Complete Check
        /// <summary>
        /// Action parameter is invoked immediately if Realization is currently complete.
        /// The method call returns immediately if realization is incomplete. When realization completes, the action will be invoked.
        /// </summary>
        public void NotifyOnRealizationCompleted(Action action)
        {
            lock (this.realizationDelegates)
            {
                this.realizationDelegates.Enqueue(action);
            }
            if (this.realizeOperation == null)
            {
                this.OnRealizationComplete();
            }
        }

        private void OnRealizationComplete()
        {
            while (this.realizationDelegates.Count > 0)
            {
                Action action = null;
                lock (this.realizationDelegates)
                {
                    action = this.realizationDelegates.Dequeue();
                }
                action();
            }

            if (itemsAdded > 0 || itemsRemoved > 0 || itemsChanged > 0)
            {
                itemsAdded = itemsRemoved = itemsChanged = 0;
                if (RealizationCompleted != null)
                {
                    RealizationCompleted(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// This event is raised any time realization has been completed.
        /// </summary>
        public event EventHandler RealizationCompleted;

        /// <summary>
        /// List of delegates to be invoked on Realization
        /// </summary>
        private Queue<Action> realizationDelegates = new Queue<Action>();
        #endregion
    }
}
