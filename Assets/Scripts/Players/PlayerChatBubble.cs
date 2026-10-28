using TMPro;
using UnityEngine;

namespace SusBrush.Players
{
    public sealed class PlayerChatBubble : MonoBehaviour
    {
        [SerializeField] private GameObject bubble;
        [SerializeField] private RectTransform body;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float displaySeconds = 3;
        [SerializeField] private Vector2 minimumSize = new Vector2(80, 70);
        [SerializeField, Min(1)] private float maximumWidth = 380;
        private float hideAt;
        private void Awake() => Hide();
        private void OnDisable() => Hide();

        public void Show(string message)
        {
            message = PlayerChat.NormalizeChat(message);
            if (string.IsNullOrEmpty(message)) return;
            messageText.richText = false;
            messageText.text = message;
            // Message stretches inside Body; use the padding authored on its RectTransform.
            var textRect = messageText.rectTransform;
            var padding = new Vector2(Mathf.Max(0, textRect.offsetMin.x - textRect.offsetMax.x),
                Mathf.Max(0, textRect.offsetMin.y - textRect.offsetMax.y));
            var unwrapped = messageText.GetPreferredValues(message, Mathf.Infinity, Mathf.Infinity);
            var width = Mathf.Clamp(Mathf.Ceil(unwrapped.x + padding.x),
                Mathf.Max(minimumSize.x, padding.x + 1), Mathf.Max(maximumWidth, minimumSize.x, padding.x + 1));
            var wrapped = messageText.GetPreferredValues(message, Mathf.Max(1, width - padding.x), Mathf.Infinity);
            body.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            body.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(minimumSize.y, Mathf.Ceil(wrapped.y + padding.y)));
            hideAt = Time.unscaledTime + displaySeconds;
            bubble.SetActive(true);
        }

        private void Update()
        {
            if (bubble.activeSelf && Time.unscaledTime >= hideAt) Hide();
        }

        public void Hide()
        {
            if (bubble != null) bubble.SetActive(false);
        }
    }
}
