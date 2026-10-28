using UnityEngine;

namespace SusBrush.Gameplay
{
    public sealed class DrawingArea : MonoBehaviour
    {
        [Tooltip("World-space drawing surface. Edit its BoxCollider2D in the scene.")]
        [SerializeField] private BoxCollider2D worldBounds;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform strokeRoot;
        [SerializeField, Min(0)] private float edgePadding = 0.35f;
        public int SpawnPointCount => spawnPoints == null ? 0 : spawnPoints.Length;
        public static DrawingArea Instance { get; private set; }
        public Camera GameplayCamera => gameplayCamera;
        public Transform StrokeRoot => strokeRoot;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public Vector3 GetSpawnPosition(int slot)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return Clamp(Vector3.zero);
            return Clamp(spawnPoints[slot % spawnPoints.Length].position);
        }

        public Vector3 Clamp(Vector3 position)
            => Clamp(position, Vector2.one * edgePadding, Vector2.one * edgePadding);

        public Vector3 Clamp(Vector3 position, Collider2D body, Vector3 playerPosition)
        {
            if (body == null) return Clamp(position);
            var bounds = body.bounds;
            var origin = worldBounds.transform.InverseTransformPoint(playerPosition);
            var min = Vector2.positiveInfinity;
            var max = Vector2.negativeInfinity;
            for (var i = 0; i < 4; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y, playerPosition.z);
                var offset = (Vector2)(worldBounds.transform.InverseTransformPoint(corner) - origin);
                min = Vector2.Min(min, offset);
                max = Vector2.Max(max, offset);
            }
            return Clamp(position, Vector2.Max(Vector2.one * edgePadding, -min),
                Vector2.Max(Vector2.one * edgePadding, max));
        }

        private Vector3 Clamp(Vector3 position, Vector2 minimumPadding, Vector2 maximumPadding)
        {
            // Local collider coordinates also support moving or rotating the entire board.
            var local = worldBounds.transform.InverseTransformPoint(position);
            var halfSize = worldBounds.size * 0.5f;
            var min = worldBounds.offset - halfSize + minimumPadding;
            var max = worldBounds.offset + halfSize - maximumPadding;
            if (min.x > max.x) min.x = max.x = (min.x + max.x) * 0.5f;
            if (min.y > max.y) min.y = max.y = (min.y + max.y) * 0.5f;
            local.x = Mathf.Clamp(local.x, min.x, max.x);
            local.y = Mathf.Clamp(local.y, min.y, max.y);
            local.z = 0;
            position = worldBounds.transform.TransformPoint(local);
            return position;
        }

        public bool TryGetPointerTarget(Vector2 screenPosition, out Vector2 target)
        {
            target = default;
            if (!TryGetWorldPoint(screenPosition, out var point) || !Contains(point)) return false;
            target = Clamp(point);
            return true;
        }

        public bool Contains(Vector3 point)
        {
            var local = (Vector2)worldBounds.transform.InverseTransformPoint(point);
            return new Rect(worldBounds.offset - worldBounds.size * 0.5f, worldBounds.size).Contains(local);
        }

        public bool TryGetWorldPoint(Vector2 screenPosition, out Vector2 target)
        {
            target = default;
            // Screen pixels are converted to a point on the same world plane at every resolution.
            if (!gameplayCamera.pixelRect.Contains(screenPosition))
                return false;
            var ray = gameplayCamera.ScreenPointToRay(screenPosition);
            var surface = worldBounds.transform;
            if (!new Plane(surface.forward, surface.position).Raycast(ray, out var distance))
                return false;
            target = ray.GetPoint(distance);
            return true;
        }
    }
}
