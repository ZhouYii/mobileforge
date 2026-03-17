using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Simple pub/sub event system. Uses string event names with Dictionary payloads.
    /// Can be used as a plain C# singleton (for testing) or wrapped in MonoBehaviour.
    /// </summary>
    public class EventBus
    {
        private readonly Dictionary<string, List<Action<Dictionary<string, object>>>> _listeners = new();

        private static EventBus _instance;
        public static EventBus Instance => _instance ??= new EventBus();

        // For testing: reset singleton
        public static void ResetInstance() { _instance = null; }
        public static void SetInstance(EventBus bus) { _instance = bus; }

        public void Subscribe(string eventName, Action<Dictionary<string, object>> callback)
        {
            if (!_listeners.ContainsKey(eventName))
                _listeners[eventName] = new List<Action<Dictionary<string, object>>>();

            if (!_listeners[eventName].Contains(callback))
                _listeners[eventName].Add(callback);
        }

        public void Unsubscribe(string eventName, Action<Dictionary<string, object>> callback)
        {
            if (_listeners.TryGetValue(eventName, out var list))
            {
                list.Remove(callback);
                if (list.Count == 0)
                    _listeners.Remove(eventName);
            }
        }

        public void Emit(string eventName, Dictionary<string, object> payload = null)
        {
            payload ??= new Dictionary<string, object>();

            if (!_listeners.TryGetValue(eventName, out var list)) return;

            // Iterate copy to allow unsubscribe during emit
            var copy = new List<Action<Dictionary<string, object>>>(list);
            foreach (var callback in copy)
                callback.Invoke(payload);
        }

        public int SubscriberCount(string eventName)
        {
            return _listeners.TryGetValue(eventName, out var list) ? list.Count : 0;
        }

        public void ClearAll()
        {
            _listeners.Clear();
        }
    }
}
