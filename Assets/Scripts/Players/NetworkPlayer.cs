using SusBrush.Gameplay;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace SusBrush.Players
{
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        [SerializeField, Min(0.01f)] private float movementSpeed = 2.8f;
        [SerializeField, Min(0.05f)] private float inputTimeout = 0.3f;
        [SerializeField, Min(0)] private float arrivalDistance = 0.03f;
        [SerializeField, Min(0)] private float spawnClearance = 0.6f;
        [SerializeField] private Collider2D movementBounds;
        [SerializeField] private PlayerRole role;
        [SerializeField] private PlayerCombat combat;
        public readonly NetworkVariable<bool> Ready = new(false);
        public readonly NetworkVariable<bool> Drawing = new(false);
        private NetworkTickSystem movementTicks;
        private Vector2 moveInput;
        private Vector2 pointerTarget;
        private bool wantsToDraw;
        private double lastInputTime;
        public event System.Action<Vector3, Vector3, bool> MovementSimulated;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            movementTicks = NetworkManager.NetworkTickSystem;
            movementTicks.Tick += SimulateTick;
            if (DrawingArea.Instance == null) return;
            var occupied = new bool[DrawingArea.Instance.SpawnPointCount];
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                var other = client.PlayerObject;
                if (other == null || other == NetworkObject) continue;
                for (var i = 0; i < occupied.Length; i++)
                    if (Vector3.Distance(other.transform.position, DrawingArea.Instance.GetSpawnPosition(i)) < spawnClearance)
                        occupied[i] = true;
            }
            var slot = System.Array.FindIndex(occupied, used => !used);
            var position = ClampToPlayfield(DrawingArea.Instance.GetSpawnPosition(Mathf.Max(0, slot)));
            GetComponent<NetworkTransform>().Teleport(position, transform.rotation, transform.localScale);
        }

        public void ToggleReady()
        {
            if (IsSpawned && IsOwner) SetReadyRpc(!Ready.Value);
        }

        private Vector3 ClampToPlayfield(Vector3 position)
            => DrawingArea.Instance.Clamp(position, movementBounds, transform.position);

        public void SendInput(Vector2 movement, Vector2 target, bool drawing)
        {
            if (IsSpawned && IsOwner) SubmitInputRpc(movement, target, drawing);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetReadyRpc(bool ready)
        {
            if (RoundController.Instance == null || RoundController.Instance.CanReady) Ready.Value = ready;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
        private void SubmitInputRpc(Vector2 movement, Vector2 target, bool drawing)
        {
            if (!float.IsFinite(movement.x) || !float.IsFinite(movement.y) ||
                !float.IsFinite(target.x) || !float.IsFinite(target.y)) return;
            moveInput = Vector2.ClampMagnitude(movement, 1);
            pointerTarget = target;
            wantsToDraw = drawing;
            lastInputTime = Time.unscaledTimeAsDouble;
        }

        public override void OnNetworkDespawn()
        {
            if (movementTicks != null) movementTicks.Tick -= SimulateTick;
            movementTicks = null;
            moveInput = Vector2.zero;
            wantsToDraw = false;
        }

        private void SimulateTick()
        {
            if (!IsSpawned || !IsServer || DrawingArea.Instance == null) return;
            var recent = Time.unscaledTimeAsDouble - lastInputTime <= inputTimeout && (role == null || role.CanMove);
            Drawing.Value = recent && wantsToDraw && (role == null || role.CanDraw) && (combat == null || !combat.Aiming.Value);
            var previous = transform.position;
            if (!recent)
            {
                MovementSimulated?.Invoke(previous, previous, false);
                return;
            }
            var step = 1f / NetworkManager.NetworkConfig.TickRate;
            Vector3 next;
            if (Drawing.Value)
            {
                var target = ClampToPlayfield(pointerTarget);
                next = Vector2.Distance(transform.position, target) <= arrivalDistance
                    ? transform.position
                    : Vector3.MoveTowards(transform.position, target, movementSpeed * step);
            }
            else next = transform.position + (Vector3)(moveInput * (movementSpeed * step));
            transform.position = ClampToPlayfield(next);
            MovementSimulated?.Invoke(previous, transform.position, Drawing.Value);
        }
    }
}
