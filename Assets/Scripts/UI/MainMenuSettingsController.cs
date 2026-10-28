using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuSettingsController : MonoBehaviour
{
    public static event System.Action VolumesChanged;
    private static readonly Vector2Int[] Resolutions = {new(960, 540), new(1280, 720), new(1920, 1080)};
    [SerializeField] private GameObject settingsPopup, soundPage, displayPage, languagePage;
    [SerializeField] private TMP_Dropdown resolutionDropdown, screenModeDropdown, languageDropdown;
    [SerializeField] private CanvasGroup resolutionGroup;
    [SerializeField] private Slider sfxVolumeSlider, masterVolumeSlider, musicVolumeSlider;
    [SerializeField] private TMP_Text sfxValueText, masterValueText, musicValueText;
    [SerializeField] private Toggle[] resolutionOptions, screenModeOptions;
    public static float SfxVolume => Mathf.Clamp01(PlayerPrefs.GetFloat("SfxVolume", 0.8f));
    public static int LanguageIndex => Mathf.Clamp(PlayerPrefs.GetInt("LanguageIndex", 0), 0, 2);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RestorePreferences()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f));
        if (!Application.isEditor)
            ApplyDisplay(PlayerPrefs.GetInt("DisplayResolutionIndex", 0), PlayerPrefs.GetInt("DisplayModeIndex", 0));
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetVolumeEvents() => VolumesChanged = null;
    private static void ApplyDisplay(int index, int mode)
    {
        var resolution = Resolutions[mode == 1 ? 2 : Mathf.Clamp(index, 0, 2)];
        Screen.SetResolution(resolution.x, resolution.y, mode == 1 ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
    }
    public void ToggleSettings() { if (SusBrush.UI.PopupTransition.IsVisible(settingsPopup)) CloseSettings(); else OpenSettings(); }
    public void OpenSettings()
    {
        settingsPopup.transform.SetAsLastSibling();
        SusBrush.UI.PopupTransition.SetVisible(settingsPopup, true);
        masterVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVolume", 1f));
        musicVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 1f));
        sfxVolumeSlider.SetValueWithoutNotify(SfxVolume);
        SetPercent(masterValueText, masterVolumeSlider.value);
        SetPercent(musicValueText, musicVolumeSlider.value);
        SetPercent(sfxValueText, SfxVolume);
        languageDropdown.SetValueWithoutNotify(LanguageIndex);
        screenModeDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("DisplayModeIndex", Screen.fullScreen ? 1 : 0));
        resolutionDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("DisplayResolutionIndex", Screen.width <= 960 ? 0 : Screen.width <= 1280 ? 1 : 2));
        RefreshDisplayOptions();
        SelectSettingsTab(0);
    }
    public void CloseSettings() { HideDisplayMenus(); SusBrush.UI.PopupTransition.SetVisible(settingsPopup, false); }
    public void SelectSettingsTab(int tab)
    {
        HideDisplayMenus();
        languageDropdown.Hide();
        soundPage.SetActive(tab == 0);
        displayPage.SetActive(tab == 1);
        languagePage.SetActive(tab == 2);
    }
    public void ToggleResolutionMenu()
    {
        if (screenModeDropdown.value != 0) return;
        var show = !resolutionDropdown.template.gameObject.activeSelf;
        HideDisplayMenus();
        resolutionDropdown.template.gameObject.SetActive(show);
    }
    public void ToggleScreenModeMenu()
    {
        var show = !screenModeDropdown.template.gameObject.activeSelf;
        HideDisplayMenus();
        screenModeDropdown.template.gameObject.SetActive(show);
    }
    public void SelectResolution(int index)
    {
        if (screenModeDropdown.value != 0) return;
        resolutionDropdown.SetValueWithoutNotify(index);
        HideDisplayMenus();
        RefreshDisplayOptions();
    }
    public void SelectScreenMode(int index)
    {
        screenModeDropdown.SetValueWithoutNotify(index);
        if (index == 1) resolutionDropdown.SetValueWithoutNotify(2);
        HideDisplayMenus();
        RefreshDisplayOptions();
    }
    private void RefreshDisplayOptions()
    {
        var windowed = screenModeDropdown.value == 0;
        if (!windowed) resolutionDropdown.SetValueWithoutNotify(2);
        resolutionGroup.interactable = resolutionGroup.blocksRaycasts = windowed;
        // Keep authored graphics intact; Selectable handles disabled interaction.
        for (var i = 0; i < resolutionOptions.Length; i++) resolutionOptions[i].SetIsOnWithoutNotify(i == resolutionDropdown.value);
        for (var i = 0; i < screenModeOptions.Length; i++) screenModeOptions[i].SetIsOnWithoutNotify(i == screenModeDropdown.value);
        resolutionDropdown.RefreshShownValue();
        screenModeDropdown.RefreshShownValue();
    }
    private void HideDisplayMenus()
    {
        resolutionDropdown.template.gameObject.SetActive(false);
        screenModeDropdown.template.gameObject.SetActive(false);
    }
    public void ApplySettings()
    {
        PlayerPrefs.SetInt("DisplayResolutionIndex", resolutionDropdown.value);
        PlayerPrefs.SetInt("DisplayModeIndex", screenModeDropdown.value);
        PlayerPrefs.SetInt("LanguageIndex", languageDropdown.value);
        UpdateMasterVolume(masterVolumeSlider.value);
        UpdateMusicVolume(musicVolumeSlider.value);
        UpdateSfxValueText(sfxVolumeSlider.value);
        ApplyDisplay(resolutionDropdown.value, screenModeDropdown.value);
        PlayerPrefs.Save();
        CloseSettings();
    }
    public void UpdateMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("MasterVolume", AudioListener.volume);
        SetPercent(masterValueText, value);
        VolumesChanged?.Invoke();
    }
    public void UpdateMusicVolume(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", Mathf.Clamp01(value));
        SetPercent(musicValueText, value);
        VolumesChanged?.Invoke();
    }
    public void UpdateSfxValueText(float value)
    {
        PlayerPrefs.SetFloat("SfxVolume", Mathf.Clamp01(value));
        SetPercent(sfxValueText, value);
        VolumesChanged?.Invoke();
    }
    private static void SetPercent(TMP_Text label, float value) => label.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100)}%";
}
