using SusBrush.Drawing;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.Players
{
    public sealed class PlayerColorView : NetworkBehaviour
    {
        [SerializeField] private PlayerDrawing drawing;
        [SerializeField] private PenPalette palette;
        [Tooltip("색상을 바꿀 캐릭터 레이어만 연결합니다.")]
        [SerializeField] private SpriteRenderer tintRenderer;

        public override void OnNetworkSpawn()
        {
            drawing.PenIndex.OnValueChanged += OnPenChanged;
            ApplyColor(drawing.PenIndex.Value);
        }

        public override void OnNetworkDespawn()
        {
            drawing.PenIndex.OnValueChanged -= OnPenChanged;
        }

        private void OnPenChanged(int previous, int current) => ApplyColor(current);

        private void ApplyColor(int index)
        {
            if (palette.TryGetStroke(index, out var stroke))
                tintRenderer.color = stroke.startColor;
        }
    }
}
