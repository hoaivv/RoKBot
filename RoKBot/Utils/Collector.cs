using AForge.Imaging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace RoKBot.Utils
{
    static class Collector
    {        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static IntPtr hWnd = IntPtr.Zero;

        
        public static Bitmap CaptureScreen(out Rectangle bounds)
        {

            if (hWnd == IntPtr.Zero)
            {
                hWnd = GetForegroundWindow();
            }
            else
            {                
                SetForegroundWindow(hWnd);
            }

            MoveWindow(hWnd, 0, 0, 800, 600, true);

            var rect = new Rect();
            GetWindowRect(hWnd, ref rect);

            int height = (rect.Bottom - rect.Top) - 44;
            int width = height * 1770 / 996;

            bounds = new Rectangle(rect.Left + 3, rect.Top + 42, width, height);

            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(new Point(bounds.Left, bounds.Top), new Point { X = 0, Y = 0 }, bounds.Size);
            }

            return bitmap;
        }
        

        public static Bitmap Uniform(Bitmap image, Bitmap bounds)
        {
            Bitmap buffer = new Bitmap(image.Width * bounds.Width / 1770, image.Height * bounds.Height / 996, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(buffer))
            {
                g.DrawImage(image, new Rectangle { X = 0, Y = 0, Width = buffer.Width, Height = buffer.Height }, new Rectangle { X = 0, Y = 0, Width = image.Width, Height = image.Height }, GraphicsUnit.Pixel);
            }

            return buffer;
        }

        public static object Find(Bitmap template, Bitmap source, float threshold = 0.9f)
        {
            Rectangle[] results = new ExhaustiveTemplateMatching(threshold).ProcessImage(source, template).OrderByDescending(i => i.Similarity).Select(i => i.Rectangle).ToArray();

            return results.Length > 0 ? (object)results[0] : null;
        }
    }
}
