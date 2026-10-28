using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public sealed class NetworkChatUIController : MonoBehaviour
{
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private TMP_Text historyText;
    [SerializeField] private UnityEngine.UI.ScrollRect historyScroll;
    [SerializeField] private GameObject chatPopup;
    [SerializeField] private TMP_Text latestMessageText;
    private readonly Queue<string> messages = new();
    private bool refocusInput;
    private bool historyDirty;
    private void OnEnable()
    {
        SusBrush.Players.PlayerChat.ChatReceived += Receive;
    }
    private void OnDisable()
    {
        SusBrush.Players.PlayerChat.ChatReceived -= Receive;
    }

    public bool IsOpen => SusBrush.UI.PopupTransition.IsVisible(chatPopup);
    public void ToggleChat() => SetChatOpen(!IsOpen);
    public void CloseChat() => SetChatOpen(false);
    public void SetChatOpen(bool open)
    {
        if (chatPopup == null) return;
        if (!open)
        {
            chatInput.DeactivateInputField();
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null && events.currentSelectedGameObject == chatInput.gameObject)
                events.SetSelectedGameObject(null);
        }
        SusBrush.UI.PopupTransition.SetVisible(chatPopup, open);
        if (open) { chatPopup.transform.SetAsLastSibling(); historyDirty = true; chatInput?.ActivateInputField(); }
    }
    public void Submit(string _) => Send();
    public void Send()
    {
        if (chatInput == null) return;
        refocusInput = IsOpen;
        var obj = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (obj == null || !obj.TryGetComponent(out SusBrush.Players.PlayerChat player) || !player.SendChat(chatInput.text)) return;
        chatInput.SetTextWithoutNotify("");
        chatInput.ActivateInputField();
    }
    private void LateUpdate()
    {
        // TMP can deactivate the field after invoking onSubmit; restore focus afterwards.
        if (historyDirty && IsOpen)
        {
            historyDirty = false;
            if (historyText != null) { historyText.richText = true; historyText.text = string.Join("\n", messages); }
            RefreshHistoryScroll();
        }
        if (!refocusInput) return;
        refocusInput = false;
        if (IsOpen && chatInput != null) chatInput.ActivateInputField();
    }
    [SerializeField] private Color[] nameColors;

    public string FormatMessage(int colorIndex, string name, string message)
    {
        string color = ColorUtility.ToHtmlStringRGB(nameColors[Mathf.Clamp(colorIndex, 0, nameColors.Length - 1)]);
        // Keep player-supplied text from injecting TMP formatting tags.
        string safeName = (name ?? string.Empty).Replace("<", "＜").Replace(">", "＞");
        string safeMessage = (message ?? string.Empty).Replace("<", "＜").Replace(">", "＞");
        return $"<color=#{color}>{safeName}</color>: {safeMessage}";
    }

    private void Receive(int colorIndex, string name, string message)
    {
        string formatted = FormatMessage(colorIndex, name, message);
        messages.Enqueue(formatted);
        if (latestMessageText != null)
        {
            latestMessageText.richText = true;
            latestMessageText.text = formatted;
        }
        while (messages.Count > 100) messages.Dequeue();
        historyDirty = true;
    }

    private void RefreshHistoryScroll()
    {
        if (historyScroll == null || !historyScroll.gameObject.activeInHierarchy) return;
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(historyScroll.content);
        historyScroll.StopMovement();
        historyScroll.verticalNormalizedPosition = 0f;
    }

}
