using TMPro;
using UnityEngine;

namespace SusBrush.Players
{
    public sealed class PlayerReadyView : MonoBehaviour
    {
        [SerializeField] private NetworkPlayer player;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string waitingText = "준비 중";
        [SerializeField] private string readyText = "준비 완료";
        [SerializeField] private Color readyColor = Color.white;
        private Color waitingColor;
        private void Awake() => waitingColor = label.color;
        private void OnEnable()
        {
            player.Ready.OnValueChanged += Refresh;
            Refresh(false, player.Ready.Value);
        }
        private void OnDisable() => player.Ready.OnValueChanged -= Refresh;
        private void Update()
        {
            var game = SusBrush.Gameplay.RoundController.Instance;
            var show = game == null || game.CanReady;
            if (label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
        }
        private void Refresh(bool previous, bool current)
        {
            label.text = current ? readyText : waitingText;
            label.color = current ? readyColor : waitingColor;
        }
    }
}
