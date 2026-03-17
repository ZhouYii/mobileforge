using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Maps screenId to UI component (page) factories.
    /// Mirrors ScreenRegistry but for the visual layer.
    /// </summary>
    public class UIComponentRegistry
    {
        private readonly Dictionary<string, Func<IUIComponent>> _factories = new();

        public void Register(string screenId, Func<IUIComponent> factory)
        {
            if (string.IsNullOrEmpty(screenId))
                throw new ArgumentException("Screen ID cannot be null or empty.", nameof(screenId));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factories[screenId] = factory;
        }

        public IUIComponent Create(string screenId)
        {
            if (!_factories.TryGetValue(screenId, out var factory))
                return null;

            return factory.Invoke();
        }

        public bool HasComponent(string screenId)
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
    }
}
