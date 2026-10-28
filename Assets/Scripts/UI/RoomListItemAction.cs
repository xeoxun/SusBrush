using TMPro;
using UnityEngine;
using Unity.Services.Multiplayer;

// References and the JoinButton event are authored on the room-list prefab.
public sealed class RoomListItemAction : MonoBehaviour
{
    [SerializeField] private TMP_Text roomName;
    [SerializeField] private TMP_Text hostName;
    [SerializeField] private TMP_Text playerCount;
    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text privacy;
    private RoomNetworkUIController owner;
    private ISessionInfo room;
    public void Configure(RoomNetworkUIController controller, ISessionInfo info)
    {
        owner = controller;
        room = info;
        roomName.text = info.Name;
        hostName.text = LiarPainterSessionManager.GetHostNickname(info);
        playerCount.text = $"{info.MaxPlayers - info.AvailableSlots} / {info.MaxPlayers}";
        status.text = info.IsLocked ? "게임 중" : info.AvailableSlots <= 0 ? "정원 마감" : "대기 중";
        privacy.text = LiarPainterSessionManager.RequiresJoinCode(info) ? "비공개" : "공개";
    }
    public void Join()
    {
        if (owner != null && room != null) owner.SelectRoom(room);
    }
}
