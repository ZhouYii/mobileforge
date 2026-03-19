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
        private MFScreenTransition _transition;

        public IUIComponent CurrentComponent => _currentComponent;

        /// <summary>
        /// Initialize the router. Call once during bootstrap.
        /// </summary>
        public void Setup(UIRouter uiRouter, UIComponentRegistry registry, Transform root,
            float transitionDuration = 0.25f)
        {
            _uiRouter = uiRouter;
            _registry = registry;
            _screenRoot = root;
            _uiRouter.OnNavigated += OnScreenChanged;

            // Create transition overlay (sits above pages in the same root's parent)
            var transGo = new GameObject("ScreenTransition");
            transGo.transform.SetParent(root.parent, false);
            _transition = transGo.AddComponent<MFScreenTransition>();
            _transition.FadeDuration = transitionDuration;
        }

        void OnDestroy()
        {
            if (_uiRouter != null)
                _uiRouter.OnNavigated -= OnScreenChanged;
        }

        private void OnScreenChanged(string screenId, Dictionary<string, object> parameters)
        {
            Debug.Log($"[UIComponentRouter] Navigated to: {screenId}");

            // Use transition if we have an existing page, otherwise swap immediately
            if (_currentComponent != null && _transition != null)
            {
                _transition.DoTransition(() => SwapPage(screenId));
            }
            else
            {
                SwapPage(screenId);
                // Fade out from black on first page load
                _transition?.FadeOut();
            }

            NotifyBridge(screenId);
        }

        private void SwapPage(string screenId)
        {
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
        }

        private void NotifyBridge(string screenId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AutomationBridge.NotifyScreenChanged(screenId);
#endif
        }
    }
}
