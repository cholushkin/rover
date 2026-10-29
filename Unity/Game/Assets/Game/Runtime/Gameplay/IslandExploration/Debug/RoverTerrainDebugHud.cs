using Game.Gameplay.IslandExploration.Probe;
using UnityEngine;

namespace Game.Gameplay.IslandExploration.Debugging
{
    // todo: surface LOD-depth distribution once a per-depth chunk count is worth the extra GetDebugArray() scan
    [DisallowMultipleComponent]
    public sealed class RoverTerrainDebugHud : MonoBehaviour
    {
        [SerializeField] private RoverFlyingProbe _probe;

        private GUIStyle _labelStyle;

        private void OnGUI()
        {
            _labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };

            int activeChunkCount = TerraxelWorld.ChunkManager?.GetDebugArray().Count ?? 0;

            GUI.Box(new Rect(10, 10, 260, 110), string.Empty);
            GUILayout.BeginArea(new Rect(20, 16, 240, 100));
            GUILayout.Label($"Seed: {TerraxelWorld.seed}", _labelStyle);
            GUILayout.Label($"Active chunks: {activeChunkCount}", _labelStyle);

            if (_probe != null)
            {
                Vector3 pos = _probe.transform.position;
                GUILayout.Label($"Position: {pos.x:F0}, {pos.y:F0}, {pos.z:F0}", _labelStyle);
                GUILayout.Label($"Speed: {_probe.CurrentSpeed:F0} u/s", _labelStyle);
            }

            GUILayout.EndArea();
        }
    }
}
