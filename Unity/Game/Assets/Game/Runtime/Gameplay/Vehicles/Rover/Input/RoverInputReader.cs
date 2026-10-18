using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Vehicles.Rover.Input
{
    // todo: replace with a proper .inputactions asset once control remapping is needed
    [DisallowMultipleComponent]
    public sealed class RoverInputReader : MonoBehaviour
    {
        [SerializeField] private VehicleController _controller;

        private bool _loggedKeyboardStatus;
        private VehicleInputSample _lastLoggedSample;

        private void Awake()
        {
            Debug.Log($"[RoverInputReader] Awake on '{name}'. _controller assigned = {_controller != null}.");
        }

        private void Update()
        {
            if (_controller == null)
            {
                Debug.LogError("[RoverInputReader] _controller is NOT ASSIGNED — input will never reach the vehicle.");
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (!_loggedKeyboardStatus)
            {
                Debug.Log($"[RoverInputReader] First Update. Keyboard.current = {(keyboard == null ? "NULL (no keyboard device detected by the Input System)" : keyboard.displayName)}");
                _loggedKeyboardStatus = true;
            }

            if (keyboard == null)
                return;

            float throttle = Axis(keyboard.wKey.isPressed, keyboard.sKey.isPressed);
            float steer = Axis(keyboard.dKey.isPressed, keyboard.aKey.isPressed);
            float pitch = Axis(keyboard.upArrowKey.isPressed, keyboard.downArrowKey.isPressed);
            float roll = Axis(keyboard.rightArrowKey.isPressed, keyboard.leftArrowKey.isPressed);
            float ascend = Axis(keyboard.spaceKey.isPressed, keyboard.leftShiftKey.isPressed);

            var sample = new VehicleInputSample(throttle, steer, pitch, roll, steer, ascend, 0f);

            if (!SameAxes(sample, _lastLoggedSample))
            {
                Debug.Log($"[RoverInputReader] input changed: throttle={throttle}, steer={steer}, pitch={pitch}, roll={roll}, ascend={ascend}");
                _lastLoggedSample = sample;
            }

            _controller.SetInput(sample);

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                Debug.Log("[RoverInputReader] '1' pressed -> EnterHoverMode");
                _controller.EnterHoverMode();
            }
            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("[RoverInputReader] '2' pressed -> EnterFlightMode");
                _controller.EnterFlightMode();
            }
            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                Debug.Log("[RoverInputReader] '3' pressed -> EnterWaterMode");
                _controller.EnterWaterMode();
            }
            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                Debug.Log("[RoverInputReader] '4' pressed -> EnterUnderWaterMode");
                _controller.EnterUnderWaterMode();
            }
        }

        private static bool SameAxes(VehicleInputSample a, VehicleInputSample b)
        {
            return a.Throttle == b.Throttle && a.Steer == b.Steer && a.Pitch == b.Pitch
                && a.Roll == b.Roll && a.Ascend == b.Ascend;
        }

        private static float Axis(bool positive, bool negative)
        {
            float value = 0f;
            if (positive)
                value += 1f;
            if (negative)
                value -= 1f;

            return value;
        }
    }
}
