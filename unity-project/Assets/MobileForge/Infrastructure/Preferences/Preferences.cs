using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Lightweight key-value preferences store. Survives save wipes.
    /// Storage backend is injected (Unity wires to PlayerPrefs).
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class Preferences
    {
        private readonly Dictionary<string, string> _cache = new();

        /// <summary>Fired when a preference changes. Args: key, value.</summary>
        public event Action<string, string> OnPreferenceChanged;

        /// <summary>Injected write backend: (key, value) -> void.</summary>
        public Action<string, string> WriteBackend { get; set; }

        /// <summary>Injected read backend: (key) -> value or null.</summary>
        public Func<string, string> ReadBackend { get; set; }

        /// <summary>Injected delete backend: (key) -> void.</summary>
        public Action<string> DeleteBackend { get; set; }

        /// <summary>Injected key listing backend: () -> all keys.</summary>
        public Func<IEnumerable<string>> ListKeysBackend { get; set; }

        /// <summary>Set a preference value.</summary>
        public void SetPref(string key, string value)
        {
            _cache[key] = value;
            WriteBackend?.Invoke(key, value);
            OnPreferenceChanged?.Invoke(key, value);
        }

        /// <summary>Set a typed preference (stored as string).</summary>
        public void SetPref<T>(string key, T value)
        {
            SetPref(key, value?.ToString() ?? "");
        }

        /// <summary>Get a preference value, or default if not set.</summary>
        public string GetPref(string key, string defaultValue = null)
        {
            if (_cache.TryGetValue(key, out var val)) return val;
            var stored = ReadBackend?.Invoke(key);
            if (stored != null)
            {
                _cache[key] = stored;
                return stored;
            }
            return defaultValue;
        }

        /// <summary>Get a typed preference.</summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            var s = GetPref(key);
            return s != null && int.TryParse(s, out var v) ? v : defaultValue;
        }

        /// <summary>Get a typed preference.</summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            var s = GetPref(key);
            return s != null && float.TryParse(s, out var v) ? v : defaultValue;
        }

        /// <summary>Get a typed preference.</summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            var s = GetPref(key);
            return s != null && bool.TryParse(s, out var v) ? v : defaultValue;
        }

        /// <summary>Check if a preference exists.</summary>
        public bool HasPref(string key) =>
            _cache.ContainsKey(key) || ReadBackend?.Invoke(key) != null;

        /// <summary>Delete a preference.</summary>
        public void DeletePref(string key)
        {
            _cache.Remove(key);
            DeleteBackend?.Invoke(key);
        }

        /// <summary>Load all keys from backend into cache.</summary>
        public void LoadAll()
        {
            if (ListKeysBackend == null) return;
            _cache.Clear();
            foreach (var key in ListKeysBackend())
            {
                var val = ReadBackend?.Invoke(key);
                if (val != null) _cache[key] = val;
            }
        }
    }
}
