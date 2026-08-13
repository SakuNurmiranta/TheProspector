using System;
using System.Collections.Generic;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Movement;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.Kvlt.Standing;
using SEMM91.GamePlay.Score;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Authoritative Camp-3 composition of one KVLT
    /// Scene settlement.
    ///
    /// The service owns ordering only. All semantic
    /// and mathematical policy remains delegated to
    /// the established domain services.
    /// </summary>
    public sealed class KvltSceneSettlementService
    {
        private readonly
            KvltExistingFieldMovementSettlementService
            existingFieldMovementSettlement =
                new();

        private readonly
            KvltCanonizationSettlementService
            canonizationSettlement =
                new();

        private readonly
            SceneReleaseOuterBoundaryEvaluator
            outerBoundaryEvaluator =
                new();

        private readonly
            SceneReleaseOuterBoundarySettlementService
            outerBoundarySettlement =
                new();

        private readonly
            SceneReleaseScoreAwardService
            ordinaryScoreService =
                new();

        private readonly
            CanonRetainedScoreAwardService
            retainedScoreService =
                new();

        private readonly
            HistoricalCanonInstitutionalGravityScoreAwardService
            historicalScoreService =
                new();

        private readonly
            SceneStandingCorpusProjector
            standingProjector =
                new();

        private readonly
            ScenePressureRebuilder
            pressureRebuilder =
                new();

        private readonly
            CanonicalNormativeCentreDeriver
            normativeDeriver =
                new();

        public KvltSceneSettlementResult Settle(
            string sceneId,
            int settledTurn,
            string currentKeeperTenureId,
            CanonState currentCanon,
            TrackEvaluationEnvironment
                currentEnvironment,
            IReadOnlyList<SceneRelease> releases,
            IReadOnlyList<DemoTape> demoTapes,
            ScoreLedger scoreLedger,
            KvltSceneSettlementPolicy policy)
        {
            sceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            currentKeeperTenureId =
                RequireText(
                    currentKeeperTenureId,
                    nameof(currentKeeperTenureId)
                );

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            if (currentCanon == null)
            {
                throw new ArgumentNullException(
                    nameof(currentCanon)
                );
            }

            if (currentEnvironment == null)
            {
                throw new ArgumentNullException(
                    nameof(currentEnvironment)
                );
            }

            if (currentEnvironment.SettledTurn !=
                settledTurn)
            {
                throw new ArgumentException(
                    "Evaluation environment belongs to " +
                    "another settlement turn.",
                    nameof(currentEnvironment)
                );
            }

            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            if (demoTapes == null)
            {
                throw new ArgumentNullException(
                    nameof(demoTapes)
                );
            }

            if (scoreLedger == null)
            {
                throw new ArgumentNullException(
                    nameof(scoreLedger)
                );
            }

            if (policy == null)
            {
                throw new ArgumentNullException(
                    nameof(policy)
                );
            }

            Dictionary<string, SceneRelease>
                releasesById =
                    BuildReleaseLookup(
                        releases
                    );

            Dictionary<string, DemoTape>
                tapesById =
                    BuildTapeLookup(
                        demoTapes
                    );

            /*
             * PASS 1 — frozen existing-Field movement.
             *
             * This is now an independently callable phase.
             *
             * The legacy Camp-3 composer still consumes its
             * movement-time legitimacy/breakthrough evaluations
             * below so existing behavior remains unchanged.
             */
            KvltExistingFieldMovementSettlementResult
                movementPhase =
                    existingFieldMovementSettlement
                        .Settle(
                            sceneId,
                            settledTurn,
                            currentCanon,
                            currentEnvironment,
                            releases,
                            demoTapes,
                            policy
                        );

            CanonState sceneStartCanon =
                movementPhase.SceneStartCanon;

            SceneFieldSurfaceSnapshot
                surfaceSnapshot =
                    movementPhase.FieldSurfaceSnapshot;

            List<
                    SceneReleaseLegitimacyEvaluation>
                legitimacyEvaluations =
                    new(
                        movementPhase
                            .MovementLegitimacyEvaluations
                    );

            Dictionary<
                    string,
                    SceneReleaseLegitimacyEvaluation>
                legitimacyByRelease =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                KeyValuePair<
                        string,
                        SceneReleaseLegitimacyEvaluation>
                    pair
                in movementPhase
                    .MovementLegitimacyByRelease)
            {
                legitimacyByRelease.Add(
                    pair.Key,
                    pair.Value
                );
            }

            Dictionary<
                    string,
                    SceneReleaseCanonBreakthroughEvaluation>
                breakthroughs =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                KeyValuePair<
                        string,
                        SceneReleaseCanonBreakthroughEvaluation>
                    pair
                in movementPhase
                    .MovementBreakthroughsByRelease)
            {
                breakthroughs.Add(
                    pair.Key,
                    pair.Value
                );
            }

            IReadOnlyList<
                    SceneReleaseMovementApplication>
                movementApplications =
                    movementPhase.MovementApplications;

