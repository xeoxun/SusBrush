using System.Collections.Generic;
using SusBrush.Gameplay;
using SusBrush.Players;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.Drawing
{
    [RequireComponent(typeof(NetworkPlayer))]
    public sealed class PlayerDrawing : NetworkBehaviour
    {
        [SerializeField] private NetworkPlayer player;
        [SerializeField] private PenPalette palette;
        [Tooltip("새 점을 기록할 최소 이동 거리입니다. 월드 단위입니다.")]
        [SerializeField, Min(0.001f)] private float minimumPointDistance = 0.04f;
        [Tooltip("그리는 도중 점을 기록하는 간격입니다. 0.05초는 최대 초당 20회입니다.")]
        [SerializeField, Min(0.01f)] private float sampleInterval = 0.05f;
        [SerializeField, Range(64, 16000)] private int maximumPoints = 8192;
        private NetworkList<StrokePoint> points;
        private readonly List<LineRenderer> strokes = new();
        private LineRenderer currentStroke;
        private Vector3 lastPoint;
        private bool drawing;
        private double nextSampleTime;
        public int PointCount => points.Count;
        public int StrokeCount => strokes.Count;
        public readonly NetworkVariable<int> PenIndex = new(-1);

        // Each sampled point is sent as a list delta while drawing, including to non-owners.
        // The server alone records the movement path; new arrivals receive the current list.
        private void Awake() => points = new NetworkList<StrokePoint>(null,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            PenIndex.OnValueChanged += OnPenChanged;
            if (IsServer) PenIndex.Value = AllocatePen();
            points.OnListChanged += OnPointsChanged;
            RebuildVisuals();
            if (IsServer) player.MovementSimulated += RecordMovement;
        }

        public override void OnNetworkDespawn()
        {
            PenIndex.OnValueChanged -= OnPenChanged;
            points.OnListChanged -= OnPointsChanged;
            player.MovementSimulated -= RecordMovement;
            drawing = false;
            ClearVisuals();
        }

        private int AllocatePen()
        {
            var occupied = new bool[palette.Count];
            foreach (var obj in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                var other = obj.GetComponent<PlayerDrawing>();
                if (other == null || other == this) continue;
                var index = other.PenIndex.Value;
                if (index >= 0 && index < occupied.Length) occupied[index] = true;
            }
            for (var i = 0; i < occupied.Length; i++)
                if (!occupied[i]) return i;
            Debug.LogError("No unused pen slot is available. SusBrush supports up to eight players.", this);
            return -1;
        }

        private void OnPenChanged(int previous, int current) => RebuildVisuals();

        private void RecordMovement(Vector3 previous, Vector3 current, bool isDrawing)
        {
            if (!isDrawing)
            {
                // The release frame can already contain WASD movement: end at its starting point.
                if (drawing) Append(previous, false, 0.000001f);
                drawing = false;
                return;
            }
            if (points.Count >= maximumPoints - 1) return;
            if (!drawing)
            {
                Append(previous, true, 0);
                drawing = true;
                nextSampleTime = 0;
            }
            if (Time.unscaledTimeAsDouble < nextSampleTime) return;
            if (Append(current, false, minimumPointDistance * minimumPointDistance))
                nextSampleTime = Time.unscaledTimeAsDouble + sampleInterval;
        }

        private bool Append(Vector3 position, bool startsStroke, float squaredDistance)
        {
            if (points.Count >= maximumPoints || (!startsStroke && (lastPoint - position).sqrMagnitude <= squaredDistance))
                return false;
            points.Add(new StrokePoint(position, startsStroke));
            lastPoint = position;
            return true;
        }

        // Called by the round controller on the host at the start of a round.
        public void ClearDrawing()
        {
            if (!IsSpawned || !IsServer) return;
            drawing = false;
            points.Clear();
        }

        private void OnPointsChanged(NetworkListEvent<StrokePoint> change)
        {
            if (change.Type == NetworkListEvent<StrokePoint>.EventType.Add) RenderPoint(change.Value);
            else if (change.Type == NetworkListEvent<StrokePoint>.EventType.Clear) ClearVisuals();
            else RebuildVisuals();
        }

        private void RebuildVisuals()
        {
            ClearVisuals();
            for (var i = 0; i < points.Count; i++) RenderPoint(points[i]);
        }

        private void RenderPoint(StrokePoint point)
        {
            if (!palette.TryGetStroke(PenIndex.Value, out var strokePrefab)) return;
            if (point.StartsStroke || currentStroke == null)
            {
                var parent = DrawingArea.Instance != null ? DrawingArea.Instance.StrokeRoot : null;
                currentStroke = Instantiate(strokePrefab, parent);
                currentStroke.positionCount = 0;
                strokes.Add(currentStroke);
            }
            var index = currentStroke.positionCount;
            currentStroke.positionCount = index + 1;
            currentStroke.SetPosition(index, point.Position);
        }

        private void ClearVisuals()
        {
            foreach (var stroke in strokes)
                if (stroke != null) Destroy(stroke.gameObject);
            strokes.Clear();
            currentStroke = null;
        }
    }
}
