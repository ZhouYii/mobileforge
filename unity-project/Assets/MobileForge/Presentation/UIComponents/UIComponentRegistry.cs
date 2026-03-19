using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        /// <summary>
        /// Register a screen with orientation-aware prefab variants.
        /// Pass both paths to allow rotation; pass only one to lock orientation.
        /// </summary>
        public void RegisterWithOrientation(string screenId,
            string portraitPrefabPath, string landscapePrefabPath = null)
        {
            if (string.IsNullOrEmpty(screenId))
                throw new ArgumentException("Screen ID cannot be null or empty.", nameof(screenId));
            if (string.IsNullOrEmpty(portraitPrefabPath) && string.IsNullOrEmpty(landscapePrefabPath))
                throw new ArgumentException("At least one prefab path must be provided.");

            Register(screenId, () =>
            {
                bool isLandscape = Screen.width >= Screen.height;
                string path;
                if (isLandscape && !string.IsNullOrEmpty(landscapePrefabPath))
                    path = landscapePrefabPath;
                else if (!string.IsNullOrEmpty(portraitPrefabPath))
                    path = portraitPrefabPath;
                else
                    path = landscapePrefabPath;

                var prefab = Resources.Load<MonoBehaviour>(path);
                if (prefab != null)
                    return UnityEngine.Object.Instantiate(prefab) as IUIComponent;

                Debug.LogWarning($"[UIComponentRegistry] Prefab not found at '{path}'");
                return null;
            });

            // Lock orientation if only one variant provided
            if (!string.IsNullOrEmpty(portraitPrefabPath) && string.IsNullOrEmpty(landscapePrefabPath))
                Screen.orientation = ScreenOrientation.Portrait;
            else if (string.IsNullOrEmpty(portraitPrefabPath) && !string.IsNullOrEmpty(landscapePrefabPath))
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            else
                Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
