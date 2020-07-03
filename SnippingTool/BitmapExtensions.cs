//-----------------------------------------------------------------------
// <copyright file="BitmapExtensions.cs" company="Pics.rs">
//     Author: Filip Dunđer
//     Copyright (c) Pics.rs
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SnippingTool
{
    /// <summary>
    ///     Extension class for <see cref="Bitmap" />.
    /// </summary>
    public static class BitmapExtensions
    {
        /// <summary>
        ///     Gets bitmap source from existing bitmap.
        /// </summary>
        /// <param name="bitmap">Bitmap from which we want to get bitmap source.</param>
        /// <returns>New instance of <see cref="BitmapSource" />.</returns>
        public static BitmapSource GetBitmapSource(this Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();

            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                NativeMethods.DeleteObject(hBitmap);
            }
        }

        /// <summary>
        ///     Copies rectangle area from source bitmap to a new bitmap.
        /// </summary>
        /// <param name="sourceBitmap">Source bitmap from which area will be copied.</param>
        /// <param name="destinationRectangle">Destination area in new bitmap to which we want to copy.</param>
        /// <param name="sourceRectangle">Source area from which we want to copy.</param>
        /// <returns>New instance of <see cref="Bitmap" />.</returns>
        public static Bitmap CopyAreaToNewBitmap(this Bitmap sourceBitmap,
            Rectangle destinationRectangle, Rectangle sourceRectangle)
        {
            var bitmap = new Bitmap(destinationRectangle.Width, destinationRectangle.Height);

            using (var graph = Graphics.FromImage(bitmap))
            {
                graph.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graph.DrawImage(sourceBitmap, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);
            }

            return bitmap;
        }
    }
}