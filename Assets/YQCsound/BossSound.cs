using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BossSound : MonoBehaviour
{
    [Header("Shared 3D Distance (Boss arena ~50x50)")]
    [Tooltip("Within this distance, sound stays near full volume (meters).")]
    public float commonMinDistance = 8f;

    [Tooltip("Beyond this distance, sound becomes silent (meters).")]
    public float commonMaxDistance = 70f;

    [Header("Shared Rolloff (slower attenuation)")]
    [Tooltip("Custom volume rolloff curve vs normalized distance (0=min, 1=max).")]
    public AnimationCurve commonRolloff = new AnimationCurve(
        new Keyframe(0.00f, 1.00f),
        new Keyframe(0.30f, 0.95f),
        new Keyframe(0.55f, 0.80f),
        new Keyframe(0.75f, 0.55f),
        new Keyframe(0.90f, 0.25f),
        new Keyframe(1.00f, 0.00f)
    );

    [Header("Audio Source (auto if empty)")]
    public AudioSource sfx;

    [Header("Clips + per-sound volume + debug delay")]
    public AudioClip Boss_Roar;
    [Range(0f, 1f)] public float roarVolume = 1.0f;
    [Range(0f, 2f)] public float roarDelay = 0f;

    public AudioClip Boss_Attack; // AttackB/C
    [Range(0f, 1f)] public float attackVolume = 0.9f;
    [Range(0f, 2f)] public float attackDelay = 1f;

    public AudioClip Boss_AOE;    // AttackA
    [Range(0f, 1f)] public float aoeVolume = 1.0f;
    [Range(0f, 2f)] public float aoeDelay = 0f;

    public AudioClip Boss_Summon; // AttackD
    [Range(0f, 1f)] public float summonVolume = 1.0f;
    [Range(0f, 2f)] public float summonDelay = 0f;

    [Header("Optional small pitch variation (adds life)")]
    public bool randomPitch = true;
    [Range(0.8f, 1.2f)] public float pitchMin = 0.95f;
    [Range(0.8f, 1.2f)] public float pitchMax = 1.05f;

    void Awake()
    {
        if (!sfx) sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.loop = false;

        // 3D settings
        sfx.spatialBlend = 1f;
        sfx.dopplerLevel = 0f; // Boss 声音一般不需要多普勒
        sfx.spread = 0f;

        ApplySharedDistanceAndRolloff();
    }

    void OnValidate()
    {
        // 让你在 Inspector 调 min/max/curve 时立即生效（运行/非运行都尽量同步）
        if (commonMinDistance < 0.01f) commonMinDistance = 0.01f;
        if (commonMaxDistance < commonMinDistance + 0.01f) commonMaxDistance = commonMinDistance + 0.01f;

        if (sfx) ApplySharedDistanceAndRolloff();
    }

    void ApplySharedDistanceAndRolloff()
    {
        sfx.minDistance = commonMinDistance;
        sfx.maxDistance = commonMaxDistance;

        // 用 Custom 曲线来实现“衰减更慢、更真实”
        sfx.rolloffMode = AudioRolloffMode.Custom;
        sfx.SetCustomCurve(AudioSourceCurveType.CustomRolloff, commonRolloff);
    }

    float GetPitch()
    {
        if (!randomPitch) return 1f;
        float a = Mathf.Min(pitchMin, pitchMax);
        float b = Mathf.Max(pitchMin, pitchMax);
        return Random.Range(a, b);
    }

    // ---------------- Public API ----------------

    public void PlayRoar()   => PlayClip(Boss_Roar, roarVolume, roarDelay);
    public void PlayAttack() => PlayClip(Boss_Attack, attackVolume, attackDelay);
    public void PlayAOE()    => PlayClip(Boss_AOE, aoeVolume, aoeDelay);
    public void PlaySummon() => PlayClip(Boss_Summon, summonVolume, summonDelay);

    // ---------------- Internals ----------------

    void PlayClip(AudioClip clip, float volume, float delay)
    {
        if (!clip || !sfx) return;

        if (delay <= 0f)
        {
            sfx.pitch = GetPitch();
            sfx.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
        else
        {
            StartCoroutine(PlayDelayed(clip, Mathf.Clamp01(volume), delay));
        }
    }

    IEnumerator PlayDelayed(AudioClip clip, float volume, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!clip || !sfx) yield break;

        sfx.pitch = GetPitch();
        sfx.PlayOneShot(clip, volume);
    }
}
