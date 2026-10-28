using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

namespace SusBrush.Gameplay
{
    public struct PaintMarkData : INetworkSerializable, System.IEquatable<PaintMarkData>
    {
        public Vector3 Position;
        public Color Color;
        public FixedString64Bytes Name;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        { s.SerializeValue(ref Position); s.SerializeValue(ref Color); s.SerializeValue(ref Name); }
        public bool Equals(PaintMarkData other) => Position == other.Position && Color == other.Color && Name.Equals(other.Name);
    }
    public sealed class PaintMarks : NetworkBehaviour
    {
        [SerializeField] private PaintMarkView prefab;
        [SerializeField] private Transform markRoot;
        private NetworkList<PaintMarkData> marks;
        private void Awake() => marks = new NetworkList<PaintMarkData>();
        public override void OnNetworkSpawn() { marks.OnListChanged += Changed; Rebuild(); }
        public override void OnNetworkDespawn() { marks.OnListChanged -= Changed; ClearViews(); }
        public void AddServer(Vector3 position, Color color, string name)
        { if (IsServer) marks.Add(new PaintMarkData { Position = position, Color = color, Name = new FixedString64Bytes(name) }); }
        public void ClearServer() { if (IsServer) marks.Clear(); }
        private void Changed(NetworkListEvent<PaintMarkData> change)
        { if (change.Type == NetworkListEvent<PaintMarkData>.EventType.Add) Create(change.Value); else Rebuild(); }
        private void Rebuild() { ClearViews(); foreach (var mark in marks) Create(mark); }
        private void Create(PaintMarkData mark) { var view = Instantiate(prefab, mark.Position, Quaternion.identity, markRoot); view.Show(mark.Color, mark.Name.ToString()); }
        private void ClearViews() { if (markRoot != null) foreach (Transform child in markRoot) Destroy(child.gameObject); }
    }
}
