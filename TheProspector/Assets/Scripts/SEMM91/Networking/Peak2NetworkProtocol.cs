using System;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace SEMM91.Networking
{
    /// <summary>
    /// Explicit compatibility boundary for the Peak-2 runtime protocol.
    /// A stale ParrelSync clone must be rejected during the NGO handshake
    /// instead of attempting to deserialize a different replicated schema.
    /// </summary>
    public static class Peak2NetworkProtocol
    {
        public const ushort ProtocolVersion = 4201;
        public const string SchemaId = "P2-E42-20260815";

        public static string DisplayLabel =>
            $"{SchemaId} / protocol {ProtocolVersion}";

        public static string RuntimeFingerprint =>
            $"{SchemaId}|protocol={ProtocolVersion}|" +
            $"unity={Application.unityVersion}|" +
            $"build={Application.version}";

        public static string ConnectionPayload =>
            $"DEMONTAPES|{ProtocolVersion}|{SchemaId}";

        public static byte[] CreateConnectionPayload()
        {
            return Encoding.UTF8.GetBytes(
                ConnectionPayload
            );
        }

        public static bool IsCompatibleConnectionPayload(
            byte[] payload)
        {
            if (payload == null ||
                payload.Length == 0)
            {
                return false;
            }

            string received =
                Encoding.UTF8.GetString(payload);

            return string.Equals(
                received,
                ConnectionPayload,
                StringComparison.Ordinal
            );
        }

        public static void ApplyTo(
            NetworkManager networkManager)
        {
            if (networkManager == null)
                return;

            networkManager.NetworkConfig.ProtocolVersion =
                ProtocolVersion;

            networkManager.NetworkConfig.ConnectionApproval =
                true;

            networkManager.NetworkConfig.ConnectionData =
                CreateConnectionPayload();
        }
    }
}
