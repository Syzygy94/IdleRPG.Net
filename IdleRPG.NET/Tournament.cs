using System;
using System.Collections.Generic;

namespace IdleRPG.NET {
    public class Tournament {
        public List<Player> Players { get; set; }
        public int Round { get; set; }
        public int Battle { get; set; }
        public int LowestRoll { get; set; }
        public Player LowestRoller { get; set; }
        public DateTime TournamentTime { get; set; }
        public int TournamentCount { get; set; }

        public Tournament() {
            Players = new List<Player>();
            LowestRoller = new Player();
            TournamentTime = DateTime.Now.AddSeconds(7200).AddSeconds(Random.Next(3600));
        }
    }
}
