//-----------------------------------------------------------------------
// <copyright file="UIDispatcher.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Windows.Threading;

namespace VirtualCanvasDemo.Helpers
{
    /// <summary>
    /// This class provides a way to dispatch method calls to the UI thread.
    /// </summary>
    public static class UIDispatcher
    {
        private static Dispatcher dispatcher;
        private static int managedUIThread;

        /// <summary>
        /// Initialize the UI thread dispatcher
        /// </summary>
        /// <param name="mainUIDispatcher">The Dispatcher for the UI thread</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.Rules", "SA1305:FieldNamesMustNotUseHungarianNotation")]
        internal static void Initialize(Dispatcher mainUIDispatcher)
        {
            UIThread.Initialize();
            dispatcher = mainUIDispatcher;
            managedUIThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Invoke the given delegate on the UI thread
        /// </summary>
        /// <param name="target">The delegate to invoke</param>
        /// <param name="args">Optional arguments to pass to the delegate</param>
        /// <returns>The result from the delegate</returns>
        public static object Invoke(Delegate target, params object[] args)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            // Sometimes WPF throws exception if you try and do a blocking Dispatcher.Invoke on the UI thread
            if (managedUIThread != System.Threading.Thread.CurrentThread.ManagedThreadId)
            {
                return dispatcher.Invoke(target, args);
            }
            else
            {
                return target.DynamicInvoke(args);
            }
        }

        /// <summary>
        /// Asynchronously invoke the given delegate on the UI thread.
        /// </summary>
        /// <param name="target">The delegate to invoke</param>
        /// <param name="args">Optional arguments to pass to the delegate</param>
        public static void BeginInvoke(Delegate target, params object[] args)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            dispatcher.BeginInvoke(target, args);
        }
    }
}
