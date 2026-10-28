using SusBrush.Gameplay;
using UnityEngine;

namespace SusBrush.UI
{
    public sealed class SceneAudio : MonoBehaviour
    {
        public static SceneAudio Instance { get; private set; }
        [SerializeField] private AudioSource[] musicSources;
        [SerializeField] private AudioSource effectsSource;
        [SerializeField] private AudioClip mainButton;
        [SerializeField] private AudioClip subButton;
        [SerializeField] private AudioClip timerWarning;
        [SerializeField] private AudioClip gameAlarm;
        [SerializeField] private AudioClip gunshot;
        [SerializeField] private AudioClip emptyGun;
        [SerializeField] private AudioClip[] countdownSounds = new AudioClip[4];
        [SerializeField] private RoundController rounds;
        private float[] musicGains;
        private float effectsGain;
        private double warnedDeadline = double.NaN;

        private void Awake()
        {
            Instance = this;
            musicGains = new float[musicSources.Length];
            for (var i = 0; i < musicSources.Length; i++) musicGains[i] = musicSources[i].volume;
            effectsGain = effectsSource.volume;
            ApplyVolumes();
        }
        private void OnEnable() => MainMenuSettingsController.VolumesChanged += ApplyVolumes;
        private void OnDisable() => MainMenuSettingsController.VolumesChanged -= ApplyVolumes;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void ApplyVolumes()
        {
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1));
            for (var i = 0; i < musicSources.Length; i++)
                musicSources[i].volume = musicGains[i] * Mathf.Clamp01(PlayerPrefs.GetFloat("MusicVolume", 1));
            effectsSource.volume = effectsGain * MainMenuSettingsController.SfxVolume;
        }

        public void PlayButton(bool main) => Play(main ? mainButton : subButton);
        public void PlayGameAlarm() => Play(gameAlarm);
        public void PlayGunshot() => Play(gunshot);
        public void PlayEmptyGun() => Play(emptyGun);
        public void PlayCountdown(int index)
        {
            if (index >= 0 && index < countdownSounds.Length) Play(countdownSounds[index]);
        }
        private void Play(AudioClip clip) { if (clip != null) effectsSource.PlayOneShot(clip); }
        private void Update()
        {
            if (rounds == null || !rounds.IsSpawned || !rounds.IsPlaying) return;
            var remaining = rounds.Remaining;
            if (remaining > 0 && remaining <= 5 && warnedDeadline != rounds.PhaseEndsAt.Value)
            {
                warnedDeadline = rounds.PhaseEndsAt.Value;
                Play(timerWarning);
            }
        }
    }
}
