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
        static void Work()
        {
            List<Func<bool>> tasks = new List<Func<bool>>(new Func<bool>[]{

                Routine.GatherResources,
                Routine.AllianceTasks,
                Routine.ClaimCampaign,
                Routine.ReadMails,
                Routine.ClaimVIP,
                Routine.Recruit,
                Routine.Explore,
                Routine.CollectResources,
                Routine.TrainTroops,
                Routine.ClaimQuests,
                Routine.Build,
                Routine.Build
            });

            Random random = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));

            if (!Routine.IsReady) Routine.Click(.24, .24);

            while (true)
            {
                foreach (Func<bool> task in tasks.OrderBy(i => random.Next()))
                {
                    Console.WriteLine();
                    Console.WriteLine("Running " + task.Method.Name);
                    task();

                }

                Console.WriteLine();
                Routine.SwitchAccount();

                Keyboard.Send(Keyboard.ScanCode.ESCAPE);
                Routine.Wait(3, 5);
                Routine.Click(0.42, 0.67);
                Routine.Wait(3, 5);

                if (!Routine.Click(.24,.24))
                {
                    break;
                }

                while (!Routine.IsReady) Routine.Wait(1, 2);
                Routine.Wait(20, 25);

                Console.WriteLine();
                Console.WriteLine("Starting new routine");
            }
        }


        static void Test()
        {
            while (true)
            {
                //Routine.GatherResources();
                using (Bitmap screen = Collector.CaptureScreen(out Rectangle bounds))
                {
                    Point pos = Mouse.GetCursorPosition();

                    Console.WriteLine("X: " + ((double)(pos.X - bounds.X) / bounds.Width).ToString("0.00") + " Y: " + ((double)(pos.Y - bounds.Y) / bounds.Height).ToString("0.00") + " W: " + bounds.Width + " H: " + bounds.Height);
                }
            }

        }
        static void Main(string[] args)
        {
            Console.ReadLine();
            Console.WriteLine("Start after 3 seconds ...");
            Thread.CurrentThread.Join(3000);
            Console.WriteLine("Started");

            bool test = false;
                

            if (test)
            {
                Test();
            }
            else
            {
                Work();
            }
        }
    }
}
