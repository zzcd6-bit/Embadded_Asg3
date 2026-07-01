using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class GlobalWindController : MonoBehaviour
{
    private static readonly int ToonScapesWindDirection = Shader.PropertyToID("ToonScapesGlobalWindDirection");
    private static readonly int ToonScapesWindStrength = Shader.PropertyToID("ToonScapesGlobalWindStrength");
    private static readonly int ToonScapesWindSpeed = Shader.PropertyToID("ToonScapesGlobalWindSpeed");
    private static readonly int ToonScapesWindScale = Shader.PropertyToID("ToonScapesGlobalWindScale");
    private static readonly int ToonScapesWindJitter = Shader.PropertyToID("ToonScapesGlobalWindJitter");
    private static readonly int ToonScapesNoiseTexture = Shader.PropertyToID("ToonScapesGlobalNoiseTexture");

    private const string DefaultNoiseTexturePath = "Assets/ToonScapes/Shared Assets/Textures/TS_Noise_Texture_3A.png";

    [Header("ToonScapes Wind")]
    [SerializeField] private Vector3 direction = new Vector3(0.1f, 0f, 0.1f);
    [SerializeField, Range(0f, 20f)] private float strength = 12.1f;
    [SerializeField, Range(0f, 20f)] private float scale = 2f;
    [SerializeField, Range(0f, 20f)] private float speed = 5.5f;
    [SerializeField, Range(0f, 20f)] private float jitter = 2.8f;
    [SerializeField] private Texture noiseTexture;

    private Vector3 previousDirection;
    private float previousStrength;
    private float previousScale;
    private float previousSpeed;
    private float previousJitter;
    private Texture previousNoiseTexture;
    private bool hasApplied;

    private void OnEnable()
    {
        EnsureNoiseTexture();
        ApplyWind();
    }

    private void Start()
    {
        EnsureNoiseTexture();
        ApplyWind();
    }

    private void Update()
    {
        if (HasWindSettingsChanged())
        {
            ApplyWind();
        }
    }

    private void OnValidate()
    {
        EnsureNoiseTexture();
        ApplyWind();
    }

    [ContextMenu("Apply Wind Now")]
    public void ApplyWind()
    {
        Shader.SetGlobalVector(ToonScapesWindDirection, direction);
        Shader.SetGlobalFloat(ToonScapesWindStrength, strength);
        Shader.SetGlobalFloat(ToonScapesWindScale, scale);
        Shader.SetGlobalFloat(ToonScapesWindSpeed, speed);
        Shader.SetGlobalFloat(ToonScapesWindJitter, jitter);

        if (noiseTexture != null)
        {
            Shader.SetGlobalTexture(ToonScapesNoiseTexture, noiseTexture);
        }

        previousDirection = direction;
        previousStrength = strength;
        previousScale = scale;
        previousSpeed = speed;
        previousJitter = jitter;
        previousNoiseTexture = noiseTexture;
        hasApplied = true;
    }

    private bool HasWindSettingsChanged()
    {
        return !hasApplied ||
               direction != previousDirection ||
               strength != previousStrength ||
               scale != previousScale ||
               speed != previousSpeed ||
               jitter != previousJitter ||
               noiseTexture != previousNoiseTexture;
    }

    private void EnsureNoiseTexture()
    {
#if UNITY_EDITOR
        if (noiseTexture == null)
        {
            noiseTexture = AssetDatabase.LoadAssetAtPath<Texture>(DefaultNoiseTexturePath);
        }
#endif
    }
}
