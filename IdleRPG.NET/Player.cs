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
    }
}
