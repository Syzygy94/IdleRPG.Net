using System;
using System.Collections.Generic;

namespace IdleRPG.NET {
    public enum QuestType {
        Quest1,
        Quest2
    }

    public class Quest {
        public QuestType QuestType { get; set; }
        public List<Player> Players { get; set; }
        public Pos Pos1 { get; set; }
        public Pos Pos2 { get; set; }
        public DateTime QuestTime { get; set; }
        public string QuestText { get; set; }
        public int Stage { get; set; }

        public Quest() {
            QuestType = QuestType.Quest1;
            Players = new List<Player>();
            Pos1 = new Pos(0, 0);
            Pos2 = new Pos(0, 0);
            QuestTime = DateTime.Now.AddSeconds(Random.Next(21600));
            QuestText = string.Empty;
            Stage = 1;
        }
    }
}
