//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;

namespace VirtualCanvasDemo.Helpers
{
    /// <summary>
    /// Extension methods for Math calculations
    /// </summary>
    public static class Extensions
    {
        internal static bool IsBetween(this double value, double a, double b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        internal static bool IsNaN(this double value)
        {
            return Double.IsNaN(value);
        }

        internal static double AtLeast(this double a, double b)
        {
            return Math.Max(a, b);
        }

        internal static double AtMost(this double a, double b)
        {
            return Math.Min(a, b);
        }
    }
}
