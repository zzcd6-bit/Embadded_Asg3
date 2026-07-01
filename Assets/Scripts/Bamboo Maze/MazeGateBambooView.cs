using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MazeGateBambooView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MazeGate gate;
    [SerializeField] private Transform bamboosRoot;
    [SerializeField] private Transform endLeft;
    [SerializeField] private Transform endRight;

    [Header("Shake")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeAngle = 4f;
    [SerializeField] private float shakeFrequency = 22f;
    [SerializeField] private float shakePosition = 0.035f;

    [Header("Open")]
    [SerializeField] private float openDuration = 0.65f;
    [SerializeField] private float fadeStartDistance = 0.2f;
    [SerializeField] private bool disableAfterOpen = true;

    [Header("Local Shader Wind")]
    [SerializeField] private bool useLocalShaderWind = true;
    [SerializeField] private float windBendStrength = 2.5f;
    [SerializeField] private float windMicroStrength = 1.2f;
    [SerializeField] private float windSwayStrength = 2.5f;

    [Header("Close")]
    [SerializeField] private float closeDuration = 0.45f;

    private readonly List<BambooPart> bamboos = new();
    private Coroutine animationRoutine;
    private bool initialized;
#if UNITY_EDITOR
    private double editorAnimationStartTime;
    private bool editorAnimationOpening;
    private bool editorAnimationRunning;
    private bool editorAnimationPrepared;
#endif

    private void Awake()
    {
        if (gate == null)
            gate = GetComponent<MazeGate>();
    }

    private void Start()
    {
        EnsureInitialized();
        ApplyInitialStateInstant();
    }

    public void PlayOpen()
    {
        EnsureInitialized();
        Debug.Log($"[MazeGateBambooView] PlayOpen received on {name}. Application.isPlaying: {Application.isPlaying}.", this);

        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            StartEditorAnimation(true);
#else
            ApplyOpenInstant();
#endif
            return;
        }

        StartAnimation(OpenRoutine());
    }

    public void PlayClose()
    {
        EnsureInitialized();
        Debug.Log($"[MazeGateBambooView] PlayClose received on {name}. Application.isPlaying: {Application.isPlaying}.", this);

        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            StartEditorAnimation(false);
#else
            ApplyClosedInstant();
#endif
            return;
        }

        StartAnimation(CloseRoutine());
    }

    public void ResetBamboosClosedInstant()
    {
        EnsureInitialized();
        StopAnyAnimation();
        Debug.Log($"[MazeGateBambooView] ResetBamboosClosedInstant on {name}. Bamboo count: {bamboos.Count}.", this);
        ApplyClosedInstant();
    }

    public void ResetBamboosOpenInstant()
    {
        EnsureInitialized();
        StopAnyAnimation();
        Debug.Log($"[MazeGateBambooView] ResetBamboosOpenInstant on {name}. Bamboo count: {bamboos.Count}.", this);
        ApplyOpenInstant();
    }

    private void StopAnyAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

#if UNITY_EDITOR
        StopEditorAnimation();
