using System.Collections.Generic;
using SEMM91.Core.Collectives;
using SEMM91.Core.Entities;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.Kvlt.Settlement;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Score;
using SEMM91.GamePlay.SceneSpace;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Society;
using UnityEngine;

namespace SEMM91.GamePlay.World
{
    public class SeededWorldState
    {
        private string dominantOutputOwnerEntityId;
        private float dominantOutputScore;
        public string DominantOutputOwnerEntityId => dominantOutputOwnerEntityId;
        public float DominantOutputScore => dominantOutputScore;

        private readonly List<GameEntity> entities = new();
        private readonly List<EntityHostingRecord> hostingRecords = new();
        private readonly List<SceneRelease> sceneReleases = new();
        private readonly List<SceneOutputStanding> latestSceneOutputStandings = new();

        public IReadOnlyList<SceneOutputStanding> LatestSceneOutputStandings => latestSceneOutputStandings;
        public SceneSpaceGraph SceneSpaceGraph { get; } = new SceneSpaceGraph();

        public CanonState KvltCanon { get; } =
            new CanonState();

        public SocietyNormativeProfile SocietyNorms { get; } =
            new SocietyNormativeProfile();

        public NormativeCentre KvltNormativeCentre { get; private set; } =
            NormativeCentre.Neutral;

        public SettledScenePressure KvltScenePressure { get; private set; } =
            SettledScenePressure.Empty;

        public ScoreLedger KvltScoreLedger { get; } =
            new ScoreLedger();

        /*
         * Durable authoritative social/institutional state
         * used by the every-turn Happening pipeline.
         *
         * These are session-domain objects. GameCoordinator
         * orchestrates them but does not own parallel copies.
         */
        public HappeningRegistry
            KvltHappeningRegistry { get; } =
            new HappeningRegistry();

        public AcceptedTransgressionState
            KvltAcceptedTransgressions { get; } =
            new AcceptedTransgressionState(
                StartingCollectiveBootstrapper
                    .KvltEntityId
            );

        public AllegianceCrisisRegistry
            KvltAllegianceCrisisRegistry { get; } =
            new AllegianceCrisisRegistry();
        
        public int LastAppliedKvltSettlementTurn { get; private set; } = -1;

        public PhysicalMapGrid PhysicalMapGrid { get; } =
            new PhysicalMapGrid();

        public CollectiveRegistry CollectiveRegistry { get; }

        public IReadOnlyList<GameEntity> Entities => entities;
        public IReadOnlyList<EntityHostingRecord> HostingRecords => hostingRecords;

        public IReadOnlyList<SceneRelease> SceneReleases => sceneReleases;

        public SeededWorldState(CollectiveRegistry collectiveRegistry)
        {
            CollectiveRegistry = collectiveRegistry;
        }

        public void ApplySettledNormativeCentre(
            NormativeCentre normativeCentre)
        {
            if (normativeCentre == null)
            {
                throw new System.ArgumentNullException(
                    nameof(normativeCentre)
                );
            }

            KvltNormativeCentre =
                normativeCentre;
        }

        public bool AddEntity(GameEntity entity)
        {
            if (entity == null)
            {
                Debug.LogWarning("[SeededWorldState] Cannot add null entity");
                return false;
            }

            if (FindEntity(entity.EntityId) != null)
            {
                Debug.LogWarning($"[SeededWorldState] Entity already registered: {entity.EntityId}");
                return false;
            }

            entities.Add(entity);
            Debug.Log($"[SeededWorldState] Registered entity: {entity.DisplayName} ({entity.EntityType})");
            return true;
        }

        public void ApplySettledScenePressure(
            SettledScenePressure scenePressure)
        {
            if (scenePressure == null)
            {
                throw new System.ArgumentNullException(
                    nameof(scenePressure)
                );
            }

            KvltScenePressure =
                scenePressure;
        }

