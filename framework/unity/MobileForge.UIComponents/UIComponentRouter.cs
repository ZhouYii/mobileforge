using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;

namespace MobileForge.UIComponents
{
    /// <summary>
    /// Subscribes to UIRouter.OnNavigated and instantiates the registered page for each screen.
    /// Replaces the monolithic GameRenderer switch statement.
    /// </summary>
    public class UIComponentRouter : MonoBehaviour
    {
        private UIComponentRegistry _registry;
        private UIRouter _uiRouter;
        private IUIComponent _currentComponent;
        private Transform _screenRoot;

        public IUIComponent CurrentComponent => _currentComponent;

        /// <summary>
        /// Optional callback fired after a page is rendered. Used by the game layer
        /// to notify external systems (e.g., AutomationBridge for WebGL).
        /// </summary>
        public Action<string> OnScreenRendered;

        /// <summary>
        /// Initialize the router. Call once during bootstrap.
        /// </summary>
        public void Setup(UIRouter uiRouter, UIComponentRegistry registry, Transform root)
        {
            _uiRouter = uiRouter;
            _registry = registry;
            _screenRoot = root;
            _uiRouter.OnNavigated += OnScreenChanged;
        }

        void OnDestroy()
        {
            if (_uiRouter != null)
                _uiRouter.OnNavigated -= OnScreenChanged;
        }

        private void OnScreenChanged(string screenId, Dictionary<string, object> parameters)
        {
            Debug.Log($"[UIComponentRouter] Navigated to: {screenId}");

            // Unmount current page
            _currentComponent?.Unmount();
            _currentComponent = null;

            // Create and bind the new page
            var component = _registry.Create(screenId);
            if (component == null)
            {
                Debug.LogWarning($"[UIComponentRouter] No page registered for '{screenId}'");
                return;
            }

            // Get the screen from the router stack
            var screen = _uiRouter.GetScreenAt(_uiRouter.StackDepth - 1);
            if (screen != null)
                component.BindUntyped(screen);

            component.Mount(_screenRoot);
            _currentComponent = component;

            OnScreenRendered?.Invoke(screenId);
        }
    }
}