#endif
    }

    private void StartAnimation(IEnumerator routine)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(routine);
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        if (bamboosRoot == null)
            bamboosRoot = transform.Find("Bamboos");

        if (endLeft == null)
            endLeft = transform.Find("End Left") ?? transform.Find("Left End");

        if (endRight == null)
            endRight = transform.Find("End Right") ?? transform.Find("Right End");

        bamboos.Clear();

        if (bamboosRoot == null)
        {
            Debug.LogWarning($"{name} has no Bamboos child root.");
            return;
        }

        for (int i = 0; i < bamboosRoot.childCount; i++)
        {
            Transform bamboo = bamboosRoot.GetChild(i);
            if (bamboo != null)
                bamboos.Add(new BambooPart(bamboo));
        }
    }

    private void ApplyInitialStateInstant()
    {
        if (gate != null && gate.IsOpen)
        {
            ApplyOpenInstant();
            return;
        }

        ApplyClosedInstant();
    }

    private void ApplyClosedInstant()
    {
        for (int i = 0; i < bamboos.Count; i++)
            bamboos[i].RestoreClosed();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    private void ApplyOpenInstant()
    {
        if (!HasOpenTargets())
            return;

        PrepareOpenTargets();

        for (int i = 0; i < bamboos.Count; i++)
        {
            BambooPart bamboo = bamboos[i];
            bamboo.Transform.position = bamboo.TargetPosition;
            bamboo.Transform.rotation = bamboo.ClosedRotation;
            bamboo.RestoreWind();
            bamboo.SetAlpha(0f);

            if (disableAfterOpen)
                bamboo.Transform.gameObject.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void StartEditorAnimation(bool opening)
    {
        StopEditorAnimation();

        if (opening && !HasOpenTargets())
            return;

        editorAnimationOpening = opening;
        editorAnimationRunning = true;
        editorAnimationPrepared = false;
        editorAnimationStartTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += UpdateEditorAnimation;
    }

    private void StopEditorAnimation()
    {
        if (!editorAnimationRunning)
            return;

        editorAnimationRunning = false;
        EditorApplication.update -= UpdateEditorAnimation;
    }

    private void UpdateEditorAnimation()
    {
        if (!this)
        {
            StopEditorAnimation();
            return;
        }

        EnsureInitialized();

        if (editorAnimationOpening)
        {
            UpdateEditorOpenAnimation();
            return;
        }

        UpdateEditorCloseAnimation();
    }

    private void UpdateEditorOpenAnimation()
    {
        if (!editorAnimationPrepared)
        {
            PrepareOpenTargets();
            editorAnimationPrepared = true;
        }

        double elapsed = EditorApplication.timeSinceStartup - editorAnimationStartTime;
        float shakeEnd = shakeDuration;
        float openEnd = shakeDuration + openDuration;

        if (elapsed < shakeEnd)
        {
            float timer = (float)elapsed;
            float t = Mathf.Clamp01(timer / Mathf.Max(shakeDuration, 0.0001f));
            float strength = Mathf.Sin(t * Mathf.PI);

            for (int i = 0; i < bamboos.Count; i++)
            {
                BambooPart bamboo = bamboos[i];
                float wave = Mathf.Sin(timer * shakeFrequency + bamboo.ShakePhase);
                bamboo.Transform.position = bamboo.ClosedPosition + transform.right * (wave * shakePosition * strength);
                bamboo.Transform.rotation = Quaternion.AngleAxis(wave * shakeAngle * strength, transform.forward) * bamboo.ClosedRotation;
            }

            SceneView.RepaintAll();
            return;
        }

        float openTimer = (float)(elapsed - shakeEnd);
        float openT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(openTimer / Mathf.Max(openDuration, 0.0001f)));
        float bendWeight = Mathf.Sin(openT * Mathf.PI);

        for (int i = 0; i < bamboos.Count; i++)
        {
            BambooPart bamboo = bamboos[i];
            bamboo.Transform.position = Vector3.Lerp(bamboo.ClosedPosition, bamboo.TargetPosition, openT);
            bamboo.Transform.rotation = bamboo.ClosedRotation;

            if (useLocalShaderWind)
                bamboo.SetWind(-bamboo.MoveDirection, bendWeight, windBendStrength, windMicroStrength, windSwayStrength);

            float remainingDistance = Vector3.Distance(bamboo.Transform.position, bamboo.TargetPosition);
            float alpha = fadeStartDistance <= 0f ? 1f : Mathf.Clamp01(remainingDistance / fadeStartDistance);
            bamboo.SetAlpha(alpha);
        }

        if (elapsed >= openEnd)
        {
            ApplyOpenInstant();
            StopEditorAnimation();
        }

        SceneView.RepaintAll();
    }

    private void UpdateEditorCloseAnimation()
    {
        if (!editorAnimationPrepared)
        {
            for (int i = 0; i < bamboos.Count; i++)
            {
                BambooPart bamboo = bamboos[i];
                bamboo.Transform.gameObject.SetActive(true);
                bamboo.CaptureMoveStart();
                bamboo.SetAlpha(1f);
            }

            editorAnimationPrepared = true;
        }

        double elapsed = EditorApplication.timeSinceStartup - editorAnimationStartTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((float)elapsed / Mathf.Max(closeDuration, 0.0001f)));

        for (int i = 0; i < bamboos.Count; i++)
        {
            BambooPart bamboo = bamboos[i];
            bamboo.Transform.position = Vector3.Lerp(bamboo.MoveStartPosition, bamboo.ClosedPosition, t);
            bamboo.Transform.rotation = Quaternion.Slerp(bamboo.MoveStartRotation, bamboo.ClosedRotation, t);
            bamboo.SetAlpha(t);
        }

        if (elapsed >= closeDuration)
        {
            ApplyClosedInstant();
            StopEditorAnimation();
        }

        SceneView.RepaintAll();
    }
#endif

    private IEnumerator OpenRoutine()
    {
        if (!HasOpenTargets())
        {
            animationRoutine = null;
            yield break;
        }

        PrepareOpenTargets();

        yield return AnimateShake();
        yield return AnimateOpenMove();

        if (disableAfterOpen)
        {
            for (int i = 0; i < bamboos.Count; i++)
                bamboos[i].Transform.gameObject.SetActive(false);
        }

        animationRoutine = null;
    }

    private IEnumerator CloseRoutine()
    {
        for (int i = 0; i < bamboos.Count; i++)
        {
            BambooPart bamboo = bamboos[i];
            bamboo.Transform.gameObject.SetActive(true);
            bamboo.CaptureMoveStart();
            bamboo.SetAlpha(1f);
        }

        float timer = 0f;
        while (timer < closeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / Mathf.Max(closeDuration, 0.0001f)));

            for (int i = 0; i < bamboos.Count; i++)
            {
                BambooPart bamboo = bamboos[i];
                bamboo.Transform.position = Vector3.Lerp(bamboo.MoveStartPosition, bamboo.ClosedPosition, t);
                bamboo.Transform.rotation = Quaternion.Slerp(bamboo.MoveStartRotation, bamboo.ClosedRotation, t);
                bamboo.SetAlpha(t);
            }

            yield return null;
        }

        for (int i = 0; i < bamboos.Count; i++)
            bamboos[i].RestoreClosed();

        animationRoutine = null;
    }

    private bool HasOpenTargets()
    {
        if (endLeft != null && endRight != null)
            return true;

        Debug.LogWarning($"{name} needs End Left and End Right children for bamboo opening.");
        return false;
    }

    private void PrepareOpenTargets()
    {
        for (int i = 0; i < bamboos.Count; i++)
        {
            BambooPart bamboo = bamboos[i];
            bamboo.RestoreClosed();
            bamboo.ShakePhase = Random.Range(0f, Mathf.PI * 2f);

            Transform targetEnd = Random.value < 0.5f ? endLeft : endRight;
            bamboo.TargetPosition = targetEnd.position;
            bamboo.MoveDirection = (bamboo.TargetPosition - bamboo.ClosedPosition).normalized;
        }
    }

    private IEnumerator AnimateShake()
    {
        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / Mathf.Max(shakeDuration, 0.0001f));
            float strength = Mathf.Sin(t * Mathf.PI);

            for (int i = 0; i < bamboos.Count; i++)
            {
                BambooPart bamboo = bamboos[i];
                float wave = Mathf.Sin(timer * shakeFrequency + bamboo.ShakePhase);

                bamboo.Transform.position = bamboo.ClosedPosition + transform.right * (wave * shakePosition * strength);
                bamboo.Transform.rotation = Quaternion.AngleAxis(wave * shakeAngle * strength, transform.forward) * bamboo.ClosedRotation;
            }

            yield return null;
        }

        for (int i = 0; i < bamboos.Count; i++)
            bamboos[i].RestoreClosed();
    }

    private IEnumerator AnimateOpenMove()
    {
        float timer = 0f;

        while (timer < openDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / Mathf.Max(openDuration, 0.0001f)));
            float bendWeight = Mathf.Sin(t * Mathf.PI);

            for (int i = 0; i < bamboos.Count; i++)
            {
                BambooPart bamboo = bamboos[i];
                bamboo.Transform.position = Vector3.Lerp(bamboo.ClosedPosition, bamboo.TargetPosition, t);
                bamboo.Transform.rotation = bamboo.ClosedRotation;

                if (useLocalShaderWind)
                    bamboo.SetWind(-bamboo.MoveDirection, bendWeight, windBendStrength, windMicroStrength, windSwayStrength);

                float remainingDistance = Vector3.Distance(bamboo.Transform.position, bamboo.TargetPosition);
                float alpha = fadeStartDistance <= 0f ? 1f : Mathf.Clamp01(remainingDistance / fadeStartDistance);
                bamboo.SetAlpha(alpha);
            }

            yield return null;
        }

        for (int i = 0; i < bamboos.Count; i++)
        {
            BambooPart bamboo = bamboos[i];
            bamboo.Transform.position = bamboo.TargetPosition;
            bamboo.Transform.rotation = bamboo.ClosedRotation;
            bamboo.RestoreWind();
            bamboo.SetAlpha(0f);
        }
    }

    private sealed class BambooPart
    {
        public readonly Transform Transform;
        public readonly Vector3 ClosedLocalPosition;
        public readonly Quaternion ClosedLocalRotation;
        public readonly Renderer[] Renderers;
        public readonly Color[][] OriginalColors;
        private readonly WindSettings[][] originalWindSettings;
        private readonly MaterialPropertyBlock[][] propertyBlocks;

        public Vector3 MoveStartPosition;
        public Quaternion MoveStartRotation;
        public Vector3 TargetPosition;
        public Vector3 MoveDirection;
        public float ShakePhase;

        public Vector3 ClosedPosition => Transform.parent != null
            ? Transform.parent.TransformPoint(ClosedLocalPosition)
            : ClosedLocalPosition;

        public Quaternion ClosedRotation => Transform.parent != null
            ? Transform.parent.rotation * ClosedLocalRotation
            : ClosedLocalRotation;

        public BambooPart(Transform transform)
        {
            Transform = transform;
            ClosedLocalPosition = transform.localPosition;
            ClosedLocalRotation = transform.localRotation;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Transform sourceTransform = PrefabUtility.GetCorrespondingObjectFromSource(transform);
                if (sourceTransform != null)
                {
                    ClosedLocalPosition = sourceTransform.localPosition;
                    ClosedLocalRotation = sourceTransform.localRotation;
                }
            }
#endif
            Renderers = transform.GetComponentsInChildren<Renderer>(true);
            OriginalColors = new Color[Renderers.Length][];
            originalWindSettings = new WindSettings[Renderers.Length][];
            propertyBlocks = new MaterialPropertyBlock[Renderers.Length][];

            for (int i = 0; i < Renderers.Length; i++)
            {
                Material[] materials = Renderers[i].sharedMaterials;

                OriginalColors[i] = new Color[materials.Length];
                originalWindSettings[i] = new WindSettings[materials.Length];
                propertyBlocks[i] = new MaterialPropertyBlock[materials.Length];

                for (int j = 0; j < materials.Length; j++)
                {
                    OriginalColors[i][j] = GetMaterialColor(materials[j]);
                    OriginalColors[i][j].a = Mathf.Max(OriginalColors[i][j].a, 1f);
                    originalWindSettings[i][j] = new WindSettings(materials[j]);
                    propertyBlocks[i][j] = new MaterialPropertyBlock();
                }
            }
        }

        public void CaptureMoveStart()
        {
            MoveStartPosition = Transform.position;
            MoveStartRotation = Transform.rotation;
        }

        public void RestoreClosed()
        {
            Transform.gameObject.SetActive(true);
            Transform.localPosition = ClosedLocalPosition;
            Transform.localRotation = ClosedLocalRotation;
            RestoreWind();
            SetAlpha(1f);
        }

        public void SetAlpha(float alpha)
        {
            for (int i = 0; i < Renderers.Length; i++)
            {
                Material[] materials = Renderers[i].sharedMaterials;

                for (int j = 0; j < materials.Length; j++)
                {
                    Color color = OriginalColors[i][j];
                    color.a *= alpha;
                    MaterialPropertyBlock block = GetBlock(i, j);
                    Renderers[i].GetPropertyBlock(block, j);
                    SetBlockColor(materials[j], block, color);
                    Renderers[i].SetPropertyBlock(block, j);
                }
            }
        }

        public void SetWind(Vector3 worldDirection, float weight, float baseStrength, float microStrength, float swayStrength)
        {
            Vector4 direction = worldDirection.sqrMagnitude > 0.0001f
                ? new Vector4(worldDirection.normalized.x, worldDirection.normalized.y, worldDirection.normalized.z, 0f)
                : Vector4.zero;

            for (int i = 0; i < Renderers.Length; i++)
            {
                Material[] materials = Renderers[i].sharedMaterials;

                for (int j = 0; j < materials.Length; j++)
                {
                    MaterialPropertyBlock block = GetBlock(i, j);
                    Renderers[i].GetPropertyBlock(block, j);
                    SetBlockWind(materials[j], block, originalWindSettings[i][j], direction, weight, baseStrength, microStrength, swayStrength);
                    Renderers[i].SetPropertyBlock(block, j);
                }
            }
        }

        public void RestoreWind()
        {
            for (int i = 0; i < Renderers.Length; i++)
            {
                Material[] materials = Renderers[i].sharedMaterials;

                for (int j = 0; j < materials.Length; j++)
                {
                    MaterialPropertyBlock block = GetBlock(i, j);
                    Renderers[i].GetPropertyBlock(block, j);
                    originalWindSettings[i][j].ApplyTo(block);
                    Renderers[i].SetPropertyBlock(block, j);
                }
            }
        }

        private MaterialPropertyBlock GetBlock(int rendererIndex, int materialIndex)
        {
            MaterialPropertyBlock block = propertyBlocks[rendererIndex][materialIndex];
            if (block == null)
            {
                block = new MaterialPropertyBlock();
                propertyBlocks[rendererIndex][materialIndex] = block;
            }

            return block;
        }

        private static Color GetMaterialColor(Material material)
        {
            if (material == null)
                return Color.white;

            if (material.HasProperty("_BaseColor"))
                return material.GetColor("_BaseColor");

            return material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
        }

        private static void SetBlockColor(Material material, MaterialPropertyBlock block, Color color)
        {
            if (material == null || block == null)
                return;

            if (material.HasProperty("_BaseColor"))
                block.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                block.SetColor("_Color", color);
        }

        private static void SetBlockWind(
            Material material,
            MaterialPropertyBlock block,
            WindSettings original,
            Vector4 direction,
            float weight,
            float baseStrength,
            float microStrength,
            float swayStrength)
        {
            if (material == null || block == null)
                return;

            if (original.HasWindMultiplier)
                block.SetFloat("_WindMultiplier", original.WindMultiplier + baseStrength * weight);

            if (original.HasMicroWindMultiplier)
                block.SetFloat("_MicroWindMultiplier", original.MicroWindMultiplier + microStrength * weight);

            if (original.HasWindForce)
                block.SetFloat("_WindForce", original.WindForce + baseStrength * weight);

            if (original.HasWindStrenght)
                block.SetFloat("_WindStrenght", original.WindStrenght + swayStrength * weight);

            if (original.HasWindSwayStrength)
                block.SetFloat("_WindSwayStrength", original.WindSwayStrength + swayStrength * weight);

            if (original.HasWindDirection && direction != Vector4.zero)
                block.SetVector("_WindDirection", direction);
        }

        private readonly struct WindSettings
        {
            public readonly bool HasWindMultiplier;
            public readonly bool HasMicroWindMultiplier;
            public readonly bool HasWindForce;
            public readonly bool HasWindStrenght;
            public readonly bool HasWindSwayStrength;
            public readonly bool HasWindDirection;

            public readonly float WindMultiplier;
            public readonly float MicroWindMultiplier;
            public readonly float WindForce;
            public readonly float WindStrenght;
            public readonly float WindSwayStrength;
            public readonly Vector4 WindDirection;

            public WindSettings(Material material)
            {
                HasWindMultiplier = material != null && material.HasProperty("_WindMultiplier");
                HasMicroWindMultiplier = material != null && material.HasProperty("_MicroWindMultiplier");
                HasWindForce = material != null && material.HasProperty("_WindForce");
                HasWindStrenght = material != null && material.HasProperty("_WindStrenght");
                HasWindSwayStrength = material != null && material.HasProperty("_WindSwayStrength");
                HasWindDirection = material != null && material.HasProperty("_WindDirection");

                WindMultiplier = HasWindMultiplier ? material.GetFloat("_WindMultiplier") : 0f;
                MicroWindMultiplier = HasMicroWindMultiplier ? material.GetFloat("_MicroWindMultiplier") : 0f;
                WindForce = HasWindForce ? material.GetFloat("_WindForce") : 0f;
                WindStrenght = HasWindStrenght ? material.GetFloat("_WindStrenght") : 0f;
                WindSwayStrength = HasWindSwayStrength ? material.GetFloat("_WindSwayStrength") : 0f;
                WindDirection = HasWindDirection ? material.GetVector("_WindDirection") : Vector4.zero;
            }

            public void ApplyTo(MaterialPropertyBlock block)
            {
                if (block == null)
                    return;

                if (HasWindMultiplier)
                    block.SetFloat("_WindMultiplier", WindMultiplier);

                if (HasMicroWindMultiplier)
                    block.SetFloat("_MicroWindMultiplier", MicroWindMultiplier);

                if (HasWindForce)
                    block.SetFloat("_WindForce", WindForce);

                if (HasWindStrenght)
                    block.SetFloat("_WindStrenght", WindStrenght);

                if (HasWindSwayStrength)
                    block.SetFloat("_WindSwayStrength", WindSwayStrength);

                if (HasWindDirection)
                    block.SetVector("_WindDirection", WindDirection);
            }
        }
    }
}
