using SusBrush.Gameplay;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.Players
{
    public sealed class PlayerVoteView : MonoBehaviour
    {
        [SerializeField] private PlayerRole role;
        [SerializeField] private PlayerVote vote;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject highlight;
        [SerializeField] private SpriteRenderer glow;
        [SerializeField] private SpriteRenderer source;
        private int displayed = -1;
        private void Update()
        {
            var round = RoundController.Instance;
            var voting = round != null && round.IsSpawned && (round.Phase.Value == RoundPhase.Voting || round.Phase.Value == RoundPhase.Defense);
            var visible = voting && role.IsSpawned && role.Alive.Value;
            countText.gameObject.SetActive(visible);
            if (visible && displayed != vote.Count.Value) { displayed = vote.Count.Value; countText.SetText("{0}표", displayed); }
            var manager = NetworkManager.Singleton;
            var local = manager != null && manager.IsConnectedClient ? manager.LocalClient?.PlayerObject : null;
            var selected = visible && local != null && local.GetComponent<PlayerRole>().Alive.Value && local.GetComponent<PlayerVote>().Target.Value == role.NetworkObjectId;
            highlight.SetActive(selected);
            if (selected)
            {
                glow.sprite = source.sprite;
                var color = glow.color; color.a = .45f + .25f * Mathf.Sin(Time.unscaledTime * 7); glow.color = color;
            }
        }
    }
}
