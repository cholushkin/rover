using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.IslandExploration.Probe
{
    // todo: swap raw device polling for a .inputactions asset once control remapping is needed (mirrors RoverInputReader's own todo)
    [DisallowMultipleComponent]
    public sealed class RoverFlyingProbe : MonoBehaviour
    {
        #region --- Serialization & Properties ---
        [Header("Movement")]
        [field: SerializeField, Min(1f)] public float BaseSpeed { get; private set; } = 40f;
        [field: SerializeField, Min(1f)] public float MinSpeed { get; private set; } = 5f;
        [field: SerializeField, Min(1f)] public float MaxSpeed { get; private set; } = 400f;
        [field: SerializeField, Min(1f)] public float BoostMultiplier { get; private set; } = 4f;
        [field: SerializeField, Min(0.01f)] public float SpeedScrollStep { get; private set; } = 4f;

        [Header("Look")]
        [field: SerializeField, Min(0.01f)] public float MouseSensitivity { get; private set; } = 0.12f;

        public float CurrentSpeed { get; private set; }
        public bool CursorLocked { get; private set; } = true;
        #endregion

        #region --- Private Fields ---
        private float _yaw;
        private float _pitch;
        #endregion

        #region --- Unity Lifecycle ---
        private void Awake()
        {
            CurrentSpeed = BaseSpeed;

            Vector3 startEuler = transform.eulerAngles;
            _yaw = startEuler.y;
            _pitch = startEuler.x;
        }

        private void OnEnable()
        {
            SetCursorLocked(true);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
                SetCursorLocked(!CursorLocked);

            if (CursorLocked)
                TickLook(mouse);

            TickSpeedScroll(mouse);
            TickMovement(keyboard);
        }
        #endregion

        #region --- Implementation ---
        private void TickLook(Mouse mouse)
        {
            if (mouse == null)
                return;

            Vector2 delta = mouse.delta.ReadValue();
            _yaw += delta.x * MouseSensitivity;
            _pitch -= delta.y * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void TickSpeedScroll(Mouse mouse)
        {
            if (mouse == null)
                return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f))
                return;

            float delta = scroll > 0f ? SpeedScrollStep : -SpeedScrollStep;
            BaseSpeed = Mathf.Clamp(BaseSpeed + delta, MinSpeed, MaxSpeed);
        }

        private void TickMovement(Keyboard keyboard)
        {
            float forward = Axis(keyboard.wKey.isPressed, keyboard.sKey.isPressed);
            float right = Axis(keyboard.dKey.isPressed, keyboard.aKey.isPressed);
            float up = Axis(keyboard.spaceKey.isPressed, keyboard.leftCtrlKey.isPressed);

            Vector3 direction = transform.forward * forward + transform.right * right + Vector3.up * up;
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            bool boosting = keyboard.leftShiftKey.isPressed;
            CurrentSpeed = BaseSpeed * (boosting ? BoostMultiplier : 1f);

            transform.position += direction * CurrentSpeed * Time.deltaTime;
        }

        private void SetCursorLocked(bool locked)
        {
            CursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
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
        #endregion
    }
}
