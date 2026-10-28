using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.Gameplay
{
    public sealed class PlayerGuess : NetworkBehaviour
    {
        [SerializeField] private PlayerRole role;
        public void Submit(string guess)
        {
            if (!IsSpawned || !IsOwner || string.IsNullOrWhiteSpace(guess) || guess.Length > 32) return;
            GuessRpc(new FixedString128Bytes(guess.Trim()));
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void GuessRpc(FixedString128Bytes guess)
        {
            var game = RoundController.Instance;
            if (game != null) game.SubmitGuessServer(role, guess.ToString());
        }
    }
}
