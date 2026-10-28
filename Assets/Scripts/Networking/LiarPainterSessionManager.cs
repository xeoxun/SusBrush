using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class LiarPainterSessionManager : MonoBehaviour
{
    private const string GameTypeProperty = "gameType";
    private const string GameTypeValue = "LiarPainter";
    private const string CodeRequiredProperty = "codeRequired";
    private const string CodeCheckProperty = "codeCheck";
    private const string HostNicknameProperty = "hostNickname";
    private static string editorTestProfile;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTestProfile()
        => editorTestProfile = "editor_" + Guid.NewGuid().ToString("N").Substring(0, 20);

    private static Dictionary<string, PlayerProperty> CreatePlayerProperties() => new()
    {
        ["nickname"] = new PlayerProperty(NicknamePopupController.CurrentNickname)
    };

    public static string GetHostNickname(ISessionInfo room) => room.Properties != null &&
        room.Properties.TryGetValue(HostNicknameProperty, out var nickname) && !string.IsNullOrWhiteSpace(nickname.Value)
            ? nickname.Value : "방장";

    public static bool RequiresJoinCode(ISessionInfo room) => room.Properties != null &&
        room.Properties.TryGetValue(CodeRequiredProperty, out var flag) && flag.Value == "true";

    public static bool MatchesJoinCode(ISessionInfo room, string code) => room.Properties != null &&
        room.Properties.TryGetValue(CodeCheckProperty, out var check) &&
        check.Value == HashJoinCode(room.Id, code);

    private static string HashJoinCode(string id, string code)
    {
        using (var hash = System.Security.Cryptography.SHA256.Create())
            return Convert.ToBase64String(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(
                id + ":" + (code ?? string.Empty).Trim().ToUpperInvariant())));
    }
    public static LiarPainterSessionManager Instance { get; private set; }

    [SerializeField] private SusBrush.Gameplay.MatchRules rules;
    public int MaximumPlayers => rules.MaximumPlayers;
    public const int MaximumRoomNameLength = 10;
    [SerializeField] private string lobbySceneName = "MainScene";

    public ISession CurrentSession { get; private set; }
    public bool IsHost => CurrentSession != null && CurrentSession.IsHost;
    public string JoinCode => CurrentSession?.Code ?? string.Empty;
    public int RoomCapacity => CurrentSession?.MaxPlayers ?? MaximumPlayers;
    public bool IsBusy { get; private set; }
    private readonly List<ISessionInfo> lastRoomList = new();
    private readonly System.Threading.SemaphoreSlim roomQueryGate = new(1, 1);
    private DateTime nextRoomQueryUtc = DateTime.MinValue;
    public bool LastRoomQueryFailed { get; private set; }
    public string LastJoinError { get; private set; } = string.Empty;
    private readonly System.Threading.SemaphoreSlim roomStateGate = new(1, 1);

    public static string GetRoomEntryError(ISessionInfo room)
    {
        if (room.IsLocked) return "게임 중이므로 입장할 수 없습니다.";
        if (room.AvailableSlots <= 0) return "방 정원이 가득 차 입장할 수 없습니다.";
        return string.Empty;
    }

    public async Task<bool> SetRoomPlayingAsync(bool playing)
    {
        await roomStateGate.WaitAsync();
        try
        {
            if (CurrentSession == null || !CurrentSession.IsHost) return false;
            var host = CurrentSession.AsHost();
            host.IsLocked = playing;
            await host.SavePropertiesAsync();
            return true;
        }
        catch (Exception exception) { ReportError(exception); return false; }
        finally { roomStateGate.Release(); }
    }

    private void ReportJoinError(Exception exception)
    {
        LastJoinError = "방에 입장할 수 없습니다. 방 상태와 참가 코드를 확인해 주세요.";
        for (Exception current = exception; current != null; current = current.InnerException)
        {
            if (current is Unity.Services.Lobbies.LobbyServiceException lobby)
            {
                if (lobby.Reason == Unity.Services.Lobbies.LobbyExceptionReason.LobbyFull)
                    LastJoinError = "방 정원이 가득 차 입장할 수 없습니다.";
                else if (lobby.Reason == Unity.Services.Lobbies.LobbyExceptionReason.LobbyLocked)
                    LastJoinError = "게임 중이므로 입장할 수 없습니다.";
                else if (lobby.Reason == Unity.Services.Lobbies.LobbyExceptionReason.LobbyNotFound)
                    LastJoinError = "방이 종료되었거나 참가 코드가 올바르지 않습니다.";
            }
        }
        Debug.LogException(exception);
        SessionError?.Invoke(LastJoinError);
    }

    public event Action<ISession> SessionJoined;
    public event Action<string> SessionClosed;
    public event Action<string> SessionError;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        BindNetworkCallbacks();
    }

    private void OnDisable()
    {
        UnbindNetworkCallbacks();
    }

    public async Task<bool> CreateRoomAsync(string roomName, bool isPrivate = false)
    {
        roomName = roomName?.Trim();
        if (string.IsNullOrWhiteSpace(roomName) || roomName.Length > MaximumRoomNameLength)
        {
            SessionError?.Invoke(string.IsNullOrWhiteSpace(roomName) ? "방 제목을 입력하세요." : "방 제목은 최대 10글자까지 입력할 수 있습니다.");
            return false;
        }
        if (IsBusy || CurrentSession != null) return false;

        IsBusy = true;
        try
        {
            await WaitForNetworkShutdownAsync();
            await EnsureServicesReadyAsync();
            SessionOptions options = new SessionOptions
            {
                PlayerProperties = CreatePlayerProperties(),
                Name = roomName,
                // 세션 서비스가 이 값을 기준으로 초과 입장을 거절합니다.
                MaxPlayers = MaximumPlayers,
                // Both room types must appear in discovery; codeRequired controls our UI.
                IsPrivate = false,
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    [HostNicknameProperty] = new SessionProperty(
                        NicknamePopupController.CurrentNickname, VisibilityPropertyOptions.Public),
                    [CodeRequiredProperty] = new SessionProperty(isPrivate ? "true" : "false", VisibilityPropertyOptions.Public),
                    [GameTypeProperty] = new SessionProperty(
                        GameTypeValue,
                        VisibilityPropertyOptions.Public,
                        PropertyIndex.String1)
                }
            }.WithRelayNetwork();

            IHostSession createdSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            try
            {
                if (isPrivate)
                {
                    createdSession.SetProperty(CodeCheckProperty, new SessionProperty(
                        HashJoinCode(createdSession.Id, createdSession.Code), VisibilityPropertyOptions.Public));
                    await createdSession.SavePropertiesAsync();
                }
            }
            catch
            {
                await createdSession.DeleteAsync();
                throw;
            }
            CurrentSession = createdSession;
            SubscribeSession(CurrentSession);
            BindNetworkCallbacks();
            SessionJoined?.Invoke(CurrentSession);
            return true;
        }
        catch (Exception exception)
        {
            ReportError(exception);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> JoinRoomByCodeAsync(string joinCode)
    {
        LastJoinError = string.Empty;
        if (IsBusy || CurrentSession != null || string.IsNullOrWhiteSpace(joinCode)) return false;
        IsBusy = true;
        try
        {
            await WaitForNetworkShutdownAsync();
            await EnsureServicesReadyAsync();
            CurrentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode.Trim().ToUpperInvariant(),
                new JoinSessionOptions { PlayerProperties = CreatePlayerProperties() });
            SubscribeSession(CurrentSession);
            BindNetworkCallbacks();
            SessionJoined?.Invoke(CurrentSession);
            return true;
        }
        catch (Exception exception)
        {
            ReportJoinError(exception);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> JoinRoomByIdAsync(string sessionId)
    {
        LastJoinError = string.Empty;
        if (IsBusy || CurrentSession != null || string.IsNullOrWhiteSpace(sessionId)) return false;

        IsBusy = true;
        try
        {
            await WaitForNetworkShutdownAsync();
            await EnsureServicesReadyAsync();
            CurrentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId.Trim(),
                new JoinSessionOptions { PlayerProperties = CreatePlayerProperties() });
            SubscribeSession(CurrentSession);
            BindNetworkCallbacks();
            SessionJoined?.Invoke(CurrentSession);
            return true;
        }
        catch (Exception exception)
        {
            CurrentSession = null;
            ReportJoinError(exception);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<IList<ISessionInfo>> QueryRoomsAsync()
    {
        await roomQueryGate.WaitAsync();
        var rooms = new List<ISessionInfo>();
        try
        {
            if (DateTime.UtcNow < nextRoomQueryUtc) return new List<ISessionInfo>(lastRoomList);
            LastRoomQueryFailed = false;
            await WaitForNetworkShutdownAsync();
            await EnsureServicesReadyAsync();
            string continuation = null;
            do
            {
                QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions
                {
                    Count = 50,
                    ContinuationToken = continuation,
                    FilterOptions = new List<FilterOption>
                    {
                        new FilterOption(FilterField.StringIndex1, GameTypeValue, FilterOperation.Equal)
                    }
                });
                rooms.AddRange(results.Sessions);
                // A token can be returned even when the final page is not full.
                if (results.Sessions.Count < 50) break;
                string next = results.ContinuationToken;
                if (next == continuation) break;
                continuation = next;
                if (!string.IsNullOrEmpty(continuation)) await Task.Delay(1500);
            } while (!string.IsNullOrEmpty(continuation));
            lastRoomList.Clear();
            lastRoomList.AddRange(rooms);
            nextRoomQueryUtc = DateTime.UtcNow.AddSeconds(2);
            return new List<ISessionInfo>(lastRoomList);
        }
        catch (Exception exception)
        {
            LastRoomQueryFailed = true;
            nextRoomQueryUtc = DateTime.UtcNow.AddSeconds(5);
            // Keep successful pages and previously displayed rooms on query failure.
            foreach (var room in rooms)
            {
                int index = lastRoomList.FindIndex(previous => previous.Id == room.Id);
                if (index >= 0) lastRoomList[index] = room;
                else lastRoomList.Add(room);
            }
            ReportError(exception);
            return new List<ISessionInfo>(lastRoomList);
        }
        finally
        {
            roomQueryGate.Release();
        }
    }




    public Task LeaveRoomAsync() => LeaveCurrentSessionAsync(
        IsHost ? "호스트가 방을 종료했습니다." : "방에서 나갔습니다.");

    private async Task LeaveCurrentSessionAsync(string reason)
    {
        if (IsBusy) return;
        IsBusy = true;
        var session = CurrentSession;
        CurrentSession = null;
        UnsubscribeSession(session);
        UnbindNetworkCallbacks();
        try
        {
            // The session SDK stops its network modules and removes lobby membership together.
            // Do not shut NGO down or change scenes before it has completed.
            if (session != null && session.IsHost) await session.AsHost().DeleteAsync();
            else if (session != null) await session.LeaveAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
        finally
        {
            try
            {
                var manager = NetworkManager.Singleton;
                if (manager != null && manager.IsListening && !manager.ShutdownInProgress) manager.Shutdown();
                await WaitForNetworkShutdownAsync();
                lastRoomList.Clear();
                nextRoomQueryUtc = DateTime.MinValue;
                ReturnToLobby(reason);
            }
            finally { IsBusy = false; }
        }
    }

    private static async Task WaitForNetworkShutdownAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (NetworkManager.Singleton != null && NetworkManager.Singleton.ShutdownInProgress)
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("이전 방 연결을 종료하는 중입니다. 잠시 후 다시 입장해 주세요.");
            await Task.Delay(25);
        }
    }
    private static async Task EnsureServicesReadyAsync()
    {
        var profile = GetLocalTestProfile();
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions options = new InitializationOptions();
            options.SetProfile(profile);
            await UnityServices.InitializeAsync(options);
        }
        // Also isolate Play sessions when Enter Play Mode skips domain reload.
        if (AuthenticationService.Instance.Profile != profile)
        {
            if (AuthenticationService.Instance.IsSignedIn) AuthenticationService.Instance.SignOut();
            AuthenticationService.Instance.SwitchProfile(profile);
        }
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private static string GetLocalTestProfile()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (string.Equals(arguments[i], "-liarProfile", StringComparison.OrdinalIgnoreCase))
                return SanitizeProfile(arguments[i + 1]);
        }

        // 같은 Windows 빌드를 여러 개 실행해도 각 프로세스가 서로 다른 익명 플레이어가 됩니다.
        return Application.isEditor
            ? editorTestProfile ??= "editor_" + Guid.NewGuid().ToString("N").Substring(0, 20)
            : $"local_{System.Diagnostics.Process.GetCurrentProcess().Id}";
    }

    private static string SanitizeProfile(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "local_player";
        char[] characters = value.Trim().ToCharArray();
        for (int i = 0; i < characters.Length; i++)
        {
            char character = characters[i];
            if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                characters[i] = '_';
        }
        return new string(characters, 0, Mathf.Min(characters.Length, 30));
    }

    private void BindNetworkCallbacks()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null) return;
        manager.OnClientDisconnectCallback -= HandleClientDisconnected;
        manager.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    private void UnbindNetworkCallbacks()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null) manager.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private async void HandleClientDisconnected(ulong clientId)
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.IsServer || clientId != manager.LocalClientId ||
            IsBusy || CurrentSession == null) return;
        // A dropped transport must also leave its service session before another join.
        try { await LeaveCurrentSessionAsync("호스트 연결이 끊겨 방이 종료되었습니다."); }
        catch (Exception exception) { ReportError(exception); }
    }
    private void SubscribeSession(ISession session)
    {
        if (session != null) session.RemovedFromSession += HandleRemovedFromSession;
    }

    private void UnsubscribeSession(ISession session)
    {
        if (session != null) session.RemovedFromSession -= HandleRemovedFromSession;
    }

    private void HandleRemovedFromSession()
    {
        if (CurrentSession == null) return;
        UnsubscribeSession(CurrentSession);
        CurrentSession = null;
        ReturnToLobby("방이 종료되었습니다.");
    }

    private void ReturnToLobby(string reason)
    {
        UnbindNetworkCallbacks();
        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening) manager.Shutdown();
        SessionClosed?.Invoke(reason);
        PlayerPrefs.SetString("LiarPainter.LastDisconnectReason", reason);
        if (!string.IsNullOrWhiteSpace(lobbySceneName) && SceneManager.GetActiveScene().name != lobbySceneName)
            SceneManager.LoadScene(lobbySceneName);
    }

    private void ReportError(Exception exception)
    {
        Debug.LogException(exception);
        SessionError?.Invoke(exception.Message);
    }
}