/*
 * The extraction did not change lifecycle state.
 * Reconstruct the same deterministic starting Field
 * population used by the old downstream Camp-3 pass.
 */

            int scoreStartIndex =
                scoreLedger.Count;

            /*
             * PASS 2/3 — Canonization.
             *
             * The legacy Camp-3 facade deliberately supplies
             * movement-time breakthrough screening here so its
             * observable behavior remains unchanged.
             *
             * The final Peak-2 chronology will instead invoke
             * this same service only at year end, with fresh
             * post-Happening screening.
             */
            KvltCanonizationSettlementResult
                canonizationPhase =
                    canonizationSettlement.Settle(
                        sceneId,
                        settledTurn,
                        currentKeeperTenureId,
                        sceneStartCanon,
                        currentEnvironment,
                        releases,
                        demoTapes,
                        breakthroughs,
                        policy
                    );

            CanonState nextCanon =
                canonizationPhase.NextCanon;

            CanonSimultaneousMergeEvaluation
                canonMerge =
                    canonizationPhase.CanonMerge;

            IReadOnlyList<
                    SceneReleaseNexusBoundaryEvaluation>
                nexusEvaluations =
                    canonizationPhase.NexusEvaluations;

            IReadOnlyList<
                    SceneReleaseCanonFreezeApplication>
                freezeApplications =
                    canonizationPhase.FreezeApplications;
            
                
            /*
             * PASS 4 — Outer Boundary.
             *
             * Canonized releases are already
             * CanonRetained and therefore excluded.
             */
            
            List<SceneReleaseOuterBoundaryEvaluation>
                boundaryEvaluations =
                    new();

            foreach (
                SceneRelease release
                in GetOrderedSceneReleases(
                    releases,
                    sceneId,
                    SceneReleaseLifecycleState.Field
                ))
            {
                boundaryEvaluations.Add(
                    outerBoundaryEvaluator.Evaluate(
                        release,
                        policy.OuterBoundary,
                        settledTurn
                    )
                );
            }

            IReadOnlyList<
                    SceneReleaseOuterBoundaryApplication>
                rejectionApplications =
                    outerBoundarySettlement.Apply(
                        releases,
                        boundaryEvaluations
                    );

            /*
             * PASS 5 — Score.
             *
             * Historical Institutional Gravity is
             * evaluated against Scene_t Canon, never
             * NewCanon. This preserves prospective
             * supersession.
             */
            foreach (
                SceneRelease release
                in GetOrderedSceneReleases(
                    releases,
                    sceneId,
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                ))
            {
                historicalScoreService
                    .AwardEligibleClaims(
                        release,
                        sceneStartCanon,
                        scoreLedger,
                        settledTurn
                    );
            }

            /*
             * Both pre-existing and newly canonized
             * retained releases receive the full
             * frozen tenure payout.
             */
            foreach (
                SceneRelease release
                in GetOrderedSceneReleases(
                    releases,
                    sceneId,
                    SceneReleaseLifecycleState
                        .CanonRetained
                ))
            {
                retainedScoreService.TryAward(
                    release,
                    currentKeeperTenureId,
                    scoreLedger,
                    settledTurn,
                    out _
                );
            }

            /*
             * Only releases still in ordinary Field
             * after Canonization and rejection receive
             * recurring Field Gravity.
             */
            foreach (
                SceneRelease release
                in GetOrderedSceneReleases(
                    releases,
                    sceneId,
                    SceneReleaseLifecycleState.Field
                ))
            {
                if (!legitimacyByRelease.TryGetValue(
                        release.ReleaseId,
                        out
                        SceneReleaseLegitimacyEvaluation
                            evaluation))
                {
                    throw new InvalidOperationException(
                        "Final Field release has no " +
                        "Scene_t legitimacy evaluation | " +
                        $"release={release.ReleaseId}"
                    );
                }

                ordinaryScoreService
                    .TryAwardFieldGravity(
                        release,
                        evaluation,
                        scoreLedger,
                        settledTurn,
                        out _
                    );
            }

            /*
             * PASS 6 — settled remembered Standing.
             */
            List<SceneStandingEvaluation>
                standingEvaluations =
                    BuildStanding(
                        sceneId,
                        settledTurn,
                        releases,
                        policy
                            .StandingProjectionPolicy
                    );

            /*
             * PASS 7 — next Dynamic Pressure.
             *
             * Rejected and canonized releases have
             * already left ordinary Field, so the
             * rebuilder automatically excludes them.
             */
            ScenePressureRebuildEvaluation
                nextPressure =
                    pressureRebuilder.Rebuild(
                        sceneId,
                        settledTurn,
                        releases,
                        demoTapes,
                        nextCanon,
                        policy
                            .PressureRebuildPolicy
                    );

            /*
             * PASS 8 — next Normative Centre.
             *
             * This output is not fed back into any
             * earlier evaluation in the same call.
             */
            NormativeCentre
                nextCanonNormativeCentre =
                    normativeDeriver.Derive(
                        nextCanon
                    );

            NormativeCentreDerivationEvaluation
                nextNormativeCentre =
                    normativeDeriver
                        .DeriveWithPressure(
                            nextCanon,
                            nextPressure,
                            policy
                                .NormativePressureBlendPolicy
                        );

            List<ScoreEvent>
                newScoreEvents =
                    new();

            for (int index = scoreStartIndex;
                 index < scoreLedger.Count;
                 index++)
            {
                newScoreEvents.Add(
                    scoreLedger.History[index]
                );
            }

            return new KvltSceneSettlementResult(
                sceneId,
                settledTurn,
                sceneStartCanon,
                nextCanon,
                canonMerge,
                surfaceSnapshot,
                legitimacyEvaluations,
                movementApplications,
                nexusEvaluations,
                freezeApplications,
                rejectionApplications,
                standingEvaluations,
                nextPressure,
                nextNormativeCentre,
                nextCanonNormativeCentre,
                newScoreEvents
            );
        }

        private List<SceneStandingEvaluation>
            BuildStanding(
                string sceneId,
                int settledTurn,
                IReadOnlyList<SceneRelease> releases,
                SceneStandingProjectionPolicy policy)
        {
            HashSet<string> ownerSet =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                SceneRelease release
                in releases)
            {
                if (release == null ||
                    release.HostedSceneNodeId !=
                    sceneId)
                {
                    continue;
                }

                ownerSet.Add(
                    release.SourceOwnerEntityId
                );
            }

            string[] owners =
                new string[ownerSet.Count];

            ownerSet.CopyTo(
                owners
            );

            Array.Sort(
                owners,
                StringComparer.Ordinal
            );

            List<SceneStandingEvaluation>
                results =
                    new();

            foreach (
                string owner
                in owners)
            {
                results.Add(
                    standingProjector.Project(
                        owner,
                        sceneId,
                        settledTurn,
                        releases,
                        policy
                    )
                );
            }

            return results;
        }

        private static Dictionary<string, SceneRelease>
            BuildReleaseLookup(
                IReadOnlyList<SceneRelease> releases)
        {
            Dictionary<string, SceneRelease> result =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                SceneRelease release
                in releases)
            {
                if (release == null)
                {
                    throw new ArgumentException(
                        "Settlement release population " +
                        "cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!result.TryAdd(
                        release.ReleaseId,
                        release))
                {
                    throw new ArgumentException(
                        "Settlement release population " +
                        "contains duplicate identity.",
                        nameof(releases)
                    );
                }
            }

            return result;
        }

        private static Dictionary<string, DemoTape>
            BuildTapeLookup(
                IReadOnlyList<DemoTape> demoTapes)
        {
            Dictionary<string, DemoTape> result =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                DemoTape tape
                in demoTapes)
            {
                if (tape == null)
                {
                    throw new ArgumentException(
                        "Settlement DemoTape population " +
                        "cannot contain null.",
                        nameof(demoTapes)
                    );
                }

                if (!result.TryAdd(
                        tape.DemoTapeId,
                        tape))
                {
                    throw new ArgumentException(
                        "Settlement DemoTape population " +
                        "contains duplicate identity.",
                        nameof(demoTapes)
                    );
                }
            }

            return result;
        }

        private static DemoTape RequireTapeForRelease(
            SceneRelease release,
            IReadOnlyDictionary<string, DemoTape>
                tapesById)
        {
            if (!tapesById.TryGetValue(
                    release.SourceDemoTapeId,
                    out DemoTape tape))
            {
                throw new InvalidOperationException(
                    "Settlement SceneRelease has no " +
                    "source DemoTape | " +
                    $"release={release.ReleaseId} | " +
                    $"demo={release.SourceDemoTapeId}"
                );
            }

            return tape;
        }

        private static List<SceneRelease>
            GetOrderedSceneReleases(
                IReadOnlyList<SceneRelease> releases,
                string sceneId,
                SceneReleaseLifecycleState state)
        {
            List<SceneRelease> result =
                new();

            foreach (
                SceneRelease release
                in releases)
            {
                if (release.HostedSceneNodeId ==
                    sceneId &&
                    release.LifecycleState ==
                    state)
                {
                    result.Add(
                        release
                    );
                }
            }

            result.Sort(
                (
                        left,
                        right
                    ) =>
                    string.CompareOrdinal(
                        left.ReleaseId,
                        right.ReleaseId
                    )
            );

            return result;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Settlement identity cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}