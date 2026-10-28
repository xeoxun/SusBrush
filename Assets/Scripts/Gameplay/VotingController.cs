using System.Linq;
using Unity.Netcode;
using UnityEngine;
using SusBrush.Players;

namespace SusBrush.Gameplay
{
    public sealed class VotingController : NetworkBehaviour
    {
        public const ulong NoTarget = ulong.MaxValue;
        [SerializeField] private RoundController rounds;
        public readonly NetworkVariable<ulong> Defendant = new(NoTarget);
        public void BeginServer() => ClearServer();
        public void ClearServer()
        {
            if (!IsServer) return;
            Defendant.Value = NoTarget;
            foreach (var role in rounds.Participants)
            {
                var vote = role.GetComponent<PlayerVote>();
                vote.Target.Value = NoTarget;
                vote.Count.Value = 0;
            }
        }
        public void CastServer(PlayerVote voter, ulong targetId)
        {
            if (!IsServer || rounds.Phase.Value != RoundPhase.Voting || rounds.Remaining <= 0 ||
                !rounds.Participants.Contains(voter.GetComponent<PlayerRole>()) || !voter.GetComponent<PlayerRole>().Alive.Value) return;
            var target = rounds.Participants.FirstOrDefault(p => p.NetworkObjectId == targetId && p.Alive.Value);
            if (target == null || voter.Target.Value == targetId) return;
            voter.Target.Value = targetId;
            foreach (var role in rounds.Participants)
                role.GetComponent<PlayerVote>().Count.Value = rounds.Participants.Count(p => p.Alive.Value && p.GetComponent<PlayerVote>().Target.Value == role.NetworkObjectId);
        }
        public bool ResolveServer()
        {
            if (!IsServer) return false;
            var ranked = rounds.Participants.Where(p => p.Alive.Value).OrderByDescending(p => p.GetComponent<PlayerVote>().Count.Value).ToArray();
            if (ranked.Length == 0) return false;
            var max = ranked[0].GetComponent<PlayerVote>().Count.Value;
            if (max == 0 || ranked.Count(p => p.GetComponent<PlayerVote>().Count.Value == max) != 1) return false;
            Defendant.Value = ranked[0].NetworkObjectId;
            return true;
        }
        public string DefendantName => NetworkManager != null && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(Defendant.Value, out var obj)
            ? obj.GetComponent<PlayerNickname>().Nickname.Value.ToString() : "";
        public void ExecuteServer()
        {
            if (IsServer && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(Defendant.Value, out var obj))
                obj.GetComponent<PlayerRole>().ExecuteServer();
        }
    }
}
