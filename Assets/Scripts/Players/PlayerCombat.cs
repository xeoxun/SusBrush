using SusBrush.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.Players
{
    public sealed class PlayerCombat : NetworkBehaviour
    {
        [SerializeField] private PlayerRole role;
        [SerializeField] private Bullet bulletPrefab;
        [SerializeField] private GameObject aimVisual;
        [SerializeField] private Texture2D crosshairCursor;
        [SerializeField] private Vector2 cursorHotspot;
        [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
        private bool cursorApplied;
        public readonly NetworkVariable<bool> Aiming = new(false);
        public readonly NetworkVariable<int> ShotsRemaining = new(0, NetworkVariableReadPermission.Owner);
        private bool localAim;
        private Vector2 direction = Vector2.right;
        public bool IsAiming => IsOwner ? localAim : Aiming.Value;
        public bool CanAim => role.CanDraw && role.Job.Value == PlayerJob.Guard &&
            RoundController.Instance != null && RoundController.Instance.IsPlaying;
        public override void OnNetworkSpawn() => Aiming.OnValueChanged += OnAimChanged;
        public override void OnNetworkDespawn() { Aiming.OnValueChanged -= OnAimChanged; aimVisual.SetActive(false); RestoreCursor(); }
        private void OnDisable() => RestoreCursor();
        private void RestoreCursor()
        {
            if (!cursorApplied) return;
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
            cursorApplied = false;
        }
        private void OnAimChanged(bool previous, bool current) => localAim = current;
        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;
            if (!CanAim) localAim = false;
            var showCursor = localAim && Application.isFocused;
            if (showCursor && !cursorApplied && crosshairCursor != null)
            {
                Cursor.SetCursor(crosshairCursor, cursorHotspot, cursorMode);
                cursorApplied = true;
            }
            else if (!showCursor) RestoreCursor();
            aimVisual.SetActive(localAim);
            if (localAim) aimVisual.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            if (IsServer && !CanAim && Aiming.Value) Aiming.Value = false;
        }
        public void ToggleAim()
        {
            if (!IsOwner || !CanAim) return;
            localAim = !localAim;
            SetAimRpc(localAim);
        }
        public void PointAt(Vector2 target)
        {
            var delta = target - (Vector2)transform.position;
            if (delta.sqrMagnitude > 0.0001f) direction = delta.normalized;
        }
        public void Fire()
        {
            if (!IsOwner || !CanAim || !localAim) return;
            if (ShotsRemaining.Value <= 0)
            {
                if (UI.SceneAudio.Instance != null) UI.SceneAudio.Instance.PlayEmptyGun();
                return;
            }
            FireRpc(direction);
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetAimRpc(bool aiming) => Aiming.Value = CanAim && aiming;
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void FireRpc(Vector2 shotDirection)
        {
            if (!CanAim || !Aiming.Value || RoundController.Instance.Remaining <= 0 ||
                !float.IsFinite(shotDirection.x) || !float.IsFinite(shotDirection.y) || shotDirection.sqrMagnitude < 0.001f) return;
            if (ShotsRemaining.Value <= 0)
            {
                PlayEmptyGunRpc();
                return;
            }
            ShotsRemaining.Value--;
            var bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            bullet.Initialize(shotDirection.normalized, NetworkObjectId, RoundController.Instance.RoundNumber.Value);
            bullet.NetworkObject.Spawn();
            PlayGunshotRpc();
            Aiming.Value = false;
        }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void PlayGunshotRpc()
        {
            if (UI.SceneAudio.Instance != null) UI.SceneAudio.Instance.PlayGunshot();
        }
        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void PlayEmptyGunRpc()
        {
            if (UI.SceneAudio.Instance != null) UI.SceneAudio.Instance.PlayEmptyGun();
        }
        public void ResetServer()
        {
            if (!IsServer) return;
            Aiming.Value = false;
            ShotsRemaining.Value = role.Job.Value == PlayerJob.Guard ? RoundController.Instance.ShotsPerGame : 0;
        }
        public void CancelAimServer() { if (IsServer) Aiming.Value = false; }
    }
}
