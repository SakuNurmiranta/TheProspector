using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Composes authoritative initial Field placement
    /// for turn t+1.
    ///
    /// All Band Scene Position bases are evaluated
    /// against the completed turn-t resident corpus
    /// before any newly fettered release is mutated.
    /// </summary>
    public sealed class
        KvltNextTurnIngressSettlementService
    {
        private readonly
            BandScenePositionEvaluator
            bandPositionEvaluator =
                new();

        private readonly
            SceneReleaseIngressEvaluator
            evaluator =
                new();

        private readonly
            SceneReleaseIngressService
            ingress =
                new();

        public KvltNextTurnIngressSettlementResult
            Settle(
                string sceneId,
                int completedTurn,
                float freshReleasePosition,
                float innerFieldEntryCeiling,
                IReadOnlyList<SceneRelease> releases)
        {
            sceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (completedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedTurn)
                );
            }

            if (completedTurn == int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedTurn),
                    "No representable next turn exists."
                );
            }

            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            int placementTurn =
                completedTurn + 1;

            List<SceneRelease> eligible =
                GetOrderedEligibleReleases(
                    releases,
                    sceneId,
                    completedTurn
                );

            /*
             * Freeze every owner's Band Scene Position
             * BEFORE applying any new placement.
             *
             * Several newly fettered releases from the
             * same band therefore observe exactly the
             * same pre-entry resident corpus.
             */
            Dictionary<
                    string,
                    BandScenePositionEvaluation>
                bandPositions =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                SceneRelease release
                in eligible)
            {
                string owner =
                    release.SourceOwnerEntityId;

                if (bandPositions.ContainsKey(
                        owner))
                {
                    continue;
                }

                bandPositions.Add(
                    owner,
                    bandPositionEvaluator.Evaluate(
                        owner,
                        sceneId,
                        completedTurn,
                        releases
                    )
                );
            }

            List<SceneReleaseIngressEvaluation>
                evaluations =
                    new();

            /*
             * Evaluate all placements before mutation.
             */
            foreach (
                SceneRelease release
                in eligible)
            {
                evaluations.Add(
                    evaluator.Evaluate(
                        release,
                        bandPositions[
                            release
                                .SourceOwnerEntityId
                        ],
                        freshReleasePosition,
                        innerFieldEntryCeiling,
                        placementTurn
                    )
                );
            }

            /*
             * Only after the complete placement set is
             * frozen do we mutate authoritative releases.
             */
            for (int index = 0;
                 index < eligible.Count;
                 index++)
            {
                if (!ingress.TryEstablish(
                        eligible[index],
                        evaluations[index]))
                {
                    throw new InvalidOperationException(
                        "Evaluated next-turn initial " +
                        "Field placement could not be " +
                        "applied | " +
                        $"release=" +
                        $"{eligible[index].ReleaseId} | " +
                        $"placementTurn={placementTurn}"
                    );
                }
            }

            return new
                KvltNextTurnIngressSettlementResult(
                    sceneId,
                    completedTurn,
                    placementTurn,
                    evaluations
                );
        }

        private static List<SceneRelease>
            GetOrderedEligibleReleases(
                IReadOnlyList<SceneRelease> releases,
                string sceneId,
                int completedTurn)
        {
            List<SceneRelease> result =
                new();

            HashSet<string> releaseIds =
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
                        "Initial Field placement release " +
                        "population cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!releaseIds.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Initial Field placement release " +
                        "population contains duplicate " +
                        "identity.",
                        nameof(releases)
                    );
                }

                if (release.HostedSceneNodeId !=
                    sceneId)
                {
                    continue;
                }

                if (release.LifecycleState !=
                    SceneReleaseLifecycleState.Field)
                {
                    continue;
                }

                if (release.HasFieldPosition)
                {
                    continue;
                }

                if (!WasFetteredAtTurn(
                        release,
                        completedTurn))
                {
                    throw new InvalidOperationException(
                        "Field release without position " +
                        "was not fettered during the " +
                        "just-completed turn | " +
                        $"release={release.ReleaseId} | " +
                        $"completedTurn={completedTurn}"
                    );
                }

                result.Add(
                    release
                );
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

        private static bool WasFetteredAtTurn(
            SceneRelease release,
            int globalTurn)
        {
            foreach (
                SceneReleaseLifecycleTransition
                    transition
                in release.LifecycleTransitions)
            {
                if (transition.FromState ==
                        SceneReleaseLifecycleState.Fringe &&
                    transition.ToState ==
                        SceneReleaseLifecycleState.Field &&
                    transition.GlobalTurn ==
                        globalTurn)
                {
                    return true;
                }
            }

            return false;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Initial Field placement identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}