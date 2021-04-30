//-----------------------------------------------------------------------
// <copyright file="IScopedChanges.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;

namespace VirtualCanvasDemo.Interfaces
{
    /// <summary>
    /// An implementer of this interface fires matched events at the start and end of changes
    /// </summary>
    internal interface IScopedChanges
    {
        event EventHandler BeginChanges;
        event EventHandler EndChanges;
    }
}