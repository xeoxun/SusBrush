using System;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.Drawing
{
    public struct StrokePoint : INetworkSerializable, IEquatable<StrokePoint>
    {
        public Vector3 Position;
        public bool StartsStroke;

        public StrokePoint(Vector3 position, bool startsStroke)
        {
            Position = position;
            StartsStroke = startsStroke;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref StartsStroke);
        }

        public bool Equals(StrokePoint other)
            => Position.Equals(other.Position) && StartsStroke == other.StartsStroke;
    }
}
