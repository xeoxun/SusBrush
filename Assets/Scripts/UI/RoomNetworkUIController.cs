using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RoomNetworkUIController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private GameObject roomBrowserPanel, createRoomPopup, joinCodePopup, roomEntryNotice;
    [SerializeField] private TMP_InputField roomNameSearchInput, roomNameInput, joinCodeInput;
    [SerializeField] private Toggle privateRoomToggle;
    [SerializeField] private Transform roomListContent;
    [SerializeField] private RoomListItemAction roomItemPrefab;
    [SerializeField] private GameObject emptyRoomMessage, loadingPanel;
    [SerializeField] private TMP_Text loadingText, joinCodeError, roomEntryMessage;
    private readonly List<ISessionInfo> cachedRooms = new();
    private readonly List<RoomListItemAction> displayedRooms = new();
    private ISessionInfo codeSelectedRoom;
    private bool refreshingRooms;
    [SerializeField, Min(2)] private float roomRefreshSeconds = 5;
    private float nextRoomRefreshAt;
    private void Update()
    {
        var sessions = LiarPainterSessionManager.Instance;
        if (SusBrush.UI.PopupTransition.IsVisible(roomBrowserPanel) && !refreshingRooms &&
            sessions != null && !sessions.IsBusy && Time.unscaledTime >= nextRoomRefreshAt)
            RefreshRooms();
    }

    private static void Show(GameObject panel)
    {
        panel.transform.SetAsLastSibling();
        SusBrush.UI.PopupTransition.SetVisible(panel, true);
    }
    public void ShowRoomBrowser() { Show(roomBrowserPanel); RefreshRooms(); }
    public void CloseRoomBrowser() => SusBrush.UI.PopupTransition.SetVisible(roomBrowserPanel, false);
    public void ShowCreatePopup() => Show(createRoomPopup);
    public void HideCreatePopup() => SusBrush.UI.PopupTransition.SetVisible(createRoomPopup, false);
    public void CloseRoomEntryNotice() => SusBrush.UI.PopupTransition.SetVisible(roomEntryNotice, false);
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public async void CreateRoom()
    {
        var sessions = LiarPainterSessionManager.Instance;
        if (sessions == null || sessions.IsBusy || SusBrush.UI.RoomEntryLoading.IsOpen) return;
        var name = roomNameInput.text.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > LiarPainterSessionManager.MaximumRoomNameLength)
        {
            ShowRoomEntryNotice("방 제목을 1~10글자로 입력해 주세요.");
            return;
        }
        SetLoading(false, "");
        if (!SusBrush.UI.RoomEntryLoading.Begin(gameSceneName)) return;
        var created = await sessions.CreateRoomAsync(name, privateRoomToggle.isOn);
        if (created) SusBrush.UI.RoomEntryLoading.MarkSessionReady();
        else SusBrush.UI.RoomEntryLoading.Cancel();
        if (this == null) return;
        if (!created) { ShowRoomEntryNotice("방 생성에 실패했습니다."); return; }
        HideCreatePopup();
        await EnterGameSceneAsHostAsync();
    }
    public async void RefreshRooms()
    {
        var sessions = LiarPainterSessionManager.Instance;
        if (refreshingRooms || sessions == null) return;
        refreshingRooms = true;
        nextRoomRefreshAt = Time.unscaledTime + roomRefreshSeconds;
        try
        {
            SetLoading(true, "방 목록을 불러오는 중...");
            var rooms = await sessions.QueryRoomsAsync();
            if (this == null) return;
            cachedRooms.Clear();
            cachedRooms.AddRange(rooms);
            FilterRooms(roomNameSearchInput.text);
            SetLoading(false, "");
            if (sessions.LastRoomQueryFailed && cachedRooms.Count == 0)
                ShowRoomEntryNotice("방 목록 조회에 실패했습니다. 잠시 후 새로고침해 주세요.");
        }
        finally { refreshingRooms = false; }
    }
    public void FilterRooms(string query)
    {
        foreach (var item in displayedRooms) if (item != null) Destroy(item.gameObject);
        displayedRooms.Clear();
        foreach (var room in cachedRooms)
        {
            if (!string.IsNullOrEmpty(query) && (room.Name == null || room.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)) continue;
            var item = Instantiate(roomItemPrefab, roomListContent);
            item.gameObject.SetActive(true);
            item.Configure(this, room);
            displayedRooms.Add(item);
        }
        emptyRoomMessage.SetActive(displayedRooms.Count == 0);
    }
    public async void SelectRoom(ISessionInfo room)
    {
        var sessions = LiarPainterSessionManager.Instance;
        if (room == null || sessions == null || sessions.IsBusy || SusBrush.UI.RoomEntryLoading.IsOpen) return;
        // Discovery is a snapshot: the service must decide whether the room is still full or locked.
        if (LiarPainterSessionManager.RequiresJoinCode(room))
        {
            ShowJoinCodePopup();
            codeSelectedRoom = room;
            return;
        }
        SetLoading(false, "");
        if (!SusBrush.UI.RoomEntryLoading.Begin(gameSceneName)) return;
        var joined = await sessions.JoinRoomByIdAsync(room.Id);
        if (joined) SusBrush.UI.RoomEntryLoading.MarkSessionReady();
        else SusBrush.UI.RoomEntryLoading.Cancel();
        if (this != null && !joined) ShowRoomEntryNotice(sessions.LastJoinError);
    }
    public void ShowJoinCodePopup()
    {
        codeSelectedRoom = null;
        joinCodeInput.text = "";
        joinCodeError.text = "";
        Show(joinCodePopup);
        joinCodeInput.ActivateInputField();
    }
    public void CloseCodePopup() { codeSelectedRoom = null; SusBrush.UI.PopupTransition.SetVisible(joinCodePopup, false); }
    public void SubmitJoinCode(string _) => SubmitJoinCode();
    public async void SubmitJoinCode()
    {
        var sessions = LiarPainterSessionManager.Instance;
        if (sessions == null || sessions.IsBusy || SusBrush.UI.RoomEntryLoading.IsOpen) return;
        var code = joinCodeInput.text.Trim();
        if (string.IsNullOrWhiteSpace(code)) { joinCodeError.text = "참가 코드를 입력하세요."; return; }
        if (codeSelectedRoom != null && !LiarPainterSessionManager.MatchesJoinCode(codeSelectedRoom, code))
        { joinCodeError.text = "선택한 방의 참가 코드와 일치하지 않습니다."; return; }
        joinCodeError.text = "입장하는 중...";
        SetLoading(false, "");
        if (!SusBrush.UI.RoomEntryLoading.Begin(gameSceneName)) return;
        var joined = await sessions.JoinRoomByCodeAsync(code);
        if (joined) SusBrush.UI.RoomEntryLoading.MarkSessionReady();
        else SusBrush.UI.RoomEntryLoading.Cancel();
        if (this == null) return;
        if (joined) CloseCodePopup();
        else joinCodeError.text = sessions.LastJoinError;
    }
    private void ShowRoomEntryNotice(string message)
    {
        SusBrush.UI.RoomEntryLoading.Cancel();
        SetLoading(false, "");
        roomEntryMessage.text = message;
        Show(roomEntryNotice);
    }
    private async Task EnterGameSceneAsHostAsync()
    {
        var manager = NetworkManager.Singleton;
        for (var i = 0; i < 120 && (manager == null || !manager.IsListening); i++)
        { await Task.Delay(50); manager = NetworkManager.Singleton; }
        if (this == null) return;
        if (manager != null && manager.IsServer && manager.IsListening)
        {
            var result = manager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            if (result != SceneEventProgressStatus.Started) ShowRoomEntryNotice("GameScene 전환 실패: " + result);
        }
        else ShowRoomEntryNotice("네트워크 시작을 확인하지 못했습니다.");
    }
    private void SetLoading(bool visible, string message)
    {
        loadingText.text = message;
        loadingPanel.SetActive(visible);
    }
}
