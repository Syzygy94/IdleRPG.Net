using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleRPG.NET {
    public class World {
        public static readonly string[] Items = { "ring", "amulet", "charm", "weapon", "helm", "tunic", "pair of gloves", "set of leggings", "shield", "pair of boots" };
        public static readonly string[] Penalties = { "quit", "nick", "msg", "part", "kick", "logout", "quest" };

        public Dictionary<Pos, List<Item>> MapItems { get; private set; }
        public List<Player> Players { get; private set; }

        public World() {
            MapItems = new Dictionary<Pos, List<Item>>();
            Players = new List<Player>();
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
                Notice(p, $"You found a level {newItem.Level} {newItem.ItemType}. Your current {newItem.ItemType} is level " +
                    $"{p.Items[newItem.ItemType].Level}, so it seems luck is against you. You toss the {newItem.ItemType}.");
                DropItem(p.Pos, newItem);
            }
        }

        public void ChallengeOpp(Player p) {
            if (p.Level < 25 && Random.Next(4) != 0)
                return;

            List<Player> opps = Players.Where(player => player != p).ToList();
            if (opps == null || opps.Count == 0)
                return;

            Player opp = Random.Next(opps.Count) < 1 ? new Player() { Name = Config.PrimNick } : Players[Players.IndexOf(opps[Random.Next(opps.Count)])];
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
                    if (battleMsg == 0)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"completely messed them up! {Duration(ttl)} is removed from {p.Name}'s clock.");
                    else if (battleMsg == 1)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"rocked it! {Duration(ttl)} is removed from {p.Name}'s clock.");
                    else if (battleMsg == 2)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"gave em what was coming! {Duration(ttl)} is removed from {p.Name}'s clock.");
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
                    if (battleMsg == 0)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"got flexed on in combat! {Duration(ttl)} is added to {p.Name}'s clock.");
                    else if (battleMsg == 1)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"realized it was a bad decision! {Duration(ttl)} is added to {p.Name}'s clock.");
                    else if (battleMsg == 2)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"didn't wake up till the next morning! {Duration(ttl)} is added to {p.Name}'s clock.");
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
                    if (battleMsg == 0)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"completely messed them up! {Duration(ttl)} is removed from {p.Name}'s clock.");
                    else if (battleMsg == 1)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"rocked it! {Duration(ttl)} is removed from {p.Name}'s clock.");
                    else if (battleMsg == 2)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"gave em what was coming! {Duration(ttl)} is removed from {p.Name}'s clock.");
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
                    if (battleMsg == 0)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"got flexed on in combat! {Duration(ttl)} is added to {p.Name}'s clock.");
                    else if (battleMsg == 1)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"realized it was a bad decision! {Duration(ttl)} is added to {p.Name}'s clock.");
                    else if (battleMsg == 2)
                        ChanMsg($"{p.Name} [{playerRoll}/{playerSum}] has come upon {opp.Name} [{oppRoll}/{oppSum}] and " +
                            $"didn't wake up till the next morning! {Duration(ttl)} is added to {p.Name}'s clock.");
                }
                p.TTL += ttl;
                ChanMsg($"{p.Name} reaches next level in {Duration(p.TTL)}.");
            }
        }

        public void ProcessItems() {
            DateTime curTime = DateTime.Now;
            foreach (Pos pos in MapItems.Keys.ToList()) {
                foreach (Item item in MapItems[pos].ToList()) {
                    int ttl = Config.RPItemBase * item.Level;
                    if (item.Age.AddSeconds(ttl).Ticks <= curTime.Ticks) {
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
                    Notice(player, $"You made to steal {targetPlayer.Name}'s {itemType}, but realized it was a lower level than your " +
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
                // TODO: Need to perform a calamity event lookup here after implementing loading of events from text file.
                string action = string.Empty;
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
                // TODO: Need to perform a godsend event lookup here after implementing loading of events from text file.
                string action = string.Empty;
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
            if (item.Tag == "a")
                ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                    $"Mattt's Omniscience Grand Crown! Their enemies fall before them as they anticipate their every move.");
            else if (item.Tag == "b")
                ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                    $"Res0's Protectorate Plate Mail! Their enemies cower in fear as their attacks have no effect.");
            else if (item.Tag == "c")
                ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                    $"Dwyn's Storm Magic Amulet! Their enemies are swept away by an elemental fury before the war has even begun.");
            else if (item.Tag == "d")
                ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                    $"Jotun's Fury Colossal Sword! Their enemies' hatred is brought to a quick end as they arc their wrist, " +
                    $"dealing the crushing blow.");
            else if (item.Tag == "e")
                ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                    $"Drdink's Cane of Blind Rage! Their enemies are tossed aside as they blindly swing their arm around hitting stuff.");
            else if (item.Tag == "f")
                ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                    $"Mrquick's Magical Boots of Swiftness! Their enemies are left choking on their dust as they run from them " +
                    $"very, very quickly.");
            else if (item.Tag == "g")
                ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                    $"Jeff's Cluehammer of Doom! Their enemies are left with a sudden and intense clarity of mind... even as they " +
                    $"relieve them of it.");
            else if (item.Tag == "h")
                ChanMsg($"The light of the gods shines down upon {p.Name}! They have found the level {item.Level} " +
                    $"Juliet's Glorious Ring of Sparkliness! Their enemies are blinded by both its glory and their greed as they " +
                    $"bring desolation upon them.");
            else
                Notice(p, $"You found a level {item.Level} {item.ItemType}! Your current {item.ItemType} is only " +
                    $"level {p.Items[item.ItemType].Level}, so it seems luck is with you!");

            DropItem(p.Pos, p.Items[item.ItemType]);
            p.Items[item.ItemType] = item;
        }

        private void DropItem(Pos pos, Item item) {
            if (item.Level > 0) {
                if (MapItems.ContainsKey(pos) == false)
                    MapItems[pos] = new List<Item>();

                item.Age = DateTime.Now;
                MapItems[pos].Add(item);
            }
        }

        private void DowngradeItem(Item item) {
            Dictionary<string, int> minLevel = new Dictionary<string, int> { [""] = 0, ["a"] = 50, ["h"] = 50, ["b"] = 75, ["d"] = 150, ["e"] = 175, ["f"] = 250, ["g"] = 300 };
            item.Tag = item.Level == minLevel[item.Tag] ? string.Empty : item.Tag;
            if (item.Level > 0)
                item.Level -= 1;
        }

        private int ItemSum(Player user, bool battle = false) {
            if (user == null)
                return -1;

            int itemSum = 0;

            if (user.Name == Config.PrimNick) {
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

        }

        private void Notice(Player player, string msg) {

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
