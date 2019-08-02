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
        public int Idled { get; set; }
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

            Pos = new Pos(0, 0);
            CreateTime = DateTime.Now;
            LastLogin = DateTime.Now;
        }

        public static bool operator ==(Player p1, Player p2) {
            return p1.Name == p2.Name && p1.Class == p2.Class && p1.Level == p2.Level && p1.UHost == p2.UHost && p1.Pos == p2.Pos;
        }

        public static bool operator !=(Player p1, Player p2) {
            return p1.Name != p2.Name || p1.Class != p2.Class || p1.Level != p2.Level || p1.UHost != p2.UHost || p1.Pos == p2.Pos;
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is Player))
                return false;

            Player p = (Player)obj;
            return Name == p.Name && Class == p.Class && Level == p.Level && UHost == p.UHost && Pos == p.Pos;
        }

        public override int GetHashCode() =>
            HashCode.Combine(Name, Class, Level, UHost, Pos);
    }
}
