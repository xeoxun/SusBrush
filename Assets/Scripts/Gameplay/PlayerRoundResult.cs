using System;
using Unity.Collections;
using Unity.Netcode;

namespace SusBrush.Gameplay
{
    // Published by the host only when the complete game finishes, before roles reset.
    public struct PlayerRoundResult : INetworkSerializable, IEquatable<PlayerRoundResult>
    {
        public FixedString64Bytes Nickname;
        public FixedString128Bytes Word;
        public PlayerJob Job;
        public bool DifferentWord;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Nickname);
            serializer.SerializeValue(ref Word);
            serializer.SerializeValue(ref Job);
            serializer.SerializeValue(ref DifferentWord);
        }
        public bool Equals(PlayerRoundResult other) => Nickname.Equals(other.Nickname) && Word.Equals(other.Word) && Job == other.Job && DifferentWord == other.DifferentWord;
    }
}
