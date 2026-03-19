using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Base type for all game definitions loaded from JSON.
    /// Wraps a Dictionary for flexible field access.
    /// </summary>
    public class Definition
    {
        public int Id { get; }
        private readonly Dictionary<string, object> _data;

        public Definition(Dictionary<string, object> data)
        {
            _data = data ?? new Dictionary<string, object>();
            Id = GetInt("id");
        }

        public object GetField(string key, object defaultValue = null)
        {
            return _data.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is int i) return i;
                if (value is long l) return (int)l;
                if (value is double d) return (int)d;
                if (value is float f) return (int)f;
                if (int.TryParse(value?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is float f) return f;
                if (value is double d) return (float)d;
                if (value is int i) return i;
                if (value is long l) return l;
                if (float.TryParse(value?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
        }

        public string GetString(string key, string defaultValue = "")
        {
            if (_data.TryGetValue(key, out var value))
                return value?.ToString() ?? defaultValue;
            return defaultValue;
        }

        public List<object> GetArray(string key, List<object> defaultValue = null)
        {
            if (!_data.TryGetValue(key, out var value))
                return defaultValue ?? new List<object>();

            if (value is List<object> list)
                return list;

            // Handle Newtonsoft JArray (common when deserializing nested JSON)
            if (value is Newtonsoft.Json.Linq.JArray jArr)
                return jArr.ToObject<List<object>>();

            // Handle any IEnumerable
            if (value is System.Collections.IEnumerable enumerable)
            {
                var result = new List<object>();
                foreach (var item in enumerable)
                    result.Add(item);
                return result;
            }

            return defaultValue ?? new List<object>();
        }

        public Dictionary<string, object> GetDict(string key)
        {
            if (!_data.TryGetValue(key, out var value))
                return new Dictionary<string, object>();

            if (value is Dictionary<string, object> dict)
                return dict;

            // Handle Newtonsoft JObject
            if (value is Newtonsoft.Json.Linq.JObject jObj)
                return jObj.ToObject<Dictionary<string, object>>();

            return new Dictionary<string, object>();
        }

        public bool HasField(string key) => _data.ContainsKey(key);

        public Dictionary<string, object> Raw() => new Dictionary<string, object>(_data);
    }
}
