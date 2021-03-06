﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace RoKBot.Utils
{
    static partial class Routine
    {
        public static void Wait(int minSeconds, int maxSeconds)
        {            
            Thread.CurrentThread.Join(Helper.RandomGenerator.Next(minSeconds * 1000, maxSeconds * 1000 + 1));
        }        
    }
   
    partial class Routine
    {
        public static bool Ready => Device.Match(0x1e, 0x1bf, "button.city", out Rectangle match1) || Device.Match(0x1e, 0x1bf, "button.map", out Rectangle match2);

        public static void OpenCity()
        {
            while (!Ready) Wait(1, 2);

            if (Device.Match(0x1d, 0x185, "button.search", out Rectangle match, 0.95f))
            {
                Device.Tap(0x1e, 0x1bf);
                Wait(2, 3);
            }            
        }

        public static void OpenMap()
        {
            while (!Ready) Wait(1, 2);

            if (!Device.Match(0x1d, 0x185, "button.search", out Rectangle match, 0.95f))
            {
                Device.Tap(0x1e, 0x1bf); 
                Wait(2, 3);
            }
        }

        public static void OpenRandom()
        {
            if (Helper.RandomGenerator.Next(0,2) < 1)
            {
                OpenCity();
            }
            else
            {
                OpenMap();
            }
        }

        private static bool UpgradeCurrentBuilding(string name)
        {
            if (Device.Tap("icon.upgrade"))
            {
                Wait(2, 3);
                if (Device.Tap(0x1eb, 0x14a, "button.upgrade"))
                {
                    Wait(2, 3);

                    if (Device.Match(0x13d, 0x66, "label.busy", out Rectangle busy))
                    {
                        Device.Tap(0x222, 0x65); // close
                        Wait(1, 2);
                        Device.Tap(0x21f, 0x64); // close
                        Wait(1, 2);
                        return false;
                    }

                    if (Device.Match("button.purchase", out Rectangle purchase) || Device.Match("button.getmore", out Rectangle getmore))
                    {
                        Device.Tap(0x1f6, 0x82); // close
                        Wait(1, 2);
                        Device.Tap(0x21f, 0x64); // close
                        Wait(1, 2);
                        return false;
                    }

                    Wait(1, 2);
                    Device.Tap(0x141, 0xdd); // request help
                    Helper.Print("Upgrading " + name);

                    return true;

                }
                else
                {
                    Device.Tap(0x21f, 0x64); // close                    
                    Wait(1, 2);
                }
            }

            return false;
        }

    }

    partial class Routine
    {
        public static bool Research()
        {
            if (Current.Get("research").TotalHours < 1) return false;

            try
            {
                OpenCity();
                Device.Tap(0x222, 0x84);
                Wait(1, 2);

                if (Device.Match("icon.speedup", out Rectangle speedup)) return false;

                if (!Device.Match("button.research", out Rectangle research))
                {
                    OpenMap();
                    OpenCity();

                    Device.Tap(0x222, 0x84);
                    Wait(1, 2);

                    if (!Device.Match("button.research", out research)) return false;
                }

                if (UpgradeCurrentBuilding("academy")) return true;

                OpenMap();
                OpenCity();

                Device.Tap(0x222, 0x84);
                Wait(1, 2);

                Device.Tap(research);
                Wait(1, 2);                

                if (Helper.RandomGenerator.Next() % 2 == 0)
                {
                    Device.Tap(0x27, 0x96); //economy
                }
                else
                {
                    Device.Tap(0x27, 0xd2); //military
                }

                Wait(1, 2);

                Rectangle[] technologies;
                while (!Device.Match("icon.technology", out technologies, null, 0.92f) && !Device.Match(0x22f, 0x191, "icon.corner.research", out Rectangle corner, 0.96f))
                {
                    Device.Swipe(0x221, 0x107, 0x67, 0x107, 1000);
                    Wait(1, 2);
                }
                
                if (technologies.Length == 0)
                {
                    Wait(0, 1);
                    Device.Tap(0x22d, 0x5f); // close

                    return false;
                }
                else
                {
                    foreach (Rectangle technology in technologies)
                    {
                        Device.Tap(technology);

                        Wait(1, 1);

                        if (Device.Tap("icon.research"))
                        {
                            Wait(1, 1);

                            if (Device.Match("button.getmore", out Rectangle getmore))
                            {
                                Device.Tap(0x1f7, 0x82); // close getmore
                                Wait(1, 1);
                                Device.Tap(0x222, 0x67); // close technology popup
                                Wait(1, 1);
                                continue;
                            }

                            Device.Tap(0x1c2, 0x83); // request help
                            Wait(0, 1);
                            Device.Tap(0x22d, 0x5f); // close

                            Helper.Print("Researching");
                            return true;
                        }


                        Device.Tap(0x222, 0x67); // close technology popup                            
                        Wait(1, 1);                        
                    }

                    Wait(1, 2);
                    Device.Tap(0x22d, 0x5f); // close

                    return false;
                }
            }
            finally
            {
                OpenMap();
                Current.Set("research");
            }
        }
    }

    partial class Routine
    {
        public static bool ClaimDaily()
        {
            if (Current.Get("daily").TotalHours < 12) return false;

            OpenMap();            
            Device.Tap(0x25d, 0xc);

            Wait(2, 3);
            if (Device.Match("icon.daily", out Rectangle match))
            {
                Device.Tap(match);
                Wait(2, 3);
                Device.Tap(0x23f, 0x91);
                Wait(1, 2);
            }

            Device.Tap(0x13, 0x13); // back;
            Current.Set("daily");

            return true;
        }
    }

    partial class Routine
    {
        public static bool HealTroops()
        {
            OpenCity();

            if (Device.Tap(0xf8,0x156, "icon.heal"))
            {
                Wait(2, 3);
                if (Device.Tap(0x1ca,0x164, "icon.clock"))
                {
                    Wait(2, 3);
                    Device.Tap(0xf9, 0x161); // request help
                    Wait(1, 2);
                    OpenMap();

                    Helper.Print("Healing");

                    return true;
                }
                else
                {
                    Device.Tap(0x221, 0x65); // close
                }
            }
            else
            {
                Device.Tap(0xf8, 0x156); // collect healed troops
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
            Device.Tap(0x13f, 0xf1); // click food icon in map

            Wait(1, 2);
            Device.Tap(0x1dd, 0x132); // click gather button

            Wait(1, 2);

            if (Device.Tap(0x1f5, 0x83, "action.troop"))
            {
                Wait(2, 3);

                if (Device.Tap(0x1ca, 0x16e, "action.march"))
                {
                    Wait(1, 1);

                    if (!Device.Match(0x1ca, 0x16e, "action.march", out Rectangle match))
                    {
                        Helper.Print("Gathering " + type);
                        return true;
                    }
                    else
                    {
                        Device.Tap(0x22c, 0x53); // close
                    }
                }
                else
                {
                    Device.Tap(0x22c, 0x53); // close
                }
            }
            else
            {
                Device.Tap(0x13f, 0xf1);
            }

            return false;
        }

        public static bool GatherFood()
        {
            // GATHER FOOD

            OpenMap();

            if (!Device.Tap(0x1c, 0x186, "button.search")) // open gathering menu
            {
                return false;
            }

            Wait(1,2);
            Device.Tap(0xdf, 0x1b4); // click food icon in menu

            Wait(1, 2);

            if (!Device.Match("label.search", out Rectangle search) || search.X < 0x9c || search.X + search.Width > 0x114)
            {
                Device.Tap(0x13f, 0xf1);
                Wait(1, 2);

                return true;
            }

            Device.Tap(0x114, 0x141, 0); // max level;

            Wait(0,1);
            Device.Tap(search);

            Wait(0, 1);
            while (Device.Match("label.search", out Rectangle match, search))
            {
                Device.Tap(0x9c, 0x141); // minus
                Wait(0, 1);
                Device.Tap(search); // click search
                Wait(0, 1);
            }

            return SendGatheringTroops("food");
        }

        public static bool GatherWood()
        {
            // GATHER WOOD

            OpenMap();

            if (!Device.Tap(0x1c, 0x186, "button.search")) // open gathering menu
            {
                return false;
            }

            Wait(1, 2);
            Device.Tap(0x13e, 0x1b4); // click wood icon in menu

            Wait(1, 2);

            if (!Device.Match("label.search", out Rectangle search) || search.X < 0xfc || search.X + search.Width > 0x174)
            {
                Device.Tap(0x13f, 0xf1);
                Wait(1, 2);

                return true;
            }

            Device.Tap(0x174, 0x141, 0); // max level;

            Wait(0, 1);
            Device.Tap(search);

            Wait(0, 1);
            while (Device.Match("label.search", out Rectangle match, search))
            {
                Device.Tap(0xfc, 0x141); // minus
                Wait(0, 1);
                Device.Tap(search); // click search
                Wait(0, 1);
            }

            return SendGatheringTroops("wood");
        }

        public static bool GatherStone()
        {
            // GATHER STONE

            OpenMap();

            if (!Device.Tap(0x1c, 0x186, "button.search")) // open gathering menu
            {
                return false;
            }

            Wait(1, 2);
            Device.Tap(0x1a0, 0x1b4); // click stone icon in menu

            Wait(1, 2);

            if (!Device.Match("label.search", out Rectangle search) || search.X < 0x15b || search.X + search.Width > 0x1d3)
            {
                Device.Tap(0x13f, 0xf1);
                Wait(1, 2);

                return true;
            }

            Device.Tap(0x1d3, 0x141, 0); // max level;

            Wait(0, 1);
            Device.Tap(search);

            Wait(1, 2);
            while (Device.Match("label.search", out Rectangle match, search))
            {
                Device.Tap(0x15b, 0x141); // minus
                Wait(0, 1);
                Device.Tap(search); // click search
                Wait(0, 1);
            }

            return SendGatheringTroops("stone");
        }

        public static bool GatherGold()
        {
            // GATHER GOLD

            OpenMap();

            if (!Device.Tap(0x1c, 0x186, "button.search")) // open gathering menu
            {
                return false;
            }

            Wait(1, 2);
            Device.Tap(0x200, 0x1b4); // click gold icon in menu

            Wait(1, 2);

            if (!Device.Match("label.search", out Rectangle search) || search.X < 0x1b9 || search.X + search.Width > 0x231)
            {
                Device.Tap(0x13f, 0xf1);
                Wait(1, 2);

                return true;
            }

            Device.Tap(0x231, 0x141); // max level;

            Wait(0, 1);
            Device.Tap(search);

            Wait(0, 1);
            while (Device.Match("label.search", out Rectangle match, search))
            {
                Device.Tap(0x1b9, 0x141); // minus
                Wait(0, 1);
                Device.Tap(search); // click search
                Wait(0, 1);
            }

            return SendGatheringTroops("gold");
        }

        public static bool GatherResources()
        {
            int count = Current.GatheringOrder.Count;

            bool pass = true;

            while(count-- > 0)
            {
                string task = Current.GatheringOrder.Peek();

                switch(task)
                {
                    case "F": pass = GatherFood(); break;
                    case "W": pass = GatherWood(); break;
                    case "S": pass = GatherStone(); break;
                    case "G": pass = GatherGold(); break;
                    default: pass = false; break;
                }
                
                if (pass)
                {
                    Current.GatheringOrder.Dequeue();
                    Current.GatheringOrder.Enqueue(task);
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
            Wait(2, 3);
            bool trainable = !Device.Match(0x1e4, 0x15f, "button.speedup", out Rectangle speedup, 0.95f);

            if (!trainable)
            {
                Device.Tap(0x226, 0x64); // close
            }
            else
            {
                Device.Tap(0x1e4, 0x15f); // click train
                Wait(1, 2);

                if (Device.Match("button.getmore", out Rectangle getmore))
                {
                    trainable = false;

                    Device.Tap(0x1f7, 0x82); // close
                    Wait(1, 2);
                    Device.Tap(0x225, 0x66); // close                    
                }
                else
                {
                    Helper.Print("Traning " + type);
                }
            }

            OpenMap();

            return trainable;
        }

        public static bool TrainInfantry()
        {            
            OpenCity();
            Device.Tap(0x13d, 0xb2); 
            Wait(1, 2);

            if (Device.Match("icon.speedup", out Rectangle speedup))
            {
                OpenMap();
                return false;
            }

            if (!Device.Match("icon.infantry", out Rectangle infantry))
            {                
                OpenMap();
                OpenCity();
                Device.Tap(0x13d, 0xb2);
                Wait(1, 2);
            }

            if (UpgradeCurrentBuilding("barracks"))
            {
                OpenMap();
                return true;
            }
            else
            {
                OpenMap();
                OpenCity();
                Device.Tap(0x13d, 0xb2);
                Wait(1, 2);
            }

            if (!Device.Tap("icon.infantry"))
            {
                OpenMap();
                return false;
            }

            Wait(1, 2);

            return CommitTraining("infantry");
        }

        public static bool TrainArcher()
        {
            OpenCity();

            Device.Tap(0x1a0, 0xf0); 
            Wait(1, 2);

            if (Device.Match("icon.speedup", out Rectangle speedup))
            {
                OpenMap();
                return false;
            }

            if (!Device.Match("icon.archer", out Rectangle archer))
            {
                OpenMap();                
                OpenCity();
                Device.Tap(0x1a0, 0xf0);
                Wait(1, 2);
            }            

            if (UpgradeCurrentBuilding("archery range"))
            {
                OpenMap();
                return true;
            }
            else
            {
                OpenMap();
                OpenCity();
                Device.Tap(0x1a0, 0xf0);
                Wait(1, 2);
            }

            if (!Device.Tap("icon.archer"))
            {
                OpenMap();
                return false;
            }

            Wait(1, 2);

            return CommitTraining("archer");
        }

        public static bool TrainCavalry()
        {
            OpenCity();

            Device.Tap(0x144, 0x136); 
            Wait(1, 2);

            if (Device.Match("icon.speedup", out Rectangle speedup))
            {
                OpenMap();
                return false;
            }

            if (!Device.Match("icon.cavalry", out Rectangle cavalry))
            {                
                OpenMap();
                OpenCity();
                Device.Tap(0x144, 0x136);
                Wait(1, 2);
            }

            if (UpgradeCurrentBuilding("stable"))
            {
                OpenMap();
                return true;
            }
            else
            {
                OpenMap();
                OpenCity();
                Device.Tap(0x144, 0x136);
                Wait(1, 2);
            }

            if (!Device.Tap("icon.cavalry"))
            {
                OpenMap();
                return false;
            }

            Wait(1, 2);

            return CommitTraining("cavalry");
        }

        public static bool TrainSiege()
        {
            OpenCity();

            Device.Tap(0xe7, 0xf2); 
            Wait(1, 2);

            if (Device.Match("icon.speedup", out Rectangle speedup))
            {
                OpenMap();
                return false;
            }

            if (!Device.Match("icon.siege", out Rectangle siege))
            {                
                OpenMap();
                OpenCity();
                Device.Tap(0xe7, 0xf2);
                Wait(1, 2);
            }            

            if (UpgradeCurrentBuilding("siege workshop"))
            {
                OpenMap();
                return true;
            }
            else
            {
                OpenMap();
                OpenCity();
                Device.Tap(0xe7, 0xf2);
                Wait(1, 2);
            }

            if (!Device.Tap("icon.siege"))
            {
                OpenMap();
                return false;
            }

            Wait(1, 2);
            
            Device.Tap(0x13a, 0x94); // T1
            Wait(1, 2);

            return CommitTraining("siege");
        }

        public static bool TrainTroops()
        {
            return TrainInfantry() & TrainArcher() & TrainCavalry() & TrainSiege();
        }
    }

    partial class Routine
    {
        public static bool Recruit()
        {
            OpenCity();

            Device.Tap(0x18f, 0x81); // open menu
            Wait(1, 2);

            if (!Device.Tap("button.recruit")) return false;

            bool recruited = false;

            Wait(3, 4);
            while (Device.Tap(0xca, 0x16a, "button.open") || Device.Tap(0x1c9, 0x16a, "button.open"))
            {
                recruited = true;
                Wait(5, 6);
                while (Device.Tap("button.confirm")) Wait(5, 6);
            }

            Wait(3, 4);
            Device.Tap(0x13, 0x13); // back

            if (recruited) Helper.Print("Keys used");

            OpenMap();

            return true;
        }
    }

    partial class Routine
    {
        public static bool CollectResources()
        {
            if (Current.Get("rss").TotalHours < 1) return false;

            OpenCity();

            if (Device.Tap(0x85, 0x109, "collect.food") || Device.Tap(0x85, 0x109, "collect.food.old")) Helper.Print("Food collected");
            Wait(1, 2);
            if (Device.Tap(0x44, 0xdc, "collect.wood") || Device.Tap(0x44, 0xdc, "collect.wood.old")) Helper.Print("Wood collected");
            Wait(1, 2);
            if (Device.Tap(0x8a, 0xad, "collect.stone") || Device.Tap(0x8a, 0xad, "collect.stone.old")) Helper.Print("Stone collected");
            Wait(1, 2);
            if (Device.Tap(0xcd, 0x81, "collect.gold") || Device.Tap(0xcd, 0x81, "collect.gold.old")) Helper.Print("Gold collected");
            Wait(1, 2);

            Current.Set("rss");

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
                Device.Tap(0x1ee, 0xbb); // open menu

                Wait(1, 2);
                if (!Device.Tap("icon.explore")) break;

                Wait(2, 3);
                if (Device.Tap(0x1f3, 0xda, "button.explore") || Device.Tap(0x1f3, 0x11d, "button.explore") || Device.Tap(0x1f3, 0x15f, "button.explore"))
                {
                    Wait(2, 3);
                    if (Device.Tap(0x191, 0x13e, "button.explore"))
                    {
                        Wait(2, 3);
                        send |= Device.Tap(0x1f6, 0x91, "button.send") || Device.Tap(0x1f6, 0xd0, "button.send") || Device.Tap(0x1f6, 0x110, "button.send");
                        Wait(2, 3);

                        OpenCity();
                    }
                }
                else
                {
                    Device.Tap(0x221, 0x66); // close                    
                    break;
                }
            }

            if (send) Helper.Print("Exploring");

            OpenMap();

            return true;
        }
    }

    partial class Routine
    {
        public static bool ClaimCampaign()
        {
            if (Current.Get("campaign").TotalHours < 12) return false;

            OpenCity();

            if (!Device.Tap(0x19e, 0x1c6, "button.campaign"))
            {
                Wait(2, 3);
                Device.Tap(0x267, 0x1c6);
                Wait(2, 3);
                if (!Device.Tap(0x19e, 0x1c6, "button.campaign")) return false;
            }

            Wait(2, 3);
            Device.Tap(0x6b, 0xdd);
            Wait(2, 3);
            Device.Tap(0x3e, 0xaf);
            Wait(2, 3);
            if (Device.Tap(0x141, 0x145, "button.confirm")) Helper.Print("Rewards claimed (Campaign)");
            Wait(2, 3);
            Device.Tap(0x13, 0x13);
            Wait(2, 3);
            Device.Tap(0x13, 0x13);
            Wait(2, 3);

            Current.Set("campaign");

            return true;
        }
    }

    partial class Routine
    { 
        public static bool AllianceTasks()
        {
            OpenCity();
            
            if (!Device.Tap(0x202, 0x1c5, "button.alliance"))
            {
                Wait(2, 3);
                Device.Tap(0x268, 0x1c5);
                Wait(2, 3);
                if (!Device.Tap(0x202, 0x1c5, "button.alliance")) return false;
            }

            Wait(2, 3);

            if (Device.Match(0x137, 0x109, "icon.war", out Rectangle war)) // check if account is a member of an alliance
            {

                // HELP ALLIANCE MEMBERS

                Device.Tap(0x1fa, 0x108); // help icon in alliance menu

                Wait(2, 3);
                if (Device.Tap(0x13e, 0x184, "button.help")) // button help
                {
                    Helper.Print("Allies helped");
                }
                else
                {
                    Device.Tap(0x22e, 0x57); // close button of help menu
                }

                // COLLECT TERRITORY RESOURCES

                if (Current.Get("territory").TotalMinutes > 30)
                {
                    Wait(2, 3);
                    Device.Tap(0x1b8, 0x109); // territory icon in alliance menu
                    Wait(2, 3);
                    Device.Tap(0x1f3, 0x7f); // claim button in territory menu
                    Wait(2, 3);
                    Device.Tap(0x22e, 0x51); // close button in territory menu
                    Current.Set("territory");
                }

                // COLLECT GIFTS

                if (Current.Get("gifts").TotalHours > 1)
                {
                    bool hasGift = false;

                    Wait(2, 3);
                    Device.Tap(0x1b9, 0x156); // gift icon in alliance menu
                    Wait(2, 3);
                    Device.Tap(0x153, 0xa3); // normal tab in gift menu
                    Wait(2, 3);
                    Device.Tap(0x22a, 0xa0); // claim all icon
                    Wait(2, 3);
                    hasGift |= Device.Tap(0x143, 0x143, "button.confirm");
                    Wait(2, 3);
                    Device.Tap(0x1d3, 0xa3); // rare tab in gift menu

                    Wait(2, 3);
                    if (Device.Tap(0x1e7, 0xc9, "button.claim.gift"))
                    {
                        hasGift = true;

                        do
                        {
                            Wait(2, 3);
                        }
                        while (Device.Tap(0x1e7, 0xfa, "button.claim.gift"));
                    }

                    Wait(2, 3);
                    Device.Tap(0xb2, 0x10c); // collect big chest
                    Wait(2, 3);
                    Device.Tap(0xb2, 0x10c); // collect big chest

                    Wait(2, 3);
                    Device.Tap(0x22d, 0x51); // close button of gift menu

                    if (hasGift) Helper.Print("Gifts claimed");
                    Current.Set("gifts");
                }

                // DONATE                

                if (Current.Get("donate").TotalMinutes > 20)
                {
                    Wait(2, 3);
                    Device.Tap(0x17a, 0x151); // technology icon in alliance menu
                    Wait(10, 11);

                    if (Device.Match("label.recommendation", out Rectangle recommendation))
                    {
                        Device.Tap(recommendation.X, recommendation.Y + 30);
                        bool donationMade = false;
                        Wait(2, 3);

                        while (Device.Tap(0x1e6, 0x14f, "button.donate"))
                        {
                            donationMade = true;
                            Wait(1, 2);
                        }

                        Wait(2, 3);
                        Device.Tap(0x221, 0x65); // close button of donate menu
                        Wait(2, 3);
                        Device.Tap(0x22d, 0x5f); // close button of technology menu

                        if (donationMade)
                        {
                            Helper.Print("Donated");
                            Current.Set("donate");
                        }
                    }
                    else
                    {
                        Device.Tap(0x22d, 0x5f); // close button of technology menu
                    }
                }
            }

            Wait(2, 3);
            Device.Tap(0x22c, 0x52); // close button of alliance menu

            return true;
        }        
    }

    partial class Routine
    {
        public static bool ClaimQuests()
        {
            if (Current.Get("quests").TotalHours < 1) return false;

            OpenCity();            

            Wait(2, 3);
            if (Device.Tap(0x16, 0x91, "button.quest")) // open quest menu
            {
                bool questClaimed = false;

                Wait(2, 3);;
                Device.Tap(0x37, 0x97); // quest tab

                Wait(2, 3);

                while (Device.Tap(0x1f5, 0xb0, "button.claim"))
                {
                    questClaimed = true;
                    Wait(2, 3); // claim main quests
                }

                while (Device.Tap(0x1f5, 0x102, "button.claim"))
                {
                    questClaimed = true;
                    Wait(2, 3); // claim side quests
                }

                Wait(2, 3);
                Device.Tap(0x38, 0xda); // daily tab

                Wait(2, 3);
                while (Device.Tap(0x1f8, 0xe7, "button.claim"))
                {
                    questClaimed = true;
                    Wait(2, 3); // claim daily objectives
                }

                for (int i = 0; i < 5; i++)
                {
                    Device.Tap(0xb2 + i * 83, 0x9e); // click chests
                    Wait(2, 3);

                    if (i == 4)
                    {
                        Device.Tap(0xb2 + i * 83, 0x9e); // click chests
                        Wait(2, 3);
                    }
                }

                Device.Tap(0x222, 0x67); // close button of quest menu

                if (questClaimed) Helper.Print("Quests claimed");

                Current.Set("quests");
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

            Device.Tap(0x265, 0x193); // open mails
            Wait(2, 3);

            if (!Device.Match(0x226, 0x15, "label.mail", out Rectangle mail)) return false;

            /*
            Wait(2, 3);
            Click(.08, .11); // personal tab
            Wait(2, 3);
            Click(.11, .93); // claim all
            Wait(2, 3);
            claimed |= (Click(.5, .75, "button.confirm"));
            */

            Wait(2, 3);
            Device.Tap(0x80, 0x14); // report tab
            Wait(2, 3);

            while (Device.Tap("icon.view"))
            {
                Wait(4, 5);
                Device.Tap(0x141, 0xe8);
                
                Wait(2, 3);

                if (Device.Match(0x1c5, 0x129, "button.investigate", out Rectangle investigate, 0.95f))
                {
                    Device.Tap(investigate);
                    Wait(2, 3);

                    if (!Device.Match(0x26a, 0x79, "icon.investigate", out investigate, 0.8f))
                    {
                        Device.Tap(0x1f6, 0x91, "button.send");
                        Wait(1, 2);
                    }
                    else
                    {
                        if (!Device.Match(0x26a, 0xb8, "icon.investigate", out investigate, 0.8f))
                        {
                            Device.Tap(0x1f6, 0xd0, "button.send");
                            Wait(1, 2);
                        }
                        else
                        {
                            if (!Device.Match(0x26a, 0xfa, "icon.investigate", out investigate, 0.8f))
                            {
                                Device.Tap(0x1f6, 0x110, "button.send");
                                Wait(1, 2);
                            }  
                            else
                            {
                                Device.Tap(0x141, 0xe8);
                                Wait(1, 2);
                            }
                        }
                    }

                    OpenCity();

                    Device.Tap(0x265, 0x193); // open mails
                    Wait(2, 3);

                    break;
                }
                else
                {
                    Device.Tap(0x141, 0xe8);
                    Wait(1, 2);
                }

                OpenCity();

                Device.Tap(0x265, 0x193); // open mails
                Wait(2, 3);
            }

            Device.Tap(0x47, 0x1c5); // claim all
            Wait(2, 3);
            claimed |= (Device.Tap(0x141, 0x144, "button.confirm"));

            

            Wait(2, 3);
            Device.Tap(0xcc, 0x14); // alliance tab
            Wait(2, 3);
            Device.Tap(0x47, 0x1c5); // claim all
            Wait(2, 3);
            claimed |= (Device.Tap(0x141, 0x144, "button.confirm"));

            Wait(2, 3);
            Device.Tap(0x11d, 0x14); // system tab
            Wait(2, 3);
            Device.Tap(0x47, 0x1c5); // claim all
            Wait(2, 3);
            claimed |= (Device.Tap(0x141, 0x144, "button.confirm"));

            Wait(2, 3);
            Device.Tap(0x268, 0x16); // close

            if (claimed) Helper.Print("Mails claimed");

            return true;
        }
    }

    partial class Routine
    {
        public static bool ClaimVIP()
        {
            if (Current.Get("vip").TotalHours < 12) return false;

            OpenRandom();

            Device.Tap(0x4b, 0x1e, 0); // open VIP menu
            Wait(2, 3);
            Device.Tap(0x1d5, 0x102); // claim chest
            Wait(2, 3);
            Device.Tap(0x1d5, 0x102); // claim chest
            Wait(2, 3);
            Device.Tap(0x1f3, 0x91); // claim daily
            Wait(2, 3);
            Device.Tap(0x1f3, 0x91); // claim daily
            Wait(2, 3);
            Device.Tap(0x221, 0x65); // close
            Wait(2, 3);

            Current.Set("vip");

            return true;
        }
    }

    partial class Routine
    {
        public static bool UpgradeCity()
        {
            OpenCity();

            Device.Tap(0x197, 0x2e);
            Wait(1, 1);            

            try
            {
                if (Device.Match("icon.speedup", out Rectangle speedup)) return false;

                Device.Tap(0x1fc, 0x143);
                Wait(2, 3);

                if (!Device.Tap(0x1eb, 0x14a, "button.upgrade"))
                {
                    while (Device.Match("button.go", out Rectangle go, null, 0.95f))
                    {
                        Device.Tap(go);                        
                        Wait(2, 3);
                        if (!Device.Tap("icon.upgrade")) return false;
                        Wait(2, 3);
                    }

                    Wait(2, 3);
                    if (!Device.Tap(0x1eb, 0x14a, "button.upgrade"))
                    {
                        Device.Tap(0x21f, 0x64); // close
                        return false;
                    }

                    Wait(2, 3);

                    if (Device.Match(0x13d, 0x66, "label.busy", out Rectangle busy))
                    {
                        Device.Tap(0x222, 0x65); // close
                        Wait(1, 2);
                        Device.Tap(0x21f, 0x64); // close
                        return false;
                    }
                    
                    if (Device.Match("button.purchase", out Rectangle purchase) || Device.Match("button.getmore", out Rectangle getmore))
                    {
                        Device.Tap(0x1f6, 0x82); // close
                        Wait(1, 2);
                        Device.Tap(0x21f, 0x64); // close
                        return false;
                    }

                    Wait(1, 2);
                    Device.Tap(0x141, 0xdd);

                    Helper.Print("Build");
                    return true;
                }
                else
                {
                    Wait(2, 3);
                    if (Device.Match("button.purchase", out Rectangle purchase) || Device.Match("button.getmore", out Rectangle getmore))
                    {
                        Device.Tap(0x1f6, 0x82); // close
                        Wait(1, 2);
                        Device.Tap(0x21f, 0x64); // close
                        return false;
                    }

                    Wait(1, 2);
                    Device.Tap(0x141, 0xdd);

                    Helper.Print("Upgrade main building");
                    return true;
                }
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
        public static bool DefeatBabarians()
        {
            OpenMap();

            Rectangle? search = null;

            bool first = true;

            while (true)
            {
                if (!Device.Tap(0x1c, 0x186, "button.search")) // open gathering menu
                    return false;

                Wait(1, 2);

                if (first)
                {                    
                    Device.Tap(0x81, 0x1b4); // click babarians icon in menu
                    Wait(1, 2);
                }

                if (!Device.Match("label.search", out Rectangle match, search))
                    return false;

                search = match;

                if (first)
                {
                    Device.Tap(0xbb, 0x141); // max level;                
                    Wait(1, 1);

                    for (int i = 0; i > Current.BarbarianLevel; i--)
                    {
                        Device.Tap(0x45, 0x141); // minus
                        Wait(1, 1);
                    }
                    
                    Device.Tap((Rectangle)search);
                    Wait(1, 1);

                    first = false;
                }
                
                while (Device.Match("label.search", out match, search))
                {
                    if (Device.Match(0x54, 0x141, "icon.min", out Rectangle min, 0.95f))
                    {
                        Device.Tap(0x13f, 0xf1); // close
                        return false;
                    }

                    Device.Tap(0x45, 0x141); // minus
                    Current.BarbarianLevel--;
                    Wait(1, 1);
                    Device.Tap((Rectangle)search); // click search
                    Wait(1, 1);                    
                }

                Wait(1, 2);
                Device.Tap(0x13f, 0xf1); // click babarians in map

                Wait(0, 1);

                if (!Device.Match("button.attack", out Rectangle attack)) continue;

                while (Device.Match("button.attack", out attack) && !Device.Tap(attack.X + attack.Width / 2, attack.Y + attack.Height / 2, "button.attack")) Wait(0, 1);

                Wait(1, 2);

                if (Device.Tap(0x1f5, 0x83, "action.troop"))
                {
                    Wait(2, 3);

                    Device.Match(0x1ca, 0x16e, "action.march", out Rectangle org);
                    Rectangle offset;

                    Device.Tap(0x99, 0xef); // primary commander
                    Wait(1, 2);
                    Device.Tap(0x23, 0x9d);
                    Wait(1, 2);

                    Device.Match(0x1ca, 0x16e, "action.march", out offset);

                    if (offset.X != org.X)
                    {
                        Device.Tap(0xcb, 0xef); // close
                        Wait(1, 2);
                    }

                    Device.Tap(0xfc, 0xf7); // secondary commander
                    Wait(1, 2);
                    Device.Tap(0x23, 0x9d);
                    Wait(1, 2);

                    Device.Match(0x1ca, 0x16e, "action.march", out offset);

                    if (offset.X != org.X)
                    {
                        Device.Tap(0x138, 0xf7); // close
                        Wait(1, 2);
                    }

                    if (Device.Tap(0x153, 0x176, "button.max", 0.98f))
                    {
                        Wait(1, 2);
                    }

                    if (!Device.Match(0x1cb, 0x15f, "label.cake", out Rectangle cake, 0.95f))
                    {
                        Device.Tap(0x22c, 0x53); // close
                        Wait(1, 2);
                        Current.BarbarianLevel++;
                        continue;
                    }

                    if (Device.Tap(0x1ca, 0x16e, "action.march"))
                    {
                        Wait(1, 1);

                        if (!Device.Match(0x1ca, 0x16e, "action.march", out match))
                        {
                            Helper.Print("Fight babarians");
                            Current.BarbarianLevel++;
                            return true;
                        }
                        else
                        {
                            Device.Tap(0x22c, 0x53); // close
                        }
                    }
                    else
                    {
                        Device.Tap(0x22c, 0x53); // close
                    }
                }
                else
                {
                    Device.Tap(0x13f, 0xf1);
                }

                return false;
            }
        }
    }

    partial class Routine
    {
        public static bool Build() => BuildOnce() && BuildOnce();

        public static bool BuildOnce()
        {
            OpenCity();

            Device.Tap(0x155, 0x4a); // click building

            try
            {
                Wait(1, 2);
                if (!Device.Tap("icon.build"))
                    return false;
                
                Wait(2, 3);
                if (!Device.Tap("button.build", .95f))
                {
                    Device.Tap(0x221, 0x67); // close
                    return false;
                }

                Wait(2, 3);                

                if (!Device.Tap("icon.upgrade"))
                    return false;

                Wait(2, 3);
                if (!Device.Tap(0x1eb, 0x14a, "button.upgrade"))
                {
                    Device.Tap(0x21f, 0x64); // close
                    return false;
                }

                Wait(2, 3);
                if (Device.Match("button.purchase", out Rectangle purchase) || Device.Match("button.getmore", out Rectangle getmore))
                {
                    Device.Tap(0x1f6, 0x82); // close
                    Wait(1, 2);
                    Device.Tap(0x21f, 0x64); // close
                    return false;
                }

                Wait(1, 2);
                Device.Tap(0x141, 0xdd);                

                Helper.Print("Build");
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
        private class AccountData
        {
            public int Index { get; set; }
            public Point Position { get; set; }
            public Queue<string> GatheringOrder { get; private set; } = new Queue<string>(new string[] { "F", "W", "S", "G" });
            public int BarbarianLevel = 0;

            private Dictionary<string, DateTime> _Actions = new Dictionary<string, DateTime>();

            public TimeSpan Get(string action)
            {
                return DateTime.UtcNow - (_Actions.ContainsKey(action) ? _Actions[action] : DateTime.MinValue);
            }

            public void Set(string action)
            {
                _Actions[action] = DateTime.UtcNow;
            }
        }

        private static Queue<AccountData> Accounts = new Queue<AccountData>();
        private static AccountData Current = null;

        public static void Initialise()
        {
            if (Current == null)
            {
                while (!Ready) Wait(1, 2);

                Helper.Print("Detecting characters");

                Device.Tap(0x15, 0x15); // open profile
                Wait(2, 3);
                Device.Tap(0x1eb, 0x143); // open settings
                Wait(2, 3);
                Device.Tap(0x92, 0xf9); // open character management
                Wait(10, 15);

                int x = 0x11c;
                int y = 0xae;

                Accounts.Clear();

                int index = 0;

                while (Device.Match(x, y, "bg.account", out Rectangle match))
                {
                    Accounts.Enqueue(new AccountData
                    {
                        Index = ++index,
                        Position = new Point { X = x, Y = y }
                    });

                    if (x == 0x20d)
                    {
                        x = 0x11c;
                        y += 67;
                    }
                    else
                    {
                        x = 0x20d;
                    }
                }

                Helper.Print("Detected " + Accounts.Count + " characters");

                while (Current == null)
                {
                    AccountData test = Accounts.Dequeue();

                    Device.Tap(test.Position.X, test.Position.Y);
                    Wait(2, 3);

                    if (!Device.Match(0x197, 0x13c, "button.yes", out Rectangle yes))
                    {
                        Current = test;
                    }
                    else
                    {
                        Device.Tap(0xef, 0x13c);
                        Wait(1, 2);
                    }

                    Accounts.Enqueue(test);
                }

                Helper.Print("Current character: #" + Current.Index);

                Wait(2, 3);
                Device.Tap(0x221, 0x65); // close account management
                Wait(2, 3);
                Device.Tap(0x22d, 0x51); // close settings
                Wait(2, 3);
                Device.Tap(0x21d, 0x65); // close profile
            }
            else
            {
                Helper.Print("Initialized, resume with character #" + Current.Index);
            }
        }

        public static bool SwitchCharacter()
        {            
            if (Accounts.Count > 1)
            {
                OpenRandom();

                Device.Tap(0x15, 0x15); // open profile
                Wait(2, 3);
                Device.Tap(0x1eb, 0x143); // open settings
                Wait(2, 3);
                Device.Tap(0x92, 0xf9); // open character management
                Wait(10, 15);

                int count = Accounts.Count;

                while (count-- > 0)
                {
                    AccountData test = Accounts.Peek();
                    Device.Tap(test.Position.X, test.Position.Y);
                    Wait(2, 3);

                    Accounts.Enqueue(Accounts.Dequeue());

                    if (Device.Tap(0x197, 0x13c, "button.yes"))
                    {
                        Current = test;
                        Helper.Print("Switched to character #" + test.Index);
                        Wait(20, 25);

                        while (!Ready) Wait(2, 3);                        

                        return true;
                    }                    
                }

                Wait(2, 3);
                Device.Tap(0x221, 0x65); // close account management
                Wait(2, 3);
                Device.Tap(0x22d, 0x51); // close settings
                Wait(2, 3);
                Device.Tap(0x21d, 0x65); // close profile
            }

            return false;
        }
    }
}
