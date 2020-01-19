﻿using SharpAdbClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
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
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {

                Shell("screencap /sdcard/screen.png");

                using (SyncService service = new SyncService(devices[0]))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        service.Pull("/sdcard/screen.png", ms, null, CancellationToken.None);

                        using (Image image = Image.FromStream(ms))
                        {
                            Bitmap bmp = new Bitmap(Math.Max(image.Width, image.Height), Math.Min(image.Width, image.Height), PixelFormat.Format24bppRgb);

                            using (Graphics g = Graphics.FromImage(bmp)) g.DrawImage(image, 0, 0);

                            return bmp;
                        }
                    }
                }
            }
        }

        public static string Shell(params string[] cmds)
        {
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            AdbClient.Instance.ExecuteRemoteCommand(string.Join(";", cmds), devices[0], receiver);
            return receiver.ToString();
        }

        public static void Press(int x, int y, int fingerWidth = 7)
        {
            Shell(
                "sendevent /dev/input/event6 3 47 0",
                "sendevent /dev/input/event6 3 53 " + x,
                "sendevent /dev/input/event6 3 54 " + y,
                "sendevent /dev/input/event6 3 48 " + fingerWidth,
                "sendevent /dev/input/event6 3 57 0",
                "sendevent /dev/input/event6 0 0 0");
        }

        public static void Release()
        {
            Shell(
                "sendevent /dev/input/event6 3 48 0",
                "sendevent /dev/input/event6 3 57 -1",                
                "sendevent /dev/input/event6 0 0 0"
            );
        }

        public static void Move(int x, int y, int fingerWidth = 7)
        {
            Shell(
                "sendevent /dev/input/event6 3 53 " + x,
                "sendevent /dev/input/event6 3 54 " + y,
                "sendevent /dev/input/event6 3 48 " + fingerWidth,
                "sendevent /dev/input/event6 0 0 0"
                );
        }

        public static void Swipe(int x1, int y1, int x2, int y2, int ms)
        {
            List<string> cmds = new List<string>();

            int count = Helper.RandomGenerator.Next(5, 10);

            Press(x1, y1);
            Thread.CurrentThread.Join(Helper.RandomGenerator.Next(50, 100));

            for (int i = 0; i < count; i++)
            {                
                int x = x1 + (x2 - x1) * i / count + Helper.RandomGenerator.Next(-3, 4);
                int y = y1 + (y2 - y1) * i / count + Helper.RandomGenerator.Next(-3, 4);

                Move(x, y);
                Thread.CurrentThread.Join(Helper.RandomGenerator.Next(50, 100));
            }

            Move(x2, y2);
            Thread.CurrentThread.Join(ms);
            Release();
        }

        public static void Swipe(Rectangle from, int offsetX, int offsetY, int ms)
        {
            int x = from.X + from.Width / 2;
            int y = from.Y + from.Height / 2;
            
            Swipe(x, y, x + offsetX, y + offsetY, ms);
        }

        public static void Tap(int x, int y, int epsilon = 0)
        {
            using (Bitmap screen = Screen)
            {
                x += Helper.RandomGenerator.Next(-epsilon / 2, epsilon / 2 + 1);
                y += Helper.RandomGenerator.Next(-epsilon / 2, epsilon / 2 + 1);

                Shell("input tap " + x + " " + y);
            }
        }

        public static void Tap(Rectangle bounds, int epsilon = 0)
        {
            Tap(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2, epsilon);
        }

        public static bool Tap(int x, int y, string file, float similarityThreshold = 0.9f, int epsilon = 0)
        {
            if (Match(x,y, file, out Rectangle match, similarityThreshold))
            {
                Tap(match, epsilon);
                return true;
            }

            return false;
        }

        public static bool Tap(string file, float similarityThreshold = 0.9f, int epsilon = 0)
        {
            if (Match(file, out Rectangle match, null, similarityThreshold))
            {
                Tap(match, epsilon);
                return true;
            }

            return false;
        }

        public static bool Match(string file, out Rectangle match, Rectangle? searchZone = null, float similarityThreshold = 0.9f)
        {
            string png = Path.Combine("assets", file + ".png");
            string jpg = Path.Combine("assets", file + ".jpg");

            file = File.Exists(png) ? png : File.Exists(jpg) ? jpg : null;

            if (file == null)
            {
                match = default(Rectangle);
                return false;
            }

            using (Bitmap tmpl = Helper.Load(file))
            {
                using (Bitmap screen = Screen)
                {
                    return Helper.Match(tmpl, screen, out match, searchZone, similarityThreshold);
                }
            }
        }

        public static bool Match(int x, int y, string file, out Rectangle match, float similarityThreshold = 0.9f)
        {
            string png = Path.Combine("assets", file + ".png");
            string jpg = Path.Combine("assets", file + ".jpg");

            file = File.Exists(png) ? png : File.Exists(jpg) ? jpg : null;

            if (file == null)
            {
                match = default(Rectangle);
                return false;
            }

            using (Bitmap tmpl = Helper.Load(file))
            {
                using (Bitmap screen = Screen)
                {
                    return Helper.Match(
                        tmpl, 
                        screen, 
                        out match, 
                        
                        new Rectangle
                        {
                            X = x - tmpl.Width,
                            Y = y - tmpl.Height,
                            Width = tmpl.Width * 2,
                            Height = tmpl.Height * 2
                        }, 
                        
                        similarityThreshold);
                }
            }
        }        
    }
}