        public bool TryApplyKvltSceneSettlement(
            KvltSceneSettlementResult settlement)
        {
            if (settlement == null)
            {
                return false;
            }

            /*
             * Settlement publication is strictly
             * chronological.
             *
             * Replaying an old result must never roll the
             * authoritative world backwards.
             */
            if (settlement.SettledTurn <=
                LastAppliedKvltSettlementTurn)
            {
                return false;
            }

            /*
             * The result must have been calculated from the
             * exact Canon history currently authoritative in
             * this world.
             *
             * This rejects stale or foreign settlement
             * results even when their maximum Canon degrees
             * happen to match.
             */
            if (!KvltCanon.HasSameHistoryAs(
                    settlement.SceneStartCanon))
            {
                return false;
            }

            if (settlement.NextPressure == null ||
                settlement.NextNormativeCentre == null)
            {
                return false;
            }

            if (settlement.NextPressure.SceneId !=
                settlement.SceneId ||
                settlement.NextPressure.SettledTurn !=
                settlement.SettledTurn)
            {
                return false;
            }

            if (settlement.NextNormativeCentre.SceneId !=
                settlement.SceneId ||
                settlement.NextNormativeCentre.SettledTurn !=
                settlement.SettledTurn)
            {
                return false;
            }

            /*
             * Commit the next settled scene interpretation.
             *
             * Canon object identity is preserved while its
             * immutable precedent history is replaced by the
             * validated next-state snapshot.
             */
            KvltCanon.ReplaceWith(
                settlement.NextCanon
            );

            ApplySettledScenePressure(
                settlement.NextPressure.Pressure
            );

            ApplySettledNormativeCentre(
                settlement
                    .NextNormativeCentre
                    .Centre
            );

            LastAppliedKvltSettlementTurn =
                settlement.SettledTurn;

            Debug.Log(
                "[KVLT SETTLEMENT PUBLISHED] " +
                $"scene={settlement.SceneId} | " +
                $"turn={settlement.SettledTurn} | " +
                $"canonRecords={KvltCanon.Records.Count} | " +
                $"pressureEntries=" +
                $"{KvltScenePressure.EntryCount} | " +
                $"normativeAffinities=" +
                $"{KvltNormativeCentre.NonZeroAffinityCount} | " +
                $"scoreEvents={KvltScoreLedger.Count}"
            );

            return true;
        }

        public bool TrySetEntityPhysicalLocation(
            GameEntity entity,
            Vector2Int coordinate)
        {
            if (entity == null)
            {
                Debug.LogWarning(
                    "[SeededWorldState] Cannot place a null entity."
                );

                return false;
            }

            GameEntity registeredEntity =
                FindEntity(entity.EntityId);

            if (registeredEntity != entity)
            {
                Debug.LogWarning(
                    "[SeededWorldState] Cannot place an entity " +
                    "that is not registered in this world | " +
                    $"entity={entity.EntityId}"
                );

                return false;
            }

            if (!PhysicalMapGrid.TryGetNode(
                    coordinate,
                    out PhysicalMapNode node
                ))
            {
                Debug.LogWarning(
                    "[SeededWorldState] Cannot place entity " +
                    "outside the physical map | " +
                    $"entity={entity.EntityId} | " +
                    $"coordinate={coordinate}"
                );

                return false;
            }

            return entity.SetPhysicalNode(
                node.NodeId
            );
        }

        public bool TryGetEntityPhysicalCoordinate(
            GameEntity entity,
            out Vector2Int coordinate)
        {
            coordinate = default;

            if (entity == null ||
                !entity.HasPhysicalLocation)
            {
                return false;
            }

            if (!PhysicalMapGrid.TryGetNode(
                    entity.PhysicalNodeId,
                    out PhysicalMapNode node
                ))
            {
                return false;
            }

            coordinate =
                node.Coordinate;

            return true;
        }

        public bool AddHostingRecord(EntityHostingRecord hostingRecord)
        {
            if (hostingRecord == null)
            {
                Debug.LogWarning("[SeededWorldState] Cannot add null hosting record");
                return false;
            }

            if (FindHostingRecord(hostingRecord.HostedEntityId, hostingRecord.HostEntityId) != null)
            {
                Debug.LogWarning(
                    "[SeededWorldState] Hosting record already registered | " +
                    $"hosted={hostingRecord.HostedEntityId}, host={hostingRecord.HostEntityId}"
                );

                return false;
            }

            hostingRecords.Add(hostingRecord);

            Debug.Log(
                "[SeededWorldState] Registered hosting record | " +
                $"hosted={hostingRecord.HostedEntityId}, host={hostingRecord.HostEntityId}"
            );

            return true;
        }

        public GameEntity FindEntity(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return null;

            foreach (GameEntity entity in entities)
            {
                if (entity != null && entity.EntityId == entityId)
                    return entity;
            }

            return null;
        }

        public GameEntity FindFirstEntityByType(GameEntityType entityType)
        {
            foreach (GameEntity entity in entities)
            {
                if (entity != null && entity.EntityType == entityType)
                    return entity;
            }

            return null;
        }

