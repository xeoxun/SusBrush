using UnityEngine;

namespace SusBrush.UI
{
    [DisallowMultipleComponent]
    public sealed class PopupTransition : MonoBehaviour
    {
        [SerializeField] private RectTransform visual;
        [SerializeField] private CanvasGroup group;
        [SerializeField, Min(.01f)] private float enterSeconds = .25f;
        [SerializeField, Min(.01f)] private float fadeInSeconds = .08f;
        [SerializeField, Min(.01f)] private float exitSeconds = .2f;
        private Vector3 originalScale;
        private float originalAlpha;
        private bool originalInteractable, originalBlocksRaycasts, initialized, closing, animating;
        private float elapsed, exitAlpha;

        public static bool IsVisible(GameObject panel) => panel != null && panel.activeSelf &&
            (!panel.TryGetComponent<PopupTransition>(out var effect) || !effect.closing);

        public static void SetVisible(GameObject panel, bool visible)
        {
            if (panel == null) return;
            if (panel.TryGetComponent<PopupTransition>(out var effect))
            {
                if (visible) effect.Show(); else effect.Hide();
            }
            else if (panel.activeSelf != visible) panel.SetActive(visible);
        }

        private void Initialize()
        {
            if (initialized) return;
            originalScale = visual.localScale;
            originalAlpha = group.alpha;
            originalInteractable = group.interactable;
            originalBlocksRaycasts = group.blocksRaycasts;
            initialized = true;
        }

        private void OnEnable()
        {
            Initialize();
            closing = false;
            animating = true;
            elapsed = 0;
            visual.localScale = originalScale * .65f;
            group.alpha = 0;
            group.interactable = originalInteractable;
            group.blocksRaycasts = originalBlocksRaycasts;
        }

        public void Show()
        {
            if (gameObject.activeSelf && !closing) return;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            else OnEnable();
        }

        public void Hide()
        {
            if (!gameObject.activeSelf || closing) return;
            if (!isActiveAndEnabled) { gameObject.SetActive(false); return; }
            closing = true;
            animating = true;
            elapsed = 0;
            exitAlpha = group.alpha;
            group.interactable = false;
            // Keep the backdrop blocking clicks until it has disappeared.
        }

        private void Update()
        {
            if (!animating) return;
            elapsed += Time.unscaledDeltaTime;
            if (closing)
            {
                group.alpha = exitAlpha * (1 - Mathf.Clamp01(elapsed / exitSeconds));
                if (elapsed >= exitSeconds) gameObject.SetActive(false);
                return;
            }
            var t = Mathf.Clamp01(elapsed / enterSeconds) - 1;
            var back = 1 + 2.70158f * t * t * t + 1.70158f * t * t;
            visual.localScale = originalScale * Mathf.LerpUnclamped(.65f, 1, back);
            group.alpha = originalAlpha * Mathf.Clamp01(elapsed / fadeInSeconds);
            if (elapsed >= Mathf.Max(enterSeconds, fadeInSeconds)) animating = false;
        }

        private void OnDisable()
        {
            if (!initialized) return;
            animating = closing = false;
            visual.localScale = originalScale;
            group.alpha = originalAlpha;
            group.interactable = originalInteractable;
            group.blocksRaycasts = originalBlocksRaycasts;
        }
    }
}
