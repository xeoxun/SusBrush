using Unity.Netcode;
using UnityEngine;

namespace SusBrush.Gameplay
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class Bullet : NetworkBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 12;
        [SerializeField, Min(0.01f)] private float radius = 0.06f;
        [SerializeField, Min(0.1f)] private float lifetime = 3;
        private Vector2 direction;
        private ulong shooter;
        private int round;
        private float age;
        private readonly NetworkVariable<float> shotAngle = new();
        public void Initialize(Vector2 heading, ulong shooterId, int roundNumber)
        {
            direction = heading.normalized;
            shooter = shooterId;
            round = roundNumber;
            // The paint head points along the sprite's local right (+X) axis.
            shotAngle.Value = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ApplyRotation();
        }
        public override void OnNetworkSpawn() => ApplyRotation();
        private void ApplyRotation() => transform.rotation = Quaternion.Euler(0, 0, shotAngle.Value);
        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer) return;
            var game = RoundController.Instance;
            age += Time.fixedDeltaTime;
            if (game == null || !game.IsPlaying || game.RoundNumber.Value != round || age >= lifetime || game.Remaining <= 0)
            { NetworkObject.Despawn(); return; }
            var distance = speed * Time.fixedDeltaTime;
            var hits = Physics2D.CircleCastAll(transform.position, radius, direction, distance);
            foreach (var hit in hits)
            {
                var target = hit.collider.GetComponentInParent<PlayerRole>();
                if (target == null || target.NetworkObjectId == shooter || !target.Alive.Value || target.AssignedRound.Value != round) continue;
                NetworkObject.Despawn();
                target.EliminateServer(direction, hit.point);
                return;
            }
            transform.position += (Vector3)(direction * distance);
            if (!DrawingArea.Instance.Contains(transform.position)) NetworkObject.Despawn();
        }
    }
}
