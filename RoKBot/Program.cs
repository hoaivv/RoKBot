using RoKBot.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace RoKBot
{
    class Program
    {
        static void RunRoutine()
        {
            try
            {
                List<Func<bool>> tasks = new List<Func<bool>>(new Func<bool>[]{

                    //Routine.DefeatBabarians,
                    //Routine.UpgradeCity,
                    //Routine.CollectResources,
                    //Routine.GatherResources,
                    //Routine.AllianceTasks,
                    //Routine.ClaimCampaign,
                    //Routine.ReadMails,
                    //Routine.ClaimVIP,
                    //Routine.Recruit,
                    //Routine.Explore,
                    //Routine.TrainInfantry,
                    //Routine.TrainArcher,
                    //Routine.TrainCavalry,
                    //Routine.TrainSiege,
                    //Routine.ClaimQuests,
                    //Routine.Build,
                    //Routine.ClaimDaily,
                    //Routine.HealTroops,
                    Routine.Research
                });

                Random random = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));

                //Console.WriteLine();
                //Console.WriteLine("Starting ROK");

                //Device.Run("com.lilithgame.roc.gp", "com.harry.engine.MainActivity");

                Thread.CurrentThread.Join(1000);

                Routine.Initialise();

                while (true)
                {                    
                    while (!Routine.IsReady) Routine.Wait(1, 2);

                    Console.WriteLine();
                    Console.WriteLine("Starting new routine");

                    foreach (Func<bool> task in tasks.OrderBy(i => random.Next()))
                    {
                        if (random.Next(0, 101) < 30 && task != Routine.GatherResources) continue;

                        Console.WriteLine();
                        Console.WriteLine("Running " + task.Method.Name);
                        task();
                    }

                    Console.WriteLine();
                    Console.WriteLine("Running SwitchAccount");                    

                    Routine.SwitchAccount();
                    Routine.Wait(10, 15);
                }

            }
            catch(ThreadAbortException)
            {
                Console.WriteLine("All tasks stopped");
            }
        }

        static void RunVerification()
        {
            try
            {
                bool restartRountine = false;

                while (true)
                {
                    while (Device.Match("button.verify", out Rectangle verify, null))
                    {
                        if (!Paused) restartRountine = true;
                        Paused = true;

                        Routine.Wait(1, 2);

                        Device.Tap(verify);

                        Routine.Wait(1, 2);
                        Rectangle slider;
                        while (!Device.Match("button.slider", out slider)) Routine.Wait(1, 2);

                        Routine.Wait(1, 2);
                        Device.Match("button.slider", out slider);

                        int top = 0x75, left = 0xc9, right = 0x1b5, bottom = 0x105;

                        int height = bottom - top;
                        int width = right - left;

                        using (Bitmap puzzle = Helper.Crop(Device.Screen, new Rectangle { X = left, Y = top, Width = width, Height = height }))
                        {
                            if (Helper.Solve(puzzle, out int offsetX))
                            {
                                Device.Swipe(slider, offsetX, Helper.RandomGenerator.Next(-5,6), Helper.RandomGenerator.Next(1000,1500));                                

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

                    if (restartRountine) Paused = false;                   
                    Routine.Wait(1, 2);

                    restartRountine = false;

                    if (Device.Match("label.hint", out Rectangle hint, null, 1) || Device.Match("label.loss", out Rectangle loss, null, 1) || Device.Match("label.network", out Rectangle network, null, 1))
                    {
                        if (!Paused) restartRountine = true;
                        Paused = true;

                        Routine.Wait(1, 2);

                        Device.Tap("button.confirm");
                    }

                    if (restartRountine) Paused = false;
                    Routine.Wait(5, 6);
                }
            }
            catch(ThreadAbortException)
            {
            }
        }

        static bool Paused = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Press Enter to start");
            Console.ReadLine();
            
            Thread V = new Thread(new ThreadStart(RunVerification));                        
            Thread T = new Thread(new ThreadStart(RunRoutine));

            bool last = Paused;

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
                                Console.WriteLine("Press Enter to resume");
                            }
                            else
                            {
                                T = new Thread(new ThreadStart(RunRoutine));
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
