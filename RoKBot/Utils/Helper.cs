using AForge.Imaging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace RoKBot.Utils
{
    static class Helper
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

        public static Bitmap Load(string file)
        {
            using (Bitmap image = (Bitmap)System.Drawing.Image.FromFile(file))
            {
                Bitmap buffer = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);

                using (Graphics g = Graphics.FromImage(buffer))
                {
                    g.DrawImage(image, 0, 0);
                }

                return buffer;
            }
        }
        
        public static object Find(Bitmap template, Bitmap source, float threshold = 0.9f)
        {
            Rectangle[] results = new ExhaustiveTemplateMatching(threshold).ProcessImage(source, template).OrderByDescending(i => i.Similarity).Select(i => i.Rectangle).ToArray();

            //template.Save("D:\\template.jpg");
            //source.Save("D:\\source.jpg");


            return results.Length > 0 ? (object)results[0] : null;
        }

        public static Bitmap Crop(Bitmap image, Rectangle bounds)
        {
            Bitmap crop = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(crop))
            {
                g.DrawImage(image, 0, 0, bounds, GraphicsUnit.Pixel);
            }

            return crop;
        }
    }
}
