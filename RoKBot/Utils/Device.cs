using SharpAdbClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace RoKBot.Utils
{
    public static class Device
    {
        static AdbServer server = new AdbServer();
        static List<DeviceData> devices;
        
        static Device()
        {
            server.StartServer(@"D:\Program Files\Microvirt\MEmu\adb.exe", restartServerIfNewer: true);
            AdbClient.Instance.Connect("localhost:21503");
            devices = AdbClient.Instance.GetDevices();
            
        }

        public static void Tap(int x, int y)
        {
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            AdbClient.Instance.ExecuteRemoteCommand("input tap " + x + " " + y, devices[0], receiver);
        }

        public static void Reboot()
        {
            AdbClient.Instance.Reboot(devices[0]);
        }

        public static void Run(string packgate, string activity)
        {
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            AdbClient.Instance.ExecuteRemoteCommand("am start -n " + packgate + "/" + activity, devices[0], receiver);
        }

        public static void Kill(string package)
        {
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            AdbClient.Instance.ExecuteRemoteCommand("am force-stop " + package, devices[0], receiver);
        }

        public static Bitmap Screen
        {
            get
            {
                bool verify = false;

                do
                {
                    ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
                    AdbClient.Instance.ExecuteRemoteCommand("screencap /sdcard/screen.png", devices[0], receiver);

                    using (SyncService service = new SyncService(devices[0]))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            service.Pull("/sdcard/screen.png", ms, null, CancellationToken.None);

                            using (Image image = Image.FromStream(ms))
                            {
                                Bitmap bmp = new Bitmap(Math.Max(image.Width, image.Height), Math.Min(image.Width, image.Height), PixelFormat.Format24bppRgb);

                                using (Graphics g = Graphics.FromImage(bmp))
                                {
                                    if (image.Height > image.Width)
                                    {
                                        g.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);
                                        g.RotateTransform(-90);
                                        g.TranslateTransform(-(float)image.Width / 2, -(float)image.Height / 2);
                                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    }

                                    g.DrawImage(image, 0, 0);
                                }

                                object findResult = null;

                                using (Bitmap test = new Bitmap(bmp.Width, 50, PixelFormat.Format24bppRgb))
                                {
                                    using (Graphics g = Graphics.FromImage(test))
                                    {
                                        g.DrawImage(bmp, 0, -90);
                                    }

                                    findResult = Helper.Find(Helper.Load("assets/label.verify.jpg"), test);
                                    verify = findResult != null;
                                }

                                if (verify)
                                {
                                    bmp.Dispose();
                                    Rectangle location = (Rectangle)findResult;

                                    Tap(location.X + location.Width / 2, location.Y + 347);
                                    Thread.CurrentThread.Join(3000);
                                }
                                else
                                {
                                    return bmp;
                                }
                            }
                        }
                    }
                }
                while (verify);

                return null;
            }
        }
    }
}
