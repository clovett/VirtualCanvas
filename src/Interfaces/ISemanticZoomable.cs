//-----------------------------------------------------------------------
// <copyright file="ISemanticZoomable.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualCanvasDemo.Interfaces
{
    /// <summary>
    /// This interface can be implemented by a visual that wants to react to zoom changes.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public interface ISemanticZoomable
    {
        void OnZoomChange(double newZoomLevel);
    }
}
