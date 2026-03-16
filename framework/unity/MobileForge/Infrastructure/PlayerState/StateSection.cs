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
    }
}
