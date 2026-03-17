using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Registry of screen factories keyed by screen ID.
    /// Screens are created on-demand via factory functions.
    /// </summary>
    public class ScreenRegistry
    {
        private readonly Dictionary<string, Func<Dictionary<string, object>, object>> _factories = new();

        public void Register(string screenId, Func<Dictionary<string, object>, object> factory)
        {
            if (string.IsNullOrEmpty(screenId))
                throw new ArgumentException("Screen ID cannot be null or empty.", nameof(screenId));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factories[screenId] = factory;
        }

        public object Create(string screenId, Dictionary<string, object> parameters = null)
        {
            if (!_factories.TryGetValue(screenId, out var factory))
                throw new KeyNotFoundException($"Screen '{screenId}' is not registered.");

            return factory.Invoke(parameters ?? new Dictionary<string, object>());
        }

        public bool HasScreen(string screenId)
        {
            return _factories.ContainsKey(screenId);
        }

        public List<string> GetRegisteredIds()
        {
            return _factories.Keys.ToList();
        }

        public void Unregister(string screenId)
        {
            _factories.Remove(screenId);
        }

        public void Clear()
        {
            _factories.Clear();
        }
    }
}
