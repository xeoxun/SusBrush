using System.Collections.Generic;
using System.Linq;
using SusBrush.Drawing;
using SusBrush.Players;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace SusBrush.Gameplay
{
    public enum RoundPhase : byte { Lobby = 0, RoleReveal = 1, Playing = 2, Finished = 4, RoundIntro = 5, Voting = 6, Defense = 7, QuestionReveal = 8 }
    public enum RoundOutcome : byte { None, Citizens, Mafia, Draw }

    public sealed class RoundController : NetworkBehaviour
    {
        [SerializeField] private MatchRules rules;
        [SerializeField] private KeywordContentCatalog contentCatalog;
        public static RoundController Instance { get; private set; }
        public readonly NetworkVariable<RoundPhase> Phase = new(RoundPhase.Lobby);
        public readonly NetworkVariable<int> RoundNumber = new(0);
        public readonly NetworkVariable<int> MatchRoundCount = new(2);
        public readonly NetworkVariable<Unity.Collections.FixedString128Bytes> Category = new();
        public readonly NetworkVariable<Unity.Collections.FixedString512Bytes> Question = new();
        private readonly HashSet<string> usedQuestions = new();
        public const int QuestionRevealSeconds = 5;
        [SerializeField] private VotingController voting;
        [SerializeField] private PaintMarks paintMarks;
        public IReadOnlyList<PlayerRole> Participants => participants;
        public readonly NetworkVariable<double> PhaseEndsAt = new(0);
        public readonly NetworkVariable<RoundOutcome> Outcome = new(RoundOutcome.None);
        public readonly NetworkVariable<bool> Cancelled = new(false);
        public bool RoomClosed { get; private set; }
        public event System.Action<string> PlayerDied;
        public event System.Action<string> GuessRejected;
        private bool hosted;
        private readonly List<PlayerRole> participants = new();
        private readonly HashSet<ulong> participantClients = new();
        private string commonWord;
        private NetworkList<PlayerRoundResult> results;
        public NetworkList<PlayerRoundResult> Results => results;
        private float nextLobbyCheck;
        public bool IsPlaying => Phase.Value == RoundPhase.Playing;
        public bool CanReady => !RoomClosed && (Phase.Value == RoundPhase.Lobby || Phase.Value == RoundPhase.Finished);
        public double Remaining => IsSpawned ? System.Math.Max(0, PhaseEndsAt.Value - NetworkManager.ServerTime.Time) : 0;
        public int ShotsPerGame => rules.GuardShotsPerGame;
        public int TotalRounds => MatchRoundCount.Value;
        public const int RoundCountdownSeconds = 4;

        private void Awake()
        {
            Instance = this;
            results = new NetworkList<PlayerRoundResult>();
        }
        public override void OnNetworkSpawn()
        {
            hosted = IsServer;
            RoomClosed = false;
            NetworkManager.OnClientDisconnectCallback += OnDisconnected;
        }
        public override void OnNetworkDespawn()
        {
            NetworkManager.OnClientDisconnectCallback -= OnDisconnected;
            if (!hosted) RoomClosed = true;
        }
        private void OnDisconnected(ulong clientId)
        {
            if (IsServer && clientId != Unity.Netcode.NetworkManager.ServerClientId && participantClients.Contains(clientId)) AbortMatch();
            else if (!hosted && clientId == NetworkManager.LocalClientId) RoomClosed = true;
        }
        public override void OnDestroy() { if (Instance == this) Instance = null; base.OnDestroy(); }
        private List<PlayerRole> ConnectedPlayers()
        {
            var result = new List<PlayerRole>();
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                var obj = client.PlayerObject;
                var role = obj == null ? null : obj.GetComponent<PlayerRole>();
                if (role != null && role.IsSpawned) result.Add(role);
            }
            return result;
        }
        private bool CanStartMatch(List<PlayerRole> players)
            => players.Count >= rules.MinimumPlayers && players.Count <= rules.MaximumPlayers &&
               players.All(p => p.GetComponent<NetworkPlayer>().Ready.Value);
        private void Update()
        {
            if (!IsSpawned || !IsServer) return;
            if (CanReady)
            {
                if (Time.unscaledTime < nextLobbyCheck) return;
                nextLobbyCheck = Time.unscaledTime + 0.2f;
                var players = ConnectedPlayers();
                if (players.Count == NetworkManager.ConnectedClients.Count && CanStartMatch(players)) StartMatch(players);
                return;
            }
            if (Phase.Value != RoundPhase.Lobby && Phase.Value != RoundPhase.Finished && participants.Any(p => p == null || !p.IsSpawned))
            {
                AbortMatch();
                return;
            }
            if (Remaining > 0) return;
            if (Phase.Value == RoundPhase.RoleReveal) SetPhase(RoundPhase.QuestionReveal, QuestionRevealSeconds);
            else if (Phase.Value == RoundPhase.QuestionReveal) SetPhase(RoundPhase.RoundIntro, RoundCountdownSeconds);
            else if (Phase.Value == RoundPhase.RoundIntro) SetPhase(RoundPhase.Playing, rules.RoundDurationSeconds);
            else if (IsPlaying)
            {
                foreach (var player in participants) player.GetComponent<PlayerCombat>().CancelAimServer();
                voting.BeginServer();
                RepositionPlayers();
                SetPhase(RoundPhase.Voting, rules.VotingSeconds);
            }
            else if (Phase.Value == RoundPhase.Voting)
            {
                if (voting.ResolveServer()) SetPhase(RoundPhase.Defense, rules.DefenseSeconds);
                else CompleteRound();
            }
            else if (Phase.Value == RoundPhase.Defense)
            {
                voting.ExecuteServer();
                if (!CanReady) CompleteRound();
            }
        }
        private void StartMatch(List<PlayerRole> players)
        {
            var error = "제시어·질문 카탈로그가 연결되지 않았습니다.";
            if (contentCatalog == null || !contentCatalog.TryValidate(out error))
            {
                // A failed start requires players to ready again instead of retrying every lobby tick.
                foreach (var player in players) player.GetComponent<NetworkPlayer>().Ready.Value = false;
                Debug.LogError(error, this);
                return;
            }
            RoundNumber.Value = 0;
            usedQuestions.Clear();
            Question.Value = default;
            MatchRoundCount.Value = MatchRules.RoundCount;
            Cancelled.Value = false;
            results.Clear();
            if (LiarPainterSessionManager.Instance != null)
                _ = LiarPainterSessionManager.Instance.SetRoomPlayingAsync(true);
            participants.Clear();
            participants.AddRange(players);
            participantClients.Clear();
            foreach (var player in players) participantClients.Add(player.OwnerClientId);
            for (var i = participants.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (participants[i], participants[j]) = (participants[j], participants[i]);
            }
            BeginRound();
        }
        private void BeginRound()
        {
            var firstRound = RoundNumber.Value == 0;
            if (firstRound)
            {
                if (!contentCatalog.TryDrawWords(out var pair))
                { Debug.LogError("배정할 제시어가 부족합니다.", this); AbortMatch(); return; }
                commonWord = pair.commonWord;
                Category.Value = new Unity.Collections.FixedString128Bytes(pair.category);
                for (var i = 0; i < participants.Count; i++)
                    participants[i].AssignServer(i == 0 ? PlayerJob.Mafia : i == 1 ? PlayerJob.Guard : PlayerJob.Citizen,
                        i == 0 ? "" : i == 2 ? pair.differentWord : pair.commonWord, 1);
            }
            RoundNumber.Value++;
            if (!contentCatalog.TryDrawQuestion(Category.Value.ToString(), usedQuestions, out var question))
            { Debug.LogError("현재 분류에서 새로 뽑을 질문이 없습니다.", this); AbortMatch(); return; }
            usedQuestions.Add(question);
            Question.Value = new Unity.Collections.FixedString512Bytes(question);
            Outcome.Value = RoundOutcome.None;
            paintMarks.ClearServer();
            voting.ClearServer();
            // Roles, words and alive state persist for the entire match.
            for (var i = 0; i < participants.Count; i++)
            {
                var role = participants[i];
                if (!firstRound) role.AdvanceRoundServer(RoundNumber.Value);
                role.GetComponent<PlayerDrawing>().ClearDrawing();
                var position = DrawingArea.Instance.GetSpawnPosition(i);
                var body = role.GetComponentInChildren<Collider2D>();
                position = DrawingArea.Instance.Clamp(position, body, role.transform.position);
                role.GetComponent<NetworkTransform>().Teleport(position, role.transform.rotation, role.transform.localScale);
            }
            SetPhase(firstRound ? RoundPhase.RoleReveal : RoundPhase.QuestionReveal,
                firstRound ? rules.RoleRevealSeconds : QuestionRevealSeconds);
        }
        public void RepositionPlayers()
        {
            for (var i = 0; i < participants.Count; i++)
            {
                var role = participants[i];
                if (!role.Alive.Value) continue;
                var position = DrawingArea.Instance.GetSpawnPosition(i);
                role.GetComponent<NetworkTransform>().Teleport(position, role.transform.rotation, role.transform.localScale);
            }
        }
        private void CompleteRound()
        {
            if (RoundNumber.Value >= TotalRounds) { Outcome.Value = RoundOutcome.Draw; FinishMatch(false); }
            else BeginRound();
        }
        private void SetPhase(RoundPhase phase, double duration)
        {
            PhaseEndsAt.Value = NetworkManager.ServerTime.Time + duration;
            Phase.Value = phase;
        }
        private void FinishWithOutcome(RoundOutcome outcome)
        {
            if (!IsServer || CanReady) return;
            Outcome.Value = outcome;
            FinishMatch(false);
        }
        public void OnPlayerEliminated(PlayerRole player)
        {
            if (!IsServer || CanReady || !participants.Contains(player)) return;
            AnnounceDeathRpc(player.GetComponent<PlayerNickname>().Nickname.Value);
            if (player.Job.Value == PlayerJob.Mafia)
            {
                FinishWithOutcome(RoundOutcome.Citizens);
                return;
            }
            var alive = participants.Where(p => p != null && p.IsSpawned && p.Alive.Value).ToArray();
            if (alive.Count(p => p.Job.Value == PlayerJob.Mafia) == 1 &&
                alive.Count(p => p.Job.Value != PlayerJob.Mafia) == 1)
                FinishWithOutcome(RoundOutcome.Mafia);
        }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void AnnounceDeathRpc(Unity.Collections.FixedString64Bytes nickname)
            => PlayerDied?.Invoke(nickname.ToString());
        public bool? SubmitGuessServer(PlayerRole player, string guess)
        {
            if (!IsServer || Phase.Value != RoundPhase.Voting || Remaining <= 0 || !participants.Contains(player) || !player.Alive.Value || player.Job.Value != PlayerJob.Mafia) return null;
            if (!string.Equals(guess.Trim(), commonWord.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                RevealWrongGuessRpc(new Unity.Collections.FixedString128Bytes(guess.Trim()));
                return false;
            }
            FinishWithOutcome(RoundOutcome.Mafia);
            return true;
        }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void RevealWrongGuessRpc(Unity.Collections.FixedString128Bytes guess) => GuessRejected?.Invoke(guess.ToString());
        private void AbortMatch()
        {
            if (!IsServer || Phase.Value == RoundPhase.Finished ||
                (Phase.Value == RoundPhase.Lobby && participants.Count == 0)) return;
            FinishMatch(true);
        }
        private void FinishMatch(bool cancelled)
        {
            Cancelled.Value = cancelled;
            if (cancelled) Outcome.Value = RoundOutcome.None;
            CaptureResults();
            paintMarks.ClearServer();
            voting.ClearServer();
            SetPhase(RoundPhase.Finished, 0);
            // Clear ready flags before accepting another game; revive spectators for the lobby.
            foreach (var player in participants.Concat(ConnectedPlayers()).Distinct())
            {
                if (player == null || !player.IsSpawned) continue;
                player.GetComponent<NetworkPlayer>().Ready.Value = false;
                player.GetComponent<PlayerDrawing>().ClearDrawing();
                player.ResetServer();
            }
            participants.Clear();
            participantClients.Clear();
            if (LiarPainterSessionManager.Instance != null)
                _ = LiarPainterSessionManager.Instance.SetRoomPlayingAsync(false);
        }
        private void CaptureResults()
        {
            results.Clear();
            foreach (var player in participants.Where(p => p != null && p.IsSpawned).OrderBy(p => p.OwnerClientId))
                results.Add(new PlayerRoundResult
                {
                    Nickname = player.GetComponent<PlayerNickname>().Nickname.Value,
                    Word = player.Word.Value,
                    Job = player.Job.Value,
                    DifferentWord = player.Job.Value == PlayerJob.Citizen && player.Word.Value.ToString() != commonWord
                });
        }
    }
}
