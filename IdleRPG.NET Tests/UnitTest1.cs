using IdleRPG.NET;
using Meebey.SmartIrc4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = IdleRPG.NET.Random;

namespace IdleRPG.NET_Tests {
    [TestClass]
    public class UnitTest1 {
        private World world;
        private List<Player> players;
        private string[] names = { "Halloc", "Fast", "Wulfriccrow", "Ruthard", "Crowna", "Chetgrim", "Gar", "Muel'ma", "Dinoso", "Frithwig", "Steni", "Samann", "Briwaru", "Aman", "Serefred", "Guthlen", "Eabur", "Isenan", "Lybet", "Riaferd" };
        private readonly string[] classes = { "Disconcerted", "Maiden", "Ward", "Savior", "Monk", "Valkyrie", "Freebooter", "Villainous", "Unworthy", "Tricky", "Merciful", "Deadeye", "Great", "Gladiator", "Slimy", "Discerning", "Depraved", "Immolator", "Combatant", "Enraged" };

        [TestInitialize]
        public void TestSetup() {
            world = new World(new IrcClient());
            players = new List<Player>();
            for (int i = 0; i < names.Length; i++) {
                Player p = new Player()
                {
                    Name = names[i],
                    Class = classes[i],
                    Level = Random.Next(45),
                    Nick = $"NickName{i}",
                    UHost = $"{Random.Next(255)}.{Random.Next(255)}.{Random.Next(255)}.{Random.Next(255)}",
                    LastLogin = DateTime.Now.AddDays(-3),
                    Align = Random.Next(2) == 0 ? "g" : "e"
                };
                foreach (string key in p.Items.Keys)
                    p.Items[key].Level = Random.Next(p.Level);

                players.Add(p);
            }
            world.Players.AddRange(players);
        }

        [TestMethod]
        public void TestWar() {
            world.War(players);
        }

        [TestMethod]
        public void TestTeamBattle() {
            world.TeamBattle(players);
        }

        [TestMethod]
        public void TestTournament() {
            world.CreateTournament(players);
            while (world.Tournament.Players.Count > 1)
                world.TournamentBattle();
        }

        [TestMethod]
        public void TestFindItem() {
            Player p = players[Random.Next(players.Count)];
            world.FindItem(p);
        }

        [TestMethod]
        public void TestChallengeOpp() {
            Player p = players[Random.Next(players.Count)];
            world.ChallengeOpp(p);
        }

        [TestMethod]
        public void TestCollisionFight() {
            Player p = players[Random.Next(players.Count)];
            List<Player> opps = players.Where(x => !x.Equals(p)).ToList();
            Player opp = opps[Random.Next(opps.Count)];
            world.CollisionFight(p, opp);
        }

        [TestMethod]
        public void TestHog() {
            world.Hog(players);
        }

        [TestMethod]
        public void TestGoodness() {
            world.Goodness(players.Where(p => p.Align == "g").ToList());
        }

        [TestMethod]
        public void TestEvilness() {
            world.Evilness(players.Where(p => p.Align == "e").ToList(), players.Where(p => p.Align == "g").ToList());
        }

        [TestMethod]
        public void TestCalamity() {
            world.Calamity(players);
        }

        [TestMethod]
        public void TestGodsend() {
            world.GodSend(players);
        }

        [TestMethod]
        public void TestMovePlayers() {
            world.Start();
            world.MovePlayers(players);
        }

        [TestMethod]
        public void TestCreateQuest() {
            world.CreateQuest(players);
        }
    }
}
