﻿using SharpAdbClient;
using System.Collections.Generic;
using System.Drawing;
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
            server.StartServer(@"D:\Android\android-sdk\platform-tools\adb.exe", restartServerIfNewer: false);
            devices = AdbClient.Instance.GetDevices();
        }

        public static void Tap(int x, int y)
        {
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            AdbClient.Instance.ExecuteRemoteCommand("input tap " + x + " " + y, devices[0], receiver);
        }

        public static void StopROK()
        {
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            AdbClient.Instance.ExecuteRemoteCommand("am force-stop com.lilithgame.roc.gp", devices[0], receiver);
        }

        public static void StartROK()
        {
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            AdbClient.Instance.ExecuteRemoteCommand("monkey -p com.lilithgame.roc.gp -v 500", devices[0], receiver);
        }

        public static Bitmap Screen
        {
            get
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
                            Bitmap bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);

                            using (Graphics g = Graphics.FromImage(bitmap))
                            {
                                g.DrawImage(image, 0, 0);
                            }

                            return bitmap;
                        }
                    }
                }
            }
        }
    }
}
