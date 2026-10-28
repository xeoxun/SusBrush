using Unity.Netcode;
using SusBrush.Gameplay;

namespace SusBrush.Players
{
    public sealed class PlayerVote : NetworkBehaviour
    {
        public readonly NetworkVariable<ulong> Target = new(VotingController.NoTarget, NetworkVariableReadPermission.Owner);
        public readonly NetworkVariable<int> Count = new();
        public void Cast(ulong target) { if (IsSpawned && IsOwner) CastRpc(target); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void CastRpc(ulong target) => RoundController.Instance.GetComponent<VotingController>().CastServer(this, target);
    }
}
