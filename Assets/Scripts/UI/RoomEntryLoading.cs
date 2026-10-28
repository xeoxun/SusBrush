using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SusBrush.UI
{
    // A separate persistent Canvas keeps covering the screen during NGO scene synchronization.
    public sealed class RoomEntryLoading : MonoBehaviour
    {
        [Serializable]
        private sealed class Walker
        {
            public Image image;
            public Sprite[] frames;
            [NonSerialized] public Vector2 position;
        }

        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image progressFill;
        [SerializeField] private Walker[] walkers;
        [SerializeField] private TMPro.TMP_Text tipText;
        [SerializeField] private string[] tips;
        private static RoomEntryLoading instance;
        private static readonly int[] Steps = { 0, 1, 2, 1 };
        private string gameScene;
        private float startedAt, shownProgress, targetProgress, finishedAt = -1;
        private bool sessionReady, sceneReady;
        private LiarPainterSessionManager sessions;

        public static bool IsOpen => instance != null;

        public static bool Begin(string sceneName)
        {
            if (instance != null) return false;
            var prefab = Resources.Load<RoomEntryLoading>("UI/RoomEntryLoading");
            if (prefab == null) { Debug.LogError("RoomEntryLoading prefab is missing."); return false; }
            instance = Instantiate(prefab);
            instance.gameScene = sceneName;
            instance.startedAt = Time.unscaledTime;
            instance.targetProgress = .12f;
            instance.group.alpha = 1;
            instance.progressFill.fillAmount = 0;
            instance.ShowRandomTip();
            return true;
        }

        public static void MarkSessionReady()
        {
            if (instance == null) return;
            instance.sessionReady = true;
            instance.targetProgress = Mathf.Max(instance.targetProgress, .68f);
        }

        public static void Cancel()
        {
            if (instance == null) return;
            var previous = instance;
            instance = null;
            previous.gameObject.SetActive(false);
            Destroy(previous.gameObject);
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            foreach (var walker in walkers) walker.position = walker.image.rectTransform.anchoredPosition;
            SceneManager.sceneLoaded += OnSceneLoaded;
            sessions = LiarPainterSessionManager.Instance;
            if (sessions != null) sessions.SessionClosed += OnSessionClosed;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (sessions != null) sessions.SessionClosed -= OnSessionClosed;
            if (instance == this) instance = null;
        }

        private void OnSessionClosed(string reason) => Cancel();

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == gameScene) { sceneReady = true; targetProgress = .9f; }
            else if (sceneReady) Cancel();
        }

        private void ShowRandomTip()
        {
            if (tipText == null || tips == null || tips.Length == 0) return;
            tipText.text = "Tip: " + tips[UnityEngine.Random.Range(0, tips.Length)];
        }
        private void Update()
        {
            var elapsed = Time.unscaledTime - startedAt;
            // Service calls have no byte progress; ease within their stage without claiming completion.
            if (!sessionReady) targetProgress = Mathf.Max(targetProgress, .12f + .38f * (1 - Mathf.Exp(-elapsed / 5)));
            for (var i = 0; i < walkers.Length; i++)
            {
                var walker = walkers[i];
                var frame = Steps[(Mathf.FloorToInt(elapsed * 7 + i) % Steps.Length)];
                walker.image.sprite = walker.frames[frame];
                walker.image.rectTransform.anchoredPosition = walker.position +
                    Vector2.up * (Mathf.Abs(Mathf.Sin(elapsed * 7 * Mathf.PI / 2 + i)) * 4);
            }

            var manager = NetworkManager.Singleton;
            if (manager != null && manager.IsConnectedClient)
                targetProgress = Mathf.Max(targetProgress, .76f);
            if (SceneManager.GetActiveScene().name == gameScene)
            {
                sceneReady = true;
                targetProgress = Mathf.Max(targetProgress, .9f);
            }
            // Scene load alone is not enough: wait for our actual network player as well.
            var ready = sessionReady && sceneReady && manager != null && manager.IsConnectedClient &&
                manager.LocalClient?.PlayerObject != null && manager.LocalClient.PlayerObject.IsSpawned;
            if (ready && elapsed >= 1.5f) targetProgress = 1;
            shownProgress = Mathf.MoveTowards(shownProgress, targetProgress, Time.unscaledDeltaTime * .65f);
            progressFill.fillAmount = shownProgress;
            if (targetProgress < 1 || shownProgress < 1) return;
            if (finishedAt < 0) finishedAt = Time.unscaledTime;
            group.alpha = 1 - Mathf.Clamp01((Time.unscaledTime - finishedAt - .2f) / .2f);
            if (group.alpha <= 0) Cancel();
        }
    }
}
