using RoKBot.Utils;
using SharpAdbClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace RoKBot
{
    class Program
    {
        static void Work()
        {
            try
            {
                List<Func<bool>> tasks = new List<Func<bool>>(new Func<bool>[]{

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
                Routine.Build,
                Routine.ClaimDaily

            });

                Random random = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));

                Console.WriteLine();
                Console.WriteLine("Starting ROK");

                Device.Run("com.lilithgame.roc.gp", "com.harry.engine.MainActivity");

                while (true)
                {                    
                    while (!Routine.IsReady) Routine.Wait(1, 2);

                    Console.WriteLine();
                    Console.WriteLine("Starting new routine");


                    Console.WriteLine("Running HealTroops");
                    Console.WriteLine();
                    Routine.HealTroops();

                    foreach (Func<bool> task in tasks.OrderBy(i => random.Next()))
                    {
                        if (random.Next(0, 101) < 30 && task != Routine.GatherResources) continue;

                        Console.WriteLine();
                        Console.WriteLine("Running " + task.Method.Name);
                        task();
                    }

                    Console.WriteLine("Running SwitchAccount");
                    Console.WriteLine();
                    Routine.SwitchAccount();

                    Routine.Wait(40, 80);
                }

            }
            catch(ThreadAbortException)
            {
                Console.WriteLine("All tasks stopped");
            }
        }


        static void Test()
        {
            while (true)
            {
                Routine.GatherWood();
                Console.WriteLine(".");

                /*
                using (Bitmap screen = Collector.CaptureScreen(out Rectangle bounds))
                {
                    Point pos = Mouse.GetCursorPosition();

                    Console.WriteLine("X: " + ((double)(pos.X - bounds.X) / bounds.Width).ToString("0.00") + " Y: " + ((double)(pos.Y - bounds.Y) / bounds.Height).ToString("0.00") + " W: " + bounds.Width + " H: " + bounds.Height);
                }
                */
            }

        }

        static bool Paused = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Press Enter to start");
            Console.ReadLine();

            Thread T = new Thread(new ThreadStart(Work));
            T.Start();

            while (true)
            {
                Console.ReadLine();
                Paused = !Paused;

                if (Paused)
                {
                    T.Abort();
                    Console.WriteLine("Press Enter to resume");
                }
                else
                {
                    T = new Thread(new ThreadStart(Work));
                    T.Start();
                }
            }
        }
    }
}
