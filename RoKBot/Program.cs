using AForge.Imaging;
using AForge.Imaging.Filters;
using RoKBot.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace RoKBot
{
    class Program
    {
        static Queue<Func<bool>> CommittingRoutines = new Queue<Func<bool>>();

        static List<Func<bool>> Routines = new List<Func<bool>>(new Func<bool>[]{

            Routine.DefeatBabarians,
            Routine.DefeatBabarians,
            Routine.DefeatBabarians,
            Routine.UpgradeCity,
            Routine.CollectResources,
            Routine.GatherResources,
            Routine.AllianceTasks,
            Routine.ClaimCampaign,
            Routine.ReadMails,
            Routine.ClaimVIP,
            Routine.Recruit,
            Routine.Explore,
            Routine.TrainInfantry,
            Routine.TrainArcher,
            Routine.TrainCavalry,
            Routine.TrainSiege,
            Routine.ClaimQuests,
            Routine.Build,
            Routine.ClaimDaily,
            Routine.HealTroops,
            Routine.Research
        });

        static DateTime RoutineStart = DateTime.UtcNow;

        static void RoutineInvokingTask()
        {
            try
            {
                Random random = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));

                Thread.CurrentThread.Join(1000);

                Helper.Print("Initializing", true);
                Routine.Initialise();

                while (true)
                {                    
                    while (!Routine.IsReady) Routine.Wait(1, 2);

                    if (CommittingRoutines.Count == 0)
                    {
                        Helper.Print("Starting new routines", true);

                        foreach (Func<bool> routine in Routines.OrderBy(i => random.Next()).ToArray()) CommittingRoutines.Enqueue(routine);
                    }
                                        
                    while(CommittingRoutines.Count > 0)
                    {
                        Func<bool> routine = CommittingRoutines.Peek();

                        if (random.Next(0, 100) < 30 && routine != Routine.GatherResources)
                        {
                            CommittingRoutines.Dequeue();
                            continue;
                        }

                        RoutineStart = DateTime.UtcNow;
                        Helper.Print("Running " + routine.Method.Name, true);
                        routine();
                        CommittingRoutines.Dequeue();
                    }

                    RoutineStart = DateTime.UtcNow;
                    Helper.Print("Running SwitchCharacter", true);                    
                    Routine.SwitchCharacter();
                    Routine.Wait(10, 15);
                }

            }
            catch(ThreadAbortException)
            {
                    Helper.Print("Routines stopped", true);
            }
        }

        static void HangProtectionTask()
        {
            try
            {
                while (true)
                {
                    Process[] processes = Process.GetProcessesByName("MEmu");

                    if ((DateTime.UtcNow - Device.LastInteractiveUtc).TotalMinutes > 5 || (DateTime.UtcNow - RoutineStart).TotalMinutes > 10 || processes.Length == 0 || !Device.Ready)
                    {
                        StopRoutines();

                        Helper.Print("Hang protection activated", true);

                        if (processes.Length > 0)
                        {
                            Helper.Print("Stopping MEmu instances");
                            foreach (Process process in processes)
                            {
                                try
                                {
                                    process.Kill();
                                }
                                catch (Exception)
                                {
                                }
                            }

                            Routine.Wait(10, 15);
                        }

                        Helper.Print("Restarting MEmu");

                        Process.Start(Path.Combine(Helper.MEmuPath, "MEmu.exe"));                        

                        Helper.Print("Restarting adb connection");                        

                        DateTime start = DateTime.UtcNow;

                        while ((DateTime.UtcNow - start).TotalMinutes < 5)
                        {
                            Device.Initialise();

                            if (Device.Tap("icon.rok"))
                            {
                                Helper.Print("Starting RoK");

                                StartRountines();

                                break;
                            }

                            Routine.Wait(1, 2);
                        }                                                       
                    }

                    Thread.CurrentThread.Join(1000);
                }
            }
            catch(ThreadAbortException)
            {
                Helper.Print("Hang protection disabled", true);
            }
        }

        static void SlideVerificationTask()
        {
            try
            {                
                while (true)
                {                    
                    if (Device.Match("button.verify", out Rectangle verify))
                    {
                        StopRoutines();

                        Helper.Print("Verification solver activated", true);

                        while (Device.Match("button.verify", out verify))
                        {                                                        
                            Helper.Print("Accquiring puzzle");

                            Device.Tap(verify);
                            Routine.Wait(5, 6);

                            if (!Device.Match("button.slider", out Rectangle slider))
                            {
                                Helper.Print("Puzzle not found, retry after 1-2 seconds");

                                Device.Tap(10, 10);
                                Routine.Wait(1, 2);                                

                                continue;
                            }

                            int top = 0x75, left = 0xc9, right = 0x1b5, bottom = 0x105;

                            int height = bottom - top;
                            int width = right - left;

                            using (Bitmap puzzle = Helper.Crop(Device.Screen, new Rectangle { X = left, Y = top, Width = width, Height = height }))
                            {                                
                                if (Helper.Solve(puzzle, out int offsetX))
                                {
                                    Device.Swipe(slider, offsetX, Helper.RandomGenerator.Next(-5, 6), Helper.RandomGenerator.Next(1000, 1500));

                                    Routine.Wait(3, 5);

                                    if (Device.Match("button.slider", out slider))
                                    {
                                        Helper.Print("False positive, retry after 1-2 seconds");

                                        Device.Tap(10, 10);
                                        Routine.Wait(1, 2);
                                    }
                                    else
                                    {
                                        Helper.Print("Puzzle solved");
                                        StartRountines();
                                    }
                                }
                                else
                                {
                                    Helper.Print("Solution not found, retry after 1-2 seconds");
                                    Device.Tap(10, 10);
                                    Routine.Wait(1, 2);
                                }
                            }
                        }
                    }

                    Thread.CurrentThread.Join(1000);
                }
            }
            catch(ThreadAbortException)
            {
            }
        }        

        static void VerificationPreventionTask()
        {
            try
            {
                while (true)
                {
                    if (Device.Match("button.verify", out Rectangle verify))
                    {
                        HangProtectionThread.Abort();
                        StopRoutines();
                        Routine.Wait(1, 2);

                        Helper.Print("Verification prevention activated", true);

                        Process[] processes = Process.GetProcessesByName("MEmu");

                        if (processes.Length > 0)
                        {
                            Helper.Print("Stopping MEmu instances");
                            foreach (Process process in processes)
                            {
                                try
                                {
                                    process.Kill();
                                }
                                catch (Exception)
                                {
                                }
                            }

                            Routine.Wait(10, 15);
                        }

                        HttpClient client = new HttpClient(new HttpClientHandler { UseProxy = false, Proxy = null });

                        JavaScriptSerializer jss = new JavaScriptSerializer();

                        HttpContent content = new StringContent(jss.Serialize(new
                        {
                            receivers = new string[] { "hoai4285@gmail.com" },
                            name = "RoK Request",
                            content = "Verification requirement detected at " + DateTime.Now,
                            subject = "Verification required",
                            mail_address_name = "info"

                        }), Encoding.UTF8, "application/json");

                        client.PostAsync("http://api.jvjsc.com:6245/mail/send", content).ContinueWith(task =>
                        {
                            client.Dispose();
                            content.Dispose();
                        });

                        Helper.Print("Waiting 5 minutes for user intervention", true);
                        Routine.Wait(300, 301);

                        HangProtectionThread = new Thread(new ThreadStart(HangProtectionTask));
                        HangProtectionThread.Start();
                    }

                    Thread.CurrentThread.Join(1000);
                }
            }
            catch (ThreadAbortException)
            {
            }
        }

        static Thread HangProtectionThread = null;
        static Thread RoutineInvokingThread = null;
        static object Locker = new object();

        static void StopRoutines()
        {
            lock (Locker)
            {
                if (RoutineInvokingThread?.IsAlive ?? false)
                {
                    RoutineInvokingThread.Abort();
                    Routine.Wait(1, 2);
                }
            }
        }

        static void StartRountines()
        {
            lock (Locker)
            {
                StopRoutines();

                RoutineInvokingThread = new Thread(new ThreadStart(RoutineInvokingTask));
                RoutineInvokingThread.Start();
            }
        }

        static void Main(string[] args)
        {

            System.Net.ServicePointManager.Expect100Continue = false;
            System.Net.ServicePointManager.UseNagleAlgorithm = false;

            Helper.Print("Starting threads", true);
                        
            Thread V = new Thread(new ThreadStart(VerificationPreventionTask));                                    
            HangProtectionThread = new Thread(new ThreadStart(HangProtectionTask));
            
            Device.Initialise();

            StartRountines();
            HangProtectionThread.Start();
            V.Start();

            while (true) Thread.CurrentThread.Join(1000);
        }
    }
}
