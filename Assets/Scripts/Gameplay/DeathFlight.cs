using UnityEngine;

namespace SusBrush.Gameplay
{
    public sealed class DeathFlight : MonoBehaviour
    {
        [SerializeField] private AnimationClip flightClip;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer tintVisual;
        [SerializeField] private SpriteRenderer hatVisual;
        [SerializeField] private Sprite civilianHat;
        [SerializeField] private Sprite guardHat;
        [SerializeField, Range(0, 1)] private float progress;
        [SerializeField, Min(0)] private float viewportMargin = 0.15f;
        private Vector3 start;
        private Vector3 end;
        private float elapsed;
        private bool initialized;

        public void StartFlight(Vector3 position, Vector2 heading, Camera camera, Color color, Vector3 characterScale, bool isGuard)
        {
            start = position;
            if (heading.sqrMagnitude < 0.001f) heading = Vector2.up;
            heading.Normalize();
            var origin = camera.WorldToViewportPoint(start);
            var along = camera.WorldToViewportPoint(start + (Vector3)heading) - origin;
            var x = Mathf.Abs(along.x) > 0.00001f ? ((along.x > 0 ? 1 + viewportMargin : -viewportMargin) - origin.x) / along.x : float.PositiveInfinity;
            var y = Mathf.Abs(along.y) > 0.00001f ? ((along.y > 0 ? 1 + viewportMargin : -viewportMargin) - origin.y) / along.y : float.PositiveInfinity;
            end = camera.ViewportToWorldPoint(origin + along * Mathf.Min(x, y));
            end.z = start.z;
            tintVisual.color = color;
            hatVisual.sprite = isGuard ? guardHat : civilianHat;
            visualRoot.localScale = characterScale;
            transform.position = start;
            initialized = true;
        }
        private void LateUpdate()
        {
            if (!initialized) return;
            transform.position = Vector3.LerpUnclamped(start, end, progress);
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= flightClip.length + 0.1f) Destroy(gameObject);
        }
    }
}
