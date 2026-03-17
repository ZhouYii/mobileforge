using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Hierarchical (nested) finite state machine.
    /// Each state can contain a child HSM for nested sub-states.
    /// </summary>
    public class HierarchicalStateMachine
    {
        private readonly Dictionary<string, StateEntry> _states = new();
        private string _currentState = "";

        public string CurrentState => _currentState;

        /// <summary>Add a state with optional enter/exit/update callbacks.</summary>
        public void AddState(string stateId, Action onEnter = null, Action onExit = null, Action<float> onUpdate = null)
        {
            _states[stateId] = new StateEntry
            {
                OnEnter = onEnter, OnExit = onExit, OnUpdate = onUpdate,
            };
        }

        /// <summary>Add a child HSM to a state for nested sub-states.</summary>
        public HierarchicalStateMachine AddChildHsm(string stateId)
        {
            if (!_states.TryGetValue(stateId, out var state)) return null;
            var child = new HierarchicalStateMachine();
            state.ChildHsm = child;
            return child;
        }

        /// <summary>Transition to a state.</summary>
        public void Transition(string stateId)
        {
            if (!_states.ContainsKey(stateId)) return;
            if (_currentState != "" && _states.TryGetValue(_currentState, out var old))
            {
                old.ChildHsm?.ExitCurrent();
                old.OnExit?.Invoke();
            }
            _currentState = stateId;
            _states[stateId].OnEnter?.Invoke();
        }

        /// <summary>Update current state and child HSM.</summary>
        public void Update(float delta)
        {
            if (_currentState == "" || !_states.TryGetValue(_currentState, out var state)) return;
            state.OnUpdate?.Invoke(delta);
            state.ChildHsm?.Update(delta);
        }

        /// <summary>Get the full state path (e.g. "combat/attacking/melee").</summary>
        public string GetStatePath()
        {
            if (_currentState == "") return "";
            var path = _currentState;
            if (_states.TryGetValue(_currentState, out var state) && state.ChildHsm != null)
            {
                var childPath = state.ChildHsm.GetStatePath();
                if (childPath != "") path += "/" + childPath;
            }
            return path;
        }

        internal void ExitCurrent()
        {
            if (_currentState == "" || !_states.TryGetValue(_currentState, out var state)) return;
            state.ChildHsm?.ExitCurrent();
            state.OnExit?.Invoke();
            _currentState = "";
        }

        private class StateEntry
        {
            public Action OnEnter;
            public Action OnExit;
            public Action<float> OnUpdate;
            public HierarchicalStateMachine ChildHsm;
        }
    }
}
