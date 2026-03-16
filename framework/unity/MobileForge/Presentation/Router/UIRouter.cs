using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Navigation manager with a screen stack. Manages IScreen lifecycle transitions.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class UIRouter
    {
        private readonly ScreenRegistry _registry;
        private readonly List<ScreenEntry> _stack = new();

        /// <summary>
        /// Optional callback fired on every navigation event.
        /// Parameters: (screenId, parameters).
        /// </summary>
        public Action<string, Dictionary<string, object>> OnNavigated;

        public string CurrentScreenId => _stack.Count > 0 ? _stack[_stack.Count - 1].ScreenId : null;
        public int StackDepth => _stack.Count;

        public UIRouter(ScreenRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Convenience: register a screen factory directly through the router.
        /// </summary>
        public void Register(string screenId, Func<Dictionary<string, object>, object> factory)
        {
            _registry.Register(screenId, factory);
        }

        /// <summary>
        /// Navigate to a screen, clearing the entire stack.
        /// </summary>
        public void Navigate(string screenId, Dictionary<string, object> parameters = null)
        {
            parameters ??= new Dictionary<string, object>();

            // Exit all screens in reverse order
            for (int i = _stack.Count - 1; i >= 0; i--)
                _stack[i].Screen?.OnExit();

            _stack.Clear();

            var screen = CreateScreen(screenId, parameters);
            _stack.Add(new ScreenEntry(screenId, screen));
            screen?.OnEnter(parameters);

            OnNavigated?.Invoke(screenId, parameters);
        }

        /// <summary>
        /// Push a new screen onto the stack. Pauses the current screen.
        /// </summary>
        public void Push(string screenId, Dictionary<string, object> parameters = null)
        {
            parameters ??= new Dictionary<string, object>();

            // Pause current
            if (_stack.Count > 0)
                _stack[_stack.Count - 1].Screen?.OnPause();

            var screen = CreateScreen(screenId, parameters);
            _stack.Add(new ScreenEntry(screenId, screen));
            screen?.OnEnter(parameters);

            OnNavigated?.Invoke(screenId, parameters);
        }

        /// <summary>
        /// Pop the top screen. Resumes the screen below it.
        /// Returns false if the stack has one or fewer screens.
        /// </summary>
        public bool Pop()
        {
            if (_stack.Count <= 1)
                return false;

            var top = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            top.Screen?.OnExit();

            // Resume previous
            var current = _stack[_stack.Count - 1];
            current.Screen?.OnResume();

            OnNavigated?.Invoke(current.ScreenId, null);
            return true;
        }

        /// <summary>
        /// Replace the top screen with a new one.
        /// </summary>
        public void Replace(string screenId, Dictionary<string, object> parameters = null)
        {
            parameters ??= new Dictionary<string, object>();

            if (_stack.Count > 0)
            {
                var top = _stack[_stack.Count - 1];
                _stack.RemoveAt(_stack.Count - 1);
                top.Screen?.OnExit();
            }

            var screen = CreateScreen(screenId, parameters);
            _stack.Add(new ScreenEntry(screenId, screen));
            screen?.OnEnter(parameters);

            OnNavigated?.Invoke(screenId, parameters);
        }

        /// <summary>
        /// Get the screen at a specific stack depth (0 = bottom).
        /// </summary>
        public IScreen GetScreenAt(int index)
        {
            if (index < 0 || index >= _stack.Count)
                return null;
            return _stack[index].Screen;
        }

        /// <summary>
        /// Clear the entire stack, calling OnExit on every screen.
        /// </summary>
        public void ClearAll()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
                _stack[i].Screen?.OnExit();
            _stack.Clear();
        }

        private IScreen CreateScreen(string screenId, Dictionary<string, object> parameters)
        {
            var obj = _registry.Create(screenId, parameters);
            return obj as IScreen;
        }

        private struct ScreenEntry
        {
            public string ScreenId;
            public IScreen Screen;

            public ScreenEntry(string screenId, IScreen screen)
            {
                ScreenId = screenId;
                Screen = screen;
            }
        }
    }
}