        public GameEntity FindEntityByDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            foreach (GameEntity entity in entities)
            {
                if (entity != null && entity.DisplayName == displayName)
                    return entity;
            }

            return null;
        }

        public Collective FindCollective(string collectiveId)
        {
            return CollectiveRegistry?.FindCollective(collectiveId);
        }

        public Collective FindKvlt()
        {
            return FindCollective(StartingCollectiveBootstrapper.KvltId);
        }

        public Collective FindSociety()
        {
            return FindCollective(StartingCollectiveBootstrapper.SocietyId);
        }

        public GameEntity FindKvltEntity()
        {
            return FindEntity(
                StartingCollectiveBootstrapper.KvltEntityId
            );
        }

        public GameEntity FindTheHole()
        {
            return FindEntityByDisplayName("The Hole");
        }

        public GameEntity FindTheHolePremises()
        {
            return FindEntityByDisplayName("The Hole Premises");
        }

        public EntityHostingRecord FindTheHoleHosting()
        {
            GameEntity theHole = FindTheHole();

            if (theHole == null)
                return null;

            return FindActiveHostFor(theHole.EntityId);
        }

        public List<Collective> FindPlayerBands()
        {
            List<Collective> playerBands = new();

            if (CollectiveRegistry == null)
                return playerBands;

            foreach (Collective collective in CollectiveRegistry.Collectives)
            {
                if (collective != null && collective.CollectiveType == CollectiveType.Band)
                    playerBands.Add(collective);
            }

            return playerBands;
        }

        public EntityHostingRecord FindHostingRecord(string hostedEntityId, string hostEntityId)
        {
            if (string.IsNullOrWhiteSpace(hostedEntityId) || string.IsNullOrWhiteSpace(hostEntityId))
                return null;

            foreach (EntityHostingRecord record in hostingRecords)
            {
                if (record != null && record.Matches(hostedEntityId, hostEntityId))
                    return record;
            }

            return null;
        }

        public EntityHostingRecord FindActiveHostFor(string hostedEntityId)
        {
            if (string.IsNullOrWhiteSpace(hostedEntityId))
                return null;

            foreach (EntityHostingRecord record in hostingRecords)
            {
                if (record != null &&
                    record.HostedEntityId == hostedEntityId &&
                    record.IsActive)
                {
                    return record;
                }
            }

            return null;
        }

        public SceneRelease FindSceneRelease(
            string releaseId)
        {
            if (string.IsNullOrWhiteSpace(
                    releaseId
                ))
            {
                return null;
            }

            foreach (
                SceneRelease release
                in sceneReleases)
            {
                if (release != null &&
                    release.ReleaseId == releaseId)
                {
                    return release;
                }
            }

            return null;
        }

        /// <summary>
        /// Selects the current vertical-slice proxy for an owner's
        /// primary cluster.
        ///
        /// The full design defines the primary cluster as the cluster
        /// contributed to most recently. Until clusters and contribution
        /// histories exist, the most recently released eligible
        /// SceneRelease represents that cluster.
        ///
        /// When multiple releases share the same ReleasedTurn, the one
        /// registered later in the authoritative sceneReleases list wins.
        /// </summary>
        public bool TrySelectPrimaryWorkProxyForOwner(
            string ownerEntityId,
            out SceneRelease selectedRelease)
        {
            selectedRelease = null;

            if (string.IsNullOrWhiteSpace(
                    ownerEntityId
                ))
            {
                return false;
            }

            int selectedReleasedTurn =
                int.MinValue;

            int selectedRegistrationIndex = -1;

            for (int i = 0;
                 i < sceneReleases.Count;
                 i++)
            {
                SceneRelease candidate =
                    sceneReleases[i];

                if (candidate == null ||
                    candidate.SourceOwnerEntityId !=
                    ownerEntityId ||
                    !candidate
                        .IsEligiblePrimaryWorkProxy)
                {
                    continue;
                }

                int candidateReleasedTurn =
                    candidate.ReleasedTurn;

                bool isMoreRecentTurn =
                    candidateReleasedTurn >
                    selectedReleasedTurn;

                bool isLaterRegistrationOnSameTurn =
                    candidateReleasedTurn ==
                    selectedReleasedTurn &&
                    i > selectedRegistrationIndex;

                if (selectedRelease == null ||
                    isMoreRecentTurn ||
                    isLaterRegistrationOnSameTurn)
                {
                    selectedRelease =
                        candidate;

                    selectedReleasedTurn =
                        candidateReleasedTurn;

                    selectedRegistrationIndex = i;
                }
            }

            return selectedRelease != null;
        }

