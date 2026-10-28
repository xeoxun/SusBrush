using System;
using System.Text;
using System.Collections.Generic;
using SusBrush.Drawing;
using Unity.Netcode;
using UnityEngine;

namespace SusBrush.Players
{
    // Chat RPCs extracted from LiarPainterNetworkPlayer; gameplay and roles stay separate.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(PlayerNickname), typeof(PlayerDrawing))]
    public sealed class PlayerChat : NetworkBehaviour
    {
        public static event Action<int, string, string> ChatReceived;
        [SerializeField, Min(.01f)] private float broadcastInterval = .3f;
        private const int MaximumPendingMessages = 32;
        private readonly Queue<string> pendingMessages = new();
        private double nextChatTime;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEvents() => ChatReceived = null;
        public bool SendChat(string message)
        {
            message = NormalizeChat(message);
            if (!IsOwner || !IsSpawned || message.Length == 0) return false;
            SendChatRpc(message);
            return true;
        }
        public static string NormalizeChat(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "";
            var clean = new StringBuilder(120);
            foreach(var c in message)
            {
                if (!char.IsControl(c)) clean.Append(c);
                if (clean.Length >= 120) break;
            }
            if (clean.Length > 0 && char.IsHighSurrogate(clean[clean.Length - 1])) clean.Length--;
            return clean.ToString().Trim();
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SendChatRpc(string message)
        {
            if (message == null || message.Length > 120) return;
            message = NormalizeChat(message);
            if (message.Length == 0 || pendingMessages.Count >= MaximumPendingMessages) return;
            pendingMessages.Enqueue(message);
            FlushPendingChat();
        }

        private void Update()
        {
            if (IsSpawned && IsServer) FlushPendingChat();
        }

        private void FlushPendingChat()
        {
            if (pendingMessages.Count == 0) return;
            var now = Time.unscaledTimeAsDouble;
            if (now < nextChatTime) return;
            nextChatTime = now + broadcastInterval;
            var message = pendingMessages.Dequeue();
            var name = GetComponent<PlayerNickname>().Nickname.Value.ToString();
            ReceiveChatRpc(GetComponent<PlayerDrawing>().PenIndex.Value,
                string.IsNullOrWhiteSpace(name) ? $"플레이어 {OwnerClientId + 1}" : name, message);
        }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void ReceiveChatRpc(int colorIndex, string name, string message)
        {
            ChatReceived?.Invoke(colorIndex, name, message);
            if (TryGetComponent<PlayerChatBubble>(out var bubble)) bubble.Show(message);
        }

        public override void OnNetworkDespawn()
        {
            pendingMessages.Clear();
            nextChatTime = 0;
            if (TryGetComponent<PlayerChatBubble>(out var bubble)) bubble.Hide();
        }
    }
}
