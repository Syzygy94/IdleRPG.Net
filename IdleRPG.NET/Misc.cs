using System;
using System.Security.Cryptography;

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

    public static class Random {
        /// <summary>
        /// Returns a non-negative random integer.
        /// </summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than System.Int32.MaxValue.</returns>
        public static int Next() {
            return Next(0, Int32.MaxValue);
        }

        /// <summary>
        /// Returns a non-negative random integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. maxValue must
        /// be greater than or equal to 0.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than maxValue;
        /// that is, the range of return values ordinarily includes 0 but not maxValue. However,
        /// if maxValue equals 0, maxValue is returned.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">maxValue is less than 0.</exception>
        public static int Next(int maxValue) {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException("maxValue", "maxValue needs to be greater than 0");
            if (maxValue == 0)
                return maxValue;

            return Next(0, maxValue);
        }

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. maxValue must be greater
        /// than or equal to minValue.</param>
        /// <returns>A 32-bit signed integer greater than or equal to minValue and less than maxValue;
        /// that is, the range of return values includes minValue but not maxValue. If minValue
        /// equals maxValue, minValue is returned.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">minValue is greater than maxValue.</exception>
        public static int Next(int minValue, int maxValue) {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException("minValue", "minValue needs to be less than or equal to maxValue");
            if (minValue == maxValue)
                return minValue;

            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[4];
            rng.GetBytes(buffer);
            int seed = BitConverter.ToInt32(buffer, 0);
            long tick = DateTime.Now.Ticks + seed;
            return new System.Random((int)(tick & 0xffffffffL) | (int)(tick >> 32)).Next(minValue, maxValue);
        }
    }
}
