using System;
using System.Collections.Generic;
using System.IO;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Loads and provides read-only access to JSON game definitions.
    /// Uses System.Text.Json or a simple JSON parser (Unity-compatible).
    /// </summary>
    public class GameData
    {
        private readonly Dictionary<string, Dictionary<int, Definition>> _definitions = new();

        private static GameData _instance;
        public static GameData Instance => _instance ??= new GameData();
        public static void ResetInstance() { _instance = null; }
        public static void SetInstance(GameData data) { _instance = data; }

        /// <summary>
        /// Load definitions from a JSON string (already read from file).
        /// Expects a JSON array of objects, each with an "id" field.
        /// </summary>
        public bool LoadDefinitionsFromJson(string type, string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent)) return false;

            try
            {
                // Use Unity's JsonUtility or Newtonsoft — for portability,
                // we use a simple approach with MiniJSON or similar
                // For now, we accept pre-parsed data
                return false; // Placeholder — see LoadDefinitions(string, List<Dictionary>)
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Load definitions from pre-parsed dictionaries.
        /// This is the primary loading method — JSON parsing is handled externally.
        /// </summary>
        public void LoadDefinitions(string type, List<Dictionary<string, object>> entries)
        {
            if (!_definitions.ContainsKey(type))
                _definitions[type] = new Dictionary<int, Definition>();

            var lookup = _definitions[type];
            foreach (var entry in entries)
            {
                var def = new Definition(entry);
                lookup[def.Id] = def;
            }
        }

        public Definition GetDefinition(string type, int id)
        {
            if (_definitions.TryGetValue(type, out var lookup) && lookup.TryGetValue(id, out var def))
                return def;
            return null;
        }

        public List<Definition> GetAllDefinitions(string type)
        {
            if (_definitions.TryGetValue(type, out var lookup))
                return new List<Definition>(lookup.Values);
            return new List<Definition>();
        }

        public bool HasDefinition(string type, int id)
        {
            return _definitions.TryGetValue(type, out var lookup) && lookup.ContainsKey(id);
        }

        public int GetDefinitionCount(string type)
        {
            return _definitions.TryGetValue(type, out var lookup) ? lookup.Count : 0;
        }

        public void ClearType(string type)
        {
            _definitions.Remove(type);
        }

        public void ClearAll()
        {
            _definitions.Clear();
        }
    }
}
