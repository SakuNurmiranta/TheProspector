using System;
using System.Collections;
using System.Collections.Generic;
using SEMM91.Networking;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SEMM91.Networking.DebugSnapshots
{

    /// <summary>
    /// Server-authored, client-readable debug snapshot bridge.
    ///
    /// Domain objects such as GameEntity, SeededWorldState, DemoTape, VhsSet,
    /// and SceneRelease remain server-side simulation objects.
    ///
    /// This component exposes only flat replicated summary rows for UI/debug use.
    /// The UI should read this snapshot layer instead of trying to inspect
    /// server-domain memory directly.
    /// </summary>
    public sealed class DomainSnapshotReplicator : NetworkBehaviour
    {
        public static DomainSnapshotReplicator Instance { get; private set; }

        public NetworkVariable<int> SnapshotVersion { get; } =
            new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        public NetworkList<PlayerInventoryDebugRow> PlayerInventoryRows { get; private set; }
        public NetworkList<SceneOutputDebugRow> SceneOutputRows { get; private set; }

        public bool IsSnapshotNetworkReady { get; private set; }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[{nameof(DomainSnapshotReplicator)}] Duplicate instance detected on {name}. " +
                    "The latest instance will replace the previous static reference.");
            }

            Instance = this;

            PlayerInventoryRows = new NetworkList<PlayerInventoryDebugRow>();
            SceneOutputRows = new NetworkList<SceneOutputDebugRow>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            IsSnapshotNetworkReady = true;

            if (IsServer)
                StartCoroutine(DeferredInitialServerSnapshotRebuild());
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();

            IsSnapshotNetworkReady = false;

            if (Instance == this)
                Instance = null;

            PlayerInventoryRows?.Dispose();
            SceneOutputRows?.Dispose();
        }

        [ContextMenu("DEBUG Rebuild Real Snapshot")]
        public void DebugRebuildRealSnapshot()
        {
            RebuildSnapshotsFromServerDomain();
        }

        /// <summary>
        /// Real rebuild entry point.
        /// Next step: GameCoordinator or a domain query service will call this
        /// after player registration, action resolution, demo release, or scene output changes.
        /// </summary>
        public void RebuildSnapshotsFromServerDomain()
        {
            if (!IsServer)
            {
                Debug.LogWarning(
                    $"[{nameof(DomainSnapshotReplicator)}] Rebuild ignored because this instance is not server.");
                return;
            }

            PlayerInventoryRows.Clear();
            SceneOutputRows.Clear();

            SEMM91.GameCoordinator coordinator = SEMM91.GameCoordinator.Instance;

            if (coordinator == null)
            {
                SnapshotVersion.Value++;

                Debug.LogWarning(
                    $"[{nameof(DomainSnapshotReplicator)}] Rebuilt snapshot v{SnapshotVersion.Value} without coordinator | " +
                    $"playerRows={PlayerInventoryRows.Count} | sceneRows={SceneOutputRows.Count}");

                return;
            }

            NetPlayerState[] playerStates =
                UnityEngine.Object.FindObjectsByType<NetPlayerState>(FindObjectsSortMode.None);

            Array.Sort(playerStates, (a, b) => GetClientId(a).CompareTo(GetClientId(b)));

            Dictionary<string, PlayerSnapshotOwnerInfo> ownerByEntityId = new();

            int rowIndex = 0;

            foreach (NetPlayerState state in playerStates)
            {
                if (state == null)
                    continue;

                ulong clientId = GetClientId(state);
                string displayName = state.DisplayNameStr;
                string leaderEntityId = "None";

                int ideaCount = 0;
                int vhsSetCount = 0;
                int vhsTrackCount = 0;
                int demoTapeCount = 0;

                string latestDemoName = "None";
                string latestDemoSceneState = "None";

                var playerEntity = state.PlayerEntity;

                if (playerEntity != null)
                {
                    leaderEntityId = playerEntity.EntityId;

                    ideaCount = playerEntity.Ideas?.Count ?? 0;
                    vhsSetCount = playerEntity.VhsSets?.Count ?? 0;
                    vhsTrackCount = playerEntity.GetTotalVhsTrackCountFromSets();
                    demoTapeCount = playerEntity.DemoTapes?.Count ?? 0;

                    if (playerEntity.DemoTapes != null && playerEntity.DemoTapes.Count > 0)
                    {
                        var latestDemo = playerEntity.DemoTapes[playerEntity.DemoTapes.Count - 1];

                        if (latestDemo != null)
                        {
                            latestDemoName = latestDemo.DisplayName;
                            latestDemoSceneState = latestDemo.SceneState.ToString();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(playerEntity.EntityId))
                    {
                        ownerByEntityId[playerEntity.EntityId] = new PlayerSnapshotOwnerInfo
                        {
                            ClientId = clientId,
                            PlayerIndex = rowIndex,
                            DisplayName = displayName
                        };
                    }
                }

                PlayerInventoryRows.Add(new PlayerInventoryDebugRow
                {
                    ClientId = clientId,
                    PlayerIndex = rowIndex,
                    DisplayName = ToFixed32(displayName),
                    LeaderEntityId = ToFixed64(leaderEntityId),

                    IdeaCount = ideaCount,
                    VhsSetCount = vhsSetCount,
                    TrackCount = vhsTrackCount,
                    DemoTapeCount = demoTapeCount,

                    LatestDemoId = ToFixed64(latestDemoName),
                    LatestDemoSceneState = ToFixed32(latestDemoSceneState)
                });

                rowIndex++;
            }

            var standings = coordinator.LatestSceneOutputStandings;
            string dominantOwnerEntityId = coordinator.DominantOutputOwnerEntityId;

            if (standings != null)
            {
                foreach (var standing in standings)
                {
                    ulong ownerClientId = ulong.MaxValue;
                    int playerIndex = -1;
                    string ownerName = standing.OwnerDisplayName;

                    if (!string.IsNullOrWhiteSpace(standing.OwnerEntityId) &&
                        ownerByEntityId.TryGetValue(standing.OwnerEntityId, out PlayerSnapshotOwnerInfo ownerInfo))
                    {
                        ownerClientId = ownerInfo.ClientId;
                        playerIndex = ownerInfo.PlayerIndex;
                        ownerName = ownerInfo.DisplayName;
                    }

                    SceneOutputRows.Add(new SceneOutputDebugRow
                    {
                        OwnerClientId = ownerClientId,
                        PlayerIndex = playerIndex,
                        OwnerName = ToFixed32(ownerName),
                        HostedReleaseCount = standing.ReleaseCount,
                        AccumulatedSceneOutput = standing.Score,
                        IsDominantOwner = standing.OwnerEntityId == dominantOwnerEntityId
                    });
                }
            }

            SnapshotVersion.Value++;

            Debug.Log(
                $"[{nameof(DomainSnapshotReplicator)}] Rebuilt real snapshot v{SnapshotVersion.Value} | " +
                $"playerRows={PlayerInventoryRows.Count} | sceneRows={SceneOutputRows.Count}");
        }

        private static ulong GetClientId(NetPlayerState state)
        {
            if (state == null)
                return ulong.MaxValue;

            return state.OwnerClientIdCached != ulong.MaxValue
                ? state.OwnerClientIdCached
                : state.OwnerClientId;
        }

        private static FixedString32Bytes ToFixed32(string value)
        {
            return new FixedString32Bytes(Truncate(value, 31));
        }

        private static FixedString64Bytes ToFixed64(string value)
        {
            return new FixedString64Bytes(Truncate(value, 63));
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength
                ? value
                : value.Substring(0, maxLength);
        }

        private struct PlayerSnapshotOwnerInfo
        {
            public ulong ClientId;
            public int PlayerIndex;
            public string DisplayName;
        }

        public struct PlayerInventoryDebugRow :
            INetworkSerializable,
            IEquatable<PlayerInventoryDebugRow>
        {
            public ulong ClientId;
            public int PlayerIndex;
            public FixedString32Bytes DisplayName;
            public FixedString64Bytes LeaderEntityId;

            public int IdeaCount;
            public int VhsSetCount;
            public int TrackCount;
            public int DemoTapeCount;

            public FixedString64Bytes LatestDemoId;
            public FixedString32Bytes LatestDemoSceneState;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer)
                where T : IReaderWriter
            {
                serializer.SerializeValue(ref ClientId);
                serializer.SerializeValue(ref PlayerIndex);
                serializer.SerializeValue(ref DisplayName);
                serializer.SerializeValue(ref LeaderEntityId);

                serializer.SerializeValue(ref IdeaCount);
                serializer.SerializeValue(ref VhsSetCount);
                serializer.SerializeValue(ref TrackCount);
                serializer.SerializeValue(ref DemoTapeCount);

                serializer.SerializeValue(ref LatestDemoId);
                serializer.SerializeValue(ref LatestDemoSceneState);
            }

            public bool Equals(PlayerInventoryDebugRow other)
            {
                return ClientId == other.ClientId &&
                       PlayerIndex == other.PlayerIndex &&
                       DisplayName.Equals(other.DisplayName) &&
                       LeaderEntityId.Equals(other.LeaderEntityId) &&
                       IdeaCount == other.IdeaCount &&
                       VhsSetCount == other.VhsSetCount &&
                       TrackCount == other.TrackCount &&
                       DemoTapeCount == other.DemoTapeCount &&
                       LatestDemoId.Equals(other.LatestDemoId) &&
                       LatestDemoSceneState.Equals(other.LatestDemoSceneState);
            }

            public override bool Equals(object obj)
            {
                return obj is PlayerInventoryDebugRow other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new HashCode();

                hash.Add(ClientId);
                hash.Add(PlayerIndex);
                hash.Add(DisplayName);
                hash.Add(LeaderEntityId);

                hash.Add(IdeaCount);
                hash.Add(VhsSetCount);
                hash.Add(TrackCount);
                hash.Add(DemoTapeCount);

                hash.Add(LatestDemoId);
                hash.Add(LatestDemoSceneState);

                return hash.ToHashCode();
            }
        }

        public struct SceneOutputDebugRow :
            INetworkSerializable,
            IEquatable<SceneOutputDebugRow>
        {
            public ulong OwnerClientId;
            public int PlayerIndex;
            public FixedString32Bytes OwnerName;

            public int HostedReleaseCount;
            public float AccumulatedSceneOutput;
            public bool IsDominantOwner;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer)
                where T : IReaderWriter
            {
                serializer.SerializeValue(ref OwnerClientId);
                serializer.SerializeValue(ref PlayerIndex);
                serializer.SerializeValue(ref OwnerName);

                serializer.SerializeValue(ref HostedReleaseCount);
                serializer.SerializeValue(ref AccumulatedSceneOutput);
                serializer.SerializeValue(ref IsDominantOwner);
            }

            public bool Equals(SceneOutputDebugRow other)
            {
                return OwnerClientId == other.OwnerClientId &&
                       PlayerIndex == other.PlayerIndex &&
                       OwnerName.Equals(other.OwnerName) &&
                       HostedReleaseCount == other.HostedReleaseCount &&
                       AccumulatedSceneOutput.Equals(other.AccumulatedSceneOutput) &&
                       IsDominantOwner == other.IsDominantOwner;
            }

            public override bool Equals(object obj)
            {
                return obj is SceneOutputDebugRow other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    OwnerClientId,
                    PlayerIndex,
                    OwnerName,
                    HostedReleaseCount,
                    AccumulatedSceneOutput,
                    IsDominantOwner);
            }

            private static ulong GetClientId(NetPlayerState state)
            {
                if (state == null)
                    return ulong.MaxValue;

                return state.OwnerClientIdCached != ulong.MaxValue
                    ? state.OwnerClientIdCached
                    : state.OwnerClientId;
            }

            private static FixedString32Bytes ToFixed32(string value)
            {
                return new FixedString32Bytes(Truncate(value, 31));
            }

            private static FixedString64Bytes ToFixed64(string value)
            {
                return new FixedString64Bytes(Truncate(value, 63));
            }

            private static string Truncate(string value, int maxLength)
            {
                if (string.IsNullOrEmpty(value))
                    return string.Empty;

                return value.Length <= maxLength
                    ? value
                    : value.Substring(0, maxLength);
            }

            private struct PlayerSnapshotOwnerInfo
            {
                public ulong ClientId;
                public int PlayerIndex;
                public string DisplayName;
            }

        }

        private IEnumerator DeferredInitialServerSnapshotRebuild()
        {
            // Wait one frame so all NetworkBehaviours on this NetworkObject
            // have completed their spawn callbacks.
            yield return null;

            RebuildSnapshotsFromServerDomain();

            Debug.Log(
                $"[{nameof(DomainSnapshotReplicator)}] Deferred initial snapshot rebuild complete | " +
                $"v{SnapshotVersion.Value} | playerRows={PlayerInventoryRows.Count} | sceneRows={SceneOutputRows.Count}");
        }

    }
}

    
