using RoKBot.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace RoKBot
{
    class Program
    {
        static void RoutineInvokingTask()
        {
            try
            {
                List<Func<bool>> tasks = new List<Func<bool>>(new Func<bool>[]{

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

                Random random = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));

                Thread.CurrentThread.Join(1000);

                Helper.Print("Initializing", true);
                Routine.Initialise();

                while (true)
                {                    
                    while (!Routine.IsReady) Routine.Wait(1, 2);

                    Helper.Print("Starting new routine", true);                    

                    foreach (Func<bool> task in tasks.OrderBy(i => random.Next()))
                    {
                        if (random.Next(0, 101) < 30 && task != Routine.GatherResources) continue;

                        Helper.Print("Running " + task.Method.Name, true);
                        task();
                    }

                    Helper.Print("Running SwitchCharacter", true);                    
                    Routine.SwitchCharacter();
                    Routine.Wait(10, 15);
                }

            }
            catch(ThreadAbortException)
            {
                Helper.Print("Routines stopped", true);                
                Helper.Print("Press Enter to resume");
            }
        }

        static void HangProtectionTask()
        {
            try
            {
                while (true)
                {
                    Process[] processes = Process.GetProcessesByName("MEmu");

                    if ((DateTime.UtcNow - Device.LastInteractiveUtc).TotalMinutes > 5 || processes.Length == 0)
                    {                                                
                        Helper.Print("Hang protection activated", true);                        
                        Paused = true;

                        Helper.Print("Stopping MEmu instances", true);
                        
                        foreach (Process process in processes) process.Kill();

                        Routine.Wait(10, 15);

                        Helper.Print("Restarting MEmu", true);

                        Process.Start(@"D:\Program Files\Microvirt\MEmu\MEmu.exe");                        

                        Routine.Wait(15, 20);

                        Device.Start();

                        Helper.Print("Starting RoK", true);                        

                        if (Device.Tap("icon.rok"))
                        {
                            Helper.Print("RoK Started");
                            Paused = false;
                        }
                    }

                    Thread.CurrentThread.Join(1000);
                }
            }
            catch(ThreadAbortException)
            {
            }
        }

        static void VerificationTask()
        {
            try
            {                
                while (true)
                {                    
                    if (Device.Match("button.verify", out Rectangle verify))
                    {
                        Helper.Print("Verification solver activated", true);

                        bool restartRountine = false;

                        while (Device.Match("button.verify", out verify))
                        {
                            if (!Paused)
                            {
                                restartRountine = true;
                                Paused = true;
                                Routine.Wait(1, 2);
                            }

                            Device.Tap(verify);
                            Routine.Wait(5, 6);

                            if (!Device.Match("button.slider", out Rectangle slider))
                            {
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
                                        Device.Tap(10, 10);
                                        Routine.Wait(1, 2);
                                    }
                                }
                                else
                                {
                                    Device.Tap(10, 10);
                                    Routine.Wait(1, 2);
                                }
                            }
                        }

                        Helper.Print("Puzzle solved");

                        if (restartRountine) Paused = false;
                    }

                    Routine.Wait(1, 2);
                }
            }
            catch(ThreadAbortException)
            {
            }
        }

        static bool Paused = false;

        static void Main(string[] args)
        {                        
            Helper.Print("Press Enter to start", true);
            Console.ReadLine();
            Helper.Print("Starting threads", true);

            Thread V = new Thread(new ThreadStart(VerificationTask));                        
            Thread T = new Thread(new ThreadStart(RoutineInvokingTask));
            Thread P = new Thread(new ThreadStart(HangProtectionTask));

            bool last = Paused;

            Device.Start();

            P.Start();
            T.Start();
            V.Start();

            new Thread(new ThreadStart(() =>
            {
                try
                {
                    while (true)
                    {
                        if (last != Paused)
                        {
                            if (Paused)
                            {
                                T.Abort();                                
                            }
                            else
                            {
                                T = new Thread(new ThreadStart(RoutineInvokingTask));
                                T.Start();
                            }

                            last = Paused;
                        }

                        Thread.CurrentThread.Join(10);
                    }
                }
                catch (ThreadAbortException)
                {
                }

            })).Start();

            new Thread(new ThreadStart(() =>
            {
                try
                {
                    while (true)
                    {
                        Console.ReadLine();
                        Paused = !Paused;                        
                    }
                }
                catch (ThreadAbortException)
                {
                }

            })).Start();            

            while (true) Thread.CurrentThread.Join(1000);
        }
    }
}
