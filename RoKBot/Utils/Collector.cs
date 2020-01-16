using AForge.Imaging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace RoKBot.Utils
{
    static class Collector
    {        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static Bitmap CaptureScreen(out Rectangle bounds)
        {
            var foregroundWindowsHandle = GetForegroundWindow();
            var rect = new Rect();
            GetWindowRect(foregroundWindowsHandle, ref rect);

            int height = (rect.Bottom - rect.Top) - 44;
            int width = height * 1770 / 996;

            bounds = new Rectangle(rect.Left + 3, rect.Top + 42, width, height);                       

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(new Point(bounds.Left, bounds.Top), new Point { X = 0, Y = 0 }, bounds.Size);
            }

            return bitmap;
        }

        public static Bitmap Uniform(Bitmap image, Rectangle bounds)
        {
            Bitmap buffer = new Bitmap(image.Width * bounds.Width / 1770, image.Height * bounds.Height / 996, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(buffer))
            {
                g.DrawImage(image, new Rectangle { X = 0, Y = 0, Width = buffer.Width, Height = buffer.Height }, new Rectangle { X = 0, Y = 0, Width = image.Width, Height = image.Height }, GraphicsUnit.Pixel);
            }

            return buffer;
        }

        static int Width = 1507;
        static int Height = 889;

        public static object Find(Bitmap template, Bitmap source, float threshold = 0.9f)
        {
            Rectangle[] results = new ExhaustiveTemplateMatching(threshold).ProcessImage(source, template).OrderByDescending(i => i.Similarity).Select(i => i.Rectangle).ToArray();

            return results.Length > 0 ? (object)results[0] : null;
        }
    }
}
