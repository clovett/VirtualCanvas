//-----------------------------------------------------------------------
// <copyright file="IVisualFactory.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System.Windows.Media;

namespace VirtualCanvasDemo.Interfaces
{
    /// <summary>
    /// This interface allows you to plugin a different way of creating visuals in the
    /// VirtualCanvas other than using the default which uses DataTemplates.
    /// </summary>
    public interface IVisualFactory
    {
        /// <summary>
        /// This method is called at the beginning of each realization scope.
        /// </summary>
        void BeginRealize();

        /// <summary>
        /// This method is called at the end of each realization scope.
        /// </summary>
        void EndRealize();

        /// <summary>
        /// This method is called to create a Visual for the given object
        /// </summary>
        /// <param name="item">The object that needs a visual</param>
        /// <param name="force">Whether factory must comply or not. For example, if the user calls RealizeItem this will
        /// be set to true, because there is a reason the user must see this visual (for example, it might be a CTRL+F
        /// scenario).  If this is false, then the Factory is free to apply any sort of semantic zooming logic and decide
        /// not to create this visual at this time.</param>
        /// <returns>The visual you want or null of you don't want any</returns>
        Visual Realize(object item, bool force);

        /// <summary>
        /// This method is called when a visual is no longer needed (perhaps it scrolled
        /// offscreen).  You can use this method to implement your own visual recycling.
        /// </summary>
        /// <param name="visual">The visual that is no longer needed</param>
        /// <returns>Return false if you don't want to virtualize this visual right now</returns>
        bool Virtualize(Visual visual);
    }
}
