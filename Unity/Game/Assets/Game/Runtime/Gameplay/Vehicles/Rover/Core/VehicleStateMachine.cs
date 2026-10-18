using System;
using System.Collections.Generic;
using Game.Gameplay.Vehicles.Rover.States;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Core
{
    public sealed class VehicleStateMachine
    {
        public event Action<VehicleStateId> StateChanged;

        public VehicleStateId CurrentStateId => _current.Id;

        private readonly Dictionary<VehicleStateId, IVehicleState> _states;
        private readonly VehicleRuntimeContext _context;

        private IVehicleState _current;
        private VehicleStateId? _pendingTransition;

        public VehicleStateMachine(VehicleRuntimeContext context, IVehicleState[] states, VehicleStateId initial)
        {
            _context = context;
            _states = new Dictionary<VehicleStateId, IVehicleState>(states.Length);
            foreach (IVehicleState state in states)
            {
                _states[state.Id] = state;
                Debug.Log($"[VehicleStateMachine] Registered state {state.Id}.");
            }

            _current = _states[initial];
            Debug.Log($"[VehicleStateMachine] Initial state = {initial}. Calling Enter...");
            _current.Enter(_context);
        }

        public bool RequestTransition(VehicleStateId next)
        {
            if (next == _current.Id)
            {
                Debug.Log($"[VehicleStateMachine] RequestTransition({next}) ignored — already in that state.");
                return false;
            }
            if (!_states.ContainsKey(next))
            {
                Debug.LogError($"[VehicleStateMachine] RequestTransition({next}) — no state registered for that id!");
                return false;
            }
            if (!_current.CanTransitionTo(next, _context))
            {
                Debug.Log($"[VehicleStateMachine] Rejected transition {_current.Id} -> {next}");
                return false;
            }

            Debug.Log($"[VehicleStateMachine] Transition accepted {_current.Id} -> {next} (applies on next FixedTick).");
            _pendingTransition = next;
            return true;
        }

        public void Tick(float deltaTime) => _current.Tick(_context, deltaTime);

        public void FixedTick(float fixedDeltaTime)
        {
            ApplyPendingTransition();
            _current.FixedTick(_context, fixedDeltaTime);
        }

        private void ApplyPendingTransition()
        {
            if (_pendingTransition is not { } next)
                return;

            _current.Exit(_context);
            _current = _states[next];
            _current.Enter(_context);
            _pendingTransition = null;

            StateChanged?.Invoke(next);
        }
    }
}
