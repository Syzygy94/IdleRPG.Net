using System;

namespace IdleRPG.NET {
    public class Item {
        public string ItemType { get; set; }
        public int Level { get; set; }
        public string Tag { get; set; }
        public DateTime Age { get; set; }
    }

    public class Pos {
        public int X { get; set; }
        public int Y { get; set; }

        public Pos(int x, int y) {
            X = x;
            Y = y;
        }

        public override string ToString() {
            return $"({X}, {Y})";
        }
    }
}
