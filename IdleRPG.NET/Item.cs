using System;

namespace IdleRPG.NET {
    public class Item {
        public string ItemType { get; set; }
        public int Level { get; set; }
        public string Tag { get; set; }
        public DateTime Age { get; set; }

        public static bool operator ==(Item item1, Item item2) {
            if (ReferenceEquals(item1, item2))
                return true;

            if (item1 is null)
                return false;

            if (item2 is null)
                return false;

            return item1.ItemType == item2.ItemType && item1.Level == item2.Level && item1.Tag == item2.Tag && item1.Age == item2.Age;
        }

        public static bool operator !=(Item item1, Item item2) {
            return !(item1 == item2);
        }

        public bool Equals(Item item) {
            if (item is null)
                return false;

            if (ReferenceEquals(this, item))
                return true;

            return ItemType.Equals(item.ItemType) && Level.Equals(item.Level) && Tag.Equals(item.Tag) && Age.Equals(item.Age);
        }

        public override bool Equals(object obj) {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            return obj.GetType() == GetType() && Equals((Item)obj);
        }

        public override int GetHashCode() {
            return Tuple.Create(ItemType, Level, Tag, Age).GetHashCode();
        }
    }
}
