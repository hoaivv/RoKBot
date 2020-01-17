using ProtoBuf;
using Shark.Runtime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace RoKBot.Utils
{
    static partial class Routine
    {
        [Flags]
        public enum Options
        {
            None = 0,
            Optional = 1,
            Click = 2,
            IgnoreVerification = 4,
            NoCache = 8
        }

        [ProtoContract]
        class CachedRectangle
        {
            [ProtoMember(1)]
            public int X;
            [ProtoMember(2)]
            public int Y;
            [ProtoMember(3)]
            public int Width;
            [ProtoMember(4)]
            public int Height;

            public static implicit operator Rectangle(CachedRectangle rect)
            {
                return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
            }

            public static implicit operator CachedRectangle(Rectangle rect)
            {
                return new CachedRectangle { X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height };
            }
        }

        private static Random RandomGenerator = new Random((int)DateTime.UtcNow.Ticks);

        public static void Delete(string file)
        {
            file = Path.Combine("assets", file + ".jpg");
            Cache<string, CachedRectangle>.Delete(file);
        }

        public static bool Click(double percentageX, double percentageY, int randomThreshold = 10)
        {            
            using (Bitmap screen = Device.Screen)
            {                
                int x = (int)(screen.Width * percentageX) + RandomGenerator.Next((-randomThreshold * screen.Width / 1770) / 2, (randomThreshold * screen.Width / 1770) / 2 + 1);
                int y = (int)(screen.Height * percentageY) + RandomGenerator.Next((-randomThreshold * screen.Width / 1770) / 2, (randomThreshold * screen.Width / 1770) / 2 + 1);

                Device.Tap(x, y);

                return true;
            }
        }

        public static bool Click(double percentageX, double percentageY, string file, Options options = Options.None, int randomThreshold = 10)
        {
            file = Path.Combine("assets", file + ".jpg");
            if (!File.Exists(file) && !options.HasFlag(Options.Optional)) return false;

            using (Bitmap image = AForge.Imaging.Image.FromFile(file))
            {
                using (Bitmap screen = Device.Screen)
                {                   
                    using (Bitmap buffer = Collector.Uniform(image, screen))
                    {                        
                        using (Bitmap cropped = new Bitmap(buffer.Width * 2, buffer.Height * 2, PixelFormat.Format24bppRgb))
                        {
                            using (Graphics g = Graphics.FromImage(cropped))
                            {
                                int x = (int)(screen.Width * percentageX);
                                int y = (int)(screen.Height * percentageY);

                                g.DrawImage(screen, 0, 0, new Rectangle { X = Math.Max(0, x - buffer.Width), Y = Math.Max(0, y - buffer.Height), Width = buffer.Width * 2, Height = buffer.Height * 2 }  , GraphicsUnit.Pixel);

                                if (Collector.Find(buffer, cropped) != null)
                                {
                                    x += RandomGenerator.Next(-(randomThreshold * buffer.Width / image.Width ) / 2, (randomThreshold * buffer.Width / image.Width) / 2 + 1);
                                    y += RandomGenerator.Next(-(randomThreshold * buffer.Width / image.Width) / 2, (randomThreshold * buffer.Width / image.Width) / 2 + 1);

                                    Device.Tap(x, y);

                                    return true;
                                }

                                return options.HasFlag(Options.Optional);                                
                            }                            
                        }
                    }
                }
            }
        }

        public static bool Click(string file, Options options = Options.None)
        {
            file = Path.Combine("assets", file + ".jpg");
            if (!File.Exists(file) && !options.HasFlag(Options.Optional)) return false;            

            using (Bitmap image = AForge.Imaging.Image.FromFile(file))
            {
                using (Bitmap screen = Device.Screen)
                {
                    object result = null;

                    if (!Cache<string, CachedRectangle>.HasEntry(file))
                    {
                        using (Bitmap buffer = Collector.Uniform(image, screen))
                        {                            
                            result = Collector.Find(buffer, screen);
                        }
                    }
                    else
                    {
                        if (!options.HasFlag(Options.IgnoreVerification))
                        {
                            using (Bitmap buffer = Collector.Uniform(image, screen))
                            {                                
                                using (Bitmap cropped = new Bitmap(buffer.Width, buffer.Height, PixelFormat.Format24bppRgb))
                                {
                                    using (Graphics g = Graphics.FromImage(cropped))
                                    {
                                        g.DrawImage(screen, 0, 0, Cache<string, CachedRectangle>.Retrive(file), GraphicsUnit.Pixel);
                                    }

                                    result = Collector.Find(buffer, cropped);
                                }

                                if (result != null)
                                {
                                    result = (Rectangle)Cache<string, CachedRectangle>.Retrive(file);
                                }
                            }
                        }
                        else
                        {
                            result = (Rectangle)Cache<string, CachedRectangle>.Retrive(file);
                        }
                    }

                    if (result == null && !options.HasFlag(Options.Optional)) return false;

                    Rectangle r = (Rectangle)result;

                    int x = r.Left + r.Width / 2 + RandomGenerator.Next(-(int)(0.075 * r.Width), +(int)(0.075 * r.Width));
                    int y = r.Top + r.Height / 2 + RandomGenerator.Next(-(int)(0.075 * r.Height), +(int)(0.075 * r.Height)); ;

                    Device.Tap(x, y);


                    if (!options.HasFlag(Options.NoCache))
                    {
                        Cache<string, CachedRectangle>.Update(file, r);
                    }

                    return true;
                }
            }
        }

        public static bool Match(double percentageX, double percentageY, string file, float threshold = 0.9f)
        {
            file = Path.Combine("assets", file + ".jpg");
            if (!File.Exists(file)) return false;

            using (Bitmap image = AForge.Imaging.Image.FromFile(file))
            {
                using (Bitmap screen = Device.Screen)
                {
                    using (Bitmap buffer = Collector.Uniform(image, screen))
                    {                        
                        using (Bitmap cropped = new Bitmap(buffer.Width * 2, buffer.Height * 2, PixelFormat.Format24bppRgb))
                        {
                            using (Graphics g = Graphics.FromImage(cropped))
                            {
                                int x = (int)(screen.Width * percentageX);
                                int y = (int)(screen.Height * percentageY);

                                g.DrawImage(screen, 0, 0, new Rectangle { X = Math.Max(0, x - buffer.Width), Y = Math.Max(0, y - buffer.Height), Width = buffer.Width * 2, Height = buffer.Height * 2 }, GraphicsUnit.Pixel);

                                return Collector.Find(buffer, cropped, threshold) != null;                                
                            }
                        }
                    }
                }
            }
        }

        public static bool Match(string file, out double percentageX, out double percentageY, float threshold = 0.9f)
        {
            percentageX = percentageY = 0;
            file = Path.Combine("assets", file + ".jpg");
            if (!File.Exists(file)) return false;

            using (Bitmap image = AForge.Imaging.Image.FromFile(file))
            {
                using (Bitmap screen = Device.Screen)
                {
                    using (Bitmap buffer = Collector.Uniform(image, screen))
                    {
                        object test = Collector.Find(buffer, screen, threshold);

                        if (test != null)
                        {
                            Rectangle r = (Rectangle)test;

                            percentageX = (double)(r.X + r.Width / 2) / screen.Width;
                            percentageY = (double)(r.Y + r.Height / 2) / screen.Height;

                            return true;
                        }

                        return false;
                    }
                }
            }

        }

        public static void Wait(int minSeconds, int maxSeconds)
        {            
            Thread.CurrentThread.Join(RandomGenerator.Next(minSeconds * 1000, maxSeconds * 1000));
        }

    }

    partial class Routine
    {
        public static bool IsReady => Match(.05, .91, "button.city") || Match(.05, .91, "button.map");

        public static void OpenCity()
        {
            while (!IsReady) Wait(1, 2);

            if (Match(.04, .76, "button.search", 0.95f))
            {
                Click(.05, .91);
                Wait(2, 3);
            }            
        }

        public static void OpenMap()
        {
            while (!IsReady) Wait(1, 2);

            if (!Match(.04, .76, "button.search", 0.95f))
            {
                Click(.05, .91); 
                Wait(2, 3);
            }
        }

        public static void OpenRandom()
        {
            if (RandomGenerator.Next(0,2) < 1)
            {
                OpenCity();
            }
            else
            {
                OpenMap();
            }
        }
    }

    partial class Routine
    {
        public static bool ClaimDaily()
        {
            OpenMap();            
            Click(.95, .03, 0);
            Wait(2, 3);
            if (Click("icon.daily", Options.NoCache))
            {
                Wait(2, 3);
                Click(.89, .23);
            }

            Click(.03, .05); // back;

            return true;
        }
    }

    partial class Routine
    {
        public static bool HealTroops()
        {
            OpenCity();

            if (Click(.41,.69, "icon.heal"))
            {
                Wait(2, 3);
                if (Click(.71,.82, "icon.clock"))
                {
                    Wait(2, 3);
                    Click(.42, .73); // request help
                    Wait(1, 2);
                    OpenMap();

                    Console.WriteLine("Healing");

                    return true;
                }
                else
                {
                    Click(.85, .11); // close
                }
            }
            else
            {
                Click(.42, .73);
                Wait(1, 2);
                OpenMap();
            }

            return false;
        }
    }

    partial class Routine
    {
        static bool SendGatheringTroops(string type)
        {
            Wait(2, 3);
            Click(.5, .5); // click food icon in map

            Wait(3, 5);
            Click(.74, .68); // click gather button

            Wait(3, 5);

            if (Click(.78, .19, "action.troop"))
            {
                Wait(3, 5);

                if (!Match(.73, .9,"icon.notroops") && Click(.72, .85, "action.march"))
                {
                    Console.WriteLine("Gathering " + type);
                    return true;
                }
                else
                {
                    Click(.87, .06); // close
                }
            }
            else
            {
                Click(.5, .5);
            }

            return false;
        }

        public static bool GatherFood()
        {
            // GATHER FOOD

            OpenMap();

            Routine.Wait(3,5);

            if (!Routine.Click(.04, .76, "button.search")) // open gathering menu
            {
                return false;
            }

            Routine.Wait(3,5);
            Routine.Click(.35, .85); // click food icon in menu

            Routine.Wait(3, 5);
            Routine.Click(.42, .56, 0); // max level;

            Routine.Wait(1,2);
            if (!Click(.35, .66, "label.search")) return false; // click search

            Routine.Wait(1, 2);
            while (Match(.35, .66, "label.search"))
            {
                Click(.24, .55); // minus
                Routine.Wait(1, 2);
                Routine.Click(.35, .66); // click search
                Routine.Wait(1, 2);
            }

            return SendGatheringTroops("food");
        }

        public static bool GatherWood()
        {
            // GATHER WOOD

            OpenMap();

            Routine.Wait(3,5);

            if (!Routine.Click(.04, .73, "button.search")) // open gathering menu
            {
                return false;
            }

            Routine.Wait(3,5);
            Routine.Click(.50, .85); // click wood icon in menu

            Routine.Wait(3, 5);
            Routine.Click(.57, .56, 0); // max level;

            Routine.Wait(1, 2);
            if (!Click(.509, .66, "label.search")) return false; // click search

            Routine.Wait(1, 2);
            while (Match(.50, .66, "label.search"))
            {
                Click(.39, .55); // minus
                Routine.Wait(1, 2);
                Routine.Click(.50, .66); // click search
                Routine.Wait(1, 2);
            }

            return SendGatheringTroops("wood");
        }

        public static bool GatherStone()
        {
            // GATHER STONE

            OpenMap();

            Wait(3,5);
            if (!Click(.04, .73, "button.search")) // open gathering menu
            {
                return false;
            }

            Wait(3,5);
            Click(.65, .85); // click stone icon in menu

            Wait(3, 5);
            if (!Click(.65, .66, "label.search")) return false; // click search

            Wait(1, 2);
            Click(.65, .66); // click search

            Wait(1, 2);
            while (Match(.65, .66, "label.search"))
            {
                Click(.54, .55); // minus
                Wait(1, 2);
                Click(.65, .66); // click search
                Wait(1, 2);
            }

            return SendGatheringTroops("stone");
        }

        public static bool GatherGold()
        {
            // GATHER GOLD

            OpenMap();

            //Wait(3,5);
            if (!Click(.04, .73, "button.search")) // open gathering menu
            {
                return false;
            }

            Wait(3,5);
            Click(.80, .85); // click gold icon in menu

            Wait(3, 5);
            Click(.87, .56, 0); // max level;

            Wait(1, 2);
            if (!Click(.80, .66, "label.search")) return false; // click search

            Wait(1, 2);
            while (Match(.80, .66, "label.search"))
            {
                Click(.69, .55); // minus
                Wait(1, 2);
                Click(.80, .66); // click search
                Wait(1, 2);
            }

            return SendGatheringTroops("gold");
        }

        private static Queue<Func<bool>> GatheringTasks = new Queue<Func<bool>>(new Func<bool>[] { GatherFood, GatherWood, GatherStone });

        public static bool GatherResources()
        {
            int count = GatheringTasks.Count;

            bool pass = true;

            while(count-- > 0)
            {
                Func<bool> task = GatheringTasks.Peek();

                pass = task();
                
                if (pass)
                {
                    GatheringTasks.Dequeue();
                    GatheringTasks.Enqueue(task);
                }
                else
                {
                    break;
                }                
            }

            return pass;
        }
    }

    partial class Routine
    {
        static bool CommitTraining(string type)
        {
            bool trained = Click(.73, .84, "icon.clock"); // click train

            if (!trained)
            {
                Click(.85, .13); // close
            }

            Wait(2, 3);

            if (Match(.5, .68, "button.getmore"))
            {
                Click(.79, .2); // close
                Wait(1, 2);
                Click(.85, .13); // close

                trained = false;
            }

            if (trained) Console.WriteLine("Traning " + type);

            return trained;
        }

        public static bool TrainInfantry()
        {
            OpenCity();

            Click(.5, .4); 
            Wait(1, 2);
            Click(.5, .4); 
            Wait(1, 2);

            if (!Click(.61, .58, "icon.infantry")) return false;
            Wait(1, 2);

            return CommitTraining("infantry");
        }

        public static bool TrainArcher()
        {
            OpenCity();

            Click(.61, .53); 
            Wait(1, 2);
            Click(.61, .53); 
            Wait(1, 2);

            if (!Click(.7, .7, "icon.archer")) return false;
            Wait(1, 2);

            return CommitTraining("archer");
        }

        public static bool TrainCavalry()
        {
            OpenCity();

            Click(.5, .65); 
            Wait(1, 2);
            Click(.5, .65); 
            Wait(1, 2);

            if (!Click(.61, .83, "icon.cavalry")) return false;
            Wait(1, 2);

            return CommitTraining("cavalry");
        }

        public static bool TrainSiege()
        {
            OpenCity();

            Click(.39, .52); 
            Wait(1, 2);
            Click(.39, .52); 
            Wait(1, 2);

            if (!Click(.5, .71, "icon.siege")) return false;
            Wait(1, 2);

            Click(.49, .28); // T1            
            Wait(1, 2);

            return CommitTraining("siege");
        }

        public static bool TrainTroops()
        {
            return TrainInfantry() & TrainCavalry() & TrainArcher() & TrainSiege();
        }
    }

    partial class Routine
    {
        public static bool Recruit()
        {
            OpenCity();
            
            Click(.6, .3); // open menu
            Wait(1, 2);

            if (!Click(.71, .49, "button.recruit")) return false;

            bool recruited = false;

            Wait(3, 4);
            while (Click(.33, .85, "button.open") || Click(.70, .85, "button.open"))
            {
                recruited = true;
                Wait(5, 6);
                while (Click(.30, .86, "button.confirm") || Click(.15, .84, "button.confirm")) Wait(5, 6);
            }

            Wait(3, 4);
            Click(.03, .09); // back

            if (recruited) Console.WriteLine("Keys used");

            return true;
        }
    }

    partial class Routine
    {
        public static bool CollectResources()
        {
            OpenCity();

            if (Click(.28, .52, "collect.food") || Click(.28, .52, "collect.food.old")) Console.WriteLine("Food collected");
            Wait(1, 2);
            if (Click(.21, .43, "collect.wood") || Click(.21, .43, "collect.wood.old")) Console.WriteLine("Wood collected");
            Wait(1, 2);
            if (Click(.28, .34, "collect.stone") || Click(.28, .34, "collect.stone.old")) Console.WriteLine("Stone collected");
            Wait(1, 2);
            if (Click(.36, .26, "collect.gold") || Click(.36, .26, "collect.gold.old")) Console.WriteLine("Gold collected");
            Wait(1, 2);

            return true;
        }
    }

    partial class Routine
    {
        public static bool Explore()
        {
            OpenCity();

            bool send = false;

            while (true)
            {
                Click(.7, .4); // open menu

                Wait(1, 2);
                if (!Click(.81, .6, "icon.explore")) break;

                Wait(2, 3);
                if (Click(.78, .46, "button.explore") || Click(.78, .64, "button.explore") || Click(.78, .81, "button.explore"))
                {
                    Wait(2, 3);
                    if (Click(.62, .73, "button.explore"))
                    {
                        Wait(2, 3);
                        send |= Click(.78, .27, "button.send") || Click(.78, .44, "button.send") || Click(.78, .60, "button.send");
                        Wait(2, 3);

                        OpenCity();
                    }
                }
                else
                {
                    Click(.85, .11); // close                    
                    break;
                }
            }

            if (send) Console.WriteLine("Exploring");

            return true;
        }
    }

    partial class Routine
    {
        public static bool ClaimCampaign()
        {
            OpenCity();

            if (!Click(.64, .93, "button.campaign"))
            {
                Wait(2, 3);
                Click(.96, .93);
                Wait(2, 3);
                if (!Click(.64, .93, "button.campaign")) return false;
            }

            Wait(2, 3);
            Click(.16, .45);
            Wait(2, 3);
            Click(.1, .32);
            Wait(2, 3);
            if (Click(.5, .72, "button.confirm")) Console.WriteLine("Rewards claimed (Campaign)");
            Wait(2, 3);
            Click(.03, .07);
            Wait(2, 3);
            Click(.03, .07);
            Wait(2, 3);

            return true;
        }
    }

    partial class Routine
    { 
        public static bool AllianceTasks()
        {
            OpenCity();
            
            if (!Click(.8, .9, "button.alliance"))
            {
                Wait(2, 3);
                Click(.96, .9);
                Wait(2, 3);
                if (!Click(.8, .9, "button.alliance")) return false;
            }

            Wait(2, 3);

            if (Match(.48, .55, "icon.war")) // check if account is a member of an alliance
            {

                // HELP ALLIANCE MEMBERS

                Click(.79, .56); // help icon in alliance menu

                Wait(2, 3);
                if (Click(.5, .88, "button.help")) // button help
                {
                    Console.WriteLine("Allies helped");
                }
                else
                {
                    Click(.87, .08); // close button of help menu
                }

                // COLLECT TERRITORY RESOURCES

                Wait(2, 3);
                Click(.69, .56); // territory icon in alliance menu
                Wait(2, 3);
                Click(.8, .2); // claim button in territory menu
                Wait(2, 3);
                Click(.87, .07); // close button in territory menu

                // COLLECT GIFTS

                bool hasGift = false;

                Wait(2, 3);
                Click(.69, .76); // gift icon in alliance menu
                Wait(2, 3);
                Click(.52, .29); // normal tab in gift menu
                Wait(2, 3);
                Click(.86, .29); // claim all icon
                Wait(2, 3);
                hasGift |= Click(.51, .72, "button.confirm");
                Wait(2, 3);
                Click(.72, .29); // rare tab in gift menu

                Wait(2, 3);
                if (Click(.76, .38, "button.claim.gift"))
                {
                    hasGift = true;

                    do
                    {
                        Wait(2, 3);
                    }
                    while (Click(.76, .51, "button.claim.gift"));
                }

                Wait(2, 3);
                Click(.28, .60); // collect big chest
                Wait(2, 3);
                Click(.28, .60); // collect big chest

                Wait(2, 3);
                Click(.87, .08); // close button of gift menu

                if (hasGift) Console.WriteLine("Gifts claimed");

                // DONATE                

                Wait(2, 3);
                Click(.59, .76); // technology icon in alliance menu
                Wait(10, 11);

                if (Match("label.recommendation", out double x, out double y))
                {
                    Click(x, y + 0.07);
                    bool donationMade = false;
                    Wait(2, 3);

                    while (Click(.76, .74, "button.donate"))
                    {
                        donationMade = true;
                        Wait(1, 2);
                    }

                    Wait(2, 3);
                    Click(.85, .13); // close button of donate menu
                    Wait(2, 3);
                    Click(.87, .11); // close button of technology menu
                   
                    if (donationMade) Console.WriteLine("Donated");
                }
                else
                {
                    Click(.87, .11); // close button of technology menu
                }
            }

            Wait(2, 3);
            Click(.87, .08); // close button of alliance menu

            return true;
        }        
    }

    partial class Routine
    {
        public static bool ClaimQuests()
        {
            OpenCity();

            Wait(2, 3);
            Click(.05, .91, "button.city", Routine.Options.Optional);

            Wait(2, 3);
            if (Click(.03, .24, "button.quest")) // open quest menu
            {
                bool questClaimed = false;

                Wait(2, 3);;
                Click(.1, .26); // quest tab

                Wait(2, 3);

                while (Click(.78, .36, "button.claim"))
                {
                    questClaimed = true;
                    Wait(2, 3); // claim main quests
                }

                while (Click(.78, .54, "button.claim"))
                {
                    questClaimed = true;
                    Wait(2, 3); // claim side quests
                }

                Wait(2, 3);
                Click(.1, .43); // daily tab

                Wait(2, 3);
                while (Click(.79, .47, "button.claim"))
                {
                    questClaimed = true;
                    Wait(2, 3); // claim daily objectives
                }

                for (int i = 0; i < 5; i++)
                {
                    Click(.28 + i * 0.13, .28); // click chests
                    Wait(2, 3);
                    Click(.28 + i * 0.13, .28); // click chests
                    Wait(2, 3);
                }

                Click(.85, .13); // close button of quest menu

                if (questClaimed) Console.WriteLine("Quests claimed");
                return true;
            }

            return false;
        }
    }

    partial class Routine
    {
        public static bool ReadMails()
        {
            OpenCity();

            bool claimed = false;

            Click(.96, .80); // open mails
            Wait(2, 3);

            if (!Match(.86, .06, "label.mail")) return false;

            /*
            Wait(2, 3);
            Click(.08, .11); // personal tab
            Wait(2, 3);
            Click(.11, .93); // claim all
            Wait(2, 3);
            claimed |= (Click(.5, .75, "button.confirm"));
            */

            Wait(2, 3);
            Click(.2, .06); // report tab
            Wait(2, 3);

            while (Click(.9, .43, "icon.view"))
            {
                Wait(4, 5);
                Click(.5, .5);
                
                Wait(2, 3);

                if (Click(.7, .66, "button.investigate"))
                {
                    Wait(2, 3);

                    if (!Match(.97, .17, "icon.investigate")) Click(.78, .27, "button.send");
                    if (!Match(.97, .35, "icon.investigate")) Click(.78, .44, "button.send");
                    if (!Match(.97, .52, "icon.investigate")) Click(.78, .60, "button.send");
                    Wait(1, 2);

                    OpenCity();

                    Click(.96, .80); // open mails
                    Wait(2, 3);

                    break;
                }
                else
                {
                    Click(.5, 5);
                    Wait(1, 2);
                }

                OpenCity();

                Click(.96, .80); // open mails
                Wait(2, 3);
            }

            Click(.11, .92); // claim all
            Wait(2, 3);
            claimed |= (Click(.5, .72, "button.confirm"));

            

            Wait(2, 3);
            Click(.31, .06); // alliance tab
            Wait(2, 3);
            Click(.11, .92); // claim all
            Wait(2, 3);
            claimed |= (Click(.5, .72, "button.confirm"));

            Wait(2, 3);
            Click(.43, .06); // system tab
            Wait(2, 3);
            Click(.11, .92); // claim all
            Wait(2, 3);
            claimed |= (Click(.5, .72, "button.confirm"));

            Wait(2, 3);
            Click(.96, .06); // close

            if (claimed) Console.WriteLine("Mails claimed");

            return true;
        }
    }

    partial class Routine
    {
        public static bool ClaimVIP()
        {
            OpenRandom();
            
            Click(.12, .09); // open VIP menu
            Wait(2, 3);
            Click(.73, .55); // claim chest
            Wait(2, 3);
            Click(.73, .54); // close popup
            Wait(2, 3);
            Click(.79, .25); // claim daily
            Wait(2, 3);
            Click(.79, .25); // close popup            
            Wait(2, 3);
            Click(.85, .11); // close
            Wait(2, 3);

            return true;
        }
    }

    partial class Routine
    {
        public static bool Build()
        {
            OpenCity();

            Click(.54, .19); // click building

            try
            {
                Wait(1, 2);
                if (!Click(.61, .53, "icon.build"))
                    return false;

                Wait(2, 3);
                if (
                    (!Match(.78, .31, "button.build", .95f) || !Click(.78, .31))
                &&
                    (!Match(.78, .54, "button.build", .95f) || !Click(.78, .54))
                )
                {
                    Click(.85, .12); // close
                    return false;
                }

                Wait(2, 3);                

                if (!Click("icon.upgrade", Options.NoCache))
                    return false;

                Wait(2, 3);
                if (!Click(.77, .73, "button.upgrade"))
                {
                    Click(.85, .12); // close
                    return false;
                }

                Wait(2, 3);
                if (Match(.5, .66, "button.purchase") || Match(.5, .68, "button.getmore"))
                {
                    Click(.79, .2); // close
                    Wait(1, 2);
                    Click(.85, .12); // close
                    return false;
                }

                Wait(1, 2);
                Click(.5, .41);                

                Console.WriteLine("Build");
                return true;
            }
            finally
            {
                Wait(1, 2);
                OpenMap();
            }
        }
    }

    partial class Routine
    {
        private static Queue<PointF> Accounts = new Queue<PointF>();

        public static bool SwitchAccount()
        {
            OpenRandom();
            
            Click(.04, .1); // open profile
            Wait(2, 3);
            Click(.77, .74); // open settings
            Wait(2, 3);
            Click(.23, .54); // open character management
            Wait(10, 15);
            
            if (Accounts.Count < 2)
            {
                float x = .44f;
                float y = .33f;

                Accounts.Clear();

                while(Match(x, y, "bg.account"))
                {
                    Accounts.Enqueue(new PointF { X = x, Y = y });
                    
                    if (x == .81f)
                    {
                        x = .44f;
                        y += .19f;
                    }
                    else
                    {
                        x = .81f;
                    }
                }
            }

            if (Accounts.Count > 1)
            {

                int count = Accounts.Count;

                while (count-- > 0)
                {
                    PointF pos = Accounts.Peek();
                    Click(pos.X, pos.Y);
                    Wait(2, 3);

                    Accounts.Enqueue(Accounts.Dequeue());

                    if (Click(.63, .72, "button.yes"))
                    {
                        Console.WriteLine("Account switched");
                        Wait(20, 25);

                        while (!Match(.05, .91, "button.map")) Wait(2, 3);                        
                        return true;
                    }                    
                }                
            }

            Wait(2, 3);
            Click(.85, .16); // close account management
            Wait(2, 3);
            Click(.87, .11); // close settings
            Wait(2, 3);
            Click(.85, .16); // close profile

            return false;
        }
    }
}
