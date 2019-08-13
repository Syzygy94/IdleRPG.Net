using System;
using System.Collections.Generic;
using System.Linq;

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

        public static bool operator ==(Quest q1, Quest q2) {
            if (ReferenceEquals(q1, q2))
                return true;

            if (q1 is null)
                return false;

            if (q2 is null)
                return false;

            return q1.QuestType == q2.QuestType && q1.Players.SequenceEqual(q2.Players) && q1.Pos1 == q2.Pos1 && 
                q1.Pos2 == q2.Pos2 && q1.QuestTime == q2.QuestTime && q1.QuestText == q2.QuestText && 
                q1.Stage == q2.Stage;
        }

        public static bool operator !=(Quest q1, Quest q2) {
            return !(q1 == q2);
        }

        public bool Equals(Quest q) {
            if (q is null)
                return false;

            if (ReferenceEquals(this, q))
                return true;

            return QuestType.Equals(q.QuestType) && Players.SequenceEqual(q.Players) && Pos1.Equals(q.Pos1) &&
                Pos2.Equals(q.Pos2) && QuestTime.Equals(q.QuestTime) && QuestText.Equals(q.QuestText) &&
                Stage.Equals(q.Stage);
        }

        public override bool Equals(object obj) {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            return obj.GetType() == GetType() && Equals((Quest)obj);
        }

        public override int GetHashCode() {
            return Tuple.Create(QuestType, Players, Pos1, Pos2, QuestTime, QuestText, Stage).GetHashCode();
        }
    }
}
