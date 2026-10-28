using UnityEngine;

namespace SusBrush.Players
{
    // NGO interpolates observers, but the authoritative host needs a separate visual interpolation.
    [DisallowMultipleComponent]
    public sealed class PlayerVisualInterpolation : MonoBehaviour
    {
        [SerializeField] private NetworkPlayer player;
        [SerializeField] private Transform[] visuals;
        private Vector3[] offsets;
        private Vector3 previousPosition, currentPosition;
        private double sampledAt;
        private bool sampled, applied;

        private void Awake()
        {
            offsets = new Vector3[visuals.Length];
            for (var i = 0; i < visuals.Length; i++) offsets[i] = visuals[i].localPosition;
        }
        private void OnEnable()
        {
            sampled = false;
            player.MovementSimulated += OnMovement;
        }
        private void OnDisable()
        {
            player.MovementSimulated -= OnMovement;
            RestoreOffsets();
        }
        private void OnMovement(Vector3 previous, Vector3 current, bool drawing)
        {
            var teleported = !sampled || (previous - currentPosition).sqrMagnitude > .000001f;
            previousPosition = teleported ? current : previous;
            currentPosition = current;
            sampledAt = Time.unscaledTimeAsDouble;
            sampled = true;
        }
        private void LateUpdate()
        {
            if (!player.IsSpawned || !player.IsServer || !sampled)
            {
                if (applied) RestoreOffsets();
                return;
            }
            // Spawn, vote and round teleports must snap instead of sliding from an old buffer.
            if ((transform.position - currentPosition).sqrMagnitude > .000001f)
                previousPosition = currentPosition = transform.position;
            var t = Mathf.Clamp01((float)((Time.unscaledTimeAsDouble - sampledAt) * player.NetworkManager.NetworkConfig.TickRate));
            var position = Vector3.Lerp(previousPosition, currentPosition, t);
            for (var i = 0; i < visuals.Length; i++)
                visuals[i].position = position + transform.TransformVector(offsets[i]);
            applied = true;
        }
        private void RestoreOffsets()
        {
            if (offsets == null) return;
            for (var i = 0; i < visuals.Length; i++)
                if (visuals[i] != null) visuals[i].localPosition = offsets[i];
            applied = false;
        }
    }
}
