using System.Collections.Generic;
using SusBrush.Gameplay;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.UI
{
    // Keep this component on the active HUD, outside the panels it controls.
    public sealed class GameplayNoticeView : MonoBehaviour
    {
        [SerializeField] private RoundController rounds;
        [SerializeField] private GameObject majorUI;
        [SerializeField] private GameObject citizen;
        [SerializeField] private GameObject guard;
        [SerializeField] private GameObject mafia;
        [SerializeField] private TMP_Text bulletText;
        [SerializeField] private CanvasGroup notice;
        [SerializeField] private TMP_Text noticeText;
        [SerializeField] private TMP_Text drawingQuestion;
        [SerializeField, Min(0.1f)] private float displaySeconds = 5;
        [SerializeField, Min(0.01f)] private float fadeSeconds = 0.7f;
        [SerializeField] private string votingMessage = "투표시간입니다. 마피아를 찾아 투표하세요!";
        [SerializeField] private string defenseFormat = "{0}님, 최후의 변론을 해주세요.";
        [SerializeField, TextArea] private string publicWrongGuessFormat = "마피아가 작성한 제시어: {0}";
        [SerializeField, TextArea] private string mafiaWrongGuessMessage = "오답입니다. 다시 제출해주세요.";
        [SerializeField] private string guessVotingOnlyMessage = "투표시간에만 제시어를 맞출 수 있습니다";
        private bool localNotice;
        private readonly Queue<string> messages = new();
        private RoundPhase previousPhase;
        private float shownAt;
        private bool showing;
        private int displayedBullets = -1;

        private void OnEnable()
        {
            rounds.PlayerDied += OnPlayerDied;
            rounds.GuessRejected += OnGuessRejected;
            previousPhase = RoundPhase.Lobby;
            HideNotice();
            majorUI.SetActive(false);
            if (drawingQuestion != null) drawingQuestion.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            rounds.PlayerDied -= OnPlayerDied;
            rounds.GuessRejected -= OnGuessRejected;
            messages.Clear();
            HideNotice();
            majorUI.SetActive(false);
            if (drawingQuestion != null) drawingQuestion.gameObject.SetActive(false);
        }

        private void OnPlayerDied(string nickname)
        {
            // Names are user input: render them as plain text, not TMP tags.
            messages.Enqueue(nickname.Replace("<", "＜").Replace(">", "＞") + "가 사망했습니다.");
        }
        public void ShowGuessVotingOnly()
        {
            messages.Clear();
            noticeText.text = guessVotingOnlyMessage;
            notice.alpha = 1;
            notice.gameObject.SetActive(true);
            notice.transform.SetAsLastSibling();
            shownAt = Time.unscaledTime;
            showing = true;
            localNotice = true;
        }

        private void OnGuessRejected(string guess)
        {
            messages.Clear();
            HideNotice();

            var manager = NetworkManager.Singleton;
            var player = manager != null && manager.IsConnectedClient ? manager.LocalClient?.PlayerObject : null;
            var role = player != null ? player.GetComponent<PlayerRole>() : null;
            var isMafia = role != null && role.IsSpawned && role.Job.Value == PlayerJob.Mafia;

            messages.Enqueue(isMafia ? mafiaWrongGuessMessage : string.Format(publicWrongGuessFormat, guess.Replace("<", "＜").Replace(">", "＞")));
        }

        private void Update()
        {
            var phase = rounds.Phase.Value;
            var active = rounds.IsSpawned && !rounds.RoomClosed && !rounds.CanReady;
            var manager = NetworkManager.Singleton;
            var player = manager != null && manager.IsConnectedClient ? manager.LocalClient?.PlayerObject : null;
            var role = player != null ? player.GetComponent<PlayerRole>() : null;
            var assigned = active && role != null && role.AssignedRound.Value == rounds.RoundNumber.Value;
            var questionVisible = active && phase != RoundPhase.RoleReveal && !string.IsNullOrEmpty(rounds.Question.Value.ToString());

            if (drawingQuestion != null)
            {
                drawingQuestion.gameObject.SetActive(questionVisible);
                if (questionVisible) drawingQuestion.text = rounds.Question.Value.ToString();
            }

            majorUI.SetActive(assigned);
            citizen.SetActive(assigned && role.Job.Value == PlayerJob.Citizen);
            guard.SetActive(assigned && role.Job.Value == PlayerJob.Guard);
            mafia.SetActive(assigned && role.Job.Value == PlayerJob.Mafia);

            if (assigned && role.Job.Value == PlayerJob.Guard)
            {
                var bullets = player.GetComponent<SusBrush.Players.PlayerCombat>().ShotsRemaining.Value;
                if (displayedBullets != bullets)
                {
                    displayedBullets = bullets;
                    bulletText.SetText("남은 총알: {0}", bullets);
                }
            }

            if (active && phase == RoundPhase.QuestionReveal && !localNotice)
            {
                messages.Clear();
                showing = false;
                noticeText.text = rounds.Question.Value.ToString();
                notice.alpha = 1;
                notice.gameObject.SetActive(true);
                notice.transform.SetAsLastSibling();
                previousPhase = phase;
                return;
            }

            if (previousPhase == RoundPhase.QuestionReveal && phase != previousPhase) HideNotice();

            if (!active || phase == RoundPhase.RoleReveal && previousPhase != phase)
            {
                messages.Clear();
                HideNotice();
            }
            if (active && previousPhase != phase && phase == RoundPhase.Voting)
            {
                messages.Clear(); HideNotice();
                messages.Enqueue(votingMessage);
            }
            if (active && previousPhase != phase && phase == RoundPhase.Defense)
            {
                messages.Clear(); HideNotice();
                messages.Enqueue(string.Format(defenseFormat, rounds.GetComponent<VotingController>().DefendantName.Replace("<", "＜").Replace(">", "＞")));
            }
            previousPhase = phase;

            if (!active) return;

            if (!showing && messages.Count > 0)
            {
                noticeText.text = messages.Dequeue();
                notice.alpha = 1;
                notice.gameObject.SetActive(true);
                shownAt = Time.unscaledTime;
                showing = true;
            }

            if (!showing) return;
            var elapsed = Time.unscaledTime - shownAt;
            var fade = Mathf.Min(fadeSeconds, displaySeconds);
            notice.alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - displaySeconds + fade) / fade));

            if (elapsed >= displaySeconds) HideNotice();
        }

        private void HideNotice()
        {
            showing = false;
            localNotice = false;
            notice.alpha = 0;
            notice.gameObject.SetActive(false);
        }
    }
}