        public bool TryBeginCanonizationSubject(
            string releaseId,
            int startedRound)
        {
            SceneRelease release =
                FindSceneRelease(releaseId);

            if (release == null)
                return false;

            if (!release.TryBeginCanonizationSubject(
                    startedRound
                ))
            {
                return false;
            }

            Debug.Log(
                "[KEEPER LEGACY] " +
                $"release={release.DisplayName} | " +
                $"releaseId={release.ReleaseId} | " +
                "state=CanonizationSubject | " +
                $"sinceRound=" +
                $"{release.CanonizationSubjectSinceRound}"
            );

            return true;
        }

        public bool TryAdvanceCanonizationSubjectYear(
            string releaseId)
        {
            SceneRelease release =
                FindSceneRelease(releaseId);

            if (release == null)
                return false;

            if (!release
                    .TryAdvanceCanonizationSubjectYear())
            {
                return false;
            }

            Debug.Log(
                "[KEEPER LEGACY] " +
                $"release={release.DisplayName} | " +
                $"releaseId={release.ReleaseId} | " +
                "state=CanonizationSubject | " +
                $"subjectYears=" +
                $"{release.CanonizationSubjectYears}"
            );

            return true;
        }

        public bool TryCanonizeSceneRelease(
            string releaseId,
            int canonizedRound,
            ulong incomingKeeperClientId)
        {
            SceneRelease release =
                FindSceneRelease(releaseId);

            if (release == null)
                return false;

            if (!release.TryCanonize(
                    canonizedRound,
                    incomingKeeperClientId
                ))
            {
                return false;
            }

            Debug.Log(
                "[KEEPER LEGACY] " +
                $"release={release.DisplayName} | " +
                $"releaseId={release.ReleaseId} | " +
                "state=Canonized | " +
                $"round={release.CanonizedRound} | " +
                $"incomingKeeper=" +
                $"{incomingKeeperClientId}"
            );

            return true;
        }

        public void DebugPrintLookupSummary()
        {
            Collective kvlt = FindKvlt();
            Collective society = FindSociety();
            GameEntity theHole = FindTheHole();
            GameEntity theHolePremises = FindTheHolePremises();
            EntityHostingRecord theHoleHosting = FindTheHoleHosting();
            List<Collective> playerBands = FindPlayerBands();

            Debug.Log(
                "[SeededWorldState] Lookup summary | " +
                $"kvltFound={kvlt != null}, " +
                $"societyFound={society != null}, " +
                $"theHoleFound={theHole != null}, " +
                $"premisesFound={theHolePremises != null}, " +
                $"theHoleHostingFound={theHoleHosting != null}, " +
                $"playerBands={playerBands.Count}"
            );
        }

        public void DebugPrintSummary()
        {
            Debug.Log(
                "[SeededWorldState] Summary | " +
                $"entities={entities.Count}, " +
                $"hostingRecords={hostingRecords.Count}, " +
                $"collectives={CollectiveRegistry?.Collectives.Count ?? 0}"
            );

            foreach (GameEntity entity in entities)
            {
                if (entity == null)
                    continue;

                Debug.Log(
                    "[SeededWorldState] Entity | " +
                    $"{entity.DisplayName}, " +
                    $"id={entity.EntityId}, " +
                    $"type={entity.EntityType}, " +
                    $"sceneReleases={sceneReleases.Count}, " +
                    $"collectiveMemberships={entity.CollectiveMemberships.Count}"
                );
            }
        }

        public void AddSceneRelease(SceneRelease release)
        {
            if (release == null)
            {
                Debug.LogWarning("[SeededWorldState] Tried to register null scene release.");
                return;
            }

            if (FindSceneRelease(
                    release.ReleaseId
                ) != null)
            {
                Debug.LogWarning(
                    "[SeededWorldState] " +
                    "Scene release already registered | " +
                    $"releaseId={release.ReleaseId}"
                );

                return;
            }

            sceneReleases.Add(release);

            Debug.Log(
                $"[SeededWorldState] Registered scene release: {release.DisplayName} " +
                $"releaseId={release.ReleaseId}, " +
                $"sourceDemo={release.SourceDemoTapeId}, " +
                $"hostedNode={release.HostedSceneNodeId}"
            );
        }

