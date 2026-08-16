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
    /// Resolves only the frozen existing-Field
    /// movement pass.
    ///
    /// It does not:
    /// - canonize;
    /// - reject at the outer boundary;
    /// - award Score;
    /// - rebuild Standing;
    /// - rebuild Pressure;
    /// - derive the next Normative Centre.
    ///
    /// All releases observe the same Scene_t Canon
    /// and environment for this movement pass.
    /// </summary>
    public sealed class
        KvltExistingFieldMovementSettlementService
    {
        private readonly
            SceneReleaseLegitimacyEvaluator
            legitimacyEvaluator =
                new();

        private readonly
            SceneFieldSurfaceSnapshotEvaluator
            surfaceEvaluator =
                new();

        private readonly
            SceneReleaseNaturalDriftEvaluator
            naturalDriftEvaluator =
                new();

        private readonly
            SceneReleaseCanonBreakthroughEvaluator
            breakthroughEvaluator =
                new();

        private readonly
            SceneReleaseCanonBreakthroughMovementEvaluator
            breakthroughMovementEvaluator =
                new();

        private readonly
            SceneReleaseMovementEvaluator
            movementEvaluator =
                new();

        private readonly
            SceneReleaseMovementSettlementService
            movementSettlement =
                new();

        public
            KvltExistingFieldMovementSettlementResult
            Settle(
                string sceneId,
                int settledTurn,
                CanonState currentCanon,
                TrackEvaluationEnvironment
                    currentEnvironment,
                IReadOnlyList<SceneRelease> releases,
                IReadOnlyList<DemoTape> demoTapes,
                KvltSceneSettlementPolicy policy)
        {
            sceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
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

            if (policy == null)
            {
                throw new ArgumentNullException(
                    nameof(policy)
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
             * The entire movement pass observes one
             * immutable copy of Canon_t.
             */
            CanonState sceneStartCanon =
                currentCanon.CreateCopy();

            List<SceneRelease>
                startFieldReleases =
                    GetOrderedSceneReleases(
                        releases,
                        sceneId
                    );

            List<
                SceneReleaseLegitimacyEvaluation>
                legitimacyEvaluations =
                    new();

            Dictionary<
                    string,
                    SceneReleaseLegitimacyEvaluation>
                legitimacyByRelease =
                    new(
                        StringComparer.Ordinal
                    );

            /*
             * MOVEMENT PASS A:
             * frozen legitimacy against Scene_t.
             */
            foreach (
                SceneRelease release
                in startFieldReleases)
            {
                DemoTape tape =
                    RequireTapeForRelease(
                        release,
                        tapesById
                    );

                SceneReleaseLegitimacyEvaluation
                    evaluation =
                        legitimacyEvaluator.Evaluate(
                            release,
                            tape,
                            currentEnvironment
                        );

                legitimacyEvaluations.Add(
                    evaluation
                );

                legitimacyByRelease.Add(
                    release.ReleaseId,
                    evaluation
                );
            }

            SceneFieldSurfaceSnapshot
                surfaceSnapshot =
                    surfaceEvaluator.Evaluate(
                        sceneId,
                        settledTurn,
                        releases,
                        legitimacyEvaluations
                    );

            IReadOnlyList<
                    SceneReleaseNaturalDriftEvaluation>
                naturalDrifts =
                    naturalDriftEvaluator.Evaluate(
                        surfaceSnapshot,
                        policy.FieldDriftScale
                    );

            /*
             * MOVEMENT PASS B:
             *
             * Breakthrough is evaluated here only to
             * contribute breakthrough drift.
             *
             * This is explicitly NOT the later
             * post-Happening canonization screening.
             */
            Dictionary<
                    string,
                    SceneReleaseCanonBreakthroughEvaluation>
                breakthroughs =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                SceneRelease release
                in startFieldReleases)
            {
                DemoTape tape =
                    RequireTapeForRelease(
                        release,
                        tapesById
                    );

                SceneReleaseCanonBreakthroughEvaluation
                    breakthrough =
                        breakthroughEvaluator.Evaluate(
                            release,
                            tape,
                            legitimacyByRelease[
                                release.ReleaseId
                            ],
                            sceneStartCanon
                        );

                breakthroughs.Add(
                    release.ReleaseId,
                    breakthrough
                );
            }

            List<
                SceneReleaseMovementEvaluation>
                movementEvaluations =
                    new();

            foreach (
                SceneReleaseNaturalDriftEvaluation
                    natural
                in naturalDrifts)
            {
                SceneReleaseCanonBreakthroughEvaluation
                    breakthrough =
                        breakthroughs[
                            natural.SceneReleaseId
                        ];

                SceneReleaseCanonBreakthroughMovementEvaluation
                    breakthroughMovement =
                        breakthroughMovementEvaluator
                            .Evaluate(
                                breakthrough,
                                policy
                                    .BreakthroughDriftMultiplier
                            );

                movementEvaluations.Add(
                    movementEvaluator.Evaluate(
                        natural,
                        breakthroughMovement
                    )
                );
            }

            IReadOnlyList<
                    SceneReleaseMovementApplication>
                movementApplications =
                    movementSettlement.Apply(
                        releases,
                        movementEvaluations
                    );

            return new
                KvltExistingFieldMovementSettlementResult(
                    sceneId,
                    settledTurn,
                    sceneStartCanon,
                    surfaceSnapshot,
                    legitimacyEvaluations,
                    movementApplications,
                    legitimacyByRelease,
                    breakthroughs
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
                        "Movement release population " +
                        "cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!ids.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Movement release population " +
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
                        "Movement DemoTape population " +
                        "cannot contain null.",
                        nameof(demoTapes)
                    );
                }

                if (!result.TryAdd(
                        tape.DemoTapeId,
                        tape))
                {
                    throw new ArgumentException(
                        "Movement DemoTape population " +
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
                IReadOnlyDictionary<string, DemoTape>
                    tapesById)
        {
            if (!tapesById.TryGetValue(
                    release.SourceDemoTapeId,
                    out DemoTape tape))
            {
                throw new InvalidOperationException(
                    "Movement SceneRelease has no " +
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
                    "Movement settlement identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}