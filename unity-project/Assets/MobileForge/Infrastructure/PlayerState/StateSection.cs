using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// A typed section of player state (e.g., currencies, inventory, settings).
    /// Does NOT emit events directly — PlayerState does that.
    /// </summary>
    public class StateSection
    {
        public string Name { get; }
        private readonly Dictionary<string, object> _data;
        private readonly Dictionary<string, List<Action<object, object>>> _watchers = new();

        public StateSection(string name, Dictionary<string, object> initialData = null)
        {
            Name = name;
            _data = initialData != null
                ? new Dictionary<string, object>(initialData)
                : new Dictionary<string, object>();
        }

        public object GetValue(string key, object defaultValue = null)
        {
            return _data.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out var value) && value is T typed)
                return typed;
            return defaultValue;
        }

        /// <summary>Returns the old value.</summary>
        public object SetValue(string key, object value)
        {
            _data.TryGetValue(key, out var old);
            _data[key] = value;
            NotifyWatchers(key, old, value);
            return old;
        }

        public bool HasKey(string key) => _data.ContainsKey(key);

        public bool Erase(string key) => _data.Remove(key);

        public ICollection<string> Keys => _data.Keys;

        public Dictionary<string, object> ToDict() => new Dictionary<string, object>(_data);

        public void FromDict(Dictionary<string, object> data)
        {
            _data.Clear();
            foreach (var kvp in data)
                _data[kvp.Key] = kvp.Value;
        }

        public void Clear() => _data.Clear();

        /// <summary>Watch a specific key for changes. Callback receives (oldValue, newValue).</summary>
        public void Watch(string key, Action<object, object> callback)
        {
            if (!_watchers.TryGetValue(key, out var list))
            {
                list = new List<Action<object, object>>();
                _watchers[key] = list;
            }
            list.Add(callback);
        }

        /// <summary>Remove a watcher for a specific key.</summary>
        public void Unwatch(string key, Action<object, object> callback)
        {
            if (!_watchers.TryGetValue(key, out var list)) return;
            list.Remove(callback);
            if (list.Count == 0) _watchers.Remove(key);
        }

        private void NotifyWatchers(string key, object oldValue, object newValue)
        {
            if (!_watchers.TryGetValue(key, out var list)) return;
            foreach (var cb in list) cb.Invoke(oldValue, newValue);
        }
    }
}
