using SusBrush.Players;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.UI
{
    public sealed class ReadyButton : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string readyText = "준비";
        [SerializeField] private string cancelText = "준비 취소";
        private NetworkPlayer localPlayer;
        private NetworkObject boundObject;
        private void Update()
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null) { button.interactable = false; return; }
            var obj = networkManager.IsConnectedClient ? networkManager.LocalClient?.PlayerObject : null;
            if (boundObject != obj)
            {
                boundObject = obj;
                localPlayer = obj != null ? obj.GetComponent<NetworkPlayer>() : null;
            }
            button.interactable = localPlayer != null && localPlayer.IsSpawned &&
                (SusBrush.Gameplay.RoundController.Instance == null || SusBrush.Gameplay.RoundController.Instance.CanReady);
            var text = localPlayer != null && localPlayer.Ready.Value ? cancelText : readyText;
            if (label.text != text) label.text = text;
        }
        public void Toggle() => localPlayer?.ToggleReady();
    }
}