        public void DebugPrintSceneReleases()
        {
            Debug.Log($"[SeededWorldState] Scene releases={sceneReleases.Count}");

            for (int i = 0; i < sceneReleases.Count; i++)
            {
                SceneRelease release = sceneReleases[i];

                if (release == null)
                {
                    Debug.Log($"[SeededWorldState] SceneRelease[{i}] null");
                    continue;
                }

                var circulation = release.CirculationState;

                Debug.Log(
                    $"[SeededWorldState] SceneRelease[{i}] " +
                    $"name={release.DisplayName}, " +
                    $"releaseId={release.ReleaseId}, " +
                    $"sourceDemo={release.SourceDemoTapeId}, " +
                    $"owner={release.SourceOwnerEntityId}, " +
                    $"node={release.HostedSceneNodeId}, " +
                    $"legacy={release.LegacyState}, " +
                    $"subjectSince=" +
                    $"{release.CanonizationSubjectSinceRound}, " +
                    $"subjectYears=" +
                    $"{release.CanonizationSubjectYears}, " +
                    $"canonizedRound=" +
                    $"{release.CanonizedRound}, " +
                    $"gen={circulation?.Generation}, " +
                    $"reach={circulation?.Reach}, " +
                    $"conveyance={circulation?.Conveyance}, " +
                    $"noise={circulation?.Noise}, " +
                    $"context={circulation?.Context}"
                );
            }
        }

        public void TickSceneReleaseCirculation(int currentTurn)
        {
            if (sceneReleases.Count == 0)
                return;

            foreach (SceneRelease release in sceneReleases)
            {
                if (release?.CirculationState == null)
                    continue;

                var emittedEvents = release.CirculationState.Tick();

                Debug.Log(
                    $"[CIRCULATION TICK] turn={currentTurn} " +
                    $"release={release.DisplayName}, " +
                    $"sourceDemo={release.SourceDemoTapeId}, " +
                    $"age={release.CirculationState.CirculationAgeTurns}, " +
                    $"gen={release.CirculationState.Generation}, " +
                    $"reach={release.CirculationState.Reach:F2}, " +
                    $"conveyance={release.CirculationState.Conveyance:F2}, " +
                    $"noise={release.CirculationState.Noise:F2}, " +
                    $"context={release.CirculationState.Context:F2}"
                );

                foreach (var eventType in emittedEvents)
                {
                    Debug.Log(
                        $"[CIRCULATION EVENT] turn={currentTurn} " +
                        $"release={release.DisplayName}, " +
                        $"event={eventType}"
                    );

                    HandleCirculationEvent(release, eventType, currentTurn);
                }
            }
        }

        private void HandleCirculationEvent(
            SceneRelease release,
            ReleaseCirculationEventType eventType,
            int currentTurn)
        {
            if (release == null)
                return;

            Debug.Log(
                $"[SCENE SPACE PROXY EVENT] turn={currentTurn} " +
                $"release={release.DisplayName}, " +
                $"sourceDemo={release.SourceDemoTapeId}, " +
                $"hostedNode={release.HostedSceneNodeId}, " +
                $"event={eventType}"
            );
        }

        private static float CalculateReleaseInfluenceScore(SceneRelease release)
        {
            var circulation = release?.CirculationState;

            if (circulation == null)
                return 0f;

            return
                (release.EffectiveVisibility * 0.40f) +
                (circulation.Conveyance * 0.25f) +
                (circulation.Context * 0.20f) +
                (circulation.Noise * 0.15f);
        }

        private static float CalculateOwnerOutputScore(List<(SceneRelease release, float score)> sortedReleaseScores)
        {
            if (sortedReleaseScores == null || sortedReleaseScores.Count == 0)
                return 0f;

            float total = 0f;

            for (int i = 0; i < sortedReleaseScores.Count; i++)
            {
                float multiplier = i switch
                {
                    0 => 1.00f,
                    1 => 0.50f,
                    2 => 0.25f,
                    _ => 0.10f
                };

                total += sortedReleaseScores[i].score * multiplier;
            }

            return total;
        }

