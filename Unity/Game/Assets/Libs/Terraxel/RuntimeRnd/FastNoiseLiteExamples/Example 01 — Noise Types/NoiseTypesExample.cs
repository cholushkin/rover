
using UnityEngine;

/// <summary>
/// Example 01: Display FastNoiseLite's six 2D noise algorithms
/// on a manually prepared Quad with an unlit material.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
[AddComponentMenu("Fast Noise Lite/Examples/01 - Noise Types")]
public sealed class NoiseTypesExample : MonoBehaviour
{
    [Header("Noise")]
    [SerializeField]
    private FastNoiseLite.NoiseType noiseType =
        FastNoiseLite.NoiseType.OpenSimplex2;

    [SerializeField] private int seed = 1337;
    [SerializeField, Min(0.0001f)] private float frequency = 0.02f;
    [SerializeField] private Vector2 sampleOffset;

    [Header("Texture")]
    [SerializeField] private FilterMode filterMode = FilterMode.Point;
    [SerializeField, Range(32, 1024)] private int resolution = 256;
    [SerializeField] private bool liveUpdate = true;

    [Header("Diagnostics")]
    [SerializeField] private bool showCheckerboard;

    private Texture2D _texture;
    private Color32[] _pixels;
    private MaterialPropertyBlock _properties;
    private MeshRenderer _renderer;
    private int _texturePropertyId;

    private static readonly int BaseMapId =
        Shader.PropertyToID("_BaseMap");

    private static readonly int MainTexId =
        Shader.PropertyToID("_MainTex");

    private void OnEnable()
    {
        Regenerate();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RegenerateDeferred;

        if (liveUpdate)
            UnityEditor.EditorApplication.delayCall += RegenerateDeferred;
#endif
    }

#if UNITY_EDITOR
    private void RegenerateDeferred()
    {
        UnityEditor.EditorApplication.delayCall -= RegenerateDeferred;

        if (this != null && isActiveAndEnabled && liveUpdate)
            Regenerate();
    }
#endif

    [ContextMenu("Regenerate Texture")]
    public void Regenerate()
    {
        if (!isActiveAndEnabled)
            return;

        _renderer = GetComponent<MeshRenderer>();

        Material material =
            _renderer != null ? _renderer.sharedMaterial : null;

        if (material == null)
            return;

        if (material.HasProperty(BaseMapId))
        {
            _texturePropertyId = BaseMapId;
        }
        else if (material.HasProperty(MainTexId))
        {
            _texturePropertyId = MainTexId;
        }
        else
        {
            Debug.LogWarning(
                "NoiseTypesExample: Material needs _BaseMap or _MainTex.",
                this
            );
            return;
        }

        int size = Mathf.Clamp(resolution, 32, 1024);

        // Create or resize texture only when necessary.
        if (_texture == null || _texture.width != size)
        {
            ReleaseTexture();

            _texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            )
            {
                name = "FastNoiseLite Preview",
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            _pixels = new Color32[size * size];
        }

        // Always apply the selected filtering mode,
        // even when reusing an existing texture.
        _texture.filterMode = filterMode;

        // Configure FastNoiseLite.
        var noise = new FastNoiseLite(seed);

        noise.SetNoiseType(noiseType);
        noise.SetFrequency(Mathf.Max(0.0001f, frequency));

        // Generate texture pixels.
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                byte gray;

                if (showCheckerboard)
                {
                    // Alternating black and white pixels.
                    gray = (byte)(
                        ((x + y) & 1) == 0 ? 255 : 0
                    );
                }
                else
                {
                    float value = noise.GetNoise(
                        x + sampleOffset.x,
                        y + sampleOffset.y
                    );

                    gray = (byte)Mathf.RoundToInt(
                        Mathf.Clamp01((value + 1f) * 0.5f) * 255f
                    );
                }

                _pixels[y * size + x] =
                    new Color32(gray, gray, gray, 255);
            }
        }

        _texture.SetPixels32(_pixels);
        _texture.Apply(false, false);

        // Assign generated texture without changing the material asset.
        if (_properties == null)
            _properties = new MaterialPropertyBlock();

        _renderer.GetPropertyBlock(_properties);
        _properties.SetTexture(_texturePropertyId, _texture);
        _renderer.SetPropertyBlock(_properties);
    }

    [ContextMenu("Log Preview Texture State")]
    private void LogPreviewTextureState()
    {
        if (_texture == null)
        {
            Debug.Log("NoiseTypesExample: No texture generated.", this);
            return;
        }

        string shaderName =
            _renderer != null && _renderer.sharedMaterial != null
                ? _renderer.sharedMaterial.shader.name
                : "None";

        Debug.Log(
            $"NoiseTypesExample:\n" +
            $"Resolution: {_texture.width}x{_texture.height}\n" +
            $"Filter Mode: {_texture.filterMode}\n" +
            $"Mipmaps: {_texture.mipmapCount}\n" +
            $"Shader: {shaderName}\n" +
            $"Checkerboard: {showCheckerboard}",
            this
        );
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RegenerateDeferred;
#endif

        if (_renderer != null)
            _renderer.SetPropertyBlock(null);

        ReleaseTexture();
    }

    private void ReleaseTexture()
    {
        if (_texture != null)
        {
            if (Application.isPlaying)
                Destroy(_texture);
            else
                DestroyImmediate(_texture);
        }

        _texture = null;
        _pixels = null;
    }
}
