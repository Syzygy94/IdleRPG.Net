using System;
using IdleRPG.NET;
using Meebey.SmartIrc4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Random = IdleRPG.NET.Random;

namespace IdleRPG.NET_Tests {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void TestMethod1() {
            World world = new World(new IrcClient());
            for (int p = 0; p < 10; p++) {
                Player player = new Player() { Name = p.ToString(), Level = p * 5, TTL = p * 200, Nick = p.ToString(), UHost = p.ToString(), Class = p.ToString() };
                foreach (var key in player.Items.Keys) {
                    player.Items[key].Level = Random.Next(10);
                }
                world.Players.Add(player);
            }
            world.War(world.Players);
        }
    }
}
