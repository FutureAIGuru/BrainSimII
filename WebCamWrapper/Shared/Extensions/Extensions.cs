//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using WindowsPoint = System.Windows.Point;
using DrawingPoint = System.Drawing.Point;

namespace Touchless.Shared.Extensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public static class Extensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (T item in items)
            {
                action(item);
            }

            return items;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public static void IfNotNull<T>(this T item, Action<T> action)
        {
            if (item != null)
            {
                action(item);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "p")]
        public static WindowsPoint ToWindowsPoint(this DrawingPoint p)
        {
            return new WindowsPoint
                       {
                           X = p.X,
                           Y = p.Y
                       };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "p")]
        public static DrawingPoint ToDrawingPoint(this WindowsPoint p)
        {
            return new DrawingPoint
                       {
                           X = (int) p.X,
                           Y = (int) p.Y
                       };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Touchless.Shared.Extensions.Extensions.DeleteObject(System.IntPtr)")]
        public static BitmapSource ToBitmapSource(this Bitmap source)
        {
            BitmapSource bs = null;

            IntPtr ip = source.GetHbitmap();
            try
            {
                bs = Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty,
                                                           BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }
    }
}