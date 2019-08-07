using System;
using System.Collections.Generic;
using System.Text;

namespace IdleRPG.NET {
    public class Player {
        public string Name { get; set; }
        public string Password { get; set; }
        public bool Admin { get; set; }
        public string Class { get; set; }
        public Dictionary<string, Item> Items { get; set; }
        public int IdleTime { get; set; }
        public int Level { get; set; }
        public int TTL { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastLogin { get; set; }
        public int LastFight { get; set; }
        public string Align { get; set; }
        public string Nick { get; set; }
        public string UHost { get; set; }
        public bool Online { get; set; }
        public Pos Pos { get; set; }
        public Dictionary<string, int> Penalties { get; set; }

        public Player() {
            Items = new Dictionary<string, Item>();
            Penalties = new Dictionary<string, int>();

            foreach (string item in World.Items)
                Items[item] = new Item() { ItemType = item, Level = 0, Tag = string.Empty };

            foreach (string penalty in World.Penalties)
                Penalties[penalty] = 0;

            TTL = Config.RPBase;
            Pos = new Pos(Random.Next(Config.MapX), Random.Next(Config.MapY));
            Align = "n";
            Admin = false;
            Online = true;
            CreateTime = DateTime.Now;
            LastLogin = DateTime.Now;
        }

        public static bool operator ==(Player p1, Player p2) {
            if (ReferenceEquals(p1, p2))
                return true;

            if (p1 is null)
                return false;

            if (p2 is null)
                return false;

            return p1.Nick == p2.Nick && p1.Name == p2.Name && p1.Class == p2.Class && p1.Level == p2.Level && p1.UHost == p2.UHost && p1.Pos == p2.Pos;
        }

        public static bool operator !=(Player p1, Player p2) {
            return !(p1 == p2);
        }

        public bool Equals(Player p) {
            if (p is null)
                return false;

            if (ReferenceEquals(this, p))
                return true;

            return Nick.Equals(p.Nick) && Name.Equals(p.Name) && Class.Equals(p.Class) &&
                Level.Equals(p.Level) && UHost.Equals(p.UHost) && Pos.Equals(p.Pos);
        }

        public override bool Equals(object obj) {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            return obj.GetType() == GetType() && Equals((Player)obj);
        }

        public override int GetHashCode() =>
            HashCode.Combine(Nick, Name, Class, Level, UHost, Pos);
    }
}
