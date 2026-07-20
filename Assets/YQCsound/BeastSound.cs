using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BeastSound : MonoBehaviour
{
    [Header("Footstep (speed driven)")]
    public AudioSource footSource;
    public AudioClip footLoop;
    [Range(0f,1f)] public float footVolume = 1f;
    public float footMinDistance = 2f;
    public float footMaxDistance = 18f;
    public float footOnSpeed = 0.5f;
    public float footOffSpeed = 0.35f;

    [Header("One-shot source")]
    public AudioSource oneShotSource;

    [Header("Growl (interval)")]
    public AudioSource growlSource;
    public AudioClip growlClip;
    [Range(0f,1f)] public float growlVolume = 0.8f;
    public float growlMinDistance = 3f;
    public float growlMaxDistance = 25f;
    public float growlIntervalMin = 6f;
    public float growlIntervalMax = 12f;
    Coroutine growlCo;

    [Header("Clips")]
    public AudioClip attackClip;
    [Range(0f,1f)] public float attackVolume = 1f;
    public float attackMinDistance = 2f;
    public float attackMaxDistance = 18f;

    public AudioClip shoutClip;
    [Range(0f,1f)] public float shoutVolume = 1f;
    public float shoutMinDistance = 4f;
    public float shoutMaxDistance = 30f;

    public AudioClip attackedClip;
    [Range(0f,1f)] public float attackedVolume = 1f;
    public float attackedMinDistance = 2f;
    public float attackedMaxDistance = 30f;

    public AudioClip deathClip;
    [Range(0f,1f)] public float deathVolume = 1f;
    public float deathMinDistance = 4f;
    public float deathMaxDistance = 30f;

    [Header("Animator detection")]
    public Animator animator;
    bool wasInAttack;
    bool wasInShout;

    [Header("Reaction -> sound (EnemyAI.ReactionValue)")]
    public float hitSoundCooldown = 0.12f;
    float hitCd;
    bool deathPlayed;

    NavMeshAgent agent;
    EnemyAI ai;

    void Awake()
    {
        agent = GetComponentInParent<NavMeshAgent>();
        if (!agent) agent = GetComponentInChildren<NavMeshAgent>();

        ai = GetComponentInParent<EnemyAI>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        footSource    = SetupSource(footSource,    loop:true,  footMinDistance,   footMaxDistance,   footVolume);
        growlSource   = SetupSource(growlSource,   loop:false, growlMinDistance,  growlMaxDistance,  growlVolume);
        oneShotSource = SetupSource(oneShotSource, loop:false, 1f,                30f,               1f);

        footSource.clip  = footLoop;
        growlSource.clip = growlClip;
    }

    AudioSource SetupSource(AudioSource src, bool loop, float min, float max, float vol)
    {
        if (!src) src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = min;
        src.maxDistance = max;
        src.volume = vol;
        return src;
    }

    void Update()
    {
        // 1) Footstep by NavMesh speed
        float speed = agent ? agent.velocity.magnitude : 0f;
        if (!footSource.isPlaying)
        {
            if (speed > footOnSpeed && footLoop) footSource.Play();
        }
        else
        {
            if (speed < footOffSpeed) footSource.Stop();
        }

        // 2) Reaction -> Hit/Death
        if (ai)
        {
            int r = ai.ReactionValue; // None=0 HitStagger=1 Stunned=2 Dead=3

            if (!deathPlayed && (ai.IsDead || r == 3))
            {
                deathPlayed = true;
                StopGrowl();
                if (footSource.isPlaying) footSource.Stop();
                PlayOneShot(deathClip, deathVolume, deathMinDistance, deathMaxDistance);
            }
            else
            {
                hitCd -= Time.deltaTime;
                if (hitCd <= 0f && (r == 1 || r == 2))
                {
                    hitCd = hitSoundCooldown;
                    PlayOneShot(attackedClip, attackedVolume, attackedMinDistance, attackedMaxDistance);
                }
            }
        }

        // 3) Animator driven sounds (state-name based)
        if (animator && !deathPlayed)
        {
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);

            bool inAttack =
                st.IsName("Attack1") ||
                st.IsName("Attack2") ||
                st.IsName("Attack3") ||
                st.IsName("Attack4");

            bool inShout = st.IsName("Shout");

            if (inAttack && !wasInAttack)
                PlayOneShot(attackClip, attackVolume, attackMinDistance, attackMaxDistance);

            if (inShout && !wasInShout)
                PlayOneShot(shoutClip, shoutVolume, shoutMinDistance, shoutMaxDistance);

            wasInAttack = inAttack;
            wasInShout  = inShout;
        }
    }

    // Growl
    public void StartGrowl()
    {
        if (growlCo != null) StopCoroutine(growlCo);
        growlCo = StartCoroutine(GrowlRoutine());
    }

    public void StopGrowl()
    {
        if (growlCo != null) StopCoroutine(growlCo);
        growlCo = null;
        if (growlSource && growlSource.isPlaying) growlSource.Stop();
    }

    IEnumerator GrowlRoutine()
    {
        while (!deathPlayed)
        {
            yield return new WaitForSeconds(Random.Range(growlIntervalMin, growlIntervalMax));
            if (growlClip) growlSource.PlayOneShot(growlClip, growlVolume);
        }
    }

    // External API (optional)
    public void PlayAttack()   => PlayOneShot(attackClip, attackVolume, attackMinDistance, attackMaxDistance);
    public void PlayShout()    => PlayOneShot(shoutClip, shoutVolume, shoutMinDistance, shoutMaxDistance);
    public void PlayAttacked() => PlayOneShot(attackedClip, attackedVolume, attackedMinDistance, attackedMaxDistance);
    public void PlayDeath()    => PlayOneShot(deathClip, deathVolume, deathMinDistance, deathMaxDistance);

    void PlayOneShot(AudioClip clip, float volume, float min, float max)
    {
        if (!clip || !oneShotSource) return;
        oneShotSource.minDistance = min;
        oneShotSource.maxDistance = max;
        oneShotSource.PlayOneShot(clip, volume);
    }
}
