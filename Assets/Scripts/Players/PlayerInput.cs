using SusBrush.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SusBrush.Players
{
    [RequireComponent(typeof(NetworkPlayer))]
    public sealed class PlayerInput : MonoBehaviour
    {
        [SerializeField] private NetworkPlayer player;
        [SerializeField] private PlayerCombat combat;
        [SerializeField, Range(10, 60)] private int sendsPerSecond = 20;
        private double nextSendTime;
        private bool previousDrawing;
        private Vector2 previousMovement;

        private void Update()
        {
            if (!player.IsSpawned || !player.IsOwner || DrawingArea.Instance == null) return;
            var movement = Vector2.zero;
            var target = Vector2.zero;
            var drawing = false;
            var focusedText = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null;
            if (Application.isFocused && !focusedText)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    movement.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                    movement.y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
                }
                var mouse = Mouse.current;
                var overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (mouse != null && !overUI)
                {
                    var round = RoundController.Instance;
                    if (round != null && round.Phase.Value == RoundPhase.Voting && mouse.leftButton.wasPressedThisFrame &&
                        DrawingArea.Instance.TryGetWorldPoint(mouse.position.ReadValue(), out var votePoint))
                    {
                        var hit = Physics2D.OverlapPoint(votePoint);
                        var candidate = hit != null ? hit.GetComponentInParent<PlayerRole>() : null;
                        if (candidate != null && candidate.Alive.Value) player.GetComponent<PlayerVote>().Cast(candidate.NetworkObjectId);
                    }
                    if (mouse.rightButton.wasPressedThisFrame) combat.ToggleAim();
                    if (combat.IsAiming)
                    {
                        if (DrawingArea.Instance.TryGetWorldPoint(mouse.position.ReadValue(), out var aimTarget)) combat.PointAt(aimTarget);
                        if (mouse.leftButton.wasPressedThisFrame) combat.Fire();
                    }
                    else drawing = mouse.leftButton.isPressed &&
                        DrawingArea.Instance.TryGetPointerTarget(mouse.position.ReadValue(), out target);
                }
            }
            // Send press/release immediately; otherwise limit continuous input traffic.
            if (drawing == previousDrawing && movement == previousMovement && Time.unscaledTimeAsDouble < nextSendTime) return;
            previousDrawing = drawing;
            previousMovement = movement;
            nextSendTime = Time.unscaledTimeAsDouble + 1d / sendsPerSecond;
            player.SendInput(movement, target, drawing);
        }
    }
}
