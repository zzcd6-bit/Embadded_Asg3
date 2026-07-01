using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

namespace Cocomotion
{
    public class CocomotionViewer : MonoBehaviour
    {
        [Header("Requirements")]
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private List<AnimationClip> animations;
        [SerializeField] private TextMeshProUGUI statusText;

        private int _currentIndex = 0;

        private void Start()
        {
            if (animations != null && animations.Count > 0)
            {
                UpdateUI();
            }
        }

        private void Update()
        {
            if (animations == null || animations.Count == 0) return;
            HandleInput();
        }

        private void HandleInput()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                _currentIndex = (_currentIndex + 1) % animations.Count;
                PlayCurrent();
            }
            
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                _currentIndex--;
                if (_currentIndex < 0) _currentIndex = animations.Count - 1;
                PlayCurrent();
            }
        }

        private void PlayCurrent()
        {
            if (targetAnimator != null && animations[_currentIndex] != null)
            {
                targetAnimator.Play(animations[_currentIndex].name, 0, 0f);
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (statusText != null && animations[_currentIndex] != null)
            {
                statusText.text = $"<size=80%>{_currentIndex + 1} / {animations.Count}</size>\n<color=#FFD700>Playing:</color> {animations[_currentIndex].name}";
            }
        }
    }
}