using SusBrush.Gameplay;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.UI
{
    public sealed class RoundView : MonoBehaviour
    {
        [SerializeField] private RoundController rounds;
        [SerializeField] private TMP_Text privateRoleAndWord;
        [SerializeField] private TMP_Text timer;
        [SerializeField] private TMP_Text nowText;
        [SerializeField] private string waitingPhaseText = "게임 대기";
        [SerializeField] private string votingPhaseText = "투표 시간";
        [SerializeField] private string defensePhaseText = "최후의 변론";
        private string defaultPhaseText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private GameObject categoryPanel;
        [SerializeField] private GameObject popup;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text wordText;
        [SerializeField] private TMP_Text guideText;
        [SerializeField] private GameObject guessPanel;
        [SerializeField] private GameplayNoticeView notices;
        [SerializeField] private TMP_InputField guessInput;
        [SerializeField] private GameObject spectatorPanel;
        [SerializeField] private GameObject readyButton;
        [SerializeField] private GameObject answerButton;
        [SerializeField] private GameObject guideButton;
        [SerializeField, TextArea] private string citizenGuide = "그림을 그려 마피아를 찾아내세요!";
        [SerializeField, TextArea] private string guardGuide = "그림을 그리며 마피아를 찾아 검거하세요!";
        [SerializeField, TextArea] private string mafiaGuide = "시민들에게 주어진 제시어를 찾아 제한시간 내에 맞추세요!";
        private PlayerRole localRole;
        private bool guessOpen;
        private void Awake()
        {
            categoryPanel.SetActive(false);
            answerButton.SetActive(false);
            guideButton.SetActive(false);
            defaultPhaseText = nowText.text;
        }
        private void Update()
        {
            var phase = rounds.Phase.Value;
            var active = rounds.IsSpawned && !rounds.RoomClosed && phase != RoundPhase.Lobby && phase != RoundPhase.Finished;
            SetText(nowText, !active ? waitingPhaseText : phase == RoundPhase.Voting ? votingPhaseText
                : phase == RoundPhase.Defense ? defensePhaseText : defaultPhaseText);
            if (categoryPanel.activeSelf != active) categoryPanel.SetActive(active);
            if (guideButton.activeSelf != active) guideButton.SetActive(active);
            if (rounds.RoomClosed)
            {
                popup.SetActive(false);
                guessPanel.SetActive(false);
                spectatorPanel.SetActive(false);
                readyButton.SetActive(false);
                answerButton.SetActive(false);
                SetText(timer, "00:00");
                return;
            }
            var manager = NetworkManager.Singleton;
            var player = manager != null && manager.IsConnectedClient ? manager.LocalClient?.PlayerObject : null;
            if (localRole == null || localRole.NetworkObject != player)
                localRole = player != null ? player.GetComponent<PlayerRole>() : null;
            if (readyButton.activeSelf != rounds.CanReady) readyButton.SetActive(rounds.CanReady);
            var assigned = active && localRole != null && localRole.AssignedRound.Value == rounds.RoundNumber.Value;
            var spectating = assigned && localRole.IsSpectating;
            var showAnswer = assigned && localRole.Alive.Value && localRole.Job.Value == PlayerJob.Mafia;
            if (answerButton.activeSelf != showAnswer) answerButton.SetActive(showAnswer);
            if (spectatorPanel.activeSelf != spectating) spectatorPanel.SetActive(spectating);
            var reveal = assigned && rounds.Phase.Value == RoundPhase.RoleReveal;
            if (popup.activeSelf != reveal)
            {
                popup.SetActive(reveal);
                if (reveal && SceneAudio.Instance != null) SceneAudio.Instance.PlayGameAlarm();
            }
            var canGuess = assigned && phase == RoundPhase.Voting && localRole.Alive.Value && localRole.Job.Value == PlayerJob.Mafia;
            if (!canGuess) guessOpen = false;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var selected = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
            if (showAnswer && Application.isFocused && keyboard != null && keyboard.fKey.wasPressedThisFrame &&
                (selected == null || selected.GetComponentInParent<TMP_InputField>() == null)) ToggleGuess();
            PopupTransition.SetVisible(guessPanel, guessOpen);
            SetText(categoryText, active ? rounds.Category.Value.ToString() : "");
            if (assigned)
            {
                var job = localRole.Job.Value;
                var name = job == PlayerJob.Guard ? "경비원" : job == PlayerJob.Mafia ? "마피아" : "시민";
                var word = job == PlayerJob.Mafia ? "제시어 없음" : localRole.Word.Value.ToString();
                SetText(privateRoleAndWord, name + "\n" + word);
                SetText(roleText, name);
                SetText(wordText, word);
                SetText(guideText, job == PlayerJob.Guard ? guardGuide : job == PlayerJob.Mafia ? mafiaGuide : citizenGuide);
            }
            else SetText(privateRoleAndWord, "직업 · 제시어\n대기 중");
            var seconds = Mathf.CeilToInt((float)rounds.Remaining);
            SetText(timer, $"{seconds / 60:00}:{seconds % 60:00}");
        }
        public void ToggleGuess()
        {
            var manager = NetworkManager.Singleton;
            var player = manager != null && manager.IsConnectedClient ? manager.LocalClient?.PlayerObject : null;
            var role = player != null ? player.GetComponent<PlayerRole>() : null;
            if (rounds.RoomClosed || !rounds.IsSpawned || rounds.CanReady ||
                role == null || role.AssignedRound.Value != rounds.RoundNumber.Value ||
                !role.Alive.Value || role.Job.Value != PlayerJob.Mafia) return;
            if (rounds.Phase.Value != RoundPhase.Voting)
            {
                notices.ShowGuessVotingOnly();
                return;
            }
            guessOpen = !guessOpen;
            PopupTransition.SetVisible(guessPanel, guessOpen);
        }
        public void SubmitGuess() { if (localRole != null) localRole.GetComponent<PlayerGuess>().Submit(guessInput.text); }
        public void SubmitGuess(string _) => SubmitGuess();
        private static void SetText(TMP_Text label, string value) { if (label != null && label.text != value) label.text = value; }
    }
}
