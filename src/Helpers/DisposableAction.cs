//-----------------------------------------------------------------------
// <copyright file="DisposableAction.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;

namespace VirtualCanvasDemo.Helpers
{
    /// <summary>
    /// This class fires the given action when it is disposed.
    /// </summary>
    public sealed class DisposableAction : IDisposable
    {
        private Action action;

        public DisposableAction(Action action)
        {
            this.action = action;
        }

        ~DisposableAction()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (action != null && disposing)
            {
                action();
                action = null;
            }
        }
    }
}
