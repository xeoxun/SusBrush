using UnityEngine;
using UnityEngine.InputSystem;

namespace SusBrush.UI
{
    // Keep this on the active Canvas, never on a popup that can be disabled.
    public sealed class SceneUIShortcuts : MonoBehaviour
    {
        [SerializeField] private MainMenuSettingsController settings;
        [SerializeField] private NetworkChatUIController chat;
        [SerializeField] private Key settingsKey = Key.Escape;
        [SerializeField] private Key chatKey = Key.Tab;
        [SerializeField] private TimedSlidePanel guidePanel;
        [SerializeField] private TimedSlidePanel roomPanel;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!Application.isFocused || keyboard == null) return;
            var focused = UnityEngine.EventSystems.EventSystem.current != null
                ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
            var typing = focused != null && (focused.GetComponentInParent<TMPro.TMP_InputField>() != null ||
                focused.GetComponentInParent<UnityEngine.UI.InputField>() != null);
            if (!typing)
            {
                if (keyboard.gKey.wasPressedThisFrame) guidePanel?.Show();
                if (keyboard.spaceKey.wasPressedThisFrame) roomPanel?.Show();
            }
            if (keyboard[settingsKey].wasPressedThisFrame)
            {
                if (chat != null && chat.IsOpen) chat.CloseChat();
                else if (settings != null) settings.ToggleSettings();
            }
            if (keyboard[chatKey].wasPressedThisFrame && chat != null) chat.ToggleChat();
            if ((keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) && chat != null && !chat.IsOpen)
            {
                var selected = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
                if (selected == null || selected.GetComponent<TMPro.TMP_InputField>() == null) chat.SetChatOpen(true);
            }
        }
    }
}
