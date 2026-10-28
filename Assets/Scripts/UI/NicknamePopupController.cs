using System.Linq;
using TMPro;
using UnityEngine;

public sealed class NicknamePopupController : MonoBehaviour
{
    public const string PlayerNameKey = "LiarPainter.PlayerName";
    public const int MaximumNicknameLength = 10;
    public static bool IsValidNickname(string value) => !string.IsNullOrEmpty(value) &&
        value.Length >= 2 && value.Length <= MaximumNicknameLength &&
        !value.Any(character => char.IsWhiteSpace(character) || char.IsControl(character));
    public static string CurrentNickname { get; private set; } = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetNickname() => CurrentNickname = string.Empty;

    [SerializeField] private GameObject popup;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TMP_Text[] playerNameLabels;
    [SerializeField] private SusBrush.UI.NicknameSavedToast savedToast;

    private void Awake()
    {
        if (nicknameInput != null)
        {
            nicknameInput.characterLimit = MaximumNicknameLength;
            nicknameInput.onValidateInput += ValidateNicknameCharacter;
        }

        if (popup != null)
        {
            popup.SetActive(true);
            popup.transform.SetAsLastSibling();
        }
        if (nicknameInput != null)
        {
            nicknameInput.text = CurrentNickname;
            nicknameInput.ActivateInputField();
        }
        UpdatePlayerNameLabels(CurrentNickname);
    }

    private void OnDestroy()
    {
        if (nicknameInput != null)
        {
            nicknameInput.onValidateInput -= ValidateNicknameCharacter;
        }
    }

    private char ValidateNicknameCharacter(string text, int index, char character) =>
        char.IsWhiteSpace(character) || char.IsControl(character) ? '\0' : character;

    public void ConfirmNickname(string _) => ConfirmNickname();

    public void ConfirmNickname()
    {
        string nickname = nicknameInput != null ? nicknameInput.text : string.Empty;
        if (!IsValidNickname(nickname))
        {
            nicknameInput?.ActivateInputField();
            return;
        }

        CurrentNickname = nickname;
        PlayerPrefs.SetString(PlayerNameKey, nickname);
        PlayerPrefs.Save();
        if (savedToast != null) savedToast.Show();
        UpdatePlayerNameLabels(nickname);
        foreach (SusBrush.Players.PlayerNickname player in FindObjectsByType<SusBrush.Players.PlayerNickname>(FindObjectsSortMode.None))
        {
            if (player.IsOwner && player.IsSpawned) player.SetNickname(nickname);
        }
    }

    private void UpdatePlayerNameLabels(string nickname)
    {
        foreach (TMP_Text label in playerNameLabels)
        {
            label.text = $"PLAYER  |  {nickname}";
        }
    }
}
