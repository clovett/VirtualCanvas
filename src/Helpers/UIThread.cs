//-----------------------------------------------------------------------
// <copyright file="UIThread.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace VirtualCanvasDemo.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows.Threading;

    /// <summary>
    /// Delegate used for tasks that are executed on the UI thread.
    /// </summary>
    /// <returns>The delegate must return TRUE if the tasks has more work to do.
    /// Otherwise, the task has completed and it should return FALSE.</returns>
    internal delegate bool TaskForUIThread(object context);

    /// <summary>
    /// This class represents a cancellable asynchronous result.  You get this from BeginExecuteTask and
    /// you can cancel the task (if it has not started executing yet) by calling the Cancel method.
    /// </summary>
    internal interface ICancellableAsyncResult : IAsyncResult
    {
        bool IsCancelled { get; }
        void Cancel();
        Exception Exception { get; }
    }

    /// <summary>
    /// UIThread is a static class that executes tasks
    /// on the UI thread
    /// </summary>
    internal static class UIThread
    {
        internal class UiPumpingWaitHandle : WaitHandle
        {
            private WaitHandle eventHandle;
            private const int MillisecondSlice = 30;
            private TaskData task;

            public UiPumpingWaitHandle(TaskData task, WaitHandle eventHandle)
            {
                this.task = task;
                this.eventHandle = eventHandle;
            }
            public override bool WaitOne()
            {
                if (UIThread.IsUIThread(Thread.CurrentThread))
                {
                    // then use our method below which pumps the task.
                    while (!WaitOne(MillisecondSlice))
                    {
                    }
                }
                else
                {
                    return eventHandle.WaitOne();
                }
                return true;
            }

            public override bool WaitOne(int millisecondsTimeout)
            {
                if (UIThread.IsUIThread(Thread.CurrentThread))
                {
                    int start = Environment.TickCount;
                    while (!task.IsCompleted && !task.IsCancelled)
                    {
                        // we're trying to execute a task on the UI thread, so pump task until it is complete
                        // or until we hit the given timeout.
                        while (UIThread.RunTask(task) && !task.IsCancelled)
                        {
                            int now = Environment.TickCount;
                            int diff = now - start;
                            if (diff > millisecondsTimeout)
                            {
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    return eventHandle.WaitOne(millisecondsTimeout);
                }
                return true;
            }
        }

        /// <summary>
        /// used to hold task data and implement ICancellableAsyncResult
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
        internal class TaskData : ICancellableAsyncResult
        {
            private bool complete;
            private AutoResetEvent eventHandle;
            private UiPumpingWaitHandle handle;

            #region ICancellableAsyncResult
            public bool IsCancelled { get; set; }

            public void Cancel()
            {
                IsCancelled = true;
            }

            public Exception Exception { get; set; }
            #endregion

            #region IAsyncResult

            public object AsyncState { get; set; }

            public WaitHandle AsyncWaitHandle
            {
                get { return handle; }
            }

            public bool CompletedSynchronously { get; set; }

            public bool IsCompleted
            {
                get
                {
                    return complete;
                }
                set
                {
                    complete = value;
                    if (value)
                    {
                        eventHandle.Set();
                    }
                }
            }

            #endregion

            /// <summary>
            /// The task delegate
            /// </summary>
            public TaskForUIThread Task { get; set; }

            /// <summary>
            /// private constructor
            /// </summary>
            private TaskData()
            {
            }

            /// <summary>
            /// TaskData constructor
            /// </summary>
            /// <param name="task">The task to be executed</param>
            /// <param name="context">The context to pass to the task</param>
            /// <param name="interval">The interval to call the task at</param>
            public TaskData(TaskForUIThread task, object context, TimeSpan interval)
                : this()
            {
                Task = task;
                AsyncState = context;
                eventHandle = new AutoResetEvent(false);
                handle = new UiPumpingWaitHandle(this, eventHandle);
                Interval = interval;
            }

            public TimeSpan Interval { get; set; }

        }

        /// <summary>
        /// list of tasks to run on UI thread
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "This is not hungarian notation")]
        private static List<TaskData> uiTasks;

        /// <summary>
        /// The static UI thread object.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "This is not hungarian notation")]
        private static Thread uiThread;

        /// <summary>
        /// initialized flag
        /// </summary>
        private static bool initialized;

        /// <summary>
        /// Whether we've been disposed.
        /// </summary>
        private static bool terminated;

        /// <summary>
        /// UI dispatcher.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "This is not hungarian notation")]
        private static Dispatcher uiDispatcher;

        /// <summary>
        /// static constructor
        /// </summary>
        static UIThread()
        {
            initialized = false;
        }

        private static DispatcherTimer timer;

        // The UIThread method allows at most one thread to claim itself as the UI thread.
        // We allow tests to bypass that restriction to allow multiple tests to be run in parallel on different threads.
        // Use this property with caution and use it only when your tests aren't dependent on actual layouting.
        internal static bool TestEnvironment { get; set; }

        /// <summary>
        /// Initializes the UIThread class
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "This is not hungarian notation")]
        public static void Initialize()
        {
            Thread uiThread = System.Threading.Thread.CurrentThread;
            System.Diagnostics.Debug.Assert(!initialized || TestEnvironment || UIThread.uiThread == uiThread, "There can only be one UI thread!");
            if (!initialized)
            {
                UIThread.uiTasks = new List<TaskData>();
                UIThread.uiThread = uiThread;
                UIThread.initialized = true;
                UIThread.timer = new DispatcherTimer(TimeSpan.FromMilliseconds(1), DispatcherPriority.Normal, new EventHandler(OnTick), Dispatcher.CurrentDispatcher);
                UIThread.timer.IsEnabled = false;
                UIThread.uiDispatcher = Dispatcher.FromThread(uiThread);
            }
        }

        /// <summary>
        /// Symmetric operation to <see cref="Initialize"/>.
        /// Only for test purposes
        /// </summary>
        internal /*for test purposes*/ static void Cleanup()
        {
            UIThread.uiTasks = null;
            UIThread.uiThread = null;
            UIThread.initialized = false;
            if (UIThread.timer != null)
            {
                UIThread.timer.Stop();
                UIThread.timer = null;
            }
            UIThread.uiDispatcher = null;
        }

        private static void OnTick(object sender, EventArgs e)
        {
            PumpTasks();
        }

        private static void Start()
        {
            timer.IsEnabled = true;
            timer.Interval = GetMinInterval();
            timer.Start();
        }

        private static TimeSpan GetMinInterval()
        {
            TimeSpan result = TimeSpan.FromSeconds(1);
            foreach (TaskData data in uiTasks)
            {
                if (data.Interval < result)
                {
                    result = data.Interval;
                }
            }
            return result;
        }

        private static void Stop()
        {
            timer.Stop();
            timer.IsEnabled = false;
        }

        /// <summary>
        /// Tell this class that we are shutting down the process so it knows not to block on UI thread any more.
        /// </summary>
        public static void Terminate()
        {
            terminated = true;
            timer.Stop();
            uiTasks.Clear(); // stop memory leaks on shutdown.
        }

        /// <summary>
        /// Get whether we have been terminated.
        /// </summary>
        public static bool IsTerminated { get { return terminated; } }


        /// <summary>
        /// Determines if passed in thread is the UI thread
        /// </summary>
        /// <param name="thread">The thread to check</param>
        /// <returns>true if the passed in thread is the UI thread, false otherwise</returns>
        public static bool IsUIThread(System.Threading.Thread thread)
        {
            return thread == uiThread;
        }

        /// <summary>
        /// Run all pending UI tasks now!
        /// </summary>
        public static void RunPendingTasks()
        {
            PumpTasks();
        }

        /// <summary>
        /// Pumps all tasks and removes any that complete from the list of tasks.
        /// </summary>
        internal static void PumpTasks()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("UIThreadClassNotInitialized");
            }

            if (Thread.CurrentThread != uiThread)
            {
                throw new InvalidOperationException("CanOnlyCallPumpTasksFromUIThread");
            }

            lock (uiTasks)
            {
                int count = uiTasks.Count;

                // special case when just 1 task exists (for better perf).
                if (count == 1)
                {
                    // if the task returns false, then task is complete and we can remove it from our list
                    TaskData data = uiTasks[0];
                    if (!UIThread.RunTask(data))
                    {
                        // Cannot clear the list here because RunTask may have re-entrantly queued another task.
                        uiTasks.Remove(data);
                    }
                }
                else if (count > 1)
                {
                    // we're inside a lock, so we can do whatever we like to the _uiTasks list.

                    for (int i = 0; i < count; i++)
                    {
                        // if the task returns false, then task is complete and we can remove it from our list
                        TaskData data = uiTasks[i];
                        if (!UIThread.RunTask(data))
                        {
                            // remove task from the list
                            uiTasks.Remove(data);
                            count = uiTasks.Count;
                            i--;
                        }
                    }
                }

                if (uiTasks.Count == 0)
                {
                    Stop();
                }
                else
                {
                    // re-calc the timer interval
                    Start();
                }
            }
        }

        /// <summary>
        /// runs the task.
        /// </summary>
        /// <param name="taskData">The task data to run</param>
        /// <returns>true if task has more work to do, false if task is done running or threw an exception</returns>
        private static bool RunTask(TaskData taskData)
        {
            bool result;

            try
            {
                if (taskData.IsCancelled)
                {
                    taskData.IsCompleted = true;
                    result = false; // task is done.
                }
                else
                {
                    result = taskData.Task(taskData.AsyncState);
                    if (!result)
                    {
                        taskData.IsCompleted = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // if an exception is thrown, abort the task. the task itself is responsible for correct handling
                // of the exception to ensure it is reported (or not). For example, the task itself might catch an
                // exception and add it to an ActionContext.Error list.
                taskData.Exception = ex;
                taskData.IsCompleted = true;
                result = false;

                if (ex is OutOfMemoryException
                    || ex is StackOverflowException
                    || ex is ThreadAbortException)
                {
                    throw;
                }
            }

            return result;
        }

        /// <summary>
        /// Queue a new UI task with given delay interval and optional context.
        /// </summary>
        /// <param name="task">The task to execute</param>
        /// <param name="context">The context to pass to the task when it is being called on the UI thread</param>
        /// <param name="interval">The interval at which to pump the task</param>
        /// <returns>The cancellable result</returns>
        public static ICancellableAsyncResult BeginExecuteTask(TaskForUIThread task, object context, TimeSpan interval)
        {
            if (!initialized)
            {
                throw new InvalidOperationException("UIThreadClassNotInitialized");
            }
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            TaskData taskData = new TaskData(task, context, interval);

            if (terminated)
            {
                taskData.Cancel(); // can't run it if UI has terminated.
                return taskData;
            }

            lock (uiTasks)
            {
                // add the task
                uiTasks.Add(taskData);
                Start();
            }
            return taskData;
        }

        /// <summary>
        /// Adds a task to be run on the main UI thread and blocks this thread until the task completes.
        /// </summary>
        public static void ExecuteTask(TaskForUIThread task, object context)
        {
            ExecuteTask(task, context, TimeSpan.FromMilliseconds(1));
        }

        /// <summary>
        /// Adds a task to be run on the main UI thread and blocks this thread until the task completes.
        /// The task is pumped at the given interval
        /// </summary>
        public static void ExecuteTask(TaskForUIThread task, object context, TimeSpan interval)
        {
            ICancellableAsyncResult result = BeginExecuteTask(task, context, interval);
            if (!result.IsCompleted)
            {
                // block forever waiting!
                result.AsyncWaitHandle.WaitOne();
            }
        }

        public static Dispatcher GetUIDispatcher()
        {
            Debug.Assert(uiDispatcher != null, "Trying to get UI Dispatcher before UIThread is initialized!");
            return uiDispatcher;
        }
    }
}
