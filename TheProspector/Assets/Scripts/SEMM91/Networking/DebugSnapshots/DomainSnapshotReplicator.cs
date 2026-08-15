using System;
using System.Collections;
using System.Collections.Generic;
using SEMM91.Networking;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Keeper;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Paradigm;
using SEMM91.GamePlay.Kvlt.TurnFlow;
using SEMM91.GamePlay.Score;
using SEMM91.GamePlay.World;
using SEMM91.Core.Entities;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;
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
    /// This component exposes flat authoritative read-model rows for UI/debug use.
    /// The UI should read this snapshot layer instead of trying to inspect
    /// server-domain memory directly. Trying to read server-domain memory will lead
    /// to null inventory output.
    /// </summary>
    public sealed class DomainSnapshotReplicator : NetworkBehaviour
    {
        public static DomainSnapshotReplicator Instance { get; private set; }

        public NetworkVariable<KeeperInterventionDebugSnapshot>
            KeeperInterventionState { get; } =
            new NetworkVariable<
                KeeperInterventionDebugSnapshot
            >(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        public NetworkList<KeeperReleaseDebugRow>
            KeeperReleaseRows { get; private set; }

        public NetworkVariable<int> SnapshotVersion { get; } =
            new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        public NetworkList<PlayerInventoryDebugRow> PlayerInventoryRows { get; private set; }

        public NetworkVariable<KvltSceneDebugSnapshot>
            KvltSceneState { get; } =
            new(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        public NetworkList<KvltTurnResolutionDebugRow>
            KvltTurnResolutionRows { get; private set; }

        public NetworkList<KvltCanonPrecedentDebugRow>
            KvltCanonPrecedentRows { get; private set; }

        public NetworkList<KvltSemanticEnvironmentDebugRow>
            KvltSemanticEnvironmentRows { get; private set; }

        public NetworkList<KvltHappeningDebugRow>
            KvltHappeningRows { get; private set; }

        public NetworkList<KvltHailDebugRow>
            KvltHailRows { get; private set; }

        public NetworkList<KvltParadigmGripDebugRow>
            KvltParadigmGripRows { get; private set; }

        public NetworkList<KvltParadigmBeefDebugRow>
            KvltParadigmBeefRows { get; private set; }

        public NetworkList<KvltPoserDebugRow>
            KvltPoserRows { get; private set; }

        public NetworkList<DemoTapeDebugRow> DemoTapeRows { get; private set; }

        public NetworkList<DemoTapeTrackSemanticDebugRow>
            DemoTapeTrackSemanticRows { get; private set; }

        public NetworkList<DemoTapeIdeaSemanticDebugRow>
            DemoTapeIdeaSemanticRows { get; private set; }

        public NetworkList<DemoTapeTagSemanticDebugRow>
            DemoTapeTagSemanticRows { get; private set; }

        public NetworkList<RehearsalSetDebugRow> RehearsalSetRows { get; private set; }

        public NetworkList<RehearsalTrackDebugRow> RehearsalTrackRows { get; private set; }

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
            KvltTurnResolutionRows =
                new NetworkList<
                    KvltTurnResolutionDebugRow>();
            KvltCanonPrecedentRows =
                new NetworkList<
                    KvltCanonPrecedentDebugRow>();
            KvltSemanticEnvironmentRows =
                new NetworkList<
                    KvltSemanticEnvironmentDebugRow>();
            KvltHappeningRows =
                new NetworkList<KvltHappeningDebugRow>();
            KvltHailRows =
                new NetworkList<KvltHailDebugRow>();
            KvltParadigmGripRows =
                new NetworkList<KvltParadigmGripDebugRow>();
            KvltParadigmBeefRows =
                new NetworkList<KvltParadigmBeefDebugRow>();
            KvltPoserRows =
                new NetworkList<KvltPoserDebugRow>();
            RehearsalSetRows = new NetworkList<RehearsalSetDebugRow>();
            RehearsalTrackRows = new NetworkList<RehearsalTrackDebugRow>();
            KeeperReleaseRows = new NetworkList<KeeperReleaseDebugRow>();
            DemoTapeRows = new NetworkList<DemoTapeDebugRow>();
            DemoTapeTrackSemanticRows =
                new NetworkList<
                    DemoTapeTrackSemanticDebugRow>();

            DemoTapeIdeaSemanticRows =
                new NetworkList<
                    DemoTapeIdeaSemanticDebugRow>();

            DemoTapeTagSemanticRows =
                new NetworkList<
                    DemoTapeTagSemanticDebugRow>();
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
            KvltTurnResolutionRows?.Dispose();
            KvltCanonPrecedentRows?.Dispose();
            KvltSemanticEnvironmentRows?.Dispose();
            KvltHappeningRows?.Dispose();
            KvltHailRows?.Dispose();
            KvltParadigmGripRows?.Dispose();
            KvltParadigmBeefRows?.Dispose();
            KvltPoserRows?.Dispose();
            RehearsalSetRows?.Dispose();
            RehearsalTrackRows?.Dispose();
            KeeperReleaseRows?.Dispose();
            DemoTapeRows?.Dispose();
            DemoTapeTrackSemanticRows?.Dispose();
            DemoTapeIdeaSemanticRows?.Dispose();
            DemoTapeTagSemanticRows?.Dispose();
        }

        [ContextMenu("DEBUG Rebuild Real Snapshot")]
        public void DebugRebuildRealSnapshot()
        {
            RebuildSnapshotsFromServerDomain();
        }

        /// <summary>
        /// Real rebuild entry point.
        /// Next step: GameCoordinator or a domain query service will call this
        /// after player registration, action resolution, demo release, or KVLT state changes.
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
            KvltTurnResolutionRows.Clear();
            KvltCanonPrecedentRows.Clear();
            KvltSemanticEnvironmentRows.Clear();
            KvltHappeningRows.Clear();
            KvltHailRows.Clear();
            KvltParadigmGripRows.Clear();
            KvltParadigmBeefRows.Clear();
            KvltPoserRows.Clear();
            RehearsalSetRows.Clear();
            RehearsalTrackRows.Clear();
            KeeperReleaseRows.Clear();
            DemoTapeRows.Clear();
            DemoTapeTrackSemanticRows.Clear();
            DemoTapeIdeaSemanticRows.Clear();
            DemoTapeTagSemanticRows.Clear();

            KeeperInterventionState.Value =
                default;

            KvltSceneState.Value =
                default;

            GameCoordinator coordinator = GameCoordinator.Instance;

            if (coordinator == null)
            {
                SnapshotVersion.Value++;

                Debug.LogWarning(
                    $"[{nameof(DomainSnapshotReplicator)}] Rebuilt snapshot v{SnapshotVersion.Value} without coordinator | " +
                    $"playerRows={PlayerInventoryRows.Count} | " +
                    "KVLT state unavailable");

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

                bool hasPromotableDemo = false;

                string promotableDemoId =
                    string.Empty;

                string promotableDemoName =
                    string.Empty;

                string promotableDemoSourceSetName =
                    string.Empty;

                int promotableDemoRecordedTurn = 0;
                int promotableDemoTrackCount = 0;
                int promotableDemoTakeCount = 0;
                float promotableDemoAverageConveyance = 0.0f;

                ulong clientId = GetClientId(state);
                string displayName = state.DisplayNameStr;
                string leaderEntityId = "None";
                bool leaderIsExhausted = false;

                TagDebugSnapshot convictionTag = default;
                TagDebugSnapshot moodTag = default;
                TagDebugSnapshot resonanceTag = default;
                TagDebugSnapshot transientTag = default;

                int ideaCount = 0;
                int vhsSetCount = 0;
                int vhsTrackCount = 0;
                int demoTapeCount = 0;

                string latestDemoId =
                    string.Empty;

                string latestDemoName =
                    "None";

                string latestDemoSceneState =
                    "None";

                var playerEntity = state.PlayerEntity;

                if (playerEntity != null)
                {
                    DemoTape promotableDemo =
                        playerEntity
                            .GetLatestUnreleasedDemoTape();

                    if (promotableDemo != null)
                    {
                        hasPromotableDemo = true;

                        promotableDemoId =
                            promotableDemo.DemoTapeId;

                        promotableDemoName =
                            promotableDemo.DisplayName;

                        promotableDemoSourceSetName =
                            promotableDemo.SourceSetName;

                        promotableDemoRecordedTurn =
                            promotableDemo.RecordedTurn;

                        promotableDemoTrackCount =
                            promotableDemo.TrackSnapshots.Count;

                        promotableDemoTakeCount =
                            promotableDemo.TakeCount;

                        promotableDemoAverageConveyance =
                            promotableDemo.AverageConveyance;
                    }

                    leaderEntityId = playerEntity.EntityId;
                    leaderIsExhausted =
                        playerEntity.IsExhausted;
                    ideaCount = playerEntity.Ideas?.Count ?? 0;
                    vhsSetCount = playerEntity.VhsSets?.Count ?? 0;
                    vhsTrackCount = playerEntity.GetTotalVhsTrackCountFromSets();
                    demoTapeCount = playerEntity.DemoTapes?.Count ?? 0;
                    AddRehearsalRows(
                        clientId,
                        playerEntity
                    );

                    AddDemoTapeRows(
                        clientId,
                        playerEntity
                    );

                    if (playerEntity.DemoTapes != null && playerEntity.DemoTapes.Count > 0)
                    {
                        var latestDemo = playerEntity.DemoTapes[playerEntity.DemoTapes.Count - 1];

                        if (latestDemo != null)
                        {
                            latestDemoId =
                                latestDemo.DemoTapeId;

                            latestDemoName =
                                latestDemo.DisplayName;

                            latestDemoSceneState =
                                latestDemo.SceneState.ToString();
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

                    convictionTag =
                        GetTagSnapshot(
                            playerEntity,
                            TagContainerType.Conviction
                        );

                    moodTag =
                        GetTagSnapshot(
                            playerEntity,
                            TagContainerType.Mood
                        );

                    resonanceTag =
                        GetTagSnapshot(
                            playerEntity,
                            TagContainerType.Resonance
                        );

                    transientTag =
                        GetTagSnapshot(
                            playerEntity,
                            TagContainerType.Transient
                        );
                }

                PlayerInventoryRows.Add(
                    new PlayerInventoryDebugRow
                    {
                        ClientId = clientId,
                        PlayerIndex = rowIndex,
                        DisplayName = ToFixed32(displayName),
                        LeaderEntityId = ToFixed64(leaderEntityId),
                        LeaderIsExhausted = leaderIsExhausted,

                        ConvictionTag = convictionTag,
                        MoodTag = moodTag,
                        ResonanceTag = resonanceTag,
                        TransientTag = transientTag,

                        IdeaCount = ideaCount,
                        VhsSetCount = vhsSetCount,
                        TrackCount = vhsTrackCount,
                        DemoTapeCount = demoTapeCount,

                        HasPromotableDemo =
                            hasPromotableDemo,

                        PromotableDemoId =
                            ToFixed64(
                                promotableDemoId
                            ),

                        PromotableDemoName =
                            ToFixed64(
                                promotableDemoName
                            ),

                        PromotableDemoSourceSetName =
                            ToFixed64(
                                promotableDemoSourceSetName
                            ),

                        PromotableDemoRecordedTurn =
                            promotableDemoRecordedTurn,

                        PromotableDemoTrackCount =
                            promotableDemoTrackCount,

                        PromotableDemoTakeCount =
                            promotableDemoTakeCount,

                        PromotableDemoAverageConveyance =
                            promotableDemoAverageConveyance,

                        LatestDemoId =
                            ToFixed64(latestDemoId),

                        LatestDemoSceneState =
                            ToFixed32(latestDemoSceneState)
                    });

                rowIndex++;
            }

            AddKeeperInterventionSnapshot(
                coordinator,
                ownerByEntityId
            );

            AddPeak2ReadModels(
                coordinator
            );

            SnapshotVersion.Value++;

            Debug.Log(
                $"[{nameof(DomainSnapshotReplicator)}] " +
                $"Rebuilt real snapshot v{SnapshotVersion.Value} | " +
                $"playerRows={PlayerInventoryRows.Count} | " +
                $"rehearsalSets={RehearsalSetRows.Count} | " +
                $"rehearsalTracks={RehearsalTrackRows.Count} | " +
                $"demoTapes={DemoTapeRows.Count} | " +
                $"demoTracks={DemoTapeTrackSemanticRows.Count} | " +
                $"demoIdeas={DemoTapeIdeaSemanticRows.Count} | " +
                $"demoTags={DemoTapeTagSemanticRows.Count} | " +
                $"kvltPlayers={PlayerInventoryRows.Count} | " +
                $"kvltReleases={KeeperReleaseRows.Count} | " +
                $"canonRows={KvltCanonPrecedentRows.Count} | " +
                $"semanticRows={KvltSemanticEnvironmentRows.Count} | " +
                $"happenings={KvltHappeningRows.Count} | " +
                $"hails={KvltHailRows.Count} | " +
                $"grips={KvltParadigmGripRows.Count} | " +
                $"beefs={KvltParadigmBeefRows.Count} | " +
                $"posers={KvltPoserRows.Count} | " +
                $"turnRecords=" +
                $"{KvltTurnResolutionRows.Count}"
            );
        }

        [ContextMenu("DEBUG Print Keeper Snapshot")]
        public void DebugPrintKeeperSnapshot()
        {
            KeeperInterventionDebugSnapshot snapshot =
                KeeperInterventionState.Value;

            Debug.Log(
                "[KEEPER SNAPSHOT] " +
                $"peer={(IsServer ? "Server" : "Client")} | " +
                $"version={SnapshotVersion.Value} | " +
                $"hasTenure={snapshot.HasTenure} | " +
                $"keeper={snapshot.KeeperClientId} | " +
                $"turn={snapshot.EvaluatedTurn} | " +
                $"pull={snapshot.Pull:F2} | " +
                $"boostAvailable=" +
                $"{snapshot.BoostAvailable} | " +
                $"suppressAvailable=" +
                $"{snapshot.SuppressAvailable} | " +
                $"targets={KeeperReleaseRows.Count}"
            );

            for (int i = 0;
                 i < KeeperReleaseRows.Count;
                 i++)
            {
                KeeperReleaseDebugRow row =
                    KeeperReleaseRows[i];

                Debug.Log(
                    "[KEEPER SNAPSHOT TARGET] " +
                    $"index={i} | " +
                    $"release={row.DisplayName} | " +
                    $"releaseId={row.ReleaseId} | " +
                    $"sourceDemo={row.SourceDemoTapeId} | " +
                    $"owner={row.OwnerName} | " +
                    $"organic={row.OrganicVisibility:F2} | " +
                    $"effective={row.EffectiveVisibility:F2} | " +
                    $"pending=" +
                    $"{row.PendingVisibilityAdjustment:F2} | " +
                    $"canBoost={row.CanReceiveBoost} | " +
                    $"canSuppress=" +
                    $"{row.CanReceiveSuppress}"
                );
            }
        }

        private static TagDebugSnapshot GetTagSnapshot(
            GameEntity entity,
            TagContainerType containerType)
        {
            if (entity == null ||
                !entity.TryGetTagContainer(
                    containerType,
                    out TagContainer container
                ) ||
                container == null ||
                !container.HasHeldTag ||
                container.HeldTag == null)
            {
                return default;
            }

            TagInstance tag =
                container.HeldTag.TagInstance;

            return new TagDebugSnapshot
            {
                HasTag = true,
                AxisValue = (byte)tag.axis,
                PoleValue = (byte)tag.pole,
                DegreeValue = (byte)tag.degree
            };
        }



        private void AddRehearsalRows(
            ulong clientId,
            GameEntity playerEntity)
        {
            if (playerEntity == null ||
                playerEntity.VhsSets == null)
            {
                return;
            }

            for (int setIndex = 0;
                 setIndex < playerEntity.VhsSets.Count;
                 setIndex++)
            {
                RehearsalSet rehearsalSet =
                    playerEntity.VhsSets[setIndex];

                if (rehearsalSet == null)
                    continue;

                bool isActive =
                    rehearsalSet.VhsSetId ==
                    playerEntity.ActiveVhsSetId;

                RehearsalSetRows.Add(
                    new RehearsalSetDebugRow
                    {
                        OwnerClientId = clientId,
                        SetIndex = setIndex,
                        SetId = ToFixed64(
                            rehearsalSet.VhsSetId
                        ),
                        DisplayName = ToFixed64(
                            rehearsalSet.DisplayName
                        ),
                        IsActive = isActive,
                        CreatedTurn =
                            rehearsalSet.CreatedTurn,
                        LastRehearsedTurn =
                            rehearsalSet.LastRehearsedTurn,
                        TrackCount =
                            rehearsalSet.VhsTracks.Count
                    }
                );

                for (int trackIndex = 0;
                     trackIndex <
                     rehearsalSet.VhsTracks.Count;
                     trackIndex++)
                {
                    Track track =
                        rehearsalSet.VhsTracks[trackIndex];

                    if (track == null)
                        continue;

                    RehearsalTrackRows.Add(
                        new RehearsalTrackDebugRow
                        {
                            OwnerClientId = clientId,
                            SetIndex = setIndex,
                            TrackIndex = trackIndex,
                            SetId = ToFixed64(
                                rehearsalSet.VhsSetId
                            ),
                            TrackId = ToFixed64(
                                track.VhsTrackId
                            ),
                            DisplayName = ToFixed64(
                                track.DisplayName
                            ),
                            IdeaCount =
                                track.Ideas?.Count ?? 0,
                            Conveyance =
                                track.Conveyance,
                            RehearsalCount =
                                track.RehearsalCount,
                            LastRehearsedTurn =
                                track.LastRehearsedTurn,
                            IsRaw =
                                track.IsRaw,
                            IsHoned =
                                track.IsHoned
                        }
                    );
                }
            }
        }


        private void AddDemoTapeRows(
            ulong clientId,
            GameEntity playerEntity)
        {
            if (playerEntity == null ||
                playerEntity.DemoTapes == null)
            {
                return;
            }

            for (int demoIndex = 0;
                 demoIndex < playerEntity.DemoTapes.Count;
                 demoIndex++)
            {
                DemoTape demoTape =
                    playerEntity.DemoTapes[demoIndex];

                if (demoTape == null)
                    continue;

                DemoTapeRows.Add(
                    new DemoTapeDebugRow
                    {
                        OwnerClientId =
                            clientId,

                        DemoIndex =
                            demoIndex,

                        DemoTapeId =
                            ToFixed64(
                                demoTape.DemoTapeId
                            ),

                        DisplayName =
                            ToFixed64(
                                demoTape.DisplayName
                            ),

                        SourceSetId =
                            ToFixed64(
                                demoTape.SourceSetId
                            ),

                        SourceSetName =
                            ToFixed64(
                                demoTape.SourceSetName
                            ),

                        RecordedTurn =
                            demoTape.RecordedTurn,

                        TrackCount =
                            demoTape.TrackSnapshots?.Count ??
                            0,

                        TakeCount =
                            demoTape.TakeCount,

                        AverageConveyance =
                            demoTape.AverageConveyance,

                        SceneStateValue =
                            (byte)demoTape.SceneState,

                        IsReleased =
                            demoTape.IsReleased,

                        IsSceneActive =
                            demoTape.IsSceneActive
                    }
                );

                AddDemoTapeSemanticRows(
                    clientId,
                    demoIndex,
                    demoTape
                );
            }
        }

        private void AddDemoTapeSemanticRows(
            ulong clientId,
            int demoIndex,
            DemoTape demoTape)
        {
            if (demoTape == null ||
                demoTape.TrackSnapshots == null)
            {
                return;
            }

            for (int trackIndex = 0;
                 trackIndex <
                 demoTape.TrackSnapshots.Count;
                 trackIndex++)
            {
                DemoTapeTrackSnapshot trackSnapshot =
                    demoTape.TrackSnapshots[trackIndex];

                if (trackSnapshot == null)
                    continue;

                DemoTapeTrackSemanticRows.Add(
                    new DemoTapeTrackSemanticDebugRow
                    {
                        OwnerClientId =
                            clientId,

                        DemoIndex =
                            demoIndex,

                        TrackIndex =
                            trackIndex,

                        DemoTapeId =
                            ToFixed64(
                                demoTape.DemoTapeId
                            ),

                        SourceTrackId =
                            ToFixed64(
                                trackSnapshot.SourceTrackId
                            ),

                        DisplayName =
                            ToFixed64(
                                trackSnapshot.DisplayName
                            ),

                        SourceConveyance =
                            trackSnapshot.SourceConveyance,

                        RecordedConveyance =
                            trackSnapshot.RecordedConveyance,

                        IdeaCount =
                            trackSnapshot.IdeaCount
                    }
                );

                for (int ideaListIndex = 0;
                     ideaListIndex <
                     trackSnapshot.IdeaSnapshots.Count;
                     ideaListIndex++)
                {
                    DemoTapeIdeaSnapshot ideaSnapshot =
                        trackSnapshot
                            .IdeaSnapshots[ideaListIndex];

                    if (ideaSnapshot == null)
                        continue;

                    DemoTapeIdeaSemanticRows.Add(
                        new DemoTapeIdeaSemanticDebugRow
                        {
                            OwnerClientId =
                                clientId,

                            DemoIndex =
                                demoIndex,

                            TrackIndex =
                                trackIndex,

                            IdeaIndex =
                                ideaSnapshot.IdeaIndex,

                            DemoTapeId =
                                ToFixed64(
                                    demoTape.DemoTapeId
                                ),

                            SourceTrackId =
                                ToFixed64(
                                    trackSnapshot.SourceTrackId
                                ),

                            SourceIdeaId =
                                ToFixed64(
                                    ideaSnapshot.SourceIdeaId
                                ),

                            AspectId =
                                ToFixed64(
                                    ideaSnapshot.AspectId
                                ),

                            PayloadTypeValue =
                                (byte)
                                ideaSnapshot.PayloadType,

                            SourceEntityId =
                                ToFixed64(
                                    ideaSnapshot.SourceEntityId
                                ),

                            SourceContainerTypeValue =
                                (byte)
                                ideaSnapshot
                                    .SourceContainerType,

                            Conveyance =
                                ideaSnapshot.Conveyance,

                            TagOccurrenceCount =
                                ideaSnapshot
                                    .TagOccurrences.Count
                        }
                    );

                    for (int tagIndex = 0;
                         tagIndex <
                         ideaSnapshot
                             .TagOccurrences.Count;
                         tagIndex++)
                    {
                        DemoTapeTagOccurrenceSnapshot
                            tagSnapshot =
                                ideaSnapshot
                                    .TagOccurrences[tagIndex];

                        if (tagSnapshot == null)
                            continue;

                        DemoTapeTagSemanticRows.Add(
                            new DemoTapeTagSemanticDebugRow
                            {
                                OwnerClientId =
                                    clientId,

                                DemoIndex =
                                    demoIndex,

                                TrackIndex =
                                    trackIndex,

                                IdeaIndex =
                                    ideaSnapshot.IdeaIndex,

                                TagOccurrenceIndex =
                                    tagIndex,

                                DemoTapeId =
                                    ToFixed64(
                                        demoTape.DemoTapeId
                                    ),

                                SourceTrackId =
                                    ToFixed64(
                                        trackSnapshot.SourceTrackId
                                    ),

                                SourceIdeaId =
                                    ToFixed64(
                                        ideaSnapshot.SourceIdeaId
                                    ),

                                AxisValue =
                                    (byte)
                                    tagSnapshot.Axis,

                                PoleValue =
                                    (byte)
                                    tagSnapshot.Pole,

                                DegreeValue =
                                    (byte)
                                    tagSnapshot.Degree,

                                RoleValue =
                                    (byte)
                                    tagSnapshot.Role
                            }
                        );
                    }
                }
            }
        }

        private static ulong GetClientId(NetPlayerState state)
        {
            if (state == null)
                return ulong.MaxValue;

            return state.OwnerClientIdCached != ulong.MaxValue
                ? state.OwnerClientIdCached
                : state.OwnerClientId;
        }

        private static FixedString32Bytes ToFixed32(
            string value)
        {
            FixedString32Bytes result = default;

            result.CopyFromTruncated(
                value ?? string.Empty
            );

            return result;
        }

        private static FixedString64Bytes ToFixed64(
            string value)
        {
            FixedString64Bytes result = default;

            result.CopyFromTruncated(
                value ?? string.Empty
            );

            return result;
        }

        private static FixedString128Bytes ToFixed128(
            string value)
        {
            FixedString128Bytes result = default;

            result.CopyFromTruncated(
                value ?? string.Empty
            );

            return result;
        }

        private struct PlayerSnapshotOwnerInfo
        {
            public ulong ClientId;
            public int PlayerIndex;
            public string DisplayName;
        }

        public struct TagDebugSnapshot :
            INetworkSerializable,
            IEquatable<TagDebugSnapshot>
        {
            public bool HasTag;

            public byte AxisValue;
            public byte PoleValue;
            public byte DegreeValue;

            public TagAxis Axis =>
                (TagAxis)AxisValue;

            public TagPole Pole =>
                (TagPole)PoleValue;

            public TagDegree Degree =>
                (TagDegree)DegreeValue;

            public void NetworkSerialize<T>(
                BufferSerializer<T> serializer)
                where T : IReaderWriter
            {
                serializer.SerializeValue(ref HasTag);
                serializer.SerializeValue(ref AxisValue);
                serializer.SerializeValue(ref PoleValue);
                serializer.SerializeValue(ref DegreeValue);
            }

            public bool Equals(
                TagDebugSnapshot other)
            {
                return HasTag == other.HasTag &&
                       AxisValue == other.AxisValue &&
                       PoleValue == other.PoleValue &&
                       DegreeValue == other.DegreeValue;
            }

            public override bool Equals(object obj)
            {
                return obj is TagDebugSnapshot other &&
                       Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    HasTag,
                    AxisValue,
                    PoleValue,
                    DegreeValue
                );
            }
        }

        public struct RehearsalSetDebugRow :
            INetworkSerializable,
            IEquatable<RehearsalSetDebugRow>
        {
            public ulong OwnerClientId;
            public int SetIndex;

            public FixedString64Bytes SetId;
            public FixedString64Bytes DisplayName;

            public bool IsActive;
            public int CreatedTurn;
            public int LastRehearsedTurn;
            public int TrackCount;

            public void NetworkSerialize<T>(
                BufferSerializer<T> serializer)
                where T : IReaderWriter
            {
                serializer.SerializeValue(
                    ref OwnerClientId
                );

                serializer.SerializeValue(
                    ref SetIndex
                );

                serializer.SerializeValue(
                    ref SetId
                );

                serializer.SerializeValue(
                    ref DisplayName
                );

                serializer.SerializeValue(
                    ref IsActive
                );

                serializer.SerializeValue(
                    ref CreatedTurn
                );

                serializer.SerializeValue(
                    ref LastRehearsedTurn
                );

                serializer.SerializeValue(
                    ref TrackCount
                );
            }

            public bool Equals(
                RehearsalSetDebugRow other)
            {
                return
                    OwnerClientId ==
                    other.OwnerClientId &&
                    SetIndex ==
                    other.SetIndex &&
                    SetId.Equals(
                        other.SetId
                    ) &&
                    DisplayName.Equals(
                        other.DisplayName
                    ) &&
                    IsActive ==
                    other.IsActive &&
                    CreatedTurn ==
                    other.CreatedTurn &&
                    LastRehearsedTurn ==
                    other.LastRehearsedTurn &&
                    TrackCount ==
                    other.TrackCount;
            }

            public override bool Equals(
                object obj)
            {
                return
                    obj is RehearsalSetDebugRow other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    OwnerClientId,
                    SetIndex,
                    SetId,
                    DisplayName,
                    IsActive,
                    CreatedTurn,
                    LastRehearsedTurn,
                    TrackCount
                );
            }
        }

        public struct RehearsalTrackDebugRow :
            INetworkSerializable,
            IEquatable<RehearsalTrackDebugRow>
        {
            public ulong OwnerClientId;
            public int SetIndex;
            public int TrackIndex;

            public FixedString64Bytes SetId;
            public FixedString64Bytes TrackId;
            public FixedString64Bytes DisplayName;

            public int IdeaCount;
            public float Conveyance;
            public int RehearsalCount;
            public int LastRehearsedTurn;

            public bool IsRaw;
            public bool IsHoned;

            public void NetworkSerialize<T>(
                BufferSerializer<T> serializer)
                where T : IReaderWriter
            {
                serializer.SerializeValue(
                    ref OwnerClientId
                );

                serializer.SerializeValue(
                    ref SetIndex
                );

                serializer.SerializeValue(
                    ref TrackIndex
                );

                serializer.SerializeValue(
                    ref SetId
                );

                serializer.SerializeValue(
                    ref TrackId
                );

                serializer.SerializeValue(
                    ref DisplayName
                );

                serializer.SerializeValue(
                    ref IdeaCount
                );

                serializer.SerializeValue(
                    ref Conveyance
                );

                serializer.SerializeValue(
                    ref RehearsalCount
                );

                serializer.SerializeValue(
                    ref LastRehearsedTurn
                );

                serializer.SerializeValue(
                    ref IsRaw
                );

                serializer.SerializeValue(
                    ref IsHoned
                );
            }


            public bool Equals(
                RehearsalTrackDebugRow other)
            {
                return
                    OwnerClientId ==
                    other.OwnerClientId &&
                    SetIndex ==
                    other.SetIndex &&
                    TrackIndex ==
                    other.TrackIndex &&
                    SetId.Equals(
                        other.SetId
                    ) &&
                    TrackId.Equals(
                        other.TrackId
                    ) &&
                    DisplayName.Equals(
                        other.DisplayName
                    ) &&
                    IdeaCount ==
                    other.IdeaCount &&
                    Conveyance.Equals(
                        other.Conveyance
                    ) &&
                    RehearsalCount ==
                    other.RehearsalCount &&
                    LastRehearsedTurn ==
                    other.LastRehearsedTurn &&
                    IsRaw ==
                    other.IsRaw &&
                    IsHoned ==
                    other.IsHoned;
            }

            public override bool Equals(
                object obj)
            {
                return
                    obj is RehearsalTrackDebugRow other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new HashCode();

                hash.Add(OwnerClientId);
                hash.Add(SetIndex);
                hash.Add(TrackIndex);
                hash.Add(SetId);
                hash.Add(TrackId);
                hash.Add(DisplayName);
                hash.Add(IdeaCount);
                hash.Add(Conveyance);
                hash.Add(RehearsalCount);
                hash.Add(LastRehearsedTurn);
                hash.Add(IsRaw);
                hash.Add(IsHoned);

                return hash.ToHashCode();
            }
        }

        public struct DemoTapeDebugRow :
            INetworkSerializable,
            IEquatable<DemoTapeDebugRow>
        {
            public ulong OwnerClientId;
            public int DemoIndex;

            public FixedString64Bytes DemoTapeId;
            public FixedString64Bytes DisplayName;

            public FixedString64Bytes SourceSetId;
            public FixedString64Bytes SourceSetName;

            public int RecordedTurn;
            public int TrackCount;
            public int TakeCount;

            public float AverageConveyance;

            public byte SceneStateValue;

            public bool IsReleased;
            public bool IsSceneActive;

            public DemoTapeSceneState SceneState =>
                (DemoTapeSceneState)
                SceneStateValue;

            public void NetworkSerialize<T>(
                BufferSerializer<T> serializer)
                where T : IReaderWriter
            {
                serializer.SerializeValue(
                    ref OwnerClientId
                );

                serializer.SerializeValue(
                    ref DemoIndex
                );

                serializer.SerializeValue(
                    ref DemoTapeId
                );

                serializer.SerializeValue(
                    ref DisplayName
                );

                serializer.SerializeValue(
                    ref SourceSetId
                );

                serializer.SerializeValue(
                    ref SourceSetName
                );

                serializer.SerializeValue(
                    ref RecordedTurn
                );

                serializer.SerializeValue(
                    ref TrackCount
                );

                serializer.SerializeValue(
                    ref TakeCount
                );

                serializer.SerializeValue(
                    ref AverageConveyance
                );

                serializer.SerializeValue(
                    ref SceneStateValue
                );

                serializer.SerializeValue(
                    ref IsReleased
                );

                serializer.SerializeValue(
                    ref IsSceneActive
                );
            }

            public bool Equals(
                DemoTapeDebugRow other)
            {
                return
                    OwnerClientId ==
                    other.OwnerClientId &&
                    DemoIndex ==
                    other.DemoIndex &&
                    DemoTapeId.Equals(
                        other.DemoTapeId
                    ) &&
                    DisplayName.Equals(
                        other.DisplayName
                    ) &&
                    SourceSetId.Equals(
                        other.SourceSetId
                    ) &&
                    SourceSetName.Equals(
                        other.SourceSetName
                    ) &&
                    RecordedTurn ==
                    other.RecordedTurn &&
                    TrackCount ==
                    other.TrackCount &&
                    TakeCount ==
                    other.TakeCount &&
                    AverageConveyance.Equals(
                        other.AverageConveyance
                    ) &&
                    SceneStateValue ==
                    other.SceneStateValue &&
                    IsReleased ==
                    other.IsReleased &&
                    IsSceneActive ==
                    other.IsSceneActive;
            }

            public override bool Equals(
                object obj)
            {
                return
                    obj is DemoTapeDebugRow other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                HashCode hash =
                    new HashCode();

                hash.Add(OwnerClientId);
                hash.Add(DemoIndex);
                hash.Add(DemoTapeId);
                hash.Add(DisplayName);
                hash.Add(SourceSetId);
                hash.Add(SourceSetName);
                hash.Add(RecordedTurn);
                hash.Add(TrackCount);
                hash.Add(TakeCount);
                hash.Add(AverageConveyance);
                hash.Add(SceneStateValue);
                hash.Add(IsReleased);
                hash.Add(IsSceneActive);

                return hash.ToHashCode();
            }
        }


        public struct PlayerInventoryDebugRow :
            INetworkSerializable,
            IEquatable<PlayerInventoryDebugRow>
        {
            public ulong ClientId;
            public int PlayerIndex;
            public FixedString32Bytes DisplayName;
            public FixedString64Bytes LeaderEntityId;
            public bool LeaderIsExhausted;

            public bool HasPromotableDemo;

            public FixedString64Bytes
                PromotableDemoId;

            public FixedString64Bytes
                PromotableDemoName;

            public FixedString64Bytes
                PromotableDemoSourceSetName;

            public int PromotableDemoRecordedTurn;
            public int PromotableDemoTrackCount;
            public int PromotableDemoTakeCount;
            public float PromotableDemoAverageConveyance;

            public TagDebugSnapshot ConvictionTag;
            public TagDebugSnapshot MoodTag;
            public TagDebugSnapshot ResonanceTag;
            public TagDebugSnapshot TransientTag;


            public int IdeaCount;
            public int VhsSetCount;
            public int TrackCount;
            public int DemoTapeCount;

            public FixedString64Bytes LatestDemoId;
            public FixedString32Bytes LatestDemoSceneState;

            public float KvltTotalScore;
            public bool HasKvltStanding;
            public float KvltStanding;
            public bool HasYearInfluence;
            public float YearInfluence;
            public bool IsKvltKeeper;
            public int KvltReleaseCount;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer)
                where T : IReaderWriter
            {
                serializer.SerializeValue(ref ClientId);
                serializer.SerializeValue(ref PlayerIndex);
                serializer.SerializeValue(ref DisplayName);
                serializer.SerializeValue(ref LeaderEntityId);
                serializer.SerializeValue(ref LeaderIsExhausted);

                serializer.SerializeValue(
                    ref HasPromotableDemo
                );

                serializer.SerializeValue(
                    ref PromotableDemoId
                );

                serializer.SerializeValue(
                    ref PromotableDemoName
                );

                serializer.SerializeValue(
                    ref PromotableDemoSourceSetName
                );

                serializer.SerializeValue(
                    ref PromotableDemoRecordedTurn
                );

                serializer.SerializeValue(
                    ref PromotableDemoTrackCount
                );

                serializer.SerializeValue(
                    ref PromotableDemoTakeCount
                );

                serializer.SerializeValue(
                    ref PromotableDemoAverageConveyance
                );

                ConvictionTag.NetworkSerialize(serializer);
                MoodTag.NetworkSerialize(serializer);
                ResonanceTag.NetworkSerialize(serializer);
                TransientTag.NetworkSerialize(serializer);

                serializer.SerializeValue(ref IdeaCount);
                serializer.SerializeValue(ref VhsSetCount);
                serializer.SerializeValue(ref TrackCount);
                serializer.SerializeValue(ref DemoTapeCount);

                serializer.SerializeValue(ref LatestDemoId);
                serializer.SerializeValue(ref LatestDemoSceneState);
                serializer.SerializeValue(ref KvltTotalScore);
                serializer.SerializeValue(ref HasKvltStanding);
                serializer.SerializeValue(ref KvltStanding);
                serializer.SerializeValue(ref HasYearInfluence);
                serializer.SerializeValue(ref YearInfluence);
                serializer.SerializeValue(ref IsKvltKeeper);
                serializer.SerializeValue(ref KvltReleaseCount);
            }

            public bool Equals(PlayerInventoryDebugRow other)
            {
                return ClientId == other.ClientId &&
                       PlayerIndex == other.PlayerIndex &&
                       DisplayName.Equals(other.DisplayName) &&
                       LeaderEntityId.Equals(other.LeaderEntityId) &&
                       LeaderIsExhausted == other.LeaderIsExhausted &&
                       ConvictionTag.Equals(other.ConvictionTag) &&
                       MoodTag.Equals(other.MoodTag) &&
                       ResonanceTag.Equals(other.ResonanceTag) &&
                       TransientTag.Equals(other.TransientTag) &&
                       IdeaCount == other.IdeaCount &&
                       VhsSetCount == other.VhsSetCount &&
                       TrackCount == other.TrackCount &&
                       DemoTapeCount == other.DemoTapeCount &&
                       LatestDemoId.Equals(other.LatestDemoId) &&
                       HasPromotableDemo ==
                       other.HasPromotableDemo &&
                       PromotableDemoId.Equals(
                           other.PromotableDemoId
                       ) &&
                       PromotableDemoName.Equals(
                           other.PromotableDemoName
                       ) &&
                       PromotableDemoSourceSetName.Equals(
                           other.PromotableDemoSourceSetName
                       ) &&
                       PromotableDemoRecordedTurn ==
                       other.PromotableDemoRecordedTurn &&
                       PromotableDemoTrackCount ==
                       other.PromotableDemoTrackCount &&
                       PromotableDemoTakeCount ==
                       other.PromotableDemoTakeCount &&
                       PromotableDemoAverageConveyance.Equals(
                           other.PromotableDemoAverageConveyance
                       ) &&
                       LatestDemoSceneState.Equals(other.LatestDemoSceneState) &&
                       KvltTotalScore.Equals(other.KvltTotalScore) &&
                       HasKvltStanding == other.HasKvltStanding &&
                       KvltStanding.Equals(other.KvltStanding) &&
                       HasYearInfluence == other.HasYearInfluence &&
                       YearInfluence.Equals(other.YearInfluence) &&
                       IsKvltKeeper == other.IsKvltKeeper &&
                       KvltReleaseCount == other.KvltReleaseCount;
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
                hash.Add(LeaderIsExhausted);

                hash.Add(HasPromotableDemo);
                hash.Add(PromotableDemoId);
                hash.Add(PromotableDemoName);
                hash.Add(PromotableDemoSourceSetName);
                hash.Add(PromotableDemoRecordedTurn);
                hash.Add(PromotableDemoTrackCount);
                hash.Add(PromotableDemoTakeCount);
                hash.Add(PromotableDemoAverageConveyance);

                hash.Add(ConvictionTag);
                hash.Add(MoodTag);
                hash.Add(ResonanceTag);
                hash.Add(TransientTag);

                hash.Add(IdeaCount);
                hash.Add(VhsSetCount);
                hash.Add(TrackCount);
                hash.Add(DemoTapeCount);

                hash.Add(LatestDemoId);
                hash.Add(LatestDemoSceneState);
                hash.Add(KvltTotalScore);
                hash.Add(HasKvltStanding);
                hash.Add(KvltStanding);
                hash.Add(HasYearInfluence);
                hash.Add(YearInfluence);
                hash.Add(IsKvltKeeper);
                hash.Add(KvltReleaseCount);

                return hash.ToHashCode();
            }
        }

        private IEnumerator DeferredInitialServerSnapshotRebuild()
        {
            // Wait one frame so all NetworkBehaviours on this NetworkObject
            // have completed their spawn callbacks.
            yield return null;

            RebuildSnapshotsFromServerDomain();

            KeeperInterventionDebugSnapshot
                keeperSnapshot =
                    KeeperInterventionState.Value;

            Debug.Log(
                $"[{nameof(DomainSnapshotReplicator)}] " +
                $"Rebuilt real snapshot " +
                $"v{SnapshotVersion.Value} | " +
                $"playerRows={PlayerInventoryRows.Count} | " +
                $"rehearsalSets={RehearsalSetRows.Count} | " +
                $"rehearsalTracks={RehearsalTrackRows.Count} | " +
                $"demoTapes={DemoTapeRows.Count} | " +
                $"demoTracks={DemoTapeTrackSemanticRows.Count} | " +
                $"demoIdeas={DemoTapeIdeaSemanticRows.Count} | " +
                $"demoTags={DemoTapeTagSemanticRows.Count} | " +
                $"turnRecords=" +
                $"{KvltTurnResolutionRows.Count} | " +
                $"keeperTenure={keeperSnapshot.HasTenure} | " +
                $"keeperPull={keeperSnapshot.Pull:F2} | " +
                $"keeperTargets={KeeperReleaseRows.Count}"
            );
        }

        private void AddPeak2ReadModels(
            GameCoordinator coordinator)
        {
            SeededWorldState world =
                coordinator.AuthoritativePeak2WorldState;

            if (world == null)
            {
                return;
            }

            KeeperTenureState tenure =
                coordinator.CurrentKeeperTenure;

            int fieldCount = 0;
            int retainedCount = 0;
            int historicalCount = 0;
            int sceneReleaseCount = 0;

            foreach (SceneRelease release
                     in world.SceneReleases)
            {
                if (release == null ||
                    release.HostedSceneNodeId !=
                        GameCoordinator.NodeKvltScene)
                {
                    continue;
                }

                sceneReleaseCount++;

                switch (release.LifecycleState)
                {
                    case SceneReleaseLifecycleState.Field:
                        fieldCount++;
                        break;

                    case SceneReleaseLifecycleState
                        .CanonRetained:
                        retainedCount++;
                        break;

                    case SceneReleaseLifecycleState
                        .HistoricalCanon:
                        historicalCount++;
                        break;
                }
            }

            KvltTurnResolutionRecord latest =
                world.LatestKvltTurnResolutionRecord;

            KvltSceneState.Value =
                new KvltSceneDebugSnapshot
                {
                    HasState = true,
                    CurrentTurn =
                        coordinator.globalTurn.Value,
                    LastSettledTurn =
                        latest?.SettledTurn ?? -1,
                    PublishedTurn =
                        latest?.PublishedTurn ??
                        coordinator.globalTurn.Value,
                    RuntimePhaseValue =
                        (int)coordinator
                            .peak2TurnResolutionPhase.Value,
                    CanonPrecedentCount =
                        world.KvltCanon.Records.Count,
                    PressureEntryCount =
                        world.KvltScenePressure.EntryCount,
                    NormativeAffinityCount =
                        world.KvltNormativeCentre
                            .NonZeroAffinityCount,
                    ScoreEventCount =
                        world.KvltScoreLedger.Count,
                    StandingOwnerCount =
                        world.KvltSceneStandingByOwner.Count,
                    SceneReleaseCount =
                        sceneReleaseCount,
                    FieldReleaseCount =
                        fieldCount,
                    CanonRetainedCount =
                        retainedCount,
                    HistoricalCanonCount =
                        historicalCount,
                    KeeperClientId =
                        tenure?.KeeperClientId ??
                        ulong.MaxValue,
                    KeeperTenureId =
                        ToFixed64(
                            tenure?.KeeperTenureId
                        ),
                    KeeperPull =
                        tenure?.Pull ?? 0f
                };

            foreach (CanonPrecedentRecord precedent
                     in world.KvltCanon.Records)
            {
                KvltCanonPrecedentRows.Add(
                    new KvltCanonPrecedentDebugRow
                    {
                        AxisValue =
                            (byte)precedent.Axis,
                        PoleValue =
                            (byte)precedent.Pole,
                        DegreeValue =
                            (byte)precedent.Degree,
                        ProvenanceKindValue =
                            (byte)precedent.ProvenanceKind,
                        SourceArtifactId =
                            ToFixed64(
                                precedent.SourceArtifactId
                            ),
                        SourceTrackId =
                            ToFixed64(
                                precedent.SourceTrackId
                            ),
                        SourceIdeaId =
                            ToFixed64(
                                precedent.SourceIdeaId
                            ),
                        EstablishedTurn =
                            precedent.EstablishedTurn
                    }
                );
            }

            foreach (TagAxis axis
                     in Enum.GetValues(typeof(TagAxis)))
            {
                foreach (TagPole pole
                         in Enum.GetValues(typeof(TagPole)))
                {
                    float rawPressure =
                        world.KvltScenePressure
                            .GetRawPressure(axis, pole);

                    float effectivePressure =
                        world.KvltScenePressure
                            .GetEffectivePressure(axis, pole);

                    foreach (TagDegree degree
                             in Enum.GetValues(
                                 typeof(TagDegree)))
                    {
                        NormativeCentreDerivationEntry
                            derivation = null;

                        bool hasDerivation =
                            latest != null &&
                            latest.NextEnvironment
                                .NormativeCentre
                                .TryGetEntry(
                                    axis,
                                    pole,
                                    degree,
                                    out derivation
                                );

                        KvltSemanticEnvironmentRows.Add(
                            new KvltSemanticEnvironmentDebugRow
                            {
                                AxisValue = (byte)axis,
                                PoleValue = (byte)pole,
                                DegreeValue = (byte)degree,
                                HasDerivation =
                                    hasDerivation,
                                HasDirectCanon =
                                    hasDerivation &&
                                    derivation.HasDirectCanon,
                                CanonicalAffinity =
                                    hasDerivation
                                        ? derivation
                                            .CanonicalAffinity
                                        : 0f,
                                RawPressure =
                                    rawPressure,
                                EffectivePressure =
                                    effectivePressure,
                                FinalAffinity =
                                    world.KvltNormativeCentre
                                        .GetAffinity(
                                            axis,
                                            pole,
                                            degree
                                        )
                            }
                        );
                    }
                }
            }

            for (int index = 0;
                 index < PlayerInventoryRows.Count;
                 index++)
            {
                PlayerInventoryDebugRow row =
                    PlayerInventoryRows[index];

                string entityId =
                    row.LeaderEntityId.ToString();

                if (string.IsNullOrWhiteSpace(
                        entityId) ||
                    entityId == "None")
                {
                    continue;
                }

                row.KvltTotalScore =
                    world.KvltScoreLedger
                        .GetLifetimeTotal(entityId);

                if (world.TryGetKvltSceneStanding(
                        entityId,
                        out var standingState) &&
                    standingState.HasCurrentStanding)
                {
                    row.HasKvltStanding = true;
                    row.KvltStanding =
                        standingState
                            .CurrentStanding.Value;
                }

                foreach (YearInfluenceEvaluation evaluation
                         in coordinator.LatestPeak2YearInfluence)
                {
                    if (evaluation.BeneficiaryEntityId !=
                        entityId)
                    {
                        continue;
                    }

                    row.HasYearInfluence = true;
                    row.YearInfluence =
                        evaluation.YearInfluence;
                    break;
                }

                row.IsKvltKeeper =
                    row.ClientId ==
                    coordinator.keeperClientId.Value;

                foreach (SceneRelease release
                         in world.SceneReleases)
                {
                    if (release != null &&
                        release.HostedSceneNodeId ==
                            GameCoordinator.NodeKvltScene &&
                        release.SourceOwnerEntityId ==
                            entityId)
                    {
                        row.KvltReleaseCount++;
                    }
                }

                PlayerInventoryRows[index] = row;
            }

            foreach (KvltTurnResolutionRecord record
                     in world.KvltTurnResolutionHistory)
            {
                KeeperTransitionResult? transition =
                    record.KeeperTransition;

                KvltTurnResolutionRows.Add(
                    new KvltTurnResolutionDebugRow
                    {
                        SceneId =
                            ToFixed64(record.SceneId),
                        SettledTurn =
                            record.SettledTurn,
                        PublishedTurn =
                            record.PublishedTurn,
                        IsYearEnd =
                            record.IsYearEnd,
                        MovementCount =
                            record.MovementCount,
                        RejectedCount =
                            record.RejectedCount,
                        HappeningCount =
                            record.HappeningCount,
                        CrisisCount =
                            record.CrisisCount,
                        CoveredActivationCount =
                            record.Happening
                                .CoveredActivationApplications,
                        CrisisLegitimizedCount =
                            record.Happening
                                .CrisisLegitimizedApplications,
                        PendingStoredCount =
                            record.Happening
                                .PendingStoredCount,
                        PendingRedeemedCount =
                            record.Happening
                                .PendingRedeemedCount,
                        AcceptedPrecedentsRaised =
                            record.Happening
                                .AcceptedPrecedentsRaised,
                        ParadigmContestCount =
                            record.Happening
                                .ParadigmContestResults.Count,
                        ParadigmBeefCount =
                            record.Happening
                                .ParadigmBeefCount,
                        NewPoserDeclarationCount =
                            record.Happening
                                .NewPoserDeclarationCount,
                        PostHappeningLegitimacyCount =
                            record.CanonizationScreening
                                .LegitimacyEvaluations.Count,
                        CanonBreakthroughCount =
                            record.CanonizationScreening
                                .BreakthroughsByRelease.Count,
                        CanonizedReleaseCount =
                            record.CanonizedReleaseCount,
                        ScoreEventCount =
                            record.ScoreEventCount,
                        StandingOwnerCount =
                            record.Standing.OwnerCount,
                        IngressedReleaseCount =
                            record.IngressedReleaseCount,
                        PressureContributionCount =
                            record.NextEnvironment
                                .Pressure
                                .SourceContributionCount,
                        HasKeeperTransition =
                            transition.HasValue,
                        PreviousKeeperClientId =
                            transition?
                                .PreviousKeeperClientId ??
                            ulong.MaxValue,
                        NextKeeperClientId =
                            transition?
                                .NextKeeperClientId ??
                            ulong.MaxValue,
                        HistoricalCanonTransitionCount =
                            record.CanonTenureTransitions.Count
                    }
                );
            }

            foreach (Happening happening
                     in world.KvltHappeningRegistry.GetAll())
            {
                int contestCount = 0;
                int beefCount = 0;
                int newPoserCount = 0;

                foreach (KvltTurnResolutionRecord record
                         in world.KvltTurnResolutionHistory)
                {
                    foreach (KvltParadigmContestResult contest
                             in record.Happening
                                 .ParadigmContestResults)
                    {
                        if (contest.HappeningId !=
                            happening.HappeningId)
                            continue;

                        contestCount++;
                        if (contest.IsBeef)
                            beefCount++;
                        newPoserCount +=
                            contest.NewPoserDeclarations.Count;
                    }
                }

                KvltHappeningRows.Add(
                    new KvltHappeningDebugRow
                    {
                        HappeningId = ToFixed128(
                            happening.HappeningId),
                        InstigatorEntityId = ToFixed64(
                            happening.InstigatorEntityId),
                        AnchorPhysicalNodeId = ToFixed128(
                            happening.AnchorPhysicalNodeId),
                        CommittedTurn = happening.CommittedTurn,
                        LifecycleStateValue =
                            (int)happening.LifecycleState,
                        ParticipantCount =
                            happening.ParticipantEntityIds.Count,
                        IntentCount =
                            happening.ParticipantIntents.Count,
                        BehaviorCount =
                            happening.BehaviorOccurrences.Count,
                        HailCount =
                            happening.HailOccurrences.Count,
                        ParadigmContestCount = contestCount,
                        BeefCount = beefCount,
                        NewPoserCount = newPoserCount
                    });

                if (happening.LifecycleState !=
                    HappeningLifecycleState.Settled)
                    continue;

                foreach (HailOccurrence hail
                         in happening.HailOccurrences)
                {
                    KvltHailRows.Add(
                        new KvltHailDebugRow
                        {
                            HappeningId = ToFixed128(
                                happening.HappeningId),
                            BehaviorOccurrenceId = ToFixed128(
                                hail.BehaviorOccurrenceId),
                            HailOccurrenceId = ToFixed128(
                                hail.HailOccurrenceId),
                            DeclarerEntityId = ToFixed64(
                                hail.DeclarerEntityId),
                            HailedAspectId = ToFixed64(
                                hail.HailedAspectId)
                        });
                }
            }

            foreach (KvltParadigmGripRecord grip
                     in world.KvltParadigmState.GripRecords)
            {
                KvltParadigmGripRows.Add(
                    new KvltParadigmGripDebugRow
                    {
                        EntityId = ToFixed64(grip.EntityId),
                        HailAspectId = ToFixed64(
                            grip.HailAspectId),
                        ReinforcementCount =
                            grip.ReinforcementCount,
                        FirstReinforcedTurn =
                            grip.FirstReinforcedTurn,
                        LastReinforcedTurn =
                            grip.LastReinforcedTurn
                    });
            }

            foreach (KvltParadigmBeefRecord beef
                     in world.KvltParadigmState.BeefRecords)
            {
                KvltParadigmBeefRows.Add(
                    new KvltParadigmBeefDebugRow
                    {
                        FirstHailAspectId = ToFixed64(
                            beef.Opposition.FirstHailAspectId),
                        SecondHailAspectId = ToFixed64(
                            beef.Opposition.SecondHailAspectId),
                        BehaviorTypeId = ToFixed128(
                            beef.BehaviorTypeId),
                        FirstHappeningId = ToFixed128(
                            beef.FirstHappeningId),
                        LatestBehaviorOccurrenceId = ToFixed128(
                            beef.LatestBehaviorOccurrenceId),
                        ReinforcementCount =
                            beef.ReinforcementCount,
                        FirstSettledTurn =
                            beef.FirstSettledTurn,
                        LastSettledTurn =
                            beef.LastSettledTurn
                    });
            }

            int currentTurn = coordinator.globalTurn.Value;

            foreach (KvltPoserDeclaration poser
                     in world.KvltParadigmState.PoserDeclarations)
            {
                KvltPoserRows.Add(
                    new KvltPoserDebugRow
                    {
                        DeclarationId = ToFixed128(
                            poser.DeclarationId),
                        EntityId = ToFixed64(poser.EntityId),
                        SourceHappeningId = ToFixed128(
                            poser.SourceHappeningId),
                        SourceBehaviorOccurrenceId = ToFixed128(
                            poser.SourceBehaviorOccurrenceId),
                        LosingHailAspectId = ToFixed64(
                            poser.LosingHailAspectId),
                        WinningHailAspectId = ToFixed64(
                            poser.WinningHailAspectId),
                        ActiveFromTurn = poser.ActiveFromTurn,
                        ActiveUntilTurnExclusive =
                            poser.ActiveUntilTurnExclusive,
                        RemainingTurns =
                            poser.GetRemainingTurnsAt(currentTurn),
                        IsActive = poser.IsActiveAt(currentTurn)
                    });
            }
        }

        private void AddKeeperInterventionSnapshot(
            GameCoordinator coordinator,
            Dictionary<string, PlayerSnapshotOwnerInfo>
                ownerByEntityId)
        {
            if (coordinator == null)
                return;

            KeeperTenureState tenure =
                coordinator.CurrentKeeperTenure;

            ulong authoritativeKeeperClientId =
                coordinator.keeperClientId.Value;

            int currentTurn =
                coordinator.globalTurn.Value;

            bool hasCurrentTenure =
                tenure != null &&
                tenure.KeeperClientId ==
                authoritativeKeeperClientId;

            bool hasSpendablePull =
                hasCurrentTenure &&
                tenure.Pull >
                KeeperPullRules.ComparisonTolerance;

            bool canUseKeeperActions =
                coordinator.IsPlayableSessionStarted &&
                hasSpendablePull;

            KeeperInterventionState.Value =
                new KeeperInterventionDebugSnapshot
                {
                    HasTenure =
                        hasCurrentTenure,

                    KeeperClientId =
                        hasCurrentTenure
                            ? tenure.KeeperClientId
                            : ulong.MaxValue,

                    EvaluatedTurn =
                        currentTurn,

                    Pull =
                        hasCurrentTenure
                            ? tenure.Pull
                            : 0.0f,

                    BoostAvailable =
                        canUseKeeperActions &&
                        !tenure.HasUsedIntervention(
                            KeeperInterventionType
                                .BoostVisibility,
                            currentTurn
                        ),

                    SuppressAvailable =
                        canUseKeeperActions &&
                        !tenure.HasUsedIntervention(
                            KeeperInterventionType
                                .SuppressVisibility,
                            currentTurn
                        )
                };

            IReadOnlyList<SceneRelease> releases =
                coordinator.SceneReleases;

            if (releases == null)
                return;

            foreach (SceneRelease release in releases)
            {
                if (release == null)
                    continue;

                ulong ownerClientId =
                    ulong.MaxValue;

                string ownerName =
                    release.SourceOwnerEntityId;

                if (!string.IsNullOrWhiteSpace(
                        release.SourceOwnerEntityId
                    ) &&
                    ownerByEntityId.TryGetValue(
                        release.SourceOwnerEntityId,
                        out PlayerSnapshotOwnerInfo ownerInfo
                    ))
                {
                    ownerClientId =
                        ownerInfo.ClientId;

                    ownerName =
                        ownerInfo.DisplayName;
                }

                bool canReceiveBoost =
                    release.TryPreviewVisibilityAdjustment(
                        requestedDelta: 1.0f,
                        out _
                    );

                bool canReceiveSuppress =
                    release.TryPreviewVisibilityAdjustment(
                        requestedDelta: -1.0f,
                        out _
                    );

                KeeperReleaseRows.Add(
                    new KeeperReleaseDebugRow
                    {
                        ReleaseId =
                            ToFixed64(
                                release.ReleaseId
                            ),

                        DisplayName =
                            ToFixed64(
                                release.DisplayName
                            ),

                        SourceDemoTapeId =
                            ToFixed64(
                                release.SourceDemoTapeId
                            ),

                        HostedSceneNodeId =
                            ToFixed64(
                                release.HostedSceneNodeId
                            ),

                        OwnerClientId =
                            ownerClientId,

                        OwnerName =
                            ToFixed32(
                                ownerName
                            ),

                        OrganicVisibility =
                            release.CirculationState
                                ?.Reach ??
                            0.0f,

                        EffectiveVisibility =
                            release.EffectiveVisibility,

                        PendingVisibilityAdjustment =
                            release
                                .PendingVisibilityAdjustment,

                        HasPendingVisibilityAdjustment =
                            release
                                .HasPendingVisibilityAdjustment,

                        CanReceiveBoost =
                            canReceiveBoost,

                        CanReceiveSuppress =
                            canReceiveSuppress,

                        LifecycleStateValue =
                            (byte)release.LifecycleState,

                        ReleasedTurn =
                            release.ReleasedTurn,

                        HasFieldPosition =
                            release.HasFieldPosition,

                        FieldPosition =
                            release.FieldPositionState
                                ?.CurrentPosition ?? 0f,

                        FieldEstablishedTurn =
                            release.FieldPositionState
                                ?.EstablishedTurn ?? -1,

                        PairActivationCount =
                            release.PairActivationStates.Count,

                        PendingActivationCount =
                            release.PendingActivations.Count,

                        ActivationHistoryCount =
                            release.ActivationHistory.Count,

                        IsActivationFrozen =
                            release.IsActivationFrozen,

                        CanonizedTurn =
                            release.CanonizedTurn,

                        CanonizedUnderKeeperTenureId =
                            ToFixed64(
                                release
                                    .CanonizedUnderKeeperTenureId
                            ),

                        FrozenPostAssimilationGravity =
                            release
                                .FrozenPostAssimilationGravity,

                        HasRejectionState =
                            release.HasRejectionState
                    }
                );
            }
        }

        public struct KeeperInterventionDebugSnapshot :
            INetworkSerializable,
            IEquatable<KeeperInterventionDebugSnapshot>
        {
            public bool HasTenure;
            public ulong KeeperClientId;
            public int EvaluatedTurn;

            public float Pull;

            public bool BoostAvailable;
            public bool SuppressAvailable;

            public void NetworkSerialize<T>(
                BufferSerializer<T> serializer)
                where T : IReaderWriter
            {
                serializer.SerializeValue(
                    ref HasTenure
                );

                serializer.SerializeValue(
                    ref KeeperClientId
                );

                serializer.SerializeValue(
                    ref EvaluatedTurn
                );

                serializer.SerializeValue(
                    ref Pull
                );

                serializer.SerializeValue(
                    ref BoostAvailable
                );

                serializer.SerializeValue(
                    ref SuppressAvailable
                );
            }

            public bool Equals(
                KeeperInterventionDebugSnapshot other)
            {
                return
                    HasTenure == other.HasTenure &&
                    KeeperClientId ==
                    other.KeeperClientId &&
                    EvaluatedTurn ==
                    other.EvaluatedTurn &&
                    Pull.Equals(other.Pull) &&
                    BoostAvailable ==
                    other.BoostAvailable &&
                    SuppressAvailable ==
                    other.SuppressAvailable;
            }

            public override bool Equals(
                object obj)
            {
                return
                    obj is
                        KeeperInterventionDebugSnapshot
                        other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    HasTenure,
                    KeeperClientId,
                    EvaluatedTurn,
                    Pull,
                    BoostAvailable,
                    SuppressAvailable
                );
            }
        }

        public struct KeeperReleaseDebugRow :
            INetworkSerializable,
            IEquatable<KeeperReleaseDebugRow>
        {
            public FixedString64Bytes ReleaseId;
            public FixedString64Bytes DisplayName;

            public FixedString64Bytes SourceDemoTapeId;
            public FixedString64Bytes HostedSceneNodeId;

            public ulong OwnerClientId;
            public FixedString32Bytes OwnerName;

            public float OrganicVisibility;
            public float EffectiveVisibility;
            public float PendingVisibilityAdjustment;

            public bool HasPendingVisibilityAdjustment;
            public bool CanReceiveBoost;
            public bool CanReceiveSuppress;

            public byte LifecycleStateValue;
            public int ReleasedTurn;
            public bool HasFieldPosition;
            public float FieldPosition;
            public int FieldEstablishedTurn;
            public int PairActivationCount;
            public int PendingActivationCount;
            public int ActivationHistoryCount;
            public bool IsActivationFrozen;
            public int CanonizedTurn;
            public FixedString64Bytes
                CanonizedUnderKeeperTenureId;
            public float FrozenPostAssimilationGravity;
            public bool HasRejectionState;

            public void NetworkSerialize<T>(
                BufferSerializer<T> serializer)
                where T : IReaderWriter
            {
                serializer.SerializeValue(
                    ref ReleaseId
                );

                serializer.SerializeValue(
                    ref DisplayName
                );

                serializer.SerializeValue(
                    ref SourceDemoTapeId
                );

                serializer.SerializeValue(
                    ref HostedSceneNodeId
                );

                serializer.SerializeValue(
                    ref OwnerClientId
                );

                serializer.SerializeValue(
                    ref OwnerName
                );

                serializer.SerializeValue(
                    ref OrganicVisibility
                );

                serializer.SerializeValue(
                    ref EffectiveVisibility
                );

                serializer.SerializeValue(
                    ref PendingVisibilityAdjustment
                );

                serializer.SerializeValue(
                    ref HasPendingVisibilityAdjustment
                );

                serializer.SerializeValue(
                    ref CanReceiveBoost
                );

                serializer.SerializeValue(
                    ref CanReceiveSuppress
                );

                serializer.SerializeValue(ref LifecycleStateValue);
                serializer.SerializeValue(ref ReleasedTurn);
                serializer.SerializeValue(ref HasFieldPosition);
                serializer.SerializeValue(ref FieldPosition);
                serializer.SerializeValue(ref FieldEstablishedTurn);
                serializer.SerializeValue(ref PairActivationCount);
                serializer.SerializeValue(ref PendingActivationCount);
                serializer.SerializeValue(ref ActivationHistoryCount);
                serializer.SerializeValue(ref IsActivationFrozen);
                serializer.SerializeValue(ref CanonizedTurn);
                serializer.SerializeValue(
                    ref CanonizedUnderKeeperTenureId
                );
                serializer.SerializeValue(
                    ref FrozenPostAssimilationGravity
                );
                serializer.SerializeValue(ref HasRejectionState);
            }

            public bool Equals(
                KeeperReleaseDebugRow other)
            {
                return
                    ReleaseId.Equals(
                        other.ReleaseId
                    ) &&
                    DisplayName.Equals(
                        other.DisplayName
                    ) &&
                    SourceDemoTapeId.Equals(
                        other.SourceDemoTapeId
                    ) &&
                    HostedSceneNodeId.Equals(
                        other.HostedSceneNodeId
                    ) &&
                    OwnerClientId ==
                    other.OwnerClientId &&
                    OwnerName.Equals(
                        other.OwnerName
                    ) &&
                    OrganicVisibility.Equals(
                        other.OrganicVisibility
                    ) &&
                    EffectiveVisibility.Equals(
                        other.EffectiveVisibility
                    ) &&
                    PendingVisibilityAdjustment.Equals(
                        other.PendingVisibilityAdjustment
                    ) &&
                    HasPendingVisibilityAdjustment ==
                    other.HasPendingVisibilityAdjustment &&
                    CanReceiveBoost ==
                    other.CanReceiveBoost &&
                    CanReceiveSuppress ==
                    other.CanReceiveSuppress &&
                    LifecycleStateValue == other.LifecycleStateValue &&
                    ReleasedTurn == other.ReleasedTurn &&
                    HasFieldPosition == other.HasFieldPosition &&
                    FieldPosition.Equals(other.FieldPosition) &&
                    FieldEstablishedTurn == other.FieldEstablishedTurn &&
                    PairActivationCount == other.PairActivationCount &&
                    PendingActivationCount == other.PendingActivationCount &&
                    ActivationHistoryCount == other.ActivationHistoryCount &&
                    IsActivationFrozen == other.IsActivationFrozen &&
                    CanonizedTurn == other.CanonizedTurn &&
                    CanonizedUnderKeeperTenureId.Equals(
                        other.CanonizedUnderKeeperTenureId
                    ) &&
                    FrozenPostAssimilationGravity.Equals(
                        other.FrozenPostAssimilationGravity
                    ) &&
                    HasRejectionState == other.HasRejectionState;
            }

            public override bool Equals(
                object obj)
            {
                return
                    obj is KeeperReleaseDebugRow other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                var hash =
                    new HashCode();

                hash.Add(ReleaseId);
                hash.Add(DisplayName);
                hash.Add(SourceDemoTapeId);
                hash.Add(HostedSceneNodeId);
                hash.Add(OwnerClientId);
                hash.Add(OwnerName);
                hash.Add(OrganicVisibility);
                hash.Add(EffectiveVisibility);
                hash.Add(
                    PendingVisibilityAdjustment
                );
                hash.Add(
                    HasPendingVisibilityAdjustment
                );
                hash.Add(CanReceiveBoost);
                hash.Add(CanReceiveSuppress);
                hash.Add(LifecycleStateValue);
                hash.Add(ReleasedTurn);
                hash.Add(HasFieldPosition);
                hash.Add(FieldPosition);
                hash.Add(FieldEstablishedTurn);
                hash.Add(PairActivationCount);
                hash.Add(PendingActivationCount);
                hash.Add(ActivationHistoryCount);
                hash.Add(IsActivationFrozen);
                hash.Add(CanonizedTurn);
                hash.Add(CanonizedUnderKeeperTenureId);
                hash.Add(FrozenPostAssimilationGravity);
                hash.Add(HasRejectionState);

                return hash.ToHashCode();
            }
        }

        public bool TryFindFirstStructuralPairCandidate(
            FixedString64Bytes demoTapeId,
            out DemoTapeStructuralPairCandidate candidate)
        {
            return DemoTapeStructuralPairQuery.TryFindFirst(
                demoTapeId,
                DemoTapeIdeaSemanticRows,
                DemoTapeTagSemanticRows,
                out candidate
            );
        }

        public bool
            TryFindFirstStructuralPairCandidateForRelease(
                FixedString64Bytes releaseId,
                out DemoTapeStructuralPairCandidate candidate)
        {
            return SceneReleaseStructuralPairQuery
                .TryFindFirst(
                    releaseId,
                    KeeperReleaseRows,
                    DemoTapeIdeaSemanticRows,
                    DemoTapeTagSemanticRows,
                    out candidate
                );
        }


