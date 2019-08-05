using System;

namespace IdleRPG.NET {
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

        public static bool operator ==(Pos pos1, Pos pos2) {
            return pos1.X == pos2.X && pos1.Y == pos2.Y;
        }

        public static bool operator !=(Pos pos1, Pos pos2) {
            return pos1.X != pos2.X || pos1.Y != pos2.Y;
        }

        public override bool Equals(object obj) {
            if (obj is null || !(obj is Pos))
                return false;

            Pos pos = (Pos)obj;
            return X == pos.X && Y == pos.Y;
        }

        public override int GetHashCode() {
            return Tuple.Create(X, Y).GetHashCode();
        }
    }
}
