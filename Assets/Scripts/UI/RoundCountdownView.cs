using SusBrush.Gameplay;
using UnityEngine;

namespace SusBrush.UI
{
    // The server phase deadline keeps every client's countdown in sync.
    public sealed class RoundCountdownView : MonoBehaviour
    {
        [SerializeField] private RoundController rounds;
        [SerializeField] private UnityEngine.UI.Image countdownImage;
        [SerializeField] private Sprite[] frames = new Sprite[4];
        private int lastIndex = -1;

        private void Awake() => countdownImage.gameObject.SetActive(false);
        private void OnDisable() { countdownImage.gameObject.SetActive(false); lastIndex = -1; }
        private void Update()
        {
            var visible = rounds.IsSpawned && !rounds.RoomClosed && rounds.Phase.Value == RoundPhase.RoundIntro;
            if (countdownImage.gameObject.activeSelf != visible)
            {
                countdownImage.gameObject.SetActive(visible);
                if (visible) countdownImage.transform.SetAsLastSibling();
            }
            if (!visible) { lastIndex = -1; return; }
            var index = Mathf.Clamp(Mathf.FloorToInt(RoundController.RoundCountdownSeconds - (float)rounds.Remaining), 0, frames.Length - 1);
            if (countdownImage.sprite != frames[index]) countdownImage.sprite = frames[index];
            if (lastIndex != index)
            {
                lastIndex = index;
                if (SceneAudio.Instance != null) SceneAudio.Instance.PlayCountdown(index);
            }
        }
    }
}
