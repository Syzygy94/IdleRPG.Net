using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleRPG.NET {
    public class DictionaryComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>> {
        private IEqualityComparer<TValue> valueComparer;

        public DictionaryComparer(IEqualityComparer<TValue> valueComparer = null) {
            this.valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
        }

        public bool Equals(Dictionary<TKey, TValue> x, Dictionary<TKey, TValue> y) {
            if (x.Count != y.Count)
                return false;
            if (x.Keys.Except(y.Keys).Any())
                return false;
            if (y.Keys.Except(x.Keys).Any())
                return false;
            foreach (var pair in x)
                if (!valueComparer.Equals(pair.Value, y[pair.Key]))
                    return false;
            return true;
        }

        public int GetHashCode(Dictionary<TKey, TValue> obj) {
            throw new NotImplementedException();
        }
    }

    public class ListComparer<T> : IEqualityComparer<List<T>> {
        private IEqualityComparer<T> valueComparer;

        public ListComparer(IEqualityComparer<T> valueComparer = null) {
            this.valueComparer = valueComparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(List<T> x, List<T> y) {
            return x.SetEquals(y, valueComparer);
        }

        public int GetHashCode(List<T> obj) {
            throw new NotImplementedException();
        }
    }
}
