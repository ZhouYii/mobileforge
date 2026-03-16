using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Central player state container with named sections.
    /// Emits events via EventBus on state changes.
    /// </summary>
    public class PlayerState
    {
        private readonly Dictionary<string, StateSection> _sections = new();
        private readonly EventBus _eventBus;

        private static PlayerState _instance;
        public static PlayerState Instance => _instance ??= new PlayerState(EventBus.Instance);
        public static void ResetInstance() { _instance = null; }
        public static void SetInstance(PlayerState state) { _instance = state; }

        public PlayerState(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public StateSection RegisterSection(string name, Dictionary<string, object> initialData = null)
        {
            if (_sections.TryGetValue(name, out var existing))
                return existing;

            var section = new StateSection(name, initialData);
            _sections[name] = section;
            return section;
        }

        public StateSection GetSection(string name)
        {
            return _sections.TryGetValue(name, out var section) ? section : null;
        }

        public bool HasSection(string name) => _sections.ContainsKey(name);

        public void SetValue(string sectionName, string key, object value)
        {
            var section = GetSection(sectionName);
            if (section == null)
            {
                System.Diagnostics.Debug.WriteLine($"PlayerState: section '{sectionName}' not registered.");
                return;
            }

            var oldValue = section.SetValue(key, value);

            _eventBus?.Emit(EventNames.StateChanged, new Dictionary<string, object>
            {
                { "section", sectionName },
                { "key", key },
                { "old_value", oldValue },
                { "new_value", value }
            });
        }

        public object GetValue(string sectionName, string key, object defaultValue = null)
        {
            var section = GetSection(sectionName);
            return section?.GetValue(key, defaultValue) ?? defaultValue;
        }

        public Dictionary<string, Dictionary<string, object>> ToSaveDict()
        {
            var result = new Dictionary<string, Dictionary<string, object>>();
            foreach (var kvp in _sections)
                result[kvp.Key] = kvp.Value.ToDict();
            return result;
        }

        public void FromSaveDict(Dictionary<string, Dictionary<string, object>> data)
        {
            foreach (var kvp in data)
            {
                if (_sections.TryGetValue(kvp.Key, out var section))
                    section.FromDict(kvp.Value);
                else
                    RegisterSection(kvp.Key, kvp.Value);
            }

            _eventBus?.Emit(EventNames.StateLoaded);
        }

        public void ClearAll()
        {
            _sections.Clear();
        }
    }
}
