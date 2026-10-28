using SusBrush.Players;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.Gameplay
{
    public enum PlayerJob : byte { Citizen, Guard, Mafia }

    public sealed class PlayerRole : NetworkBehaviour
    {
        [SerializeField] private GameObject visual;
        [SerializeField] private GameObject nickname;
        [SerializeField] private GameObject readyStatus;
        [SerializeField] private Collider2D body;
        [SerializeField] private PlayerDeathView deathView;
        public readonly NetworkVariable<PlayerJob> Job = new(PlayerJob.Citizen, NetworkVariableReadPermission.Owner);
        // Only the guard's identity is public; other jobs and words remain owner-only.
        public readonly NetworkVariable<bool> IsPublicGuard = new(false);
        public readonly NetworkVariable<FixedString128Bytes> Word = new(default, NetworkVariableReadPermission.Owner);
        public readonly NetworkVariable<int> AssignedRound = new(0, NetworkVariableReadPermission.Owner);
        public readonly NetworkVariable<bool> Alive = new(true);
        public bool IsSpectating => IsSpawned && !Alive.Value;
        public bool CanMove => Alive.Value && (RoundController.Instance == null ||
            RoundController.Instance.CanReady || (RoundController.Instance.IsPlaying &&
            RoundController.Instance.Remaining > 0 && AssignedRound.Value == RoundController.Instance.RoundNumber.Value));
        public bool CanDraw => CanMove;

        public override void OnNetworkSpawn()
        {
            Alive.OnValueChanged += OnAliveChanged;
            OnAliveChanged(true, Alive.Value);
        }
        public override void OnNetworkDespawn() => Alive.OnValueChanged -= OnAliveChanged;
        private void OnAliveChanged(bool previous, bool current)
        {
            visual.SetActive(current);
            nickname.SetActive(current);
            body.enabled = current;
            if (!current) readyStatus.SetActive(false);
        }
        public void AssignServer(PlayerJob job, string word, int round)
        {
            if (!IsServer) return;
            Job.Value = job;
            IsPublicGuard.Value = job == PlayerJob.Guard;
            Word.Value = new FixedString128Bytes(word);
            AssignedRound.Value = round;
            Alive.Value = true;
            GetComponent<PlayerCombat>().ResetServer();
        }
        public void AdvanceRoundServer(int round)
        {
            if (!IsServer) return;
            AssignedRound.Value = round;
            GetComponent<PlayerCombat>().CancelAimServer();
        }
        public void ResetServer()
        {
            if (!IsServer) return;
            Job.Value = PlayerJob.Citizen;
            IsPublicGuard.Value = false;
            Word.Value = default;
            AssignedRound.Value = 0;
            Alive.Value = true;
            GetComponent<PlayerCombat>().ResetServer();
        }
        public void EliminateServer(Vector2 hitDirection = default, Vector3? impactPoint = null)
        {
            if (!IsServer || !Alive.Value || RoundController.Instance == null || !RoundController.Instance.IsPlaying || RoundController.Instance.Remaining <= 0) return;
            Alive.Value = false;
            deathView.PlayServer(hitDirection, impactPoint ?? transform.position);
            RoundController.Instance.OnPlayerEliminated(this);
        }
        public void ExecuteServer()
        {
            if (!IsServer || !Alive.Value) return;
            Alive.Value = false;
            RoundController.Instance.OnPlayerEliminated(this);
        }
    }
}