        public void EvaluateSceneOutputStandings(int currentTurn)
        {
            latestSceneOutputStandings.Clear();

            if (sceneReleases.Count == 0)
            {
                dominantOutputOwnerEntityId = null;
                dominantOutputScore = 0f;

                Debug.Log($"[SCENE OUTPUT] turn={currentTurn} no scene releases to evaluate.");
                return;
            }

            var releaseScoresByOwner = new Dictionary<string, List<(SceneRelease release, float score)>>();

            foreach (SceneRelease release in sceneReleases)
            {
                if (release?.CirculationState == null)
                    continue;

                string ownerId = release.SourceOwnerEntityId;

                if (string.IsNullOrWhiteSpace(ownerId))
                    continue;

                float releaseScore = CalculateReleaseInfluenceScore(release);

                if (!releaseScoresByOwner.TryGetValue(ownerId, out var scores))
                {
                    scores = new List<(SceneRelease release, float score)>();
                    releaseScoresByOwner.Add(ownerId, scores);
                }

                scores.Add((release, releaseScore));

                Debug.Log(
                    $"[SCENE RELEASE STANDING] turn={currentTurn} " +
                    $"release={release.DisplayName}, " +
                    $"owner={ownerId}, " +
                    $"score={releaseScore:F2}, " +
                    $"gen={release.CirculationState.Generation}, " +
                    $"organicReach=" +
                    $"{release.CirculationState.Reach:F2}, " +
                    $"effectiveVisibility=" +
                    $"{release.EffectiveVisibility:F2}, " +
                    $"pendingVisibilityAdjustment=" +
                    $"{release.PendingVisibilityAdjustment:F2}, " +
                    $"conveyance={release.CirculationState.Conveyance:F2}, " +
                    $"noise={release.CirculationState.Noise:F2}, " +
                    $"context={release.CirculationState.Context:F2}"
                );
            }

            dominantOutputOwnerEntityId = null;
            dominantOutputScore = 0f;

            foreach (var pair in releaseScoresByOwner)
            {
                string ownerId = pair.Key;
                var scores = pair.Value;

                scores.Sort((a, b) => b.score.CompareTo(a.score));

                float ownerScore = CalculateOwnerOutputScore(scores);
                SceneRelease strongestRelease = scores.Count > 0 ? scores[0].release : null;

                GameEntity ownerEntity = FindEntity(ownerId);
                string ownerDisplayName = ownerEntity != null
                    ? ownerEntity.DisplayName
                    : ownerId;

                latestSceneOutputStandings.Add(
                    new SceneOutputStanding(
                        ownerId,
                        ownerDisplayName,
                        scores.Count,
                        ownerScore,
                        strongestRelease?.DisplayName
                    )
                );

                Debug.Log(
                    $"[SCENE OUTPUT STANDING] turn={currentTurn} " +
                    $"owner={ownerDisplayName}, " +
                    $"ownerId={ownerId}, " +
                    $"releases={scores.Count}, " +
                    $"score={ownerScore:F2}, " +
                    $"strongest={strongestRelease?.DisplayName}"
                );

                if (ownerScore > dominantOutputScore)
                {
                    dominantOutputScore = ownerScore;
                    dominantOutputOwnerEntityId = ownerId;
                }
            }

            latestSceneOutputStandings.Sort((a, b) => b.Score.CompareTo(a.Score));
            Debug.Log(
                $"[SCENE OUTPUT DOMINANT] turn={currentTurn} " +
                $"owner={dominantOutputOwnerEntityId}, " +
                $"score={dominantOutputScore:F2}"
            );

            ConsumePendingVisibilityAdjustments();
        }

        private void
            ConsumePendingVisibilityAdjustments()
        {
            foreach (SceneRelease release
                     in sceneReleases)
            {
                if (release == null)
                    continue;

                float consumedAdjustment =
                    release
                        .PendingVisibilityAdjustment;

                if (!release
                        .ConsumePendingVisibilityAdjustment())
                {
                    continue;
                }

                Debug.Log(
                    "[KEEPER VISIBILITY CONSUMED] " +
                    $"release={release.DisplayName}, " +
                    $"adjustment={consumedAdjustment:F2}, " +
                    $"organicReach=" +
                    $"{release.CirculationState?.Reach:F2}"
                );
            }
        }

        public readonly struct SceneOutputStanding
        {
            public readonly string OwnerEntityId;
            public readonly string OwnerDisplayName;
            public readonly int ReleaseCount;
            public readonly float Score;
            public readonly string StrongestReleaseName;

            public SceneOutputStanding(
                string ownerEntityId,
                string ownerDisplayName,
                int releaseCount,
                float score,
                string strongestReleaseName)
            {
                OwnerEntityId = ownerEntityId;
                OwnerDisplayName = ownerDisplayName;
                ReleaseCount = releaseCount;
                Score = score;
                StrongestReleaseName = strongestReleaseName;
            }
        }

        public void ResolveTagLifecyclesAtTurnBoundary()
        {
            foreach (GameEntity entity in entities)
            {
                if (entity == null)
                    continue;

                entity.ResolveTagLifecycleAtTurnBoundary();
            }
        }
    }
}