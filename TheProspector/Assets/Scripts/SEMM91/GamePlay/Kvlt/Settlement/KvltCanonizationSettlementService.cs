using System;
using System.Collections.Generic;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Resolves Canonization from an already-evaluated
    /// breakthrough screening state.
    ///
    /// This service owns:
    ///
    /// - phase-aware Nexus candidacy;
    /// - simultaneous NewCanon merge;
    /// - NewCanon assimilation;
    /// - final semantic freeze;
    /// - CanonRetained transition;
    /// - claim Gravity decomposition.
    ///
    /// It does NOT decide whether the current turn is
    /// a year-end turn. The turn chronology owns that.
    ///
    /// It also does NOT calculate the supplied
    /// breakthrough screening. That allows the final
    /// Peak-2 chronology to supply fresh post-Happening
    /// evaluations without changing Canonization math.
    /// </summary>
    public sealed class
        KvltCanonizationSettlementService
    {
        private readonly
            SceneReleaseNexusBoundaryEvaluator
            nexusEvaluator =
                new();

        private readonly
            CanonSimultaneousMergeEvaluator
            canonMergeEvaluator =
                new();

        private readonly
            SceneReleaseCanonAssimilationEvaluator
            assimilationEvaluator =
                new();

        private readonly
            SceneReleaseCanonAssimilationService
            assimilationService =
                new();

        private readonly
            SceneReleaseCanonFreezeService
            freezeService =
                new();

        private readonly
            SceneReleaseCanonGravityDecompositionService
            decompositionService =
                new();

        public KvltCanonizationSettlementResult
            Settle(
                string sceneId,
                int settledTurn,
                string currentKeeperTenureId,
                CanonState sceneStartCanon,
                TrackEvaluationEnvironment
                    currentEnvironment,
                IReadOnlyList<SceneRelease> releases,
                IReadOnlyList<DemoTape> demoTapes,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseCanonBreakthroughEvaluation>
                    breakthroughsByRelease,
                KvltSceneSettlementPolicy policy)
        {
            return Settle(
                sceneId,
                settledTurn,
                currentKeeperTenureId,
                sceneStartCanon,
                currentEnvironment,
                releases,
                demoTapes,
                breakthroughsByRelease,
                policy,
                SceneReleaseCanonBreakthroughEvaluationPhase
                    .PreMovement
            );
        }

        public KvltCanonizationSettlementResult
            Settle(
                string sceneId,
                int settledTurn,
                string currentKeeperTenureId,
                CanonState sceneStartCanon,
                TrackEvaluationEnvironment
                    currentEnvironment,
                IReadOnlyList<SceneRelease> releases,
                IReadOnlyList<DemoTape> demoTapes,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseCanonBreakthroughEvaluation>
                    breakthroughsByRelease,
                KvltSceneSettlementPolicy policy,
                SceneReleaseCanonBreakthroughEvaluationPhase
                    breakthroughEvaluationPhase)
        {
            return Settle(
                sceneId,
                settledTurn,
                currentKeeperTenureId,
                sceneStartCanon,
                currentEnvironment,
                releases,
                demoTapes,
                breakthroughsByRelease,
                policy,
                breakthroughEvaluationPhase,
                null);
        }

        public KvltCanonizationSettlementResult
            Settle(
                string sceneId,
                int settledTurn,
                string currentKeeperTenureId,
                CanonState sceneStartCanon,
                TrackEvaluationEnvironment
                    currentEnvironment,
                IReadOnlyList<SceneRelease> releases,
                IReadOnlyList<DemoTape> demoTapes,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseCanonBreakthroughEvaluation>
                    breakthroughsByRelease,
                KvltSceneSettlementPolicy policy,
                SceneReleaseCanonBreakthroughEvaluationPhase
                    breakthroughEvaluationPhase,
                Func<SceneRelease, bool>
                    canCanonizeRelease)
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

            if (sceneStartCanon == null)
            {
                throw new ArgumentNullException(
                    nameof(sceneStartCanon)
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

            if (breakthroughsByRelease == null)
            {
                throw new ArgumentNullException(
                    nameof(breakthroughsByRelease)
                );
            }

            if (policy == null)
            {
                throw new ArgumentNullException(
                    nameof(policy)
                );
            }

            if (!Enum.IsDefined(
                    typeof(
                        SceneReleaseCanonBreakthroughEvaluationPhase
                    ),
                    breakthroughEvaluationPhase))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(breakthroughEvaluationPhase)
                );
            }

            ValidateReleasePopulation(
                releases
            );

            Dictionary<string, DemoTape>
                tapesById =
                    BuildTapeLookup(
                        demoTapes
                    );

            /*
             * Never mutate the caller's Canon.
             *
             * Every candidate in this phase must be
             * screened against the same Canon state.
             */
            CanonState canonAtPhaseStart =
                sceneStartCanon.CreateCopy();

            List<SceneRelease>
                fieldReleases =
                    GetOrderedFieldReleases(
                        releases,
                        sceneId
                    );

            List<
                SceneReleaseNexusBoundaryEvaluation>
                nexusEvaluations =
                    new();

            List<
                SceneReleaseCanonMergeCandidate>
                mergeCandidates =
                    new();

            /*
             * PASS A — Nexus candidacy.
             *
             * The supplied breakthrough evaluations may
             * later come from post-Happening screening.
             */
            foreach (
                SceneRelease release
                in fieldReleases)
            {
                if (canCanonizeRelease != null &&
                    !canCanonizeRelease(release))
                {
                    continue;
                }

                if (!breakthroughsByRelease.TryGetValue(
                        release.ReleaseId,
                        out
                            SceneReleaseCanonBreakthroughEvaluation
                            breakthrough))
                {
                    throw new InvalidOperationException(
                        "Canonization has no breakthrough " +
                        "screening for Field release | " +
                        $"release={release.ReleaseId}"
                    );
                }

                if (!breakthrough
                        .HasQualifyingBreakthrough)
                {
                    continue;
                }

                SceneReleaseNexusBoundaryEvaluation
                    nexus =
                        nexusEvaluator.Evaluate(
                            release,
                            breakthrough,
                            policy.NexusBoundary,
                            breakthroughEvaluationPhase
                        );

                nexusEvaluations.Add(
                    nexus
                );

                if (nexus.IsCanonCandidate)
                {
                    mergeCandidates.Add(
                        new
                            SceneReleaseCanonMergeCandidate(
                                release,
                                nexus
                            )
                    );
                }
            }

            CanonSimultaneousMergeEvaluation
                canonMerge =
                    null;

            CanonState nextCanon;

            List<
                SceneReleaseCanonFreezeApplication>
                freezeApplications =
                    new();

            if (mergeCandidates.Count == 0)
            {
                nextCanon =
                    canonAtPhaseStart.CreateCopy();

                return new
                    KvltCanonizationSettlementResult(
                        sceneId,
                        settledTurn,
                        canonAtPhaseStart,
                        nextCanon,
                        canonMerge,
                        nexusEvaluations,
                        freezeApplications
                    );
            }

            /*
             * PASS B — simultaneous frontier merge.
             *
             * Every successful candidate observes the
             * same pre-merge Canon.
             */
            canonMerge =
                canonMergeEvaluator.Evaluate(
                    canonAtPhaseStart,
                    mergeCandidates,
                    currentKeeperTenureId
                );

            nextCanon =
                canonMerge.NewCanon;

            mergeCandidates.Sort(
                (
                    left,
                    right
                ) =>
                    string.CompareOrdinal(
                        left.SceneReleaseId,
                        right.SceneReleaseId
                    )
            );

            /*
             * PASS C — assimilation and final freeze.
             *
             * NewCanon is already complete before any
             * individual candidate is assimilated.
             */
            foreach (
                SceneReleaseCanonMergeCandidate
                    candidate
                in mergeCandidates)
            {
                SceneRelease release =
                    candidate.Release;

                DemoTape tape =
                    RequireTapeForRelease(
                        release,
                        tapesById
                    );

                SceneReleaseCanonAssimilationEvaluation
                    assimilation =
                        assimilationEvaluator.Evaluate(
                            release,
                            tape,
                            candidate
                                .NexusEvaluation,
                            canonMerge
                        );

                SceneReleaseCanonAssimilationApplication
                    appliedAssimilation =
                        assimilationService.Apply(
                            release,
                            assimilation
                        );

                SceneReleaseCanonFreezeApplication
                    frozen =
                        freezeService.Apply(
                            release,
                            tape,
                            currentEnvironment,
                            appliedAssimilation
                        );

                decompositionService.Apply(
                    release,
                    frozen,
                    canonMerge
                );

                freezeApplications.Add(
                    frozen
                );
            }

            return new
                KvltCanonizationSettlementResult(
                    sceneId,
                    settledTurn,
                    canonAtPhaseStart,
                    nextCanon,
                    canonMerge,
                    nexusEvaluations,
                    freezeApplications
                );
        }

        private static void
            ValidateReleasePopulation(
                IReadOnlyList<SceneRelease> releases)
        {
            HashSet<string> ids =
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
                        "Canonization release population " +
                        "cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!ids.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Canonization release population " +
                        "contains duplicate identity.",
                        nameof(releases)
                    );
                }
            }
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
                        "Canonization DemoTape population " +
                        "cannot contain null.",
                        nameof(demoTapes)
                    );
                }

                if (!result.TryAdd(
                        tape.DemoTapeId,
                        tape))
                {
                    throw new ArgumentException(
                        "Canonization DemoTape population " +
                        "contains duplicate identity.",
                        nameof(demoTapes)
                    );
                }
            }

            return result;
        }

        private static DemoTape
            RequireTapeForRelease(
                SceneRelease release,
                IReadOnlyDictionary<
                    string,
                    DemoTape>
                    tapesById)
        {
            if (!tapesById.TryGetValue(
                    release.SourceDemoTapeId,
                    out DemoTape tape))
            {
                throw new InvalidOperationException(
                    "Canonization SceneRelease has no " +
                    "source DemoTape | " +
                    $"release={release.ReleaseId} | " +
                    $"demo={release.SourceDemoTapeId}"
                );
            }

            return tape;
        }

        private static List<SceneRelease>
            GetOrderedFieldReleases(
                IReadOnlyList<SceneRelease> releases,
                string sceneId)
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
                        SceneReleaseLifecycleState.Field)
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
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Canonization identity cannot be " +
                    "empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}
