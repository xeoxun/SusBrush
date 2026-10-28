using System.Linq;
using SusBrush.Gameplay;
using TMPro;
using UnityEngine;

public sealed class GameResultPopupController : MonoBehaviour
{
    [SerializeField] private RoundController rounds;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text victoryTitleText;
    [SerializeField] private TMP_Text playerNamesText;
    [SerializeField] private TMP_Text playerRolesText;
    [SerializeField] private TMP_Text playerKeywordsText;
    [SerializeField] private TMP_Text playerResultsText;
    [SerializeField] private Color victoryColor = Color.white;
    [SerializeField] private Color defeatColor = Color.white;
    private bool finishShown;
    private int displayedCount = -1;
    private RoundOutcome displayedOutcome;
    private void Update()
    {
        if (!rounds.IsSpawned || rounds.Phase.Value != RoundPhase.Finished || rounds.RoomClosed)
        {
            finishShown = false;
            displayedCount = -1;
            if (popupRoot.activeSelf) popupRoot.SetActive(false);
            return;
        }
        if (!finishShown)
        {
            finishShown = true;
            SusBrush.UI.PopupTransition.SetVisible(popupRoot, true);
            popupRoot.transform.SetAsLastSibling();
            if (SusBrush.UI.SceneAudio.Instance != null) SusBrush.UI.SceneAudio.Instance.PlayGameAlarm();
        }
        if (popupRoot.activeSelf && (displayedCount != rounds.Results.Count || displayedOutcome != rounds.Outcome.Value)) RefreshResult();
    }
    public void HideResult() => SusBrush.UI.PopupTransition.SetVisible(popupRoot, false);
    private void RefreshResult()
    {
        displayedCount = rounds.Results.Count;
        displayedOutcome = rounds.Outcome.Value;
        var rows = Enumerable.Range(0, displayedCount).Select(i => rounds.Results[i]).ToArray();
        victoryTitleText.text = rounds.Cancelled.Value ? "참가자 이탈로 게임 중단"
            : rounds.Outcome.Value == RoundOutcome.Citizens ? "시민 승!"
            : rounds.Outcome.Value == RoundOutcome.Mafia ? "마피아 승!" : "무승부";
        playerNamesText.text = string.Join("\n", rows.Select(r => Safe(r.Nickname.ToString())));
        playerRolesText.text = string.Join("\n", rows.Select(r => r.Job == PlayerJob.Mafia ? "마피아" : r.Job == PlayerJob.Guard ? "경비원" : r.DifferentWord ? "정신병자" : "시민"));
        playerKeywordsText.text = string.Join("\n", rows.Select(r => r.Job == PlayerJob.Mafia ? "제시어 없음" : Safe(r.Word.ToString())));
        playerResultsText.text = string.Join("\n", rows.Select(ResultLabel));
    }
    private string ResultLabel(PlayerRoundResult row)
    {
        if (rounds.Cancelled.Value) return "중단";
        if (rounds.Outcome.Value == RoundOutcome.Draw) return "무승부";
        if (rounds.Outcome.Value == RoundOutcome.None) return "미집계";
        var won = (row.Job == PlayerJob.Mafia) == (rounds.Outcome.Value == RoundOutcome.Mafia);
        var color = ColorUtility.ToHtmlStringRGBA(won ? victoryColor : defeatColor);
        return $"<color=#{color}>{(won ? "승리" : "패배")}</color>";
    }
    private static string Safe(string text) => text.Replace("<", "＜").Replace(">", "＞");
}
