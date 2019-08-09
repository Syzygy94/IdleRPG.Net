using Meebey.SmartIrc4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IdleRPG.NET {
    public class World {
        public static readonly string[] Items = { "ring", "amulet", "charm", "weapon", "helm", "tunic", "pair of gloves", "set of leggings", "shield", "pair of boots" };
        public static readonly string[] Penalties = { "quit", "nick", "msg", "part", "kick", "logout", "quest" };

        public Dictionary<string, List<Item>> MapItems { get; private set; }
        public List<Player> Players { get; private set; }
        public DateTime LastTime { get; private set; }
        public Hashtable Quest { get; private set; }
        public List<Event> AllEvents { get; private set; }
        public Tournament Tournament { get; private set; }
        public bool Running { get; private set; }

        private IrcClient IrcClient;
        private int RPReport;
        private int OldRPReport;

        public World(IrcClient ircClient) {
            MapItems = new Dictionary<string, List<Item>>();
            LastTime = DateTime.MinValue;
            Players = new List<Player>();
            Quest = new Hashtable()
            {
                {"players", new List<Player>() },
                {"pos1", new Pos(0,0) },
                {"pos2", new Pos(0,0) },
                {"questTime", DateTime.Now.AddSeconds(Random.Next(21600)) },
                {"text", string.Empty },
                {"type", 1 },
                {"stage", 1 }
            };
            AllEvents = Utilities.LoadEvents();
            Tournament = new Tournament();
            IrcClient = ircClient;
            Running = true;
            RPReport = 0;
            OldRPReport = 0;
        }

        public void Start() {
            LastTime = DateTime.Now;
        }

        public void AutoLogin() {
            if (Config.AutoLogin) {
                var channel = IrcClient.GetChannel(Config.ChannelName);
                if (channel != null) {
                    if (channel.Users.Count > 0) {
                        foreach (ChannelUser user in channel.Users.Values) {
                            Player p = user.Nick == Config.PrimNick ? null : Players.FirstOrDefault(player => player.UHost == user.Host);
                            if (p != null) {
                                IrcClient.Voice(Config.ChannelName, user.Nick);
                                Players[Players.IndexOf(p)].Online = true;
                                Players[Players.IndexOf(p)].Nick = user.Nick;
                                Players[Players.IndexOf(p)].LastLogin = DateTime.Now;
                                ChanMsg($"{p.Name}, the level {p.Level} {p.Class}, is now online from nickname {user.Nick}. " +
                                        $"Next level in {Duration(p.TTL)}.");
                                Notice(user.Nick, $"Logon successful. Next level in {Duration(p.TTL)}.");
                            }
                        }
                    }
                }
            }
        }

        public void RPCheck() {
            List<Player> online = Players.Where(p => p.Online).ToList();
            if (online is null || online.Count == 0)
                return;

            List<Player> onlineEvil = online.Where(p => p.Align == "e").ToList();
            List<Player> onlineGood = online.Where(p => p.Align == "g").ToList();

            if (Random.Next((20 * 86400) / Config.Tick) < online.Count)
                Hog(online);
            if (Random.Next((24 * 86400) / Config.Tick) < online.Count)
                TeamBattle(online);
            if (Random.Next((8 * 86400) / Config.Tick) < online.Count)
                Calamity(online);
            if (Random.Next((4 * 86400) / Config.Tick) < online.Count)
                GodSend(online);
            if (Random.Next((8 * 86400) / Config.Tick) < onlineEvil.Count)
                Evilness(onlineEvil, onlineGood);
            if (Random.Next((12 * 86400) / Config.Tick) < onlineGood.Count)
                Goodness(onlineGood);
            if (Random.Next((10 * 86400) / Config.Tick) < online.Count)
                War(online);

            MovePlayers(online);
            ProcessItems();

            if ((RPReport % 120) < (OldRPReport % 120)) {
                // Save Quest
            }

            if (DateTime.Now > (DateTime)Quest["questTime"]) {
                if (((List<Player>)Quest["players"]).Count == 0)
                    CreateQuest(online);
                else if ((int)Quest["type"] == 1) {
                    List<Player> questers = (List<Player>)Quest["players"];
                    ChanMsg($"{string.Join(", ", questers.Select(p => p.Name).ToArray(), 0, 3)}, and {questers[3].Name} have " +
                        $"blessed the realm by completing their quest! 25% of their burden is elminated.");
                    foreach (Player p in questers)
                        Players[Players.IndexOf(p)].TTL = (int)(Players[Players.IndexOf(p)].TTL * .75);
                    Quest["players"] = new List<Player>();
                    Quest["questTime"] = DateTime.Now.AddSeconds(21600);
                }
            }

            if (DateTime.Now > Tournament.TournamentTime) {
                if (Tournament.Players == null || Tournament.Players.Count == 0)
                    CreateTournament(online);
                else
                    TournamentBattle();
            }

            if ((RPReport % 36000) < (OldRPReport % 36000)) {
                List<Player> players = Players.OrderByDescending(p => p.Level).ThenBy(p => p.TTL).Take(5).ToList();
                if (players != null && players.Count > 0) {
                    ChanMsg("IdleRPG Top 5 Players:");
                    foreach (Player p in players)
                        ChanMsg($"{p.Name}, the level {p.Level} {p.Class}, is #{players.IndexOf(p) + 1}! Next level in {Duration(p.TTL)}.");
                }
            }

            if ((RPReport % 3600) < (OldRPReport % 3600)) {
                List<Player> players = online.Where(p => p.Level >= 45).ToList();
                if (players != null && (players.Count / (online.Count * 1.0) > .15))
                    ChallengeOpp(players[Random.Next(players.Count)]);
            }

            if (LastTime.Equals(DateTime.MinValue) == false) {
                DateTime currTime = DateTime.Now;
                var channel = IrcClient.GetChannel(Config.ChannelName);
                if (channel != null) {
                    foreach (Player p in Players) {
                        if (p.Online && channel.Users.ContainsKey(p.Nick)) {
                            Players[Players.IndexOf(p)].TTL -= (int)(currTime - LastTime).TotalSeconds;
                            Players[Players.IndexOf(p)].IdleTime += (int)(currTime - LastTime).TotalSeconds;
                            if (Players[Players.IndexOf(p)].TTL <= 0)
                                LevelUp(Players[Players.IndexOf(p)]);
                        }
                    }
                }
                RPReport = RPReport + Config.Tick > int.MaxValue ? 0 : RPReport;
                OldRPReport = RPReport;
                RPReport += (int)(currTime - LastTime).TotalSeconds;
                LastTime = currTime;
            }
        }

        public void FindItem(Player p) {
            Item newItem = new Item() { ItemType = Items[Random.Next(Items.Length)], Level = 1, Tag = string.Empty };
            int level = 1;
            for (int i = 1; i < (int)(p.Level * 1.5); i++) {
                if (Random.Next((int)Math.Pow(1.4, i / 4)) < 1)
                    newItem.Level = i;
            }

            if (p.Level >= 25 && Random.Next(40) < 1) {
                level = 50 + Random.Next(25);
                if (level >= newItem.Level && level > p.Items["helm"].Level) {
                    ExchangeItem(p, new Item() { ItemType = "helm", Level = level, Tag = "a" });
                    return;
                }
            } else if (p.Level >= 25 && Random.Next(40) < 1) {
                level = 50 + Random.Next(25);
                if (level >= newItem.Level && level > p.Items["ring"].Level) {
                    ExchangeItem(p, new Item() { ItemType = "ring", Level = level, Tag = "h" });
                    return;
                }
            } else if (p.Level >= 30 && Random.Next(40) < 1) {
                level = 75 + Random.Next(25);
                if (level >= newItem.Level && level > p.Items["tunic"].Level) {
                    ExchangeItem(p, new Item() { ItemType = "tunic", Level = level, Tag = "b" });
                    return;
                }
            } else if (p.Level >= 35 && Random.Next(40) < 1) {
                level = 100 + Random.Next(25);
                if (level >= newItem.Level && level > p.Items["amulet"].Level) {
                    ExchangeItem(p, new Item() { ItemType = "amulet", Level = level, Tag = "c" });
                    return;
                }
            } else if (p.Level >= 40 && Random.Next(40) < 1) {
                level = 150 + Random.Next(25);
                if (level >= newItem.Level && level > p.Items["weapon"].Level) {
                    ExchangeItem(p, new Item() { ItemType = "weapon", Level = level, Tag = "d" });
                    return;
                }
            } else if (p.Level >= 45 && Random.Next(40) < 1) {
                level = 175 + Random.Next(25);
                if (level >= newItem.Level && level > p.Items["weapon"].Level) {
                    ExchangeItem(p, new Item() { ItemType = "weapon", Level = level, Tag = "e" });
                    return;
                }
            } else if (p.Level >= 48 && Random.Next(40) < 1) {
                level = 250 + Random.Next(50);
                if (level >= newItem.Level && level > p.Items["pair of boots"].Level) {
                    ExchangeItem(p, new Item() { ItemType = "pair of boots", Level = level, Tag = "f" });
                    return;
                }
            } else if (p.Level >= 52 && Random.Next(40) < 1) {
                level = 300 + Random.Next(50);
                if (level >= newItem.Level && level > p.Items["weapon"].Level) {
                    ExchangeItem(p, new Item() { ItemType = "weapon", Level = level, Tag = "g" });
                    return;
                }
            }

            if (newItem.Level > p.Items[newItem.ItemType].Level)
                ExchangeItem(p, newItem);
            else {
                Notice(p.Nick, $"You found a level {newItem.Level} {newItem.ItemType}. Your current {newItem.ItemType} is level " +
                    $"{p.Items[newItem.ItemType].Level}, so it seems luck is against you. You toss the {newItem.ItemType}.");
                DropItem(p.Pos, newItem);
            }
        }

        public void ChallengeOpp(Player p) {
            if (p.Level < 25 && Random.Next(4) != 0)
                return;

            List<Player> opps = Players.Where(x => !x.Equals(p)).ToList();
            if (opps is null || opps.Count == 0)
                return;

            Player opp = Random.Next(opps.Count) < 1 ? new Player() { Name = Config.PrimNick, Nick = Config.PrimNick } : Players[Players.IndexOf(opps[Random.Next(opps.Count)])];
            int playerSum = ItemSum(p, true);
            int oppSum = ItemSum(opp, true);
            int playerRoll = Random.Next(playerSum);
            int oppRoll = Random.Next(oppSum);

            if (playerRoll >= oppRoll) {
                int gain = opp.Name == Config.PrimNick ? 20 : opp.Level / 4;
                gain = gain < 7 ? 7 : gain;
                int ttl = (int)(p.TTL * (gain / 100.0));
                if (playerRoll < 51 && playerSum > 299 && oppSum > 299) {
                    ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                        $"knocked them out in a slapfight! {Duration(ttl)} is removed from {p.Name}'s clock.");
                } else if ((oppRoll + 300) < playerRoll && oppSum > 299) {
                    ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                        $"straight up stomped them in combat! {Duration(ttl)} is removed from {p.Name}'s clock.");
                    ChanMsg($"{opp.Name} cries.");
                } else {
                    int battleMsg = Random.Next(3);
                    switch (battleMsg) {
                        case 0:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"completely messed them up! {Duration(ttl)} is removed from {p.Name}'s clock.");
                            break;
                        case 1:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"rocked it! {Duration(ttl)} is removed from {p.Name}'s clock.");
                            break;
                        case 2:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"gave em what was coming! {Duration(ttl)} is removed from {p.Name}'s clock.");
                            break;
                    }
                }
                p.TTL -= ttl;
                ChanMsg($"{p.Name} reaches next level in {Duration(p.TTL)}.");
                int cs = p.Align == "g" ? 50 : p.Align == "e" ? 20 : 35;
                if (Random.Next(cs) < 1 && opp.Name != Config.PrimNick) {
                    ttl = (int)(5 + (Random.Next(20) / 100.0) * opp.TTL);
                    ChanMsg($"{p.Name} has dealt {opp.Name} a Critical Strike! {Duration(ttl)} is added to {opp.Name}'s clock.");
                    Players[Players.IndexOf(opp)].TTL += ttl;
                    ChanMsg($"{opp.Name} reaches next level in {Duration(Players[Players.IndexOf(opp)].TTL)}.");
                } else if (Random.Next(25) < 1 && opp.Name != Config.PrimNick && p.Level > 19) {
                    string itemType = Items[Random.Next(Items.Length)];
                    if (opp.Items[itemType].Level > p.Items[itemType].Level) {
                        ChanMsg($"In the fierce battle, {opp.Name} dropped their level {opp.Items[itemType].Level} {itemType}! " +
                            $"{p.Name} picks it up, tossing an old level {p.Items[itemType].Level} {itemType} to {opp.Name}.");
                        Item itemSwapped = p.Items[itemType];
                        p.Items[itemType] = opp.Items[itemType];
                        Players[Players.IndexOf(opp)].Items[itemType] = itemSwapped;
                    }
                }
            } else {
                int gain = opp.Name == Config.PrimNick ? 10 : opp.Level / 7;
                gain = gain < 7 ? 7 : gain;
                int ttl = (int)(p.TTL * (gain / 100.0));
                if (oppRoll < 51 && playerSum > 299 && oppSum > 299) {
                    ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] in " +
                        $"a drunken stoopor and was knocked out in a slapfight! {Duration(ttl)} is added to {p.Name}'s clock.");
                } else if ((playerRoll + 300) < oppRoll && playerSum > 299)
                    ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                        $"brought bronze weapons to an iron fight! {Duration(ttl)} is added to {p.Name}'s clock.");
                else {
                    int battleMsg = Random.Next(3);
                    switch (battleMsg) {
                        case 0:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"got flexed on in combat! {Duration(ttl)} is added to {p.Name}'s clock.");
                            break;
                        case 1:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"realized it was a bad decision! {Duration(ttl)} is added to {p.Name}'s clock.");
                            break;
                        case 2:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"didn't wake up till the next morning! {Duration(ttl)} is added to {p.Name}'s clock.");
                            break;
                    }
                }
                p.TTL += ttl;
                ChanMsg($"{p.Name} reaches next level in {Duration(p.TTL)}.");
            }
        }

        public void CollisionFight(Player p, Player opp) {
            int playerSum = ItemSum(p, true);
            int oppSum = ItemSum(opp, true);
            int playerRoll = Random.Next(playerSum);
            int oppRoll = Random.Next(oppSum);

            if (playerRoll >= oppRoll) {
                int gain = opp.Level / 4;
                gain = gain < 7 ? 7 : gain;
                int ttl = (int)(p.TTL * (gain / 100.0));
                if (playerRoll < 51 && playerSum > 299 && oppSum > 299) {
                    ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                        $"knocked them out in a slapfight! {Duration(ttl)} is removed from {p.Name}'s clock.");
                } else if ((oppRoll + 300) < playerRoll && oppSum > 299) {
                    ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                        $"straight up stomped them in combat! {Duration(ttl)} is removed from {p.Name}'s clock.");
                    ChanMsg($"{opp.Name} cries.");
                } else {
                    int battleMsg = Random.Next(3);
                    switch (battleMsg) {
                        case 0:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"completely messed them up! {Duration(ttl)} is removed from {p.Name}'s clock.");
                            break;
                        case 1:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"rocked it! {Duration(ttl)} is removed from {p.Name}'s clock.");
                            break;
                        case 2:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"gave em what was coming! {Duration(ttl)} is removed from {p.Name}'s clock.");
                            break;
                    }
                }
                p.TTL -= ttl;
                ChanMsg($"{p.Name} reaches next level in {Duration(p.TTL)}.");
                int cs = p.Align == "g" ? 50 : p.Align == "e" ? 20 : 35;
                if (Random.Next(cs) < 1 && opp.Name != Config.PrimNick) {
                    ttl = (int)(5 + (Random.Next(20) / 100.0) * opp.TTL);
                    ChanMsg($"{p.Name} has dealt {opp.Name} a Critical Strike! {Duration(ttl)} is added to {opp.Name}'s clock.");
                    Players[Players.IndexOf(opp)].TTL += ttl;
                    ChanMsg($"{opp.Name} reaches next level in {Duration(Players[Players.IndexOf(opp)].TTL)}.");
                } else if (Random.Next(25) < 1 && opp.Name != Config.PrimNick && p.Level > 19) {
                    string itemType = Items[Random.Next(Items.Length)];
                    if (opp.Items[itemType].Level > p.Items[itemType].Level) {
                        ChanMsg($"In the fierce battle, {opp.Name} dropped their level {opp.Items[itemType].Level} {itemType}! " +
                            $"{p.Name} picks it up, tossing an old level {p.Items[itemType].Level} {itemType} to {opp.Name}.");
                        Item itemSwapped = p.Items[itemType];
                        p.Items[itemType] = opp.Items[itemType];
                        Players[Players.IndexOf(opp)].Items[itemType] = itemSwapped;
                    }
                }
            } else {
                int gain = opp.Name == Config.PrimNick ? 10 : opp.Level / 7;
                gain = gain < 7 ? 7 : gain;
                int ttl = (int)(p.TTL * (gain / 100.0));
                if (oppRoll < 51 && playerSum > 299 && oppSum > 299) {
                    ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] in " +
                        $"a drunken stoopor and was knocked out in a slapfight! {Duration(ttl)} is added to {p.Name}'s clock.");
                } else if ((playerRoll + 300) < oppRoll && playerSum > 299)
                    ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                        $"brought bronze weapons to an iron fight! {Duration(ttl)} is added to {p.Name}'s clock.");
                else {
                    int battleMsg = Random.Next(3);
                    switch (battleMsg) {
                        case 0:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"got flexed on in combat! {Duration(ttl)} is added to {p.Name}'s clock.");
                            break;
                        case 1:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"realized it was a bad decision! {Duration(ttl)} is added to {p.Name}'s clock.");
                            break;
                        case 2:
                            ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                                $"didn't wake up till the next morning! {Duration(ttl)} is added to {p.Name}'s clock.");
                            break;
                    }
                }
                p.TTL += ttl;
                ChanMsg($"{p.Name} reaches next level in {Duration(p.TTL)}.");
            }
        }

        public void ProcessItems() {
            DateTime currTime = DateTime.Now;
            foreach (string pos in MapItems.Keys.ToList()) {
                foreach (Item item in MapItems[pos].ToList()) {
                    int ttl = Config.RPItemBase * item.Level;
                    if (item.Age.AddSeconds(ttl).TimeOfDay.TotalSeconds <= currTime.TimeOfDay.TotalSeconds) {
                        item.Age = item.Age.AddSeconds(ttl);
                        DowngradeItem(item);
                        if (item.Level == 0)
                            MapItems[pos].Remove(item);
                    }
                }

                if (MapItems[pos].Count == 0)
                    MapItems.Remove(pos);
            }
        }

        public void Hog(List<Player> online) {
            Player p = online[Random.Next(online.Count)];
            int ttl = (int)((5 + (Random.Next(71) / 100.0)) * p.TTL);
            if (Random.Next(5) == 1) {
                ChanMsg($"Verily I say unto thee, the Heavens have burst forth, and the blessed hand of God carried " +
                    $"{p.Name} {Duration(ttl)} toward level {p.Level + 1}");
                p.TTL -= ttl;
            } else {
                ChanMsg($"Thereupon He stretched out His little finger among them and consumed {p.Name} with fire, " +
                    $"slowing the heathen {Duration(ttl)} from level {p.Level + 1}.");
                p.TTL += ttl;
            }

            ChanMsg($"{p.Name} reaches next level in {Duration(p.TTL)}");
        }

        public void Goodness(List<Player> onlineGood) {
            if (onlineGood == null || onlineGood.Count == 0)
                return;

            if (onlineGood.Count >= 2) {
                List<Player> players = onlineGood.OrderBy(x => Random.Next()).Take(2).ToList();
                Player p1 = Players[Players.IndexOf(players[0])];
                Player p2 = Players[Players.IndexOf(players[1])];
                int gain = 5 + Random.Next(8);
                ChanMsg($"{p1.Name} and {p2.Name} have not let the iniquities of evil men poison them. Together have they prayed to " +
                    $"their god, and it is his light that now shines upon them. {gain}% of their time is removed from their clocks.");
                p1.TTL = (int)(p1.TTL * (1 - (gain / 100.0)));
                p2.TTL = (int)(p2.TTL * (1 - (gain / 100.0)));
                ChanMsg($"{p1.Name} reaches next level in {Duration(p1.TTL)}");
                ChanMsg($"{p2.Name} reaches next level in {Duration(p2.TTL)}");
            }
        }

        public void Evilness(List<Player> onlineEvil, List<Player> onlineGood) {
            if (onlineEvil == null || onlineEvil.Count == 0)
                return;

            Player player = Players[Players.IndexOf(onlineEvil[Random.Next(onlineEvil.Count)])];
            if (Random.Next(2) < 1 && (onlineGood != null && onlineGood.Count > 0)) {
                string itemType = Items[Random.Next(Items.Length)];
                Player targetPlayer = Players[Players.IndexOf(onlineGood[Random.Next(onlineGood.Count)])];
                if (targetPlayer.Items[itemType].Level > player.Items[itemType].Level) {
                    Item itemSwapped = player.Items[itemType];
                    player.Items[itemType] = targetPlayer.Items[itemType];
                    targetPlayer.Items[itemType] = itemSwapped;
                    ChanMsg($"{player.Name} stole {targetPlayer.Name}'s level {player.Items[itemType].Level} {itemType} while they were " +
                        $"sleeping! {player.Name} leaves an old level {targetPlayer.Items[itemType].Level} {itemType} behind, which " +
                        $"{targetPlayer.Name} then takes.");
                } else
                    Notice(player.Nick, $"You made to steal {targetPlayer.Name}'s {itemType}, but realized it was a lower level than your " +
                        $"own. You creep back into the shadows.");
            } else {
                int gain = 1 + Random.Next(5);
                ChanMsg($"{player.Name} is forsaken by their evil god. {Duration((int)(player.TTL * (gain / 100.0)))} is added to their " +
                    $"clock.");
                player.TTL = (int)(player.TTL * (1 + (gain / 100.0)));
                ChanMsg($"{player.Name} reaches next level in {Duration(player.TTL)}.");
            }
        }

        public void Calamity(List<Player> online) {
            if (online == null || online.Count == 0)
                return;

            Player player = Players[Players.IndexOf(online[Random.Next(online.Count)])];
            if (Random.Next(10) < 1) {
                string itemType = Items[Random.Next(Items.Length)];
                switch (itemType) {
                    case "amulet":
                        ChanMsg($"{player.Name} fell, chipping the stone in their amulet! {player.Name}'s amulet loses 10% of " +
                            $"it's effectiveness");
                        break;
                    case "charm":
                        ChanMsg($"{player.Name} slipped and dropped their charm in a dirty bog! {player.Name}'s charm loses 10% of " +
                            $"it's effectiveness");
                        break;
                    case "weapon":
                        ChanMsg($"{player.Name} left their weapon out in the rain to rust! {player.Name}'s weapon loses 10% of " +
                            $"it's effectiveness");
                        break;
                    case "tunic":
                        ChanMsg($"{player.Name} spilled a level 7 shrinking potion on their tunic! {player.Name}'s tunic loses 10% of " +
                            $"it's effectiveness");
                        break;
                    case "shield":
                        ChanMsg($"{player.Name}'s shield was damaged by a dragon's fiery breath! {player.Name}'s shield loses 10% of " +
                            $"it's effectiveness");
                        break;
                    case "set of leggings":
                        ChanMsg($"{player.Name} burned a hole through their leggings while ironing them! {player.Name}'s set of " +
                            $"leggings loses 10% of it's effectiveness");
                        break;
                    case "pair of gloves":
                        ChanMsg($"{player.Name} grabbed the lit end of their torch by mistake, and burned their pair of gloves! " +
                            $"{player.Name}'s pair of gloves loses 10% of it's effectiveness");
                        break;
                    case "ring":
                        ChanMsg($"{player.Name}'s ring slipped off their finger! {player.Name}'s ring loses 10% of " +
                            $"it's effectiveness");
                        break;
                    case "helm":
                        ChanMsg($"{player.Name} wasn't watching where they were going, and hit their head on a tree branch! " +
                            $"{player.Name}'s helm loses 10% of it's effectiveness");
                        break;
                    case "pair of boots":
                        ChanMsg($"{player.Name} stubbed their toe on a rock! {player.Name}'s pair of boots loses 10% of " +
                            $"it's effectiveness");
                        break;
                }
                Players[Players.IndexOf(player)].Items[itemType].Level = (int)(player.Items[itemType].Level * .9);
            } else {
                List<Event> events = AllEvents.Where(e => e.EventType == EventType.Calamity).ToList();
                string action = events[Random.Next(events.Count)].EventText;
                int ttl = (int)(((5 + (Random.Next(8))) / 100.0) * player.TTL);
                ChanMsg($"{player.Name} {action}. This terrible calamity has slowed them {Duration(ttl)} from level {player.Level + 1}.");
                Players[Players.IndexOf(player)].TTL += ttl;
                ChanMsg($"{player.Name} reaches next level in {Duration(player.TTL)}.");
            }
        }

        public void GodSend(List<Player> online) {
            if (online == null || online.Count == 0)
                return;

            Player player = Players[Players.IndexOf(online[Random.Next(online.Count)])];
            if (Random.Next(10) < 1) {
                string itemType = Items[Random.Next(Items.Length)];
                switch (itemType) {
                    case "amulet":
                        ChanMsg($"{player.Name}'s amulet was blessed by a passing cleric! {player.Name}'s amulet gains 10% effectiveness");
                        break;
                    case "charm":
                        ChanMsg($"{player.Name}'s charm ate a bolt of lightning! {player.Name}'s charm gains 10% effectiveness");
                        break;
                    case "weapon":
                        ChanMsg($"{player.Name} sharpened the edge of their weapon! {player.Name}'s weapon gains 10% effectiveness");
                        break;
                    case "tunic":
                        ChanMsg($"A magician cast a spell of Rigidity on {player.Name}'s tunic! {player.Name}'s tunic gains 10% effectiveness");
                        break;
                    case "shield":
                        ChanMsg($"{player.Name} reinforced their shield with a dragon's scales! {player.Name}'s shield gains 10% effectiveness");
                        break;
                    case "set of leggings":
                        ChanMsg($"The local wizard imbued {player.Name}'s pants with a Spirit of Fortitude! {player.Name}'s set of " +
                            $"leggings gains 10% effectiveness");
                        break;
                    case "pair of gloves":
                        ChanMsg($"{player.Name} had a seamstress repair all of the holes in their pair of gloves! {player.Name}'s pair " +
                            $"of gloves gains 10% effectiveness");
                        break;
                    case "ring":
                        ChanMsg($"{player.Name} polished their ring to a bright shine! {player.Name}'s ring gains 10% effectiveness");
                        break;
                    case "helm":
                        ChanMsg($"The local blacksmith fixed all of the dents in {player.Name}'s helm! {player.Name}'s helm gains 10% " +
                            $"effectiveness");
                        break;
                    case "pair of boots":
                        ChanMsg($"{player.Name} took their pair of boots to a cobbler and had new soles put on! {player.Name}'s pair of " +
                            $"boots gains 10% effectiveness");
                        break;
                }
                Players[Players.IndexOf(player)].Items[itemType].Level = (int)(player.Items[itemType].Level * 1.1);
            } else {
                List<Event> events = AllEvents.Where(e => e.EventType == EventType.Godsend).ToList();
                string action = events[Random.Next(events.Count)].EventText;
                int ttl = (int)(((5 + (Random.Next(8))) / 100.0) * player.TTL);
                ChanMsg($"{player.Name} {action}! This wondrous godsend has accelerated them {Duration(ttl)} towards level {player.Level + 1}.");
                Players[Players.IndexOf(player)].TTL -= ttl;
                ChanMsg($"{player.Name} reaches next level in {Duration(player.TTL)}.");
            }
        }

        public void TeamBattle(List<Player> online) {
            if (online == null || online.Count < 6)
                return;

            List<Player> players = online.OrderBy(x => Random.Next()).Take(6).ToList();
            players.Shuffle();

            int team1Sum = ItemSum(players[0], true) + ItemSum(players[1], true) + ItemSum(players[2], true);
            int team2Sum = ItemSum(players[3], true) + ItemSum(players[4], true) + ItemSum(players[5], true);

            int ttl = Players[Players.IndexOf(players[0])].TTL;
            for (int i = 1; i < 3; i++)
                ttl = Players[Players.IndexOf(players[i])].TTL > ttl ? Players[Players.IndexOf(players[i])].TTL : ttl;

            ttl = (int)(ttl * .20);
            int team1Roll = Random.Next(team1Sum - 1);
            int team2Roll = Random.Next(team2Sum - 1);

            if (team1Roll >= team2Roll) {
                ChanMsg($"{players[0].Name}, {players[1].Name}, and {players[2].Name} [{team1Roll}/{team1Sum}] have team battled " +
                    $"{players[3].Name}, {players[4].Name}, and {players[5].Name} [{team2Roll}/{team2Sum}] and won! {Duration(ttl)} " +
                    $"is removed from their clocks.");
                Players[Players.IndexOf(players[0])].TTL -= ttl;
                Players[Players.IndexOf(players[1])].TTL -= ttl;
                Players[Players.IndexOf(players[2])].TTL -= ttl;
            } else {
                ChanMsg($"{players[0].Name}, {players[1].Name}, and {players[2].Name} [{team1Roll}/{team1Sum}] have team battled " +
                    $"{players[3].Name}, {players[4].Name}, and {players[5].Name} [{team2Roll}/{team2Sum}] and lost! {Duration(ttl)} " +
                    $"is added to their clocks.");
                Players[Players.IndexOf(players[0])].TTL += ttl;
                Players[Players.IndexOf(players[1])].TTL += ttl;
                Players[Players.IndexOf(players[2])].TTL += ttl;
            }
        }

        public void War(List<Player> online) {
            if (online == null || online.Count == 0)
                return;

            string[] quadrants = { "Northeast", "Southeast", "Southwest", "Northwest" };
            int[] quad_sum = { 0, 0, 0, 0, 0 };
            Dictionary<Player, int> quadrant = new Dictionary<Player, int>();
            foreach (Player p in online) {
                quadrant[p] = 4;
                if (((2 * p.Pos.Y) + 1) < Config.MapY) {
                    quadrant[p] = ((2 * p.Pos.X) + 1) < Config.MapX ? 3 : quadrant[p];
                    quadrant[p] = ((2 * p.Pos.X) + 1) > Config.MapX ? 0 : quadrant[p];
                } else if (((2 * p.Pos.Y) + 1) > Config.MapY) {
                    quadrant[p] = ((2 * p.Pos.X) + 1) < Config.MapX ? 2 : quadrant[p];
                    quadrant[p] = ((2 * p.Pos.X) + 1) > Config.MapX ? 1 : quadrant[p];
                }
                quad_sum[quadrant[p]] += ItemSum(p);
            }

            int[] roll = { 0, 0, 0, 0 };
            for (int i = 0; i < 4; i++)
                roll[i] = Random.Next(quad_sum[i]);

            bool[] is_winner = { false, false, false, false };
            for (int i = 0; i < 4; i++)
                is_winner[i] = roll[i] >= roll[(i + 1) % 4] && roll[i] >= roll[(i + 3) % 4];
            List<string> winners = new List<string>();
            for (int i = 0; i < 4; i++)
                if (is_winner[i])
                    winners.Add($"the {quadrants[i]} [{roll[i]}/{quad_sum[i]}]");
            string winner_text = string.Empty;
            for (int i = 0; i < winners.Count; i++)
                winner_text = i == 0 ? winners[i] : i == 1 ? winner_text + $" and {winners[i]}" : winner_text + $", {winners[i]}";
            winner_text = winner_text != string.Empty ? $"has shown the power of {winner_text}" : winner_text;

            bool[] is_loser = { false, false, false, false };
            for (int i = 0; i < 4; i++)
                is_loser[i] = roll[i] < roll[(i + 1) % 4] && roll[i] < roll[(i + 3) % 4];
            List<string> losers = new List<string>();
            for (int i = 0; i < 4; i++)
                if (is_loser[i])
                    losers.Add($"the {quadrants[i]} [{roll[i]}/{quad_sum[i]}]");
            string loser_text = string.Empty;
            for (int i = 0; i < losers.Count; i++)
                loser_text = i == 0 ? losers[i] : i == 1 ? loser_text + $" and {losers[i]}" : loser_text + $", {losers[i]}";
            loser_text = loser_text != string.Empty ? $"led {loser_text} to perdition" : loser_text;
            List<string> neutrals = new List<string>();
            for (int i = 0; i < 4; i++)
                if (!is_winner[i] && !is_loser[i])
                    neutrals.Add($"the {quadrants[i]} [{roll[i]}/{quad_sum[i]}]");
            string neutral_text = string.Empty;
            for (int i = 0; i < neutrals.Count; i++)
                neutral_text = i == 0 ? neutrals[i] : i == 1 ? neutral_text + $" and {neutrals[i]}" : neutral_text + $", {neutrals[i]}";
            neutral_text = neutral_text != string.Empty ? $" The diplomacy of {neutral_text} was admirable." : neutral_text;
            ChanMsg("A world war has taken place in the realm!");
            if (winner_text != string.Empty && loser_text != string.Empty)
                ChanMsg($"The war between the four parts of the realm {winner_text}, whereas it {loser_text}.{neutral_text}");
            else if (winner_text == string.Empty && loser_text == string.Empty)
                ChanMsg($"The war between the four parts of the realm was well-balanced.{neutral_text}");
            else
                ChanMsg($"The war between the four parts of the realm {winner_text}{loser_text}.{neutral_text}");

            foreach (Player p in online) {
                if (is_winner[quadrant[p]]) {
                    Players[Players.IndexOf(p)].TTL = Players[Players.IndexOf(p)].TTL / 2;
                    ChanMsg($"War outcome: The {quadrants[quadrant[p]]} won, TTL of {p.Name} is halved. {p.Name} reaches " +
                        $"next level in {Duration(Players[Players.IndexOf(p)].TTL)}.");
                }
                if (is_loser[quadrant[p]]) {
                    Players[Players.IndexOf(p)].TTL = Players[Players.IndexOf(p)].TTL * 2;
                    ChanMsg($"War outcome: The {quadrants[quadrant[p]]} lost, TTL of {p.Name} is doubled. {p.Name} reaches " +
                        $"next level in {Duration(Players[Players.IndexOf(p)].TTL)}.");
                }
            }
        }

        public void MovePlayers(List<Player> online) {
            if (LastTime.Equals(DateTime.MinValue))
                return;
            if (online == null || online.Count == 0)
                return;

            for (int i = 0; i < Config.Tick; i++) {
                Dictionary<Pos, Hashtable> positions = new Dictionary<Pos, Hashtable>();

                if ((int)Quest["type"] == 2 && ((List<Player>)Quest["players"]).Count > 0) {
                    bool stageFinished = true;
                    foreach (Player p in (List<Player>)Quest["players"]) {
                        if ((int)Quest["stage"] == 1) {
                            if (Players[Players.IndexOf(p)].Pos != (Pos)Quest["pos1"]) {
                                stageFinished = false;
                                break;
                            } else {
                                if (Players[Players.IndexOf(p)].Pos != (Pos)Quest["pos2"]) {
                                    stageFinished = false;
                                    break;
                                }
                            }
                        }
                    }
                    if ((int)Quest["stage"] == 1 && stageFinished)
                        Quest["stage"] = 2;
                    else if ((int)Quest["stage"] == 2 && stageFinished) {
                        ChanMsg($"{string.Join(", ", ((List<Player>)Quest["players"]).Select(p => p.Name).ToArray(), 0, 3)}, and {((List<Player>)Quest["players"])[3].Name} " +
                            $"have completed their journey! 25% of their burden is eliminated.");
                        foreach (Player p in (List<Player>)Quest["players"])
                            Players[Players.IndexOf(p)].TTL = (int)(Players[Players.IndexOf(p)].TTL * .75);
                        Quest["players"] = new List<Player>();
                        Quest["questTime"] = DateTime.Now.AddSeconds(21600);
                        Quest["type"] = 1;
                    } else {
                        List<Player> players = online.Where(x => !((List<Player>)Quest["players"]).Any(y => y.Equals(x))).ToList();
                        foreach (Player player in players) {
                            Player p = Players[Players.IndexOf(player)];
                            p.Pos.X += Random.Next(3) - 1;
                            p.Pos.Y += Random.Next(3) - 1;
                            p.Pos.X = p.Pos.X > Config.MapX ? 0 : p.Pos.X;
                            p.Pos.Y = p.Pos.Y > Config.MapY ? 0 : p.Pos.Y;
                            p.Pos.X = p.Pos.X < 0 ? Config.MapX : p.Pos.X;
                            p.Pos.Y = p.Pos.Y < 0 ? Config.MapY : p.Pos.Y;

                            if (positions.ContainsKey(p.Pos) && (bool)positions[p.Pos]["battled"] == false) {
                                if (((Player)positions[p.Pos]["player"]).Admin && p.Admin == false && Random.Next(100) < 1)
                                    ChanMsg($"{p.Name} encounters {((Player)positions[p.Pos]["player"]).Name} and bows humbly.");
                                if (Random.Next(online.Count) < 1) {
                                    positions[p.Pos]["battled"] = true;
                                    CollisionFight(p, (Player)positions[p.Pos]["player"]);
                                }
                            } else
                                positions[p.Pos] = new Hashtable() { { "battled", false }, { "player", p } };
                        }
                        foreach (Player player in (List<Player>)Quest["players"]) {
                            Player p = Players[Players.IndexOf(player)];
                            if ((int)Quest["stage"] == 1) {
                                if (Random.Next(100) < 1) {
                                    if (p.Pos.X != ((Pos)Quest["pos1"]).X)
                                        p.Pos.X += p.Pos.X < ((Pos)Quest["pos1"]).X ? 1 : -1;
                                    if (p.Pos.Y != ((Pos)Quest["pos1"]).Y)
                                        p.Pos.Y += p.Pos.Y < ((Pos)Quest["pos1"]).Y ? 1 : -1;
                                }
                            } else if ((int)Quest["stage"] == 2) {
                                if (Random.Next(100) < 1) {
                                    if (p.Pos.X != ((Pos)Quest["pos2"]).X)
                                        p.Pos.X += p.Pos.X < ((Pos)Quest["pos2"]).X ? 1 : -1;
                                    if (p.Pos.Y != ((Pos)Quest["pos2"]).Y)
                                        p.Pos.Y += p.Pos.Y < ((Pos)Quest["pos2"]).Y ? 1 : -1;
                                }
                            }
                        }
                    }
                } else {
                    foreach (Player player in online) {
                        Player p = Players[Players.IndexOf(player)];
                        p.Pos.X += Random.Next(3) - 1;
                        p.Pos.Y += Random.Next(3) - 1;
                        p.Pos.X = p.Pos.X > Config.MapX ? 0 : p.Pos.X;
                        p.Pos.Y = p.Pos.Y > Config.MapY ? 0 : p.Pos.Y;
                        p.Pos.X = p.Pos.X < 0 ? Config.MapX : p.Pos.X;
                        p.Pos.Y = p.Pos.Y < 0 ? Config.MapY : p.Pos.Y;

                        if (positions.ContainsKey(p.Pos) && (bool)positions[p.Pos]["battled"] == false) {
                            if (((Player)positions[p.Pos]["player"]).Admin && p.Admin == false && Random.Next(100) < 1)
                                ChanMsg($"{p.Name} encounters {((Player)positions[p.Pos]["player"]).Name} and bows humbly.");
                            if (Random.Next(online.Count) < 1) {
                                positions[p.Pos]["battled"] = true;
                                CollisionFight(p, (Player)positions[p.Pos]["player"]);
                            }
                        } else
                            positions[p.Pos] = new Hashtable() { { "battled", false }, { "player", p } };
                    }
                }

                foreach (Player player in online) {
                    Player p = Players[Players.IndexOf(player)];
                    if (MapItems.ContainsKey(p.Pos.ToString())) {
                        foreach (Item item in MapItems[p.Pos.ToString()].ToList()) {
                            if (item.Level > p.Items[item.ItemType].Level) {
                                ExchangeItem(p, item);
                                MapItems[p.Pos.ToString()].Remove(item);
                                break;
                            }
                        }
                        if (MapItems[p.Pos.ToString()].Count == 0)
                            MapItems.Remove(p.Pos.ToString());
                    }
                }
            }
        }

        public void CreateQuest(List<Player> online) {
            List<Player> players = online.Where(p => Players[Players.IndexOf(p)].Level > Config.QuestLevel && (int)(DateTime.Now - Players[Players.IndexOf(p)].LastLogin).TotalSeconds > 36000).ToList();

            if (players.Count < 4)
                return;

            List<Player> pickedPlayers = new List<Player>();

            while (pickedPlayers.Count < 4) {
                Player p = players[Random.Next(players.Count)];
                players.Remove(p);
                pickedPlayers.Add(p);
            }
            Quest["players"] = pickedPlayers;

            List<Event> events = AllEvents.Where(e => e.EventType == EventType.Quest1 || e.EventType == EventType.Quest2).ToList();

            Event quest = events[Random.Next(events.Count)];
            DateTime currTime = DateTime.Now;
            if (quest.EventType == EventType.Quest1) {
                Quest["text"] = quest.EventText;
                Quest["type"] = 1;
                Quest["questTime"] = currTime.AddSeconds(43200).AddSeconds(Random.Next(43200));
                Quest["pos1"] = new Pos(0, 0);
                Quest["pos2"] = new Pos(0, 0);
            } else {
                Match match = Regex.Match(quest.EventText, @"(\d+) (\d+) (\d+) (\d+) (.*)");
                Quest["pos1"] = new Pos(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
                Quest["pos2"] = new Pos(int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value));
                Quest["text"] = match.Groups[5].Value;
                Quest["type"] = 2;
                Quest["stage"] = 1;
            }

            if ((int)Quest["type"] == 1)
                ChanMsg($"{string.Join(", ", pickedPlayers.Select(p => p.Name).ToArray(), 0, 3)}, and {pickedPlayers[3].Name} have " +
                    $"been chosen by the gods to {Quest["text"]}. Quest to end in {Duration((int)((DateTime)Quest["questTime"] - currTime).TotalSeconds)}.");
            else if ((int)Quest["type"] == 2)
                ChanMsg($"{string.Join(", ", pickedPlayers.Select(p => p.Name).ToArray(), 0, 3)}, and {pickedPlayers[3].Name} have " +
                    $"been chosen by the gods to {Quest["text"]}. Participants must first reach {((Pos)Quest["pos1"]).ToString()}, then " +
                    $"{((Pos)Quest["pos2"]).ToString()}.");

            // TODO: Save quest
        }

        public void CreateTournament(List<Player> online) {
            List<Player> players = online.Where(p => Players[Players.IndexOf(p)].Level > Config.TournamentLevel && (int)(DateTime.Now - Players[Players.IndexOf(p)].LastLogin).TotalSeconds > 14400).ToList();

            if (players.Count < 16) {
                if (Tournament.TournamentCount == 0) {
                    Tournament.TournamentTime = DateTime.Now.AddSeconds(13500);
                    Tournament.TournamentCount = 1;
                } else {
                    Tournament.TournamentTime = DateTime.Now.AddSeconds(72900);
                    Tournament.TournamentCount = 0;
                }
                return;
            }

            Tournament.Players = new List<Player>();

            while (Tournament.Players.Count < 16) {
                Player p = players[Random.Next(players.Count)];
                players.Remove(p);
                Tournament.Players.Add(p);
            }

            ChanMsg($"{string.Join(", ", Tournament.Players.Select(p => p.Name))} have been chosen by the gods to participate in the royal tournament.");
            Tournament.Round = 1;
            Tournament.Battle = 1;
            Tournament.LowestRoll = 31337;
            Tournament.Players.Shuffle();
            Tournament.TournamentTime = DateTime.Now.AddSeconds(15);
        }

        public void TournamentBattle() {
            int p1 = (Tournament.Battle * 2) - 2;
            int p2 = p1 + 1;
            int p1Sum = ItemSum(Tournament.Players[p1]);
            int p2Sum = ItemSum(Tournament.Players[p2]);
            int p1Roll = Random.Next(p1Sum - 1);
            int p2Roll = Random.Next(p2Sum - 1);
            int winner, loser;

            if (p1Roll >= p2Roll) {
                winner = p1;
                loser = p2;
                if (Tournament.LowestRoll > p2Roll) {
                    Tournament.LowestRoll = p2Roll;
                    Tournament.LowestRoller = Tournament.Players[p2];
                }
            } else {
                winner = p2;
                loser = p1;
                if (Tournament.LowestRoll > p1Roll) {
                    Tournament.LowestRoll = p1Roll;
                    Tournament.LowestRoller = Tournament.Players[p1];
                }
            }

            ChanMsg($"Tournament Round {Tournament.Round}, Fight {Tournament.Battle}: {Tournament.Players[p1].Name} [{p1Roll}/{p1Sum}] vs " +
                $"{Tournament.Players[p2].Name} [{p2Roll}/{p2Sum}] ... {Tournament.Players[winner].Name} advances!");

            if (Tournament.Players.Count == 2) {
                int ttl = (int)(5 + (Random.Next(5) / 100.0) * Players[Players.IndexOf(Tournament.Players[loser])].TTL);
                ChanMsg($"{Players[Players.IndexOf(Tournament.Players[loser])].Name} receives an honorable mention! As a reward for their " +
                    $"semi-heroic efforts, {Duration(ttl)} is removed from their time toward level " +
                    $"{Players[Players.IndexOf(Tournament.Players[loser])].Level + 1}.");
                Players[Players.IndexOf(Tournament.Players[loser])].TTL -= ttl;
            }

            Tournament.Players.RemoveAt(loser);
            Tournament.Battle += 1;

            if (Tournament.Battle > (Tournament.Players.Count / 2)) {
                Tournament.Round += 1;
                Tournament.Battle = 1;
                if (Tournament.Players.Count > 1)
                    ChanMsg($"{string.Join(", ", Tournament.Players.Select(p => p.Name))} advance to round {Tournament.Round} of the royal tournament.");

                Tournament.Players.Shuffle();
            }

            if (Tournament.Players.Count == 1) {
                int ttl = (int)((10 + (Random.Next(40) / 100.0)) * Players[Players.IndexOf(Tournament.Players[0])].TTL);
                ChanMsg($"{Tournament.Players[0].Name} has won the royal tournament! AS a reward for their heroic efforts, " +
                    $"{Duration(ttl)} is removed from their time toward level {Players[Players.IndexOf(Tournament.Players[0])].Level + 1}.");
                Players[Players.IndexOf(Tournament.Players[0])].TTL -= ttl;
                ChanMsg($"{Players[Players.IndexOf(Tournament.Players[0])].Name} reaches next level in {Duration(Players[Players.IndexOf(Tournament.Players[0])].TTL)}.");
                int gain = (int)(Players[Players.IndexOf(Tournament.LowestRoller)].Level * 60) + Random.Next(60);
                int battleMsg = Random.Next(6);
                switch (battleMsg) {
                    case 0:
                        ChanMsg($"{Tournament.LowestRoller.Name} has tripped over their own feet and rolled an outstanding {Tournament.LowestRoll}. " +
                            $"{Duration(gain)} is added to {Tournament.LowestRoller.Name}'s timer towards level {Players[Players.IndexOf(Tournament.LowestRoller)].Level + 1}.");
                        break;
                    case 1:
                        ChanMsg($"{Tournament.LowestRoller.Name} fell asleep in the heat of battle, and rolled over onto their opponent for a " +
                            $"strike of {Tournament.LowestRoll}. {Duration(gain)} is added to {Tournament.LowestRoller.Name}'s timer towards " +
                            $"level {Players[Players.IndexOf(Tournament.LowestRoller)].Level + 1}.");
                        break;
                    case 2:
                        ChanMsg($"{Tournament.LowestRoller.Name}, the noob, has dropped their weapon and delivered a striking hit of " +
                            $"{Tournament.LowestRoll} with their belt. {Duration(gain)} is added to {Tournament.LowestRoller.Name}'s timer " +
                            $"towards level {Players[Players.IndexOf(Tournament.LowestRoller)].Level + 1}.");
                        break;
                    case 3:
                        ChanMsg($"{Tournament.LowestRoller.Name} decided to have a hippy day and stopped to admire the scenery, causing a hit of " +
                            $"{Tournament.LowestRoll} to their opponent's spleen from laughing at their sissyness.{Duration(gain)} is added to " +
                            $"{Tournament.LowestRoller.Name}'s timer towards level {Players[Players.IndexOf(Tournament.LowestRoller)].Level + 1}.");
                        break;
                    case 4:
                        ChanMsg($"{Tournament.LowestRoller.Name} ran away in a panic, screaming incoherently about blood and other manly things. " +
                            $"The extreme upper pitch of this caused {Tournament.LowestRoll} damage. {Duration(gain)} is added to " +
                            $"{Tournament.LowestRoller.Name}'s timer towards level {Players[Players.IndexOf(Tournament.LowestRoller)].Level + 1}.");
                        break;
                    case 5:
                        ChanMsg($"{Tournament.LowestRoller.Name} didn't read the directions and messed up their helmet, having it on backwards " +
                            $"during combat. The accidental head-butt caused {Tournament.LowestRoll} damage. {Duration(gain)} is added to " +
                            $"{Tournament.LowestRoller.Name}'s timer towards level {Players[Players.IndexOf(Tournament.LowestRoller)].Level + 1}.");
                        break;
                }
                Players[Players.IndexOf(Tournament.LowestRoller)].TTL += gain;
                ChanMsg($"{Tournament.LowestRoller.Name} reaches next level in {Duration(Players[Players.IndexOf(Tournament.LowestRoller)].TTL)}.");

                if (Tournament.TournamentCount == 0) {
                    Tournament.TournamentTime = DateTime.Now.AddSeconds(13020);
                    Tournament.TournamentCount = 1;
                } else {
                    Tournament.TournamentTime = DateTime.Now.AddSeconds(72420);
                    Tournament.TournamentCount = 0;
                }
                Tournament.Players = new List<Player>();
            } else
                Tournament.TournamentTime = DateTime.Now.AddSeconds(30);
        }

        public void QuestPenaltyCheck(Player player) {
            if (Quest["players"] != null && ((List<Player>)Quest["players"]).Count > 0) {
                foreach (Player quester in (List<Player>)Quest["players"]) {
                    if (quester == player) {
                        ChanMsg($"{Players[Players.IndexOf(player)].Name}'s prudence and self-regard has brought the wrath of the gods upon the realm. " +
                            $"All your great wickedness makes it as if you were heavy with lead, and to tend downwards with great weight and pressure " +
                            $"towards hell. Therefore have you drawn yourselves 15 steps closer to that gaping maw.");
                        List<Player> online = Players.Where(p => p.Online).ToList();
                        if (online is null || online.Count == 0)
                            return;
                        foreach (Player p in online) {
                            int gain = 15 * PenTTL(p.Level) / Config.RPBase;
                            Players[Players.IndexOf(p)].TTL += gain;
                            Players[Players.IndexOf(p)].Penalties["quest"] += gain;
                        }
                        Quest["players"] = new List<Player>();
                        Quest["questTime"] = DateTime.Now.AddSeconds(43200);
                        // TODO: Save quest
                        break;
                    }
                }
            }

            if (Tournament.Players != null && Tournament.Players.Count > 0) {
                foreach (Player tourney in Tournament.Players) {
                    if (tourney == player) {
                        int ttl = (int)(0.10 * Players[Players.IndexOf(player)].TTL);
                        Players[Players.IndexOf(player)].TTL += ttl;
                        ChanMsg($"{Players[Players.IndexOf(player)].Name} has disobeyed the gods and declared themsevles unfit for the tournament, " +
                            $"therefore the remainder of the event shall be cancelled. The tournament participants have conspired to add {Duration(ttl)} " +
                            $"to {Players[Players.IndexOf(player)].Name}'s time to level {Players[Players.IndexOf(player)].Level + 1}. Another tournament " +
                            $"will begin within the hour!");
                        Tournament.Players = new List<Player>();
                        Tournament.TournamentTime = DateTime.Now.AddSeconds(1800).AddSeconds(Random.Next(1800));
                        break;
                    }
                }
            }
        }

        public void ParseMessage(IrcUser ircUser, string[] msg) {
            if (msg.Length > 0) {
                switch (msg[0].ToLower()) {
                    case "register":
                        if (Players.Exists(p => p.Nick == ircUser.Nick)) {
                            PrivMsg(ircUser, $"Sorry, you are already online as {Players.First(p => p.Nick == ircUser.Nick).Name}.");
                        } else {
                            if (msg.Length < 4 || msg[3].Equals(string.Empty)) {
                                PrivMsg(ircUser, "Try: REGISTER <char name> <password> <class>");
                                PrivMsg(ircUser, "IE : REGISTER Poseidon MyPassword God Of the Sea");
                            } else if (msg[1] != string.Empty && Players.Exists(p => p.Name.ToLower() == msg[1].ToLower()))
                                PrivMsg(ircUser, "Sorry, that character name is already in use.");
                            else if (msg[1].ToLower() == Config.PrimNick.ToLower())
                                PrivMsg(ircUser, "Sorry, that character name cannot be registered.");
                            else if (ircUser.JoinedChannels.Contains(Config.ChannelName) == false)
                                PrivMsg(ircUser, $"Sorry, you're not in {Config.ChannelName}.");
                            else {
                                IrcClient.Voice(Config.ChannelName, ircUser.Nick);
                                Players.Add(new Player()
                                {
                                    Name = msg[1],
                                    Class = string.Join(" ", msg, 3, msg.Length - 3),
                                    Nick = ircUser.Nick,
                                    UHost = ircUser.Host,
                                    Password = msg[2],
                                    Admin = Config.Owner == ircUser.Nick
                                });
                                ChanMsg($"Welcome {ircUser.Nick}'s new player {msg[1]}, the " +
                                    $"{string.Join(" ", msg, 3, msg.Length - 3)}! Next level in {Duration(Config.RPBase)}.");
                                PrivMsg(ircUser, $"Success! Account {msg[1]} created. You have {Config.RPBase} " +
                                    $"seconds idleness until you reach level 1.");
                                PrivMsg(ircUser, "NOTE: The point of the game is to see who can idle the longest. " +
                                    "As such, talking in the channel, parting, quitting, and changing nicks all penalize you.");
                            }
                        }
                        break;
                    case "hog":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            ChanMsg($"{ircUser.Nick} has summoned the Hand of God.");
                            Hog(Players.Where(p => p.Online).ToList());
                        } else
                            PrivMsg(ircUser, "You don't have access to HOG.");
                        break;
                    case "chpass":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            if (msg.Length < 3)
                                PrivMsg(ircUser, "Try: CHPASS <char name> <new password>");
                            else if (Players.Exists(p => p.Name == msg[1]) == false)
                                PrivMsg(ircUser, $"No such character {msg[1]}.");
                            else {
                                Players.First(p => p.Name == msg[1]).Password = msg[2];
                                PrivMsg(ircUser, $"Password for {msg[1]} changed.");
                            }
                        } else
                            PrivMsg(ircUser, "You don't have access to CHPASS.");
                        break;
                    case "chuser":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            if (msg.Length < 3)
                                PrivMsg(ircUser, "Try: CHUSER <char name> <new char name>");
                            else if (Players.Exists(p => p.Name == msg[1]) == false)
                                PrivMsg(ircUser, $"No such character {msg[1]}.");
                            else if (Players.Exists(p => p.Name == msg[1]))
                                PrivMsg(ircUser, $"Character name {msg[1]} is already taken.");
                            else {
                                Players.First(p => p.Name == msg[1]).Name = msg[2];
                                PrivMsg(ircUser, $"Character name for {msg[1]} changed to {msg[2]}.");
                            }
                        } else
                            PrivMsg(ircUser, "You don't have access to CHUSER.");
                        break;
                    case "chclass":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            if (msg.Length < 3)
                                PrivMsg(ircUser, "Try: CHCLASS <char name> <new char class>");
                            else if (Players.Exists(p => p.Name == msg[1]) == false)
                                PrivMsg(ircUser, $"No such character {msg[1]}.");
                            else {
                                Players.First(p => p.Name == msg[1]).Class = string.Join(" ", msg, 2, msg.Length - 2);
                                PrivMsg(ircUser, $"Class for {msg[1]} changed to {string.Join(" ", msg, 2, msg.Length - 2)}.");
                            }
                        } else
                            PrivMsg(ircUser, "You don't have access to CHCLASS.");
                        break;
                    case "push":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            if (msg.Length < 3 || Regex.IsMatch(msg[2], @"^\-?\d+$") == false)
                                PrivMsg(ircUser, "Try: PUSH <char name> <seconds>");
                            else if (Players.Exists(p => p.Name == msg[1]) == false)
                                PrivMsg(ircUser, $"No such character {msg[1]}.");
                            else if (int.Parse(msg[2]) > Players.First(p => p.Name == msg[1]).TTL) {
                                PrivMsg(ircUser, $"Time to level for {msg[1]} ({Players.First(p => p.Name == msg[1]).TTL}s) is " +
                                    $"lower than {msg[2]}; setting TTL to 0.");
                                ChanMsg($"{ircUser.Nick} has pushed {msg[1]} {Players.First(p => p.Name == msg[1]).TTL} seconds " +
                                    $"towards level {Players.First(p => p.Name == msg[1]).Level + 1}.");
                                Players.First(p => p.Name == msg[1]).TTL = 0;
                            } else {
                                Players.First(p => p.Name == msg[1]).TTL -= int.Parse(msg[2]);
                                ChanMsg($"{ircUser.Nick} has pushed {msg[1]} {msg[2]} seconds toward level " +
                                    $"{Players.First(p => p.Name == msg[1]).Level + 1}. {msg[1]} reaches next level in " +
                                    $"{Duration(Players.First(p => p.Name == msg[1]).TTL)}.");
                            }
                        } else
                            PrivMsg(ircUser, "You don't have access to PUSH.");
                        break;
                    case "logout":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Online)
                            Penalize(new Hashtable() { { "type", "logout" }, { "nick", ircUser.Nick } });
                        else
                            PrivMsg(ircUser, "You are not logged in.");
                        break;
                    case "quest":
                        if (Quest["players"] != null && ((List<Player>)Quest["players"]).Count > 0) {
                            if ((int)Quest["type"] == 1)
                                PrivMsg(ircUser, $"{string.Join(", ", ((List<Player>)Quest["players"]).Select(p => p.Name).ToArray(), 0, 3)}, and {((List<Player>)Quest["players"])[3].Name} are " +
                                    $"on a quest to {Quest["text"]}. Quest to complete in {Duration((int)((DateTime)Quest["questTime"] - DateTime.Now).TotalSeconds)}.");
                            else if ((int)Quest["type"] == 2)
                                PrivMsg(ircUser, $"{string.Join(", ", ((List<Player>)Quest["players"]).Select(p => p.Name).ToArray(), 0, 3)}, and {((List<Player>)Quest["players"])[3].Name} are " +
                                    $"on a quest to {Quest["text"]}. Participants must first reach {((Pos)Quest["pos1"]).ToString()}, then " +
                                    $"{((Pos)Quest["pos2"]).ToString()}.");
                        } else
                            PrivMsg(ircUser, "There is no active quest.");
                        break;
                    case "status":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Online) {
                            if (msg.Length > 1) {
                                if (Players.Exists(p => p.Name == msg[1])) {
                                    Player p = Players.First(player => player.Name == msg[1]);
                                    PrivMsg(ircUser, $"{p.Name}: Level {p.Level} {p.Class}; O{(p.Online ? "n" : "ff")}line; " +
                                        $"TTL: {Duration(p.TTL)}; Idled: {Duration(p.IdleTime)}; Item Sum: {ItemSum(p)}");
                                } else
                                    PrivMsg(ircUser, "No such user.");
                            } else {
                                Player p = Players.First(player => player.Nick == ircUser.Nick);
                                PrivMsg(ircUser, $"{p.Name}: Level {p.Level} {p.Class}; O{(p.Online ? "n" : "ff")}line; " +
                                    $"TTL: {Duration(p.TTL)}; Idled: {Duration(p.IdleTime)}; Item Sum: {ItemSum(p)}");
                            }
                        } else
                            PrivMsg(ircUser, "You are not logged in.");
                        break;
                    case "whoami":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Online) {
                            Player p = Players.First(player => player.Nick == ircUser.Nick);
                            PrivMsg(ircUser, $"You are {p.Name}, the level {p.Level} {p.Class}. Next level in {Duration(p.TTL)}.");
                        } else
                            PrivMsg(ircUser, "You are not logged in.");
                        break;
                    case "newpass":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Online) {
                            if (msg.Length < 2)
                                PrivMsg(ircUser, "Try: NEWPASS <new password>");
                            else {
                                Players.First(p => p.Nick == ircUser.Nick).Password = msg[1];
                                PrivMsg(ircUser, "Your password was changed.");
                            }
                        } else
                            PrivMsg(ircUser, "You are not logged in.");
                        break;
                    case "align":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Online) {
                            if (msg.Length < 2 || (msg[1].ToLower() != "good" && msg[1].ToLower() != "neutral" && msg[1].ToLower() != "evil"))
                                PrivMsg(ircUser, "Try: ALIGN <good|neutral|evil>");
                            else {
                                Players.First(player => player.Nick == ircUser.Nick).Align = msg[1].Substring(0, 1);
                                ChanMsg($"{Players.First(player => player.Nick == ircUser.Nick).Name} has changed alignment to: {msg[1].ToLower()}.");
                                PrivMsg(ircUser, $"You alignment was changed to: {msg[1].ToLower()}.");
                            }
                        } else
                            PrivMsg(ircUser, "You are not logged in.");
                        break;
                    case "login":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Online)
                            Notice(ircUser.Nick, $"Sorry, you are already online as {Players.First(p => p.Nick == ircUser.Nick).Name}.");
                        else {
                            if (msg.Length < 3 || msg[2].Equals(string.Empty))
                                Notice(ircUser.Nick, "Try: LOGIN <username> <password>");
                            else if (Players.Exists(p => p.Name == msg[1]) == false)
                                Notice(ircUser.Nick, "Sorry, no such account name. Note that account names are case sensitive.");
                            else if (ircUser.JoinedChannels.Contains(Config.ChannelName) == false)
                                Notice(ircUser.Nick, $"Sorry, you're not in {Config.ChannelName}.");
                            else if (Players.First(p => p.Name == msg[1]).Password != msg[2])
                                Notice(ircUser.Nick, "Wrong password.");
                            else {
                                IrcClient.Voice(Config.ChannelName, ircUser.Nick);
                                Player p = Players.First(player => player.Name == msg[1]);
                                p.Online = true;
                                p.Nick = ircUser.Nick;
                                p.UHost = ircUser.Host;
                                p.LastLogin = DateTime.Now;
                                ChanMsg($"{msg[1]}, the level {p.Level} {p.Class}, is now online from {ircUser.Nick}. " +
                                    $"Next level in {Duration(p.TTL)}.");
                                Notice(ircUser.Nick, $"Logon successful. Next level in {Duration(p.TTL)}.");
                            }
                        }
                        break;
                    case "removeme":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Online) {
                            Player p = Players.First(player => player.Nick == ircUser.Nick);
                            PrivMsg(ircUser, $"Account {p.Name} removed.");
                            ChanMsg($"{ircUser.Nick} removed their account, {p.Name}, the {p.Class}.");
                            Players.Remove(p);
                        } else
                            PrivMsg(ircUser, "You are not logged in.");
                        break;
                    case "delold":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            if (msg.Length < 2 || Regex.IsMatch(msg[2], @"^[\d\.]+$") == false)
                                PrivMsg(ircUser, "Try: DELOLD <# of days>");
                            else {
                                Players.RemoveAll(p => (DateTime.Now - p.LastLogin).Days > int.Parse(msg[1]) && p.Online == false);
                                ChanMsg($"Accounts not accessed in the last {msg[1]} days removed by {ircUser.Nick}.");
                            }
                        } else
                            PrivMsg(ircUser, "You don't have access to DELOLD.");
                        break;
                    case "del":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            if (msg.Length < 2)
                                PrivMsg(ircUser, "Try: DEL <char name>");
                            else if (Players.Exists(p => p.Name == msg[1]) == false)
                                PrivMsg(ircUser, $"No such character {msg[1]}.");
                            else {
                                Players.Remove(Players[Players.IndexOf(Players.First(p => p.Name == msg[1]))]);
                                ChanMsg($"Character {msg[1]} was removed by {ircUser.Nick}.");
                            }
                        } else
                            PrivMsg(ircUser, "You don't have access to DEL.");
                        break;
                    case "mkadmin":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            if (msg.Length < 2)
                                PrivMsg(ircUser, "Try: MKADMIN <char name>");
                            else if (Players.Exists(p => p.Name == msg[1]) == false)
                                PrivMsg(ircUser, $"No such character {msg[1]}.");
                            else {
                                Players[Players.IndexOf(Players.First(p => p.Name == msg[1]))].Admin = true;
                                PrivMsg(ircUser, $"Character {msg[1]} is now a bot admin.");
                            }
                        } else
                            PrivMsg(ircUser, "You don't have access to MKADMIN.");
                        break;
                    case "deladmin":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            if (msg.Length < 2)
                                PrivMsg(ircUser, "Try: DELADMIN <char name>");
                            else if (Players.Exists(p => p.Name == msg[1]) == false)
                                PrivMsg(ircUser, $"No such character {msg[1]}.");
                            else if (Players.First(p => p.Name == msg[1]).Nick == Config.Owner)
                                PrivMsg(ircUser, "Cannot DELADMIN owner account.");
                            else {
                                Players[Players.IndexOf(Players.First(p => p.Name == msg[1]))].Admin = false;
                                PrivMsg(ircUser, $"Character {msg[1]} is no longer a bot admin.");
                            }
                        } else
                            PrivMsg(ircUser, "You don't have access to DELADMIN.");
                        break;
                    case "top":
                        if (Players.Count > 0) {
                            List<Player> players = Players.OrderByDescending(p => p.Level).ThenBy(p => p.TTL).Take(3).ToList();
                            if (players != null && players.Count > 0) {
                                PrivMsg(ircUser, "Idle RPG Top 3 Players:");
                                foreach (Player p in players)
                                    PrivMsg(ircUser, $"{p.Name}, the level {p.Level} {p.Class}, is #{players.IndexOf(p) + 1}! Next level in {Duration(p.TTL)}.");
                            }
                        } else
                            PrivMsg(ircUser, "There are no users playing yet.");
                        break;
                    case "irc":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            PrivMsg(ircUser, "Sending IRC command...");
                            IrcClient.WriteLine(string.Join(" ", msg, 1, msg.Length - 1));
                        } else
                            PrivMsg(ircUser, "You do not have access to IRC.");
                        break;
                    case "topic":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            PrivMsg(ircUser, "Setting channel topic...");
                            IrcClient.RfcTopic(Config.ChannelName, string.Join(" ", msg, 1, msg.Length - 1));
                        } else
                            PrivMsg(ircUser, "You do not have access to TOPIC.");
                        break;
                    case "fight":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Online) {
                            if (msg.Length < 2)
                                PrivMsg(ircUser, "Try: FIGHT <char name>");
                            else if (Players.Exists(p => p.Name == msg[1]) == false)
                                PrivMsg(ircUser, $"No such character {msg[1]}.");
                            else if (Players.First(p => p.Nick == ircUser.Nick).Level < 25)
                                PrivMsg(ircUser, "You're too weak to use this command! Try when you're stronger.");
                            else {
                                if (Random.Next(4) < 1) {
                                    PrivMsg(ircUser, $"You started a fight with {msg[1]}. Good luck!");
                                    Player p = Players.First(player => player.Nick == ircUser.Nick);
                                    Player opp = Players.First(player => player.Name == msg[1]);
                                    CollisionFight(p, opp);
                                } else
                                    PrivMsg(ircUser, $"You were unable to find {msg[1]}. It seems that they slipped away to live another day.");
                            }
                        } else
                            PrivMsg(ircUser, "You are not logged in.");
                        break;
                    case "move":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            if (msg.Length < 4 || Regex.IsMatch(msg[2], @"^\d+$") == false || Regex.IsMatch(msg[3], @"^\d+$") == false)
                                PrivMsg(ircUser, "Try: MOVE <char name> <x> <y>");
                            else if (Players.Exists(p => p.Name == msg[1]) == false)
                                PrivMsg(ircUser, $"No such character {msg[1]}.");
                            else {
                                Player p = Players.First(player => player.Name == msg[1]);
                                Pos oldPos = p.Pos;
                                Players[Players.IndexOf(p)].Pos = new Pos(int.Parse(msg[2]), int.Parse(msg[3]));
                                PrivMsg(ircUser, $"Moved {msg[1]} from {oldPos.ToString()} to {Players[Players.IndexOf(p)].Pos.ToString()}.");
                            }
                        } else
                            PrivMsg(ircUser, "You do not have access to MOVE.");
                        break;
                    case "pit":
                        if (Players.Exists(p => p.Nick == ircUser.Nick) && Players.First(p => p.Nick == ircUser.Nick).Admin) {
                            if (msg.Length < 3)
                                PrivMsg(ircUser, "Try: PIT <player 1> <player 2>");
                            else if (Players.Exists(p => p.Name == msg[1]) == false)
                                PrivMsg(ircUser, $"No such character {msg[1]}.");
                            else if (Players.Exists(p => p.Name == msg[2]) == false)
                                PrivMsg(ircUser, $"No such character {msg[2]}.");
                            else {
                                Player p1 = Players.First(player => player.Name == msg[1]);
                                Player p2 = Players.First(player => player.Name == msg[2]);
                                CollisionFight(p1, p2);
                            }
                        } else
                            PrivMsg(ircUser, "You do not have access to PIT.");
                        break;
                    default:
                        PrivMsg(ircUser, "Unknown command.");
                        break;
                }
            }
        }

        public void Penalize(Hashtable penalty) {
            Player p = Players.FirstOrDefault(player => player.Nick == (string)penalty["nick"] && player.Online);

            if (p != null) {
                QuestPenaltyCheck(p);
                int pen = 0;
                switch ((string)penalty["type"]) {
                    case "quit":
                        pen = 20 * PenTTL(p.Level) / Config.RPBase;
                        pen = pen > Config.LimitPen ? Config.LimitPen : pen;
                        Players[Players.IndexOf(p)].Penalties["quit"] += pen;
                        Players[Players.IndexOf(p)].Online = false;
                        break;
                    case "nick":
                        pen = 30 * PenTTL(p.Level) / Config.RPBase;
                        pen = pen > Config.LimitPen ? Config.LimitPen : pen;
                        Players[Players.IndexOf(p)].Penalties["nick"] += pen;
                        Players[Players.IndexOf(p)].Nick = (string)penalty["newNick"];
                        Players[Players.IndexOf(p)].UHost = (string)penalty["host"];
                        Notice((string)penalty["newNick"], $"Penalty of {Duration(pen)} added to your timer for nick change.");
                        break;
                    case "logout":
                        pen = 20 * PenTTL(p.Level) / Config.RPBase;
                        pen = pen > Config.LimitPen ? Config.LimitPen : pen;
                        Players[Players.IndexOf(p)].Penalties["logout"] += pen;
                        Notice((string)penalty["nick"], $"Penalty of {Duration(pen)} added to your timer for LOGOUT command.");
                        Players[Players.IndexOf(p)].Online = false;
                        break;
                    case "msg":
                        pen = (int)penalty["textLength"] * PenTTL(p.Level) / Config.RPBase;
                        pen = pen > Config.LimitPen ? Config.LimitPen : pen;
                        Players[Players.IndexOf(p)].Penalties["msg"] += pen;
                        Notice((string)penalty["nick"], $"Penalty of {Duration(pen)} added to your timer for chatting in the channel.");
                        break;
                    case "part":
                        pen = 200 * PenTTL(p.Level) / Config.RPBase;
                        pen = pen > Config.LimitPen ? Config.LimitPen : pen;
                        Players[Players.IndexOf(p)].Penalties["part"] += pen;
                        Players[Players.IndexOf(p)].Online = false;
                        Notice((string)penalty["nick"], $"Penalty of {Duration(pen)} added to your timer for leaving the channel.");
                        break;
                    case "kick":
                        pen = 250 * PenTTL(p.Level) / Config.RPBase;
                        pen = pen > Config.LimitPen ? Config.LimitPen : pen;
                        Players[Players.IndexOf(p)].Penalties["kick"] += pen;
                        Notice((string)penalty["nick"], $"Penalty of {Duration(pen)} added to your timer for being kicked.");
                        break;
                }
                Players[Players.IndexOf(p)].TTL += pen;
            }
        }

        private void LevelUp(Player p) {
            p.Level += 1;
            p.TTL = TTL(p.Level);
            ChanMsg($"{p.Name}, the {p.Class}, has attained level {p.Level}! Next level in {Duration(p.TTL)}.");
            FindItem(p);
            ChallengeOpp(p);
        }

        private int TTL(int level) {
            if (level > 60)
                return (int)(Config.RPBase * (Math.Pow(Config.RPStep, 60) + (86400 * (level - 60))));
            else
                return (int)(Config.RPBase * Math.Pow(Config.RPStep, level));
        }

        private int PenTTL(int level) {
            if (level > 60)
                return (int)(Config.RPBase * (Math.Pow(Config.RPPenStep, 60) + (86400 * (level - 60))));
            else
                return (int)(Config.RPBase * Math.Pow(Config.RPPenStep, level));
        }

        private string Duration(int? ttl) {
            if (ttl.HasValue) {
                TimeSpan ts = TimeSpan.FromSeconds(ttl.Value);
                return $"{ts:%d} day{(ts.Days == 1 ? "" : "s")}, {ts:hh}:{ts:mm}:{ts:ss}";
            } else
                return "N/A";
        }

        private void ExchangeItem(Player p, Item item) {
            switch (item.Tag) {
                case "a":
                    ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                        $"Mattt's Omniscience Grand Crown! Their enemies fall before them as they anticipate their every move.");
                    break;
                case "b":
                    ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                        $"Res0's Protectorate Plate Mail! Their enemies cower in fear as their attacks have no effect.");
                    break;
                case "c":
                    ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                        $"Dwyn's Storm Magic Amulet! Their enemies are swept away by an elemental fury before the war has even begun.");
                    break;
                case "d":
                    ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                        $"Jotun's Fury Colossal Sword! Their enemies' hatred is brought to a quick end as they arc their wrist, " +
                        $"dealing the crushing blow.");
                    break;
                case "e":
                    ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                        $"Drdink's Cane of Blind Rage! Their enemies are tossed aside as they blindly swing their arm around hitting stuff.");
                    break;
                case "f":
                    ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                        $"Mrquick's Magical Boots of Swiftness! Their enemies are left choking on their dust as they run from them " +
                        $"very, very quickly.");
                    break;
                case "g":
                    ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                        $"Jeff's Cluehammer of Doom! Their enemies are left with a sudden and intense clarity of mind... even as they " +
                        $"relieve them of it.");
                    break;
                case "h":
                    ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                        $"Juliet's Glorious Ring of Sparkliness! Their enemies are blinded by both its glory and their greed as they " +
                        $"bring desolation upon them.");
                    break;
                default:
                    Notice(p.Nick, $"You found a level {item.Level} {item.ItemType}! Your current {item.ItemType} is only " +
                        $"level {p.Items[item.ItemType].Level}, so it seems luck is with you!");
                    break;
            }

            DropItem(p.Pos, p.Items[item.ItemType]);
            p.Items[item.ItemType] = item;
        }

        private void DropItem(Pos pos, Item item) {
            if (item.Level > 0) {
                if (MapItems.ContainsKey(pos.ToString()) == false)
                    MapItems[pos.ToString()] = new List<Item>();

                item.Age = DateTime.Now;
                MapItems[pos.ToString()].Add(item);
            }
        }

        private void DowngradeItem(Item item) {
            Dictionary<string, int> minLevel = new Dictionary<string, int> { [""] = 0, ["a"] = 50, ["h"] = 50, ["b"] = 75, ["d"] = 150, ["e"] = 175, ["f"] = 250, ["g"] = 300 };
            item.Tag = item.Level == minLevel[item.Tag] ? string.Empty : item.Tag;
            if (item.Level > 0)
                item.Level -= 1;
        }

        private int ItemSum(Player user, bool battle = false) {
            if (user is null)
                return -1;

            int itemSum = 0;

            if (user.Nick == Config.PrimNick) {
                foreach (Player player in Players)
                    itemSum = itemSum < ItemSum(player) ? ItemSum(player) : itemSum;
                return itemSum + 1;
            }

            if (Players.Contains(user) == false)
                return -1;

            itemSum = user.Items.Sum(item => item.Value.Level);

            // If this is a battle, good users get a 10% boost, and evil users get a 10% detriment.
            return battle ? (user.Align == "e" ? (int)(itemSum * .9) : user.Align == "g" ? (int)(itemSum * 1.1) : itemSum) : itemSum;
        }

        private void ChanMsg(string msg) {
            IrcClient.SendMessage(SendType.Message, Config.ChannelName, msg);
        }

        private void Notice(string nick, string msg) {
            IrcClient.SendMessage(SendType.Notice, nick, msg);
        }

        private void PrivMsg(IrcUser ircUser, string msg) {
            IrcClient.SendMessage(SendType.Message, ircUser.Nick, msg);
        }
    }

    static class Extensions {
        public static void Shuffle<T>(this IList<T> list) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
