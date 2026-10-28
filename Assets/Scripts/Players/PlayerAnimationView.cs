using SusBrush.Drawing;
using SusBrush.Gameplay;
using UnityEngine;

namespace SusBrush.Players
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(NetworkPlayer), typeof(PlayerDrawing))]
    public sealed class PlayerAnimationView : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private NetworkPlayer player;
        [SerializeField] private PlayerDrawing drawing;
        [SerializeField] private PenPalette palette;
        [SerializeField] private SpriteRenderer tintVisual;
        [SerializeField] private PlayerRole role;
        [SerializeField] private RuntimeAnimatorController civilianController;
        [SerializeField] private RuntimeAnimatorController guardController;
        [SerializeField, Min(0.001f)] private float minimumMovement = 0.0005f;
        [SerializeField, Min(0)] private float movementHoldTime = 0.12f;

        private static readonly int Moving = Animator.StringToHash("Moving");
        private static readonly int Drawing = Animator.StringToHash("Drawing");
        private static readonly int Direction = Animator.StringToHash("Direction");
        private Vector3 previousPosition;
        private float lastMovementTime;
        private int direction;
        private int previousPenIndex = int.MinValue;

        private void OnEnable()
        {
            previousPosition = transform.position;
            lastMovementTime = float.NegativeInfinity;
            previousPenIndex = int.MinValue;
        }

        private void Update()
        {
            if (animator == null || tintVisual == null || player == null || drawing == null) return;

            var controller = role != null && role.IsSpawned && role.IsPublicGuard.Value
                ? guardController : civilianController;
            if (controller != null && animator.runtimeAnimatorController != controller)
                animator.runtimeAnimatorController = controller;

            // Use replicated movement too, so observers animate without extra network messages.
            var delta = (Vector2)(transform.position - previousPosition);
            previousPosition = transform.position;
            if (delta.sqrMagnitude > 2.25f)
            {
                // Spawn/round teleports should not briefly play a walking animation.
                lastMovementTime = float.NegativeInfinity;
            }
            else if (delta.sqrMagnitude > minimumMovement * minimumMovement)
            {
                lastMovementTime = Time.time;
                direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                    ? delta.x < 0 ? 1 : 2
                    : delta.y < 0 ? 0 : 3;
            }

            var round = RoundController.Instance;
            var faceFront = round != null && round.IsSpawned &&
                (round.Phase.Value == RoundPhase.Voting || round.Phase.Value == RoundPhase.Defense);
            if (faceFront)
            {
                direction = 0; // Down is the character's front-facing pose.
                lastMovementTime = float.NegativeInfinity;
            }
            animator.SetInteger(Direction, direction);
            // Hold between server physics ticks to avoid alternating idle and walk each frame.
            animator.SetBool(Moving, !faceFront && Time.time - lastMovementTime < movementHoldTime);
            animator.SetBool(Drawing, !faceFront && player.IsSpawned && player.Drawing.Value);

            var penIndex = drawing.IsSpawned ? drawing.PenIndex.Value : 0;
            if (penIndex == previousPenIndex) return;
            previousPenIndex = penIndex;
            if (palette != null && palette.TryGetStroke(Mathf.Max(0, penIndex), out var stroke))
            {
                var color = stroke.startColor;
                color.a = 1;
                tintVisual.color = color;
            }
        }
    }
}
