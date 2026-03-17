using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Flat state machine with callable-based states and guarded transitions.
    /// Complement to HSM — no hierarchy, just states + guards.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class LightFSM
    {
        private readonly Dictionary<string, StateEntry> _states = new();
        private readonly List<TransitionEntry> _transitions = new();
        private string _current;

        public event Action<string> StateEntered;
        public event Action<string> StateExited;

        /// <summary>Add a state with optional lifecycle callbacks.</summary>
        public void AddState(string id, Action onEnter = null, Action onExit = null, Action<float> onUpdate = null)
        {
            _states[id] = new StateEntry { OnEnter = onEnter, OnExit = onExit, OnUpdate = onUpdate };
        }

        /// <summary>Add a guarded transition. Null guard = always allowed.</summary>
        public void AddTransition(string from, string to, Func<bool> guard = null)
        {
            _transitions.Add(new TransitionEntry { From = from, To = to, Guard = guard });
        }

        /// <summary>Attempt to transition. Returns false if guard rejects or state doesn't exist.</summary>
        public bool Transition(string to)
        {
            if (!_states.ContainsKey(to)) return false;

            // Check guard
            if (_current != null)
            {
                foreach (var t in _transitions)
                {
                    if (t.From == _current && t.To == to)
                    {
                        if (t.Guard != null && !t.Guard())
                            return false;
                        break;
                    }
                }
            }

            // Exit current
            if (_current != null && _states.TryGetValue(_current, out var currentState))
            {
                currentState.OnExit?.Invoke();
                StateExited?.Invoke(_current);
            }

            // Enter new
            _current = to;
            _states[to].OnEnter?.Invoke();
            StateEntered?.Invoke(to);
            return true;
        }

        /// <summary>Tick the current state's OnUpdate.</summary>
        public void Update(float delta)
        {
            if (_current != null && _states.TryGetValue(_current, out var state))
                state.OnUpdate?.Invoke(delta);
        }

        /// <summary>Get the current state ID, or null.</summary>
        public string CurrentState => _current;

        /// <summary>Check if a transition to target is allowed.</summary>
        public bool CanTransition(string to)
        {
            if (!_states.ContainsKey(to)) return false;
            foreach (var t in _transitions)
            {
                if (t.From == _current && t.To == to)
                    return t.Guard == null || t.Guard();
            }
            return true;
        }

        /// <summary>Reset to no state (calls OnExit on current).</summary>
        public void Reset()
        {
            if (_current != null && _states.TryGetValue(_current, out var state))
            {
                state.OnExit?.Invoke();
                StateExited?.Invoke(_current);
            }
            _current = null;
        }

        private class StateEntry
        {
            public Action OnEnter;
            public Action OnExit;
            public Action<float> OnUpdate;
        }

        private struct TransitionEntry
        {
            public string From;
            public string To;
            public Func<bool> Guard;
        }
    }
}
