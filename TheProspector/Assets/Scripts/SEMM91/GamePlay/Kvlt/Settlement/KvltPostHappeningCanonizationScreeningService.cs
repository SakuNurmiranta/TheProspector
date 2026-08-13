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
    /// Re-evaluates current Field semantics after
    /// Happening settlement for year-end Canonization.
    ///
    /// This service performs no movement and no
    /// Canon mutation.
    ///
    /// It intentionally re-runs legitimacy against
    /// current authoritative activation state rather
    /// than reusing the frozen movement-pass result.
    /// </summary>
    public sealed class
        KvltPostHappeningCanonizationScreeningService
    {
        private readonly
            SceneReleaseLegitimacyEvaluator
            legitimacyEvaluator =
                new();

        private readonly
            SceneReleaseCanonBreakthroughEvaluator
            breakthroughEvaluator =
                new();

        public
            KvltPostHappeningCanonizationScreeningResult
            Screen(
                string sceneId,
                int settledTurn,
                CanonState currentCanon,
                TrackEvaluationEnvironment
                    currentEnvironment,
                IReadOnlyList<SceneRelease> releases,
                IReadOnlyList<DemoTape> demoTapes)
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
                    "Post-Happening environment belongs " +
                    "to another settlement turn.",
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

            ValidateReleasePopulation(
                releases
            );

            Dictionary<string, DemoTape>
                tapesById =
                    BuildTapeLookup(
                        demoTapes
                    );

            CanonState sceneStartCanon =
                currentCanon.CreateCopy();

            List<SceneRelease>
                fieldReleases =
                    GetOrderedFieldReleases(
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

            Dictionary<
                    string,
                    SceneReleaseCanonBreakthroughEvaluation>
                breakthroughs =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                SceneRelease release
                in fieldReleases)
            {
                DemoTape tape =
                    RequireTapeForRelease(
                        release,
                        tapesById
                    );

                /*
                 * Fresh semantic projection.
                 *
                 * This sees activation/Pending
                 * redemption applied during the
                 * just-settled Happening phase.
                 */
                SceneReleaseLegitimacyEvaluation
                    legitimacy =
                        legitimacyEvaluator.Evaluate(
                            release,
                            tape,
                            currentEnvironment
                        );

                SceneReleaseCanonBreakthroughEvaluation
                    breakthrough =
                        breakthroughEvaluator.Evaluate(
                            release,
                            tape,
                            legitimacy,
                            sceneStartCanon,
                            SceneReleaseCanonBreakthroughEvaluationPhase
                                .PostHappening
                        );

                legitimacyEvaluations.Add(
                    legitimacy
                );

                legitimacyByRelease.Add(
                    release.ReleaseId,
                    legitimacy
                );

                breakthroughs.Add(
                    release.ReleaseId,
                    breakthrough
                );
            }

            return new
                KvltPostHappeningCanonizationScreeningResult(
                    sceneId,
                    settledTurn,
                    sceneStartCanon,
                    legitimacyEvaluations,
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
                        "Post-Happening release population " +
                        "cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!ids.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Post-Happening release population " +
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
                        "Post-Happening DemoTape " +
                        "population cannot contain null.",
                        nameof(demoTapes)
                    );
                }

                if (!result.TryAdd(
                        tape.DemoTapeId,
                        tape))
                {
                    throw new ArgumentException(
                        "Post-Happening DemoTape " +
                        "population contains duplicate " +
                        "identity.",
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
                    "Post-Happening Field release has " +
                    "no source DemoTape | " +
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
                    "Post-Happening screening identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}