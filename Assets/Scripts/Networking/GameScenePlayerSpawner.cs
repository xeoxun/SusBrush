using System.Collections.Generic;
using SusBrush.Gameplay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SusBrush.Networking
{
    [RequireComponent(typeof(NetworkManager))]
    public sealed class GameScenePlayerSpawner : MonoBehaviour
    {
        [SerializeField] private MatchRules rules;
        [SerializeField] private string gameScene = "GameScene";
        private NetworkManager manager;
        private NetworkSceneManager scenes;
        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
            manager.ConnectionApprovalCallback = Approve;
            manager.OnServerStarted += OnServerStarted;
        }
        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = manager.ConnectedClients.Count < rules.MaximumPlayers &&
                (RoundController.Instance == null || RoundController.Instance.CanReady);
            response.CreatePlayerObject = false;
            response.Pending = false;
            if (!response.Approved) response.Reason = "방이 가득 찼거나 이미 게임이 진행 중입니다.";
        }
        private void OnServerStarted()
        {
            UnbindScenes();
            scenes = manager.SceneManager;
            scenes.OnLoadEventCompleted += OnLoaded;
            scenes.OnSynchronizeComplete += SpawnPlayer;
            SpawnPlayer(NetworkManager.ServerClientId);
        }
        private void OnLoaded(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
        {
            if (sceneName == gameScene) foreach (var id in completed) SpawnPlayer(id);
        }
        private void SpawnPlayer(ulong id)
        {
            if (!manager.IsServer || SceneManager.GetActiveScene().name != gameScene ||
                !manager.ConnectedClients.TryGetValue(id, out var client) || client.PlayerObject != null) return;
            Instantiate(manager.NetworkConfig.PlayerPrefab).GetComponent<NetworkObject>().SpawnAsPlayerObject(id, true);
        }
        private void UnbindScenes()
        {
            if (scenes == null) return;
            scenes.OnLoadEventCompleted -= OnLoaded;
            scenes.OnSynchronizeComplete -= SpawnPlayer;
            scenes = null;
        }
        private void OnDestroy()
        {
            if (manager != null) manager.OnServerStarted -= OnServerStarted;
            UnbindScenes();
        }
    }
}
