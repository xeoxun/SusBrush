using UnityEngine;

namespace SusBrush.UI
{
    public sealed class NicknameSavedToast : MonoBehaviour
    {
        [SerializeField] private RectTransform toast;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float displaySeconds = 3;
        private float shownAt;
        private bool showing;
        private void Awake() => Hide();
        private void OnDisable() => Hide();

        public void Show()
        {
            shownAt = Time.unscaledTime;
            showing = true;
            toast.gameObject.SetActive(true);
            toast.SetAsLastSibling();
            toast.localScale = Vector3.one * 0.65f;
            group.alpha = 0;
        }

        private void Update()
        {
            if (!showing) return;
            var elapsed = Time.unscaledTime - shownAt;
            var t = Mathf.Clamp01(elapsed / 0.25f);
            var back = 1 + 2.70158f * Mathf.Pow(t - 1, 3) + 1.70158f * Mathf.Pow(t - 1, 2);
            toast.localScale = Vector3.one * Mathf.LerpUnclamped(0.65f, 1, back);
            group.alpha = Mathf.Min(Mathf.Clamp01(elapsed / 0.08f),
                Mathf.Clamp01((displaySeconds - elapsed) / 0.2f));
            if (elapsed >= displaySeconds) Hide();
        }

        private void Hide()
        {
            showing = false;
            if (toast != null) toast.gameObject.SetActive(false);
        }
    }
}
