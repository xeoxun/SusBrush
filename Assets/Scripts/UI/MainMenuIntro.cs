using System;
using System.Collections;
using UnityEngine;

namespace SusBrush.UI
{
    public sealed class MainMenuIntro : MonoBehaviour
    {
        [Serializable]
        private sealed class Entry
        {
            public RectTransform button;
            public CanvasGroup visibility;
            public RectTransform[] bubbles;
            public CanvasGroup burst;
            [NonSerialized] public Vector3 originalScale;
            [NonSerialized] public Vector2[] bubblePositions;
        }

        [SerializeField] private Entry[] entries;
        [SerializeField, Min(0)] private float initialDelay = 0.2f;
        [SerializeField, Min(0)] private float stagger = 0.16f;
        [SerializeField, Min(0.01f)] private float popSeconds = 0.5f;
        [SerializeField, Min(0.01f)] private float burstSeconds = 0.6f;
        [SerializeField] private AnimationCurve popScale = new AnimationCurve(
            new Keyframe(0, 0), new Keyframe(0.5f, 1.15f),
            new Keyframe(0.78f, 0.96f), new Keyframe(1, 1));
        private Coroutine introRoutine;

        private void Awake()
        {
            foreach (var entry in entries)
            {
                entry.originalScale = entry.button.localScale;
                entry.bubblePositions = new Vector2[entry.bubbles.Length];
                for (var i = 0; i < entry.bubbles.Length; i++)
                    entry.bubblePositions[i] = entry.bubbles[i].anchoredPosition;
            }
            HideAll();
        }

        private void Start() => PlayIntro();

        public void PlayIntro()
        {
            if (introRoutine != null) StopCoroutine(introRoutine);
            HideAll();
            introRoutine = StartCoroutine(Animate());
        }

        private void HideAll()
        {
            foreach (var entry in entries)
            {
                entry.visibility.alpha = 0;
                entry.visibility.interactable = false;
                entry.visibility.blocksRaycasts = false;
                entry.burst.alpha = 0;
                entry.burst.gameObject.SetActive(false);
            }
        }

        private IEnumerator Animate()
        {
            // Do not spend the intro duration on Editor/domain-load stalls before a frame renders.
            yield return null;
            var elapsed = 0f;
            var total = initialDelay + Mathf.Max(0, entries.Length - 1) * stagger + Mathf.Max(popSeconds, burstSeconds);
            while (elapsed < total)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var age = elapsed - initialDelay - i * stagger;
                    if (age < 0) continue;
                    var entry = entries[i];
                    var pop = Mathf.Clamp01(age / popSeconds);
                    entry.visibility.alpha = Mathf.Clamp01(pop * 4);
                    entry.button.localScale = entry.originalScale * popScale.Evaluate(pop);
                    var burst = Mathf.Clamp01(age / burstSeconds);
                    entry.burst.gameObject.SetActive(burst < 1);
                    entry.burst.alpha = Mathf.Sin(burst * Mathf.PI);
                    for (var j = 0; j < entry.bubbles.Length; j++)
                    {
                        entry.bubbles[j].anchoredPosition = entry.bubblePositions[j] * Mathf.Lerp(0.35f, 1.3f, burst);
                        entry.bubbles[j].localScale = Vector3.one * Mathf.Lerp(1.1f, 0.15f, burst);
                    }
                    entry.visibility.interactable = pop >= 1;
                    entry.visibility.blocksRaycasts = pop >= 1;
                }
                yield return null;
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            }
            Restore();
            introRoutine = null;
        }

        private void OnDisable()
        {
            if (introRoutine != null) StopCoroutine(introRoutine);
            introRoutine = null;
            Restore();
        }

        private void Restore()
        {
            foreach (var entry in entries)
            {
                entry.button.localScale = entry.originalScale;
                entry.visibility.alpha = 1;
                entry.visibility.interactable = true;
                entry.visibility.blocksRaycasts = true;
                entry.burst.alpha = 0;
                entry.burst.gameObject.SetActive(false);
            }
        }
    }
}

