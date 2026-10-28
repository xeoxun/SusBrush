using SusBrush.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.Players
{
    public sealed class PlayerDeathView : NetworkBehaviour
    {
        [SerializeField] private DeathFlight effectPrefab;
        [SerializeField] private SpriteRenderer sourceVisual;
        public void PlayServer(Vector2 direction, Vector3 impactPoint)
        {
            if (!IsServer) return;
            RoundController.Instance.GetComponent<PaintMarks>().AddServer(impactPoint, sourceVisual.color,
                GetComponent<PlayerNickname>().Nickname.Value.ToString());
            PlayEffectRpc(direction);
        }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void PlayEffectRpc(Vector2 direction)
        {
            var area = DrawingArea.Instance;
            if (area == null) return;
            var effect = Instantiate(effectPrefab);
            effect.StartFlight(sourceVisual.transform.position, direction, area.GameplayCamera,
                sourceVisual.color, sourceVisual.transform.lossyScale, GetComponent<PlayerRole>().IsPublicGuard.Value);
        }
    }
}
