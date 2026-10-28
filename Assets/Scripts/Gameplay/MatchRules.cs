using UnityEngine;

namespace SusBrush.Gameplay
{
    [CreateAssetMenu(menuName = "SusBrush/Match Rules")]
    public sealed class MatchRules : ScriptableObject
    {
        [field: SerializeField, Range(4, 8)] public int MinimumPlayers { get; private set; } = 4;
        [field: SerializeField, Range(4, 8)] public int MaximumPlayers { get; private set; } = 8;
        public const int RoundCount = 2;
        public string RoundCountSummary => "2라운드";
        [field: SerializeField, Min(1)] public int RoundDurationSeconds { get; private set; } = 40;
        [field: SerializeField, Min(1)] public int GuardShotsPerGame { get; private set; } = 2;
        [field: SerializeField, Min(1)] public int VotingSeconds { get; private set; } = 20;
        [field: SerializeField, Min(1)] public int DefenseSeconds { get; private set; } = 10;
        [field: SerializeField, Min(0.1f)] public float RoleRevealSeconds { get; private set; } = 5;
    }
}
