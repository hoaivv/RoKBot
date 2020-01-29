using RoKBot.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

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

                        Helper.Print("Running " + routine.Method.Name, true);
                        routine();
                        CommittingRoutines.Dequeue();
                    }
                    
                    Helper.Print("Running SwitchCharacter", true);                    
                    Routine.SwitchCharacter();
                    Routine.Wait(10, 15);
                }

            }
            catch(ThreadAbortException)
            {
                if (Resumable)
                {
                    Helper.Print("Routines stopped", true);
                    Helper.Print("Press Enter to resume");
                }
                else
                {
                    Helper.Print("Routines stopped", true);
                }
            }
        }

        static void HangProtectionTask()
        {
            try
            {
                while (true)
                {
                    Process[] processes = Process.GetProcessesByName("MEmu");

                    if ((DateTime.UtcNow - Device.LastInteractiveUtc).TotalMinutes > 5 || processes.Length == 0 || !Device.Ready)
                    {                                                                        
                        bool restartRoutines = !Paused;

                        Resumable = false;
                        Paused = true;

                        if (restartRoutines)
                        {
                            Routine.Wait(1, 2);
                        }                                                

                        Helper.Print("Hang protection activated", true);

                        if (processes.Length > 0)
                        {
                            Helper.Print("Stopping MEmu instances");
                            foreach (Process process in processes) process.Kill();

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

                                if (restartRoutines) Paused = false;

                                Resumable = true;
                                break;
                            }

                            Routine.Wait(1, 2);
                        }       
                        
                        if (!Resumable)
                        {
                            Helper.Print("Failed to start RoK");
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
                        Resumable = false;

                        bool restartRountine = !Paused;
                        
                        if (!Paused)
                        {
                            Paused = true;
                            Routine.Wait(1, 2);
                        }

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
                                }
                                else
                                {
                                    Helper.Print("Solution not found, retry after 1-2 seconds");
                                    Device.Tap(10, 10);
                                    Routine.Wait(1, 2);
                                }
                            }
                        }

                        Helper.Print("Puzzle solved");
                        if (restartRountine) Paused = false;

                        Resumable = true;
                    }

                    Thread.CurrentThread.Join(1000);
                }
            }
            catch(ThreadAbortException)
            {
            }
        }

        static bool Paused = false;
        static bool Resumable = true;

        static void Main(string[] args)
        {                        
            Helper.Print("Press Enter to start", true);
            Console.ReadLine();
            Helper.Print("Starting threads", true);

            Thread V = new Thread(new ThreadStart(VerificationTask));                        
            Thread T = new Thread(new ThreadStart(RoutineInvokingTask));
            Thread P = new Thread(new ThreadStart(HangProtectionTask));

            bool last = Paused;

            Device.Initialise();

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

                        if (Resumable)
                        {
                            Paused = !Paused;
                        }
                        else
                        {
                            Helper.Print("Routines enable/disable locked. try again latter", true);
                        }
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
