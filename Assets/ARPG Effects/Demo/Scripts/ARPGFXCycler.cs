using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARPGFX
{
    public class ARPGFXCycler : MonoBehaviour
    {
        [Header("Effects")]
        [SerializeField]
        private List<GameObject> listOfEffects = new List<GameObject>();

        [Header("Chest Animator")]
        [SerializeField]
        private Animator chestAnimator;

        [SerializeField]
        private string openTriggerName = "Open";

        [SerializeField]
        private bool playAnimatorOnOpen = true;

        [Header("Loop")]
        [SerializeField]
        private bool loopAfterOpen = false;

        [SerializeField]
        private float loopTimeLength = 5f;

        [Header("Open Setting")]
        [SerializeField]
        private bool openOnlyOnce = true;

        [SerializeField]
        private int startEffectIndex = 0;

        private GameObject instantiatedEffect;
        private Coroutine loopRoutine;

        private int effectIndex;
        private bool hasOpened;

        private void Awake()
        {
            ResolveAnimator();
        }

        private void Start()
        {
            effectIndex = Mathf.Clamp(
                startEffectIndex,
                0,
                Mathf.Max(0, listOfEffects.Count - 1)
            );

            // 不在 Start 自动播放任何打开动画
            // Animator 会自动停留在 Entry 指向的 ChestIdle
        }

        /// <summary>
        /// 给宝箱交互事件调用这个。
        /// </summary>
        public void OpenChest()
        {
            if (openOnlyOnce && hasOpened)
                return;

            hasOpened = true;

            PlayChestOpenAnimator();

            if (listOfEffects != null && listOfEffects.Count > 0)
            {
                PlayCurrentEffect();

                if (loopAfterOpen)
                {
                    if (loopRoutine != null)
                    {
                        StopCoroutine(loopRoutine);
                    }

                    loopRoutine = StartCoroutine(LoopEffects());
                }
            }
        }

        /// <summary>
        /// 如果你的交互系统调用的是 Interact，也可以直接用这个。
        /// </summary>
        public void Interact()
        {
            OpenChest();
        }

        public void StopEffect()
        {
            if (loopRoutine != null)
            {
                StopCoroutine(loopRoutine);
                loopRoutine = null;
            }

            if (instantiatedEffect != null)
            {
                Destroy(instantiatedEffect);
                instantiatedEffect = null;
            }
        }

        private void ResolveAnimator()
        {
            if (chestAnimator != null)
                return;

            chestAnimator = GetComponent<Animator>();

            if (chestAnimator != null)
                return;

            chestAnimator = GetComponentInChildren<Animator>(true);
        }

        private void PlayChestOpenAnimator()
        {
            if (!playAnimatorOnOpen)
                return;

            if (chestAnimator == null)
            {
                ResolveAnimator();
            }

            if (chestAnimator == null)
            {
                Debug.LogWarning(
                    "[ARPGFXCycler] No Animator found.",
                    this
                );
                return;
            }

            if (string.IsNullOrEmpty(openTriggerName))
            {
                Debug.LogWarning(
                    "[ARPGFXCycler] Open Trigger Name is empty.",
                    this
                );
                return;
            }

            chestAnimator.ResetTrigger(openTriggerName);
            chestAnimator.SetTrigger(openTriggerName);
        }

        private IEnumerator LoopEffects()
        {
            while (true)
            {
                yield return new WaitForSeconds(loopTimeLength);

                NextEffect();
                PlayCurrentEffect();
            }
        }

        private void PlayCurrentEffect()
        {
            if (instantiatedEffect != null)
            {
                Destroy(instantiatedEffect);
            }

            if (listOfEffects == null || listOfEffects.Count == 0)
                return;

            effectIndex = Mathf.Clamp(
                effectIndex,
                0,
                listOfEffects.Count - 1
            );

            instantiatedEffect = Instantiate(
                listOfEffects[effectIndex],
                transform.position,
                transform.rotation
            );
        }

        private void NextEffect()
        {
            if (listOfEffects == null || listOfEffects.Count == 0)
                return;

            effectIndex++;

            if (effectIndex >= listOfEffects.Count)
            {
                effectIndex = 0;
            }
        }
    }
}