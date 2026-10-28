using UnityEngine;

namespace SusBrush.UI
{
    // Lives on the active Canvas so an inactive role panel does not stop its timer.
    public sealed class TimedSlidePanel : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Vector2 shownPosition;
        [SerializeField] private Vector2 hiddenPosition;
        [SerializeField, Min(0)] private float displaySeconds = 5;
        [SerializeField, Min(0.01f)] private float slideSeconds = 0.3f;
        private Vector2 from;
        private float startedAt;
        private float hideAt;
        private int phase;

        private void Awake() => HideImmediately();
        private void OnDisable() => HideImmediately();

        public void Show()
        {
            if (panel == null) return;
            from = panel.anchoredPosition;
            startedAt = Time.unscaledTime;
            phase = 1;
        }

        private void Update()
        {
            if (panel == null || phase == 0) return;
            if (phase == 2)
            {
                if (Time.unscaledTime < hideAt) return;
                from = panel.anchoredPosition;
                startedAt = Time.unscaledTime;
                phase = 3;
            }
            var t = Mathf.Clamp01((Time.unscaledTime - startedAt) / slideSeconds);
            panel.anchoredPosition = Vector2.LerpUnclamped(from,
                phase == 1 ? shownPosition : hiddenPosition, Mathf.SmoothStep(0, 1, t));
            if (t < 1) return;
            if (phase == 1) { phase = 2; hideAt = Time.unscaledTime + displaySeconds; }
            else phase = 0;
        }

        private void HideImmediately()
        {
            phase = 0;
            if (panel != null) panel.anchoredPosition = hiddenPosition;
        }
    }
}
