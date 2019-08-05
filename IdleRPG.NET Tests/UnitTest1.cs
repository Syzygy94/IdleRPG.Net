using IdleRPG.NET;
using NUnit.Framework;
using System;

namespace Tests {
    public class Tests {
        [SetUp]
        public void Setup() {
        }

        //[Test]
        //public void TestFindItem() {
        //    Player p = new Player() { Pos = new Pos(210, 100), Level = 30 };
        //    foreach (var key in p.Items.Keys)
        //        p.Items[key].Level = 100;
        //    World world = new World();
        //    world.FindItem(p);
        //    Assert.Pass();
        //}

        //[Test]
        //public void TestItemSum() {
        //    World world = new World();
        //    for (int p = 0; p < 10; p ++) {
        //        Player player = new Player() { Name = p.ToString() };
        //        foreach (var key in player.Items.Keys) {
        //            player.Items[key].Level = new Random().Next(10);
        //        }
        //        world.Players.Add(player);
        //    }
        //    var result = world.ItemSum(new Player() { Name = Config.PrimNick });
        //}

        //[Test]
        //public void TestLevelUp() {
        //    World world = new World();
        //    world.LevelUp(new Player() { Name = "Syzygy", Class = "Zombie", Level = 2 });
        //}

        //[Test]
        //public void TestGoodnees() {
        //    World world = new World();
        //    for (int p = 0; p < 10; p++) {
        //        world.Players.Add(new Player() { Name = p.ToString(), TTL = p * 200 });
        //    }
        //    world.Goodness(world.Players);
        //}

        //[Test]
        //public void TestEvilness() {
        //    World world = new World();
        //    for (int p = 0; p < 10; p++) {
        //        Player player = new Player() { Name = p.ToString() };
        //        foreach (var key in player.Items.Keys) {
        //            player.Items[key].Level = new Random().Next(10);
        //        }
        //        world.Players.Add(player);
        //    }
        //    world.Evilness(world.Players, world.Players);
        //}

        //[Test]
        //public void TestCollisionFight() {
        //    World world = new World();
        //    for (int p = 0; p < 10; p++) {
        //        Player player = new Player() { Name = p.ToString(), Level = p * 5, TTL = p * 200 };
        //        foreach (var key in player.Items.Keys) {
        //            player.Items[key].Level = new Random().Next(10);
        //        }
        //        world.Players.Add(player);
        //    }
        //    world.ChallengeOpp(world.Players[5]);
        //}

        //[Test]
        //public void TestTeamBattle() {
        //    World world = new World();
        //    for (int p = 0; p < 10; p++) {
        //        Player player = new Player() { Name = p.ToString(), Level = p * 5, TTL = p * 200 };
        //        foreach (var key in player.Items.Keys) {
        //            player.Items[key].Level = new System.Random().Next(10);
        //        }
        //        world.Players.Add(player);
        //    }
        //    world.TeamBattle(world.Players);
        //}

        //[Test]
        //public void TestCreateQuest() {
        //    World world = new World();
        //    for (int p = 0; p < 10; p++) {
        //        Player player = new Player() { Name = p.ToString(), Pos = new Pos(IdleRPG.NET.Random.Next(Config.MapX), IdleRPG.NET.Random.Next(Config.MapY)), Level = p * 5, TTL = p * 200, LastLogin = DateTime.Now.AddSeconds(-46000) };
        //        foreach (var key in player.Items.Keys) {
        //            player.Items[key].Level = new System.Random().Next(10);
        //        }
        //        world.Players.Add(player);
        //    }
        //    world.CreateQuest(world.Players);
        //}

        [Test]
        public void TestCreateTournamentAndBattle() {
            World world = new World();
            for (int p = 0; p < 17; p++) {
                Player player = new Player() { Name = p.ToString(), Pos = new Pos(IdleRPG.NET.Random.Next(Config.MapX), IdleRPG.NET.Random.Next(Config.MapY)), Level = p * 5, TTL = p * 200, LastLogin = DateTime.Now.AddSeconds(-46000) };
                foreach (var key in player.Items.Keys) {
                    player.Items[key].Level = new System.Random().Next(10);
                }
                world.Players.Add(player);
            }
            world.CreateTournament(world.Players);
            while (world.Tournament.Players.Count > 0)
                world.TournamentBattle();
        }
    }
}