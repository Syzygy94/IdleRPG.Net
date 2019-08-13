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
            if (ReferenceEquals(pos1, pos2))
                return true;

            if (pos1 is null)
                return false;

            if (pos2 is null)
                return false;

            return pos1.X == pos2.X && pos1.Y == pos2.Y;
        }

        public static bool operator !=(Pos pos1, Pos pos2) {
            return !(pos1 == pos2);
        }

        public bool Equals(Pos pos) {
            if (pos is null)
                return false;

            if (ReferenceEquals(this, pos))
                return true;

            return X.Equals(pos.X) && Y.Equals(pos.Y);
        }

        public override bool Equals(object obj) {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return obj.GetType() == GetType() && Equals((Pos)obj);
        }

        public override int GetHashCode() {
            return Tuple.Create(X, Y).GetHashCode();
        }
    }
}
