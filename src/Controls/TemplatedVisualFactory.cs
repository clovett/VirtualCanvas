//-----------------------------------------------------------------------
// <copyright file="TemplatedVisualFactory.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using VirtualCanvasDemo.Interfaces;

namespace VirtualCanvasDemo.Controls
{
    /// <summary>
    /// This factory forms the base class for a factory that looks up a DataTemplate for an object and creates a visual for the object.
    /// If no template is found it creates a TextBlock.
    /// </summary>
    public abstract class TemplatedVisualFactory : IVisualFactory
    {
        /// <summary>
        /// A cache of templates for realized types.
        /// </summary>
        private Dictionary<Type, DataTemplate> templates = new Dictionary<Type, DataTemplate>();

        #region IVisualFactory
        /// <summary>
        /// Begin the pass of the realization scope
        /// </summary>
        public virtual void BeginRealize()
        {
            templates.Clear();
        }

        /// <summary>
        /// Finish the pass of the realization scope.
        /// </summary>
        public virtual void EndRealize()
        {
            templates.Clear();
        }

        /// <summary>
        /// Realizes a template-based Visual for the given item.
        /// </summary>
        /// <param name="item">The item to realize a Visual for.</param>
        /// <param name="force">Whether visual is being forced to be visible or not</param>
        /// <returns>A visual for the item.</returns>
        /// <remarks>Override this to replace the entire Realization mechanism.</remarks>
        public virtual Visual Realize(object item, bool force)
        {
            if (item == null)
            {
                return null;
            }
            DataTemplate template = null;
            if (!templates.TryGetValue(item.GetType(), out template))
            {
                template = this.ChooseTemplate(item);
                if (template != null)
                {
                    templates[item.GetType()] = template;
                }
            }
            Visual result = null;
            if (template != null)
            {
                result = this.ProduceVisual(item, template);

                // If the template was null or produced no visual, try the fallback.
                if (result == null)
                {
                    result = this.ProduceDefaultVisual(item);
                }
            }
            return result;
        }

        /// <summary>
        /// No virtualization recycling in this implementation.
        /// </summary>
        /// <returns>The constant true</returns>
        public virtual bool Virtualize(Visual visual)
        {
            // Do nothing.
            return true;
        }
        #endregion

        #region Extension Points
        /// <summary>
        /// Product a visual for a given template
        /// </summary>
        /// <param name="item">The item the Visual is being produced for</param>
        /// <param name="templateForVisual">The pre-selected template to produce a visual for</param>
        /// <returns>The visual for the given template</returns>
        protected virtual Visual ProduceVisual(object item, DataTemplate templateForVisual)
        {
            if (templateForVisual == null)
            {
                return null;
            }
            return templateForVisual.LoadContent() as Visual;
        }

        /// <summary>
        /// Get the default visual without using a template.
        /// Called by the Realize method when a template is not found for the item.
        /// </summary>
        /// <param name="item">The item the default Visual is being produced for</param>
        /// <returns>The default visual for the item</returns>
        protected virtual Visual ProduceDefaultVisual(object item)
        {
            TextBlock defaultVisual = new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 8,
                ClipToBounds = false
            };

            if (item != null)
            {
                try
                {
                    ((IAddChild)defaultVisual).AddText(string.Format(CultureInfo.CurrentCulture, "DataTemplateNotFound: {0}", item.GetType().Name));
                }
                catch (ArgumentException)
                {
                    // We can cope with failure to add text.
                }
            }

            return defaultVisual;
        }

        /// <summary>
        /// Choose a template for the given item.
        /// </summary>
        /// <param name="item">the item to choose a template for</param>
        /// <returns>A template for the given item or null if one cannot be located</returns>
        protected abstract DataTemplate ChooseTemplate(object item);
        #endregion
    }
}
