using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Presentation
{
    // todo: replace the flat color tint with real wing/ballast meshes once art exists
    [DisallowMultipleComponent]
    public sealed class VehicleVisualController : MonoBehaviour
    {
        [SerializeField] private VehicleController _controller;
        [SerializeField] private Renderer _hullRenderer;

        [SerializeField] private Color _hoverColor = Color.white;
        [SerializeField] private Color _flightColor = Color.cyan;
        [SerializeField] private Color _waterColor = new(0.2f, 0.5f, 1f);
        [SerializeField] private Color _underWaterColor = new(0.05f, 0.15f, 0.4f);

        private MaterialPropertyBlock _propertyBlock;

        private void Awake() => _propertyBlock = new MaterialPropertyBlock();

        private void OnEnable()
        {
            if (_controller == null)
                return;

            _controller.StateChanged += OnStateChanged;
            OnStateChanged(_controller.CurrentStateId);
        }

        private void OnDisable()
        {
            if (_controller != null)
                _controller.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(VehicleStateId state)
        {
            if (_hullRenderer == null)
                return;

            Color color = state switch
            {
                VehicleStateId.Hover => _hoverColor,
                VehicleStateId.Flight => _flightColor,
                VehicleStateId.Water => _waterColor,
                VehicleStateId.UnderWater => _underWaterColor,
                _ => Color.magenta,
            };

            _hullRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", color);
            _hullRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