#if UNITY_EDITOR
        [ContextMenu("DEBUG Print Demo Tape Rows")]
        private void DebugPrintDemoTapeRows()
        {
            ulong localClientId =
                NetworkManager.Singleton != null
                    ? NetworkManager.Singleton.LocalClientId
                    : ulong.MaxValue;

            Debug.Log(
                "[DEMO TAPE ROWS] " +
                $"peer={(IsServer ? "server" : "client")} | " +
                $"localClient={localClientId} | " +
                $"count={DemoTapeRows.Count}",
                this
            );

            for (int i = 0;
                 i < DemoTapeRows.Count;
                 i++)
            {
                DemoTapeDebugRow row =
                    DemoTapeRows[i];

                Debug.Log(
                    "[DEMO TAPE ROW] " +
                    $"index={i} | " +
                    $"owner={row.OwnerClientId} | " +
                    $"local={row.OwnerClientId == localClientId} | " +
                    $"id={row.DemoTapeId} | " +
                    $"name={row.DisplayName} | " +
                    $"sourceSet={row.SourceSetName} | " +
                    $"turn={row.RecordedTurn} | " +
                    $"tracks={row.TrackCount} | " +
                    $"takes={row.TakeCount} | " +
                    $"state={row.SceneState} | " +
                    $"released={row.IsReleased}",
                    this
                );
            }
        }

        [ContextMenu("DEBUG Print Demo Tape Semantic Rows")]
        private void DebugPrintDemoTapeSemanticRows()
        {
            Debug.Log(
                "[DEMO SEMANTIC ROWS] " +
                $"peer={(IsServer ? "server" : "client")} | " +
                $"tracks={DemoTapeTrackSemanticRows.Count} | " +
                $"ideas={DemoTapeIdeaSemanticRows.Count} | " +
                $"tags={DemoTapeTagSemanticRows.Count}",
                this
            );

            for (int i = 0;
                 i < DemoTapeTrackSemanticRows.Count;
                 i++)
            {
                DemoTapeTrackSemanticDebugRow row =
                    DemoTapeTrackSemanticRows[i];

                Debug.Log(
                    "[DEMO SEMANTIC TRACK] " +
                    $"demo={row.DemoTapeId} | " +
                    $"demoIndex={row.DemoIndex} | " +
                    $"trackIndex={row.TrackIndex} | " +
                    $"track={row.SourceTrackId} | " +
                    $"title={row.DisplayName} | " +
                    $"ideas={row.IdeaCount} | " +
                    $"sourceC={row.SourceConveyance:F2} | " +
                    $"recordedC={row.RecordedConveyance:F2}",
                    this
                );
            }

            for (int i = 0;
                 i < DemoTapeIdeaSemanticRows.Count;
                 i++)
            {
                DemoTapeIdeaSemanticDebugRow row =
                    DemoTapeIdeaSemanticRows[i];

                Debug.Log(
                    "[DEMO SEMANTIC IDEA] " +
                    $"demo={row.DemoTapeId} | " +
                    $"trackIndex={row.TrackIndex} | " +
                    $"ideaIndex={row.IdeaIndex} | " +
                    $"idea={row.SourceIdeaId} | " +
                    $"aspect={row.AspectId} | " +
                    $"payload={row.PayloadType} | " +
                    $"tags={row.TagOccurrenceCount}",
                    this
                );
            }

            for (int i = 0;
                 i < DemoTapeTagSemanticRows.Count;
                 i++)
            {
                DemoTapeTagSemanticDebugRow row =
                    DemoTapeTagSemanticRows[i];

                Debug.Log(
                    "[DEMO SEMANTIC TAG] " +
                    $"demo={row.DemoTapeId} | " +
                    $"trackIndex={row.TrackIndex} | " +
                    $"ideaIndex={row.IdeaIndex} | " +
                    $"tagIndex={row.TagOccurrenceIndex} | " +
                    $"axis={row.Axis} | " +
                    $"pole={row.Pole} | " +
                    $"degree={row.Degree} | " +
                    $"role={row.Role}",
                    this
                );
            }
        }

        [ContextMenu(
            "DEBUG Print Release Structural Pair Candidates"
        )]
        private void
            DebugPrintReleaseStructuralPairCandidates()
        {
            Debug.Log(
                "[RELEASE STRUCTURAL PAIRS] " +
                $"releaseCount={KeeperReleaseRows.Count}",
                this
            );

            for (int index = 0;
                 index < KeeperReleaseRows.Count;
                 index++)
            {
                KeeperReleaseDebugRow release =
                    KeeperReleaseRows[index];

                bool found =
                    TryFindFirstStructuralPairCandidateForRelease(
                        release.ReleaseId,
                        out DemoTapeStructuralPairCandidate
                            candidate
                    );

                if (!found)
                {
                    Debug.Log(
                        "[RELEASE STRUCTURAL PAIR] " +
                        $"release={release.ReleaseId} | " +
                        $"sourceDemo=" +
                        $"{release.SourceDemoTapeId} | " +
                        "candidate=False",
                        this
                    );

                    continue;
                }

                Debug.Log(
                    "[RELEASE STRUCTURAL PAIR] " +
                    $"release={release.ReleaseId} | " +
                    $"sourceDemo=" +
                    $"{release.SourceDemoTapeId} | " +
                    "candidate=True | " +
                    $"trackIndex={candidate.TrackIndex} | " +
                    $"ideaIndex={candidate.IdeaIndex} | " +
                    $"axis={candidate.Axis} | " +
                    $"dominant=" +
                    $"{candidate.DominantPole}/" +
                    $"{candidate.DominantDegree} | " +
                    $"submissive=" +
                    $"{candidate.SubmissivePole}/" +
                    $"{candidate.SubmissiveDegree}",
                    this
                );
            }
        }



        [ContextMenu(
            "DEBUG Print Demo Structural Pair Candidates"
        )]
        private void
            DebugPrintDemoStructuralPairCandidates()
        {
            Debug.Log(
                "[DEMO STRUCTURAL PAIRS] " +
                $"demoCount={DemoTapeRows.Count}",
                this
            );

            for (int i = 0;
                 i < DemoTapeRows.Count;
                 i++)
            {
                DemoTapeDebugRow demo =
                    DemoTapeRows[i];

                bool hasCandidate =
                    TryFindFirstStructuralPairCandidate(
                        demo.DemoTapeId,
                        out DemoTapeStructuralPairCandidate
                            candidate
                    );

                if (!hasCandidate)
                {
                    Debug.Log(
                        "[DEMO STRUCTURAL PAIR] " +
                        $"demo={demo.DemoTapeId} | " +
                        $"name={demo.DisplayName} | " +
                        "candidate=False",
                        this
                    );

                    continue;
                }

                Debug.Log(
                    "[DEMO STRUCTURAL PAIR] " +
                    $"demo={demo.DemoTapeId} | " +
                    $"name={demo.DisplayName} | " +
                    "candidate=True | " +
                    $"trackIndex={candidate.TrackIndex} | " +
                    $"ideaIndex={candidate.IdeaIndex} | " +
                    $"axis={candidate.Axis} | " +
                    $"dominant=" +
                    $"{candidate.DominantPole}/" +
                    $"{candidate.DominantDegree} | " +
                    $"submissive=" +
                    $"{candidate.SubmissivePole}/" +
                    $"{candidate.SubmissiveDegree}",
                    this
                );
            }
        }
    }
#endif
}
