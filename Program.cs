using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;

namespace Troller
{
    class Program
    {
        public static Obj_AI_Base Player = ObjectManager.Player;
        public static List<string> Messages;
        public static List<string> Starts;
        public static List<string> Endings;
        public static List<string> Smileys;
        public static List<string> Greetings;
        public static Dictionary<GameEventId, int> Rewards;
        public static Random rand = new Random();

        public static Menu Settings;

        public static int kills = 0;
        public static int deaths = 0;
        public static float congratsTime = 0;
        public static float lastMessage = 0;
        static void Main(string[] args)
        {
            setupMenu();
            setupMessages();
            setupRewards();

            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            CustomEvents.Game.OnGameEnd += Game_OnGameEnd;
            Game.OnStart += Game_OnGameStart;
            Game.OnNotify += Game_OnGameNotifyEvent;
            Game.OnUpdate += Game_OnGameUpdate;
        }

        static void setupMenu()
        {
            Settings = new Menu("Troller", "Troller", true);
            Settings.AddItem(new MenuItem("sayGreeting", "Say Greeting").SetValue(true));
            Settings.AddItem(new MenuItem("sayGreetingAllChat", "Say Greeting In All Chat").SetValue(true));
            Settings.AddItem(new MenuItem("sayGreetingDelayMin", "Min Greeting Delay").SetValue(new Slider(30, 1, 150)));
            Settings.AddItem(new MenuItem("sayGreetingDelayMax", "Max Greeting Delay").SetValue(new Slider(90, 1, 150)));
            Settings.AddItem(new MenuItem("sayCongratulate", "Congratulate players").SetValue(true));
            Settings.AddItem(new MenuItem("sayCongratulateDelayMin", "Min Congratulate Delay").SetValue(new Slider(5, 1, 30)));
            Settings.AddItem(new MenuItem("sayCongratulateDelayMax", "Max Congratulate Delay").SetValue(new Slider(15, 1, 30)));
            Settings.AddItem(new MenuItem("sayCongratulateInterval", "Minimum Interval between messages").SetValue(new Slider(30, 1, 600)));
            Settings.AddItem(new MenuItem("surrender", "Auto Surrender On/Off").SetValue(true));
            Settings.AddToMainMenu();
        }

        static void setupRewards()
        {
            Rewards = new Dictionary<GameEventId, int>
                          {
                              { GameEventId.OnChampionDie, 1 }, // Champion Kill                
                              { GameEventId.OnTurretDamage, 1 }, // Turret Kill
                          };
        }

        static void setupMessages()
        {
            Messages = new List<string>
                           {
                               "you suck", "bad job", "bj", "noob lol", "haha so bad", "SO SO BAD", "rofl you're baaaaaad"
                           };
            Starts = new List<string>
                           {
                             "oh", "lel", "ololo"
                           };
            Endings = new List<string>
                           {
                              " m8", " noob", " fcker", " piece of sht", " jew"
                           };
            Smileys = new List<string>
                           {
                              " xD", " >:(", " >:)", " ;x", " o.O", " u.U", " :o", " :P"
                           };
            Greetings = new List<string>
                           {
                                "have fun kids", "feed hard or die trying!", "noobs gl", "you will all burn in hell",
                                "AHAHAHAHAHA wat?",
                           };
        }

        static string getRandomElement(List<string> collection, bool firstEmpty = true)
        {
            if (firstEmpty && rand.Next(3) == 0) return collection[0];
            return collection[rand.Next(collection.Count)];
        }

        static string generateMessage()
        {
            string message = getRandomElement(Starts);
            message += getRandomElement(Messages, false);
            message += getRandomElement(Endings);
            message += getRandomElement(Smileys);
            return message;
        }

        static string generateGreeting()
        {
            string greeting = getRandomElement(Greetings, false);
            greeting += getRandomElement(Smileys);
            return greeting;
        }

        static void sayCongratulations()
        {
            if (Settings.Item("sayCongratulate").GetValue<bool>()
                && Game.ClockTime > lastMessage + Settings.Item("sayCongratulateInterval").GetValue<Slider>().Value)
            {
                lastMessage = Game.ClockTime;
                Game.Say(generateMessage());
            }
        }

        static void sayGreeting()
        {
            if (Settings.Item("sayGreetingAllChat").GetValue<bool>())
            {
                Game.Say("/all " + generateGreeting());
            }
            else
            {
                Game.Say(generateGreeting());
            }
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Game.PrintChat("<font color = \"#ffbf00\">Troller (By: TulakHord)</font>");
        }

        static void Game_OnGameStart(EventArgs arg)
        {
            if (!Settings.Item("sayGreeting").GetValue<bool>())
            {
                return;
            }
            int minDelay = Settings.Item("sayGreetingDelayMin").GetValue<Slider>().Value;
            int maxDelay = Settings.Item("sayGreetingDelayMax").GetValue<Slider>().Value;

            // Greeting Message
            Utility.DelayAction.Add(rand.Next(Math.Min(minDelay, maxDelay), Math.Max(minDelay, maxDelay)) * 1000, sayGreeting);
        }

        static void Game_OnGameEnd(EventArgs args)
        {
            Utility.DelayAction.Add((new Random(Environment.TickCount).Next(100, 1001)), () => Game.Say("/all bg you suck"));
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            // Champion kill message
            if (kills > deaths && congratsTime < Game.ClockTime && congratsTime != 0)
            {
                sayCongratulations();

                kills = 0;
                deaths = 0;
                congratsTime = 0;
            }
            else if (kills != deaths && congratsTime < Game.ClockTime)
            {
                kills = 0;
                deaths = 0;
                congratsTime = 0;
            }
        }

        static void Game_OnGameNotifyEvent(GameNotifyEventArgs args)
        {
            if (string.Equals(args.EventId.ToString(), "OnSurrenderVoteStart")
                && Settings.Item("surrender").GetValue<bool>() || args.EventId == GameEventId.OnSurrenderVoteStart)
            {
                Game.Say("/ff");
            }
            if (Rewards.ContainsKey(args.EventId))
            {
                Obj_AI_Base Killer = ObjectManager.GetUnitByNetworkId<Obj_AI_Base>((int)args.NetworkId);

                if (Killer.IsAlly)
                {
                    // We will not swear at ourself :)
                    if ((kills == 0 && !Killer.IsMe) || kills > 0)
                    {
                        kills += Rewards[args.EventId];
                    }
                }
                else
                {
                    deaths += Rewards[args.EventId];
                }
            }
            else
            {
                return;
            }
            int minDelay = Settings.Item("sayCongratulateMinDelay").GetValue<Slider>().Value;
            int maxDelay = Settings.Item("sayCongratulateMaxDelay").GetValue<Slider>().Value;

            congratsTime = Game.ClockTime + rand.Next(Math.Min(minDelay, maxDelay), Math.Max(minDelay, maxDelay));

        } 

    }
}
