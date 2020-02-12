using AForge.Imaging;
using AForge.Imaging.Filters;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace RoKBot.Utils
{
    static class Helper
    {
        public const string MEmuPath = @"D:\Program Files\Microvirt\MEmu";
        public const string BlueStacksPath = @"C:\ProgramData\BlueStacks\Client\Bluestacks.exe";
        public const string AdbPath = @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe";

        public static Random RandomGenerator = new Random((int)DateTime.UtcNow.Ticks);

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

        public static void Print(string content, bool isSubject = false)
        {
            Console.ForegroundColor = isSubject ? ConsoleColor.White : ConsoleColor.Gray;
            if (isSubject) Console.WriteLine();
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm") + "] " + content);            
        }        
        
        public static bool Match(Bitmap template, Bitmap source, out Rectangle match, Rectangle? searchZone = null, float threshold = 0.9f)
        {
            bool pass = Match(template, source, out Rectangle[] matches, searchZone, threshold);
            match = pass ? matches[0] : default(Rectangle);
            return pass;
        }

        public static bool Match(Bitmap template, Bitmap source, out Rectangle[] matches, Rectangle? searchZone = null, float threshold = 0.9f)
        {
            try
            {
                if (template == null || source == null)
                {
                    matches = new Rectangle[0];
                    return false;
                }

                matches = searchZone == null
                    ? new ExhaustiveTemplateMatching(threshold).ProcessImage(source, template).OrderByDescending(i => i.Similarity).Select(i => i.Rectangle).ToArray()
                    : new ExhaustiveTemplateMatching(threshold).ProcessImage(source, template, (Rectangle)searchZone).OrderByDescending(i => i.Similarity).Select(i => i.Rectangle).ToArray();

                return matches.Length > 0;
            }
            catch (Exception)
            {
                matches = new Rectangle[0];
                return false;
            }
        }

        public static Bitmap Crop(Bitmap image, Rectangle bounds)
        {
            if (image == null) return null;

            Bitmap crop = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(crop))
            {
                g.DrawImage(image, 0, 0, bounds, GraphicsUnit.Pixel);
            }

            return crop;
        }

        public static bool Match(Bitmap source, Rectangle cropZone, Rectangle searchZone, out Rectangle match, float similarityThreshold = 0.9f)
        {
            using (Bitmap tmpl = Helper.Crop(source, cropZone))
            {
                try
                {
                    return Match(tmpl, source, out match, searchZone, similarityThreshold);
                }
                catch(Exception)
                {
                    match = default(Rectangle);
                    return false;
                }
            }
        }

        public static bool Solve(Bitmap puzzle, out int offsetX)
        {
            if (puzzle == null)
            {
                offsetX = 0;
                return false;
            }

            using (Bitmap test = new GrayscaleBT709().Apply(puzzle))
            {
                BitmapData data = test.LockBits(new Rectangle(0, 0, test.Width, test.Height), ImageLockMode.ReadWrite, test.PixelFormat);

                byte[] points = new byte[data.Stride * test.Height];

                Marshal.Copy(data.Scan0, points, 0, points.Length);

                for (int y = 0; y < test.Height; y++)
                {
                    for (int x = 0; x < data.Stride; x++)
                    {
                        int i = y * data.Stride + x;

                        if (x > 50 && x < data.Stride - 10 && points[i] - points[i + 1] > 80)
                        {
                            points[i] = points[i + 1];
                        }
                        else
                        {
                            points[i] = 255;
                        }
                    }
                }

                Marshal.Copy(points, 0, data.Scan0, points.Length);
                test.UnlockBits(data);

                new Invert().ApplyInPlace(test);
                new BlobsFiltering { MinHeight = 5, MaxWidth = 1 }.ApplyInPlace(test);

                BlobCounter counter = new BlobCounter();

                counter.ObjectsOrder = ObjectsOrder.Size;
                counter.ProcessImage(test);

                Rectangle[] blobs = counter.GetObjectsRectangles();

                offsetX = blobs.Length == 1 ? blobs[0].X - 5 : blobs.Length == 2 && blobs[0].X == blobs[1].X ? blobs[0].X - 5 : 0;
                return offsetX > 0;
            }
        }
    }
}
