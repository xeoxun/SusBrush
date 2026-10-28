using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.Players
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PlayerNickname : NetworkBehaviour
    {
        private const int MaximumCharacters = 16;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string defaultName = "플레이어";
        public readonly NetworkVariable<FixedString64Bytes> Nickname = new();

        public override void OnNetworkSpawn()
        {
            Nickname.OnValueChanged += Refresh;
            if (IsServer && Nickname.Value.IsEmpty)
                Nickname.Value = new FixedString64Bytes(Clean(defaultName + " " + (OwnerClientId + 1)));
            Refresh(default, Nickname.Value);
            if (IsOwner && !string.IsNullOrWhiteSpace(NicknamePopupController.CurrentNickname))
                SetNickname(NicknamePopupController.CurrentNickname);
        }

        public override void OnNetworkDespawn() => Nickname.OnValueChanged -= Refresh;

        // MainScene's nickname flow can call this on the local spawned player.
        public void SetNickname(string nickname)
        {
            if (!IsSpawned || !IsOwner) return;
            var cleaned = Clean(nickname);
            if (cleaned.Length > 0) SetNicknameRpc(new FixedString64Bytes(cleaned));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetNicknameRpc(FixedString64Bytes nickname)
        {
            var cleaned = Clean(nickname.ToString());
            if (cleaned.Length > 0) Nickname.Value = new FixedString64Bytes(cleaned);
        }

        private void Refresh(FixedString64Bytes previous, FixedString64Bytes current)
            => label.text = current.ToString();

        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var result = new StringBuilder(MaximumCharacters);
            foreach (var character in value.Trim())
            {
                if (char.IsControl(character)) continue;
                if (result.Length == MaximumCharacters) break;
                result.Append(character);
            }
            if (result.Length > 0 && char.IsHighSurrogate(result[result.Length - 1]))
                result.Length--;
            return result.ToString();
        }
    }
}
