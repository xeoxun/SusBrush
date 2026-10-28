using TMPro;
using UnityEngine;

namespace SusBrush.UI
{
    public sealed class RoomHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text roomInfo;
        [SerializeField] private UnityEngine.UI.Button leaveButton;
        private LiarPainterSessionManager service;
        private string defaultText;
        private void Awake() => defaultText = roomInfo.text;
        private void OnEnable()
        {
            service = LiarPainterSessionManager.Instance;
            if (service != null) { service.SessionJoined += OnJoined; RefreshInfo(); }
        }
        private void OnDisable()
        {
            if (service != null) service.SessionJoined -= OnJoined;
        }
        private void Update()
        {
            leaveButton.interactable = service != null && !service.IsBusy;
            RefreshInfo();
        }
        private void OnJoined(Unity.Services.Multiplayer.ISession session) => RefreshInfo();
        private void RefreshInfo()
        {
            var room = service;
            if (room != null && room.CurrentSession != null)
                roomInfo.text = (room.CurrentSession?.Name ?? "") + (string.IsNullOrEmpty(room.JoinCode) ? "" : " | 참가 코드: " + room.JoinCode);
            else if (roomInfo.text != defaultText) roomInfo.text = defaultText;
        }
        public async void Leave()
        {
            leaveButton.interactable = false;
            if (LiarPainterSessionManager.Instance != null) await LiarPainterSessionManager.Instance.LeaveRoomAsync();
        }
    }
}
