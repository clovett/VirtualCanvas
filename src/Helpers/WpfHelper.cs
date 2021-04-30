//-----------------------------------------------------------------------
// <copyright file="WpfHelper.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace VirtualCanvasDemo.Helpers
{
    internal static class WpfHelper
    {
        /// <summary>
        /// Returns the center of the given rectangle.
        /// </summary>
        /// <param name="rectangle">Any rectangle</param>
        /// <returns>The center point</returns>
        public static Point GetCenter(this Rect rectangle)
        {
            return new Point(rectangle.Left + (rectangle.Width / 2), rectangle.Top + (rectangle.Height / 2));
        }

        /// <summary>
        /// Finds a child element that is also Focusable.
        /// </summary>
        /// <param name="visual">The parent of the UI hierarchy to search</param>
        /// <returns>The focusable element or null.</returns>
        public static IInputElement FindFocusableElement(DependencyObject visual)
        {
            IInputElement input = visual as IInputElement;
            if (input != null && input.Focusable)
            {
                return input;
            }
            for (int i = 0, n = VisualTreeHelper.GetChildrenCount(visual); i < n; i++)
            {
                input = FindFocusableElement(VisualTreeHelper.GetChild(visual, i));
                if (input != null)
                {
                    return input;
                }
            }
            return null;
        }

        /// <summary>
        /// Indicates whether the specified rectangle intersects with the current rectangle, properly considering the empty rect and infinities.
        /// </summary>
        /// <param name="curr">The current rectangle.</param>
        /// <param name="rect">The rectangle to check.</param>
        /// <returns><c>true</c> if the specified rectangle intersects with the current rectangle; otherwise, <c>false</c>.</returns>
        public static bool Intersects(this Rect curr, Rect rect)
        {
            return (curr.IsEmpty || rect.IsEmpty) ||
                   ((curr.Width == Double.PositiveInfinity || curr.Right >= rect.Left) &&
                   (rect.Width == Double.PositiveInfinity || rect.Right >= curr.Left) &&
                   (curr.Height == Double.PositiveInfinity || curr.Bottom >= rect.Top) &&
                   (rect.Height == Double.PositiveInfinity || rect.Bottom >= curr.Top));
        }

        /// <summary>
        /// Indicates whether the current <see cref="Rect"/> defines a real area in space.
        /// </summary>
        /// <param name="rect">The current rectangle.</param>
        /// <returns><c>true</c> if rect is well-defined, which is not the case for <see cref="Rect.Empty"/> or if any of the fields are <see cref="double.NaN"/>.</returns>
        public static bool IsDefined(this Rect rect)
        {
            return rect.Top < double.PositiveInfinity
                && rect.Left < double.PositiveInfinity
                && rect.Width > double.NegativeInfinity
                && rect.Height > double.NegativeInfinity;
        }


        /// <summary>
        /// Indicates whether the current <see cref="Size"/> defines a real area in space and is not zero or infinity or Nan
        /// </summary>
        /// <param name="size">The size to test.</param>
        /// <returns><c>true</c> if size is well-defined, which is not the case for <see cref="Rect.Empty"/> or if any of the fields are <see cref="double.NaN"/>.</returns>
        public static bool IsDefined(this Size size)
        {
            return size.Width != 0 && size.Height != 0 &&
                !double.IsNaN(size.Width) && !double.IsNaN(size.Height) &&
                !double.IsInfinity(size.Width) && !double.IsInfinity(size.Height);
        }

        /// <summary>
        /// Attaches a binding to this object, based on the provided source and property name as a path qualification to the data source.
        /// </summary>
        /// <param name="target">The <see cref="DependencyObject"/> to establish the binding on.</param>
        /// <param name="dp">Identifies the destination property where the binding should be established.</param>
        /// <param name="source">The data source used as the root of the binding.</param>
        /// <param name="property">The source dependency property used for the binding.</param>
        /// <returns>Records the conditions of the binding. This return value can be useful for error checking.</returns>
        internal static BindingExpression SetBinding(this DependencyObject target, DependencyProperty dp, object source, DependencyProperty property)
        {
            return SetBinding(target, dp, source, property, BindingMode.Default);
        }

        /// <summary>
        /// Attaches a binding to this object, based on the provided source and property name as a path qualification to the data source.
        /// </summary>
        /// <param name="target">The <see cref="DependencyObject"/> to establish the binding on.</param>
        /// <param name="dp">Identifies the destination property where the binding should be established.</param>
        /// <param name="source">The data source used as the root of the binding.</param>
        /// <param name="property">The source dependency property used for the binding.</param>
        /// <param name="mode">The direction of the data flow in the binding.</param>
        /// <returns>Records the conditions of the binding. This return value can be useful for error checking.</returns>
        internal static BindingExpression SetBinding(this DependencyObject target, DependencyProperty dp, object source, DependencyProperty property, BindingMode mode)
        {
            return SetBinding(target, dp, source, property, mode, null);
        }

        /// <summary>
        /// Attaches a binding to this object, based on the provided source and property name as a path qualification to the data source.
        /// </summary>
        /// <param name="target">The <see cref="DependencyObject"/> to establish the binding on.</param>
        /// <param name="dp">Identifies the destination property where the binding should be established.</param>
        /// <param name="source">The data source used as the root of the binding.</param>
        /// <param name="property">The source dependency property used for the binding.</param>
        /// <param name="mode">The direction of the data flow in the binding.</param>
        /// <param name="format">The string format direction of in the binding.</param>
        /// <returns>Records the conditions of the binding. This return value can be useful for error checking.</returns>
        internal static BindingExpression SetBinding(this DependencyObject target, DependencyProperty dp, object source, DependencyProperty property, BindingMode mode, string format)
        {
            return SetBinding(target, dp, source, property, mode, format, null);
        }

        /// <summary>
        /// Attaches a binding to this object, based on the provided source and property name as a path qualification to the data source.
        /// </summary>
        /// <param name="target">The <see cref="DependencyObject"/> to establish the binding on.</param>
        /// <param name="dp">Identifies the destination property where the binding should be established.</param>
        /// <param name="source">The data source used as the root of the binding.</param>
        /// <param name="property">The source dependency property used for the binding.</param>
        /// <param name="mode">The direction of the data flow in the binding.</param>
        /// <param name="format">The string format direction of in the binding.</param>
        /// <param name="converter">The value converter that is used with the new binding.</param>
        /// <returns>Records the conditions of the binding. This return value can be useful for error checking.</returns>
        internal static BindingExpression SetBinding(this DependencyObject target, DependencyProperty dp, object source, DependencyProperty property, BindingMode mode, string format, IValueConverter converter)
        {
            return SetBinding(target, dp, source, property, mode, format, converter, null);
        }

        /// <summary>
        /// Attaches a binding to this object, based on the provided source and property name as a path qualification to the data source.
        /// </summary>
        /// <param name="target">The <see cref="DependencyObject"/> to establish the binding on.</param>
        /// <param name="dp">Identifies the destination property where the binding should be established.</param>
        /// <param name="source">The data source used as the root of the binding.</param>
        /// <param name="property">The source dependency property used for the binding.</param>
        /// <param name="mode">The direction of the data flow in the binding.</param>
        /// <param name="format">The string format direction of in the binding.</param>
        /// <param name="converter">The value converter that is used with the new binding.</param>
        /// <param name="converterParameter">The parameter to pass to the <paramref name="converter"/> in the binding.</param>
        /// <returns>Records the conditions of the binding. This return value can be useful for error checking.</returns>
        internal static BindingExpression SetBinding(this DependencyObject target, DependencyProperty dp, object source, DependencyProperty property, BindingMode mode, string format, IValueConverter converter, object converterParameter)
        {
            var binding = new Binding();
            binding.Path = new PropertyPath(property);
            binding.Mode = mode;
            binding.Source = source;
            binding.StringFormat = format;
            binding.Converter = converter;
            binding.ConverterParameter = converterParameter;
            return (BindingExpression)BindingOperations.SetBinding(target, dp, binding);
        }

        /// <summary>
        /// Calculates the transform Matrix that needs to be appled to convert this bounds to newBounds.
        /// </summary>
        /// <param name="oldBounds">old Bounds</param>
        /// <param name="newBounds">new Bounds</param>
        /// <returns>A Matrix that transforms oldBounds to newBounds</returns>
        internal static Matrix TransformTo(this Rect oldBounds, Rect newBounds)
        {
            if (oldBounds == Rect.Empty || newBounds == Rect.Empty)
            {
                throw new ArgumentException("Rect parameter cannot have a value of Rect.Empty");
            }

            double scaleX = newBounds.Width / oldBounds.Width;
            double scaleY = newBounds.Height / oldBounds.Height;
            double translateX = newBounds.X - (oldBounds.X * scaleX);
            double translateY = newBounds.Y - (oldBounds.Y * scaleY);

            return new Matrix(scaleX, 0.0, 0.0, scaleY, translateX, translateY);
        }

        /// <summary>
        /// A simple approximate comparison of absolute value
        /// </summary>
        /// <param name="a">This double</param>
        /// <param name="b">The double to compare with</param>
        /// <param name="tolerance">allowed difference</param>
        /// <returns>true if absolute difference between a and b less-than-or-equal-to tolerance</returns>
        internal static bool IsAlmostEqual(this double a, double b, double tolerance)
        {
            return Math.Abs(a - b) <= tolerance;
        }

        /// <summary>
        /// Do approximate comparison of two Size objects by comparing change in their width and height against
        /// a tolerance
        /// </summary>
        /// <param name="a">This Size to compare to</param>
        /// <param name="b">The Size to compare with</param>
        /// <param name="tolerance">The tolerable difference allowed between a and b</param>
        /// <returns>true if change is smaller than tolerance</returns>
        internal static bool IsAlmostEqual(this Size a, Size b, double tolerance)
        {
            if (!IsAlmostEqual(a.Width, b.Width, tolerance))
            {
                return false;
            }
            if (!IsAlmostEqual(a.Height, b.Height, tolerance))
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Do approximate comparison of two Point objects by comparing change in their X and Y against
        /// a tolerance
        /// </summary>
        /// <param name="a">This Point to compare to</param>
        /// <param name="b">The Point to compare with</param>
        /// <param name="tolerance">The tolerable change allowed</param>
        /// <returns>true if change is smaller than tolerance</returns>
        internal static bool IsAlmostEqual(this Point a, Point b, double tolerance)
        {
            if (!IsAlmostEqual(a.X, b.X, tolerance))
            {
                return false;
            }
            if (!IsAlmostEqual(a.Y, b.Y, tolerance))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Do approximate comparison of two Rect objects by comparing change in their TopLeft and BottomRight against
        /// a tolerance
        /// </summary>
        /// <param name="a">First Rect to compare</param>
        /// <param name="b">Second Rect to compare</param>
        /// <param name="tolerance">The tolerable difference between a and b</param>
        /// <returns>true if change is smaller than tolerance</returns>
        internal static bool IsAlmostEqual(this Rect a, Rect b, double tolerance)
        {
            if (!IsAlmostEqual(a.TopLeft, b.TopLeft, tolerance))
            {
                return false;
            }
            if (!IsAlmostEqual(a.BottomRight, b.BottomRight, tolerance))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Do approximate comparison of two Thickness objects by comparing change in their Left, Right, Top and Bottom against
        /// a tolerance
        /// </summary>
        /// <param name="a">First Thickness to compare</param>
        /// <param name="b">Second Thickness to compare</param>
        /// <param name="tolerance">The tolerable difference between a and b</param>
        /// <returns>true if change is smaller than tolerance</returns>
        internal static bool IsAlmostEqual(Thickness a, Thickness b, double tolerance)
        {
            if (!IsAlmostEqual(a.Left, b.Left, tolerance))
            {
                return false;
            }
            if (!IsAlmostEqual(a.Right, b.Right, tolerance))
            {
                return false;
            }
            if (!IsAlmostEqual(a.Top, b.Top, tolerance))
            {
                return false;
            }
            if (!IsAlmostEqual(a.Bottom, b.Bottom, tolerance))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Find the first child element of the specified type.
        /// </summary>
        /// <returns>The first child of the specified type or null</returns>
        internal static T FindChildOfType<T>(this DependencyObject parent) where T : DependencyObject
        {
            T result = parent as T;
            if (result != null)
            {
                return result;
            }
            for (int i = 0, n = VisualTreeHelper.GetChildrenCount(parent); i < n; i++)
            {
                result = FindChildOfType<T>(VisualTreeHelper.GetChild(parent, i));
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Walks up the visual tree to find the ancestor of a given type
        /// </summary>
        /// <typeparam name="T">type to find</typeparam>
        /// <param name="visual">child whose heritage we are searching</param>
        /// <returns>first match or null</returns>
        public static T FindAncestorOfType<T>(this DependencyObject visual) where T : class
        {
            while (visual != null)
            {
                var found = visual as T;
                if (found != null)
                {
                    return found;
                }
                visual = VisualTreeHelper.GetParent(visual);
            }
            return null;
        }

        /// <summary>
        /// Return the center of the rectangle
        /// </summary>
        /// <param name="rect">This rectangle object</param>
        /// <returns>The center point</returns>
        public static Point Center(this Rect rect)
        {
            return new Point(rect.Left + (rect.Width / 2), rect.Top + (rect.Height / 2));
        }
    }
}