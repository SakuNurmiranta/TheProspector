using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Composes the authoritative t+1 Field ingress
    /// phase.
    ///
    /// Eligible releases are precisely:
    ///
    /// - hosted in the requested scene;
    /// - currently Field;
    /// - without an established Field Position;
    /// - fettered during the just-completed turn.
    ///
    /// Frozen Standing must be the final Standing
    /// snapshot from that completed turn.
    ///
    /// The existing ingress evaluator owns placement
    /// policy. The existing ingress service owns
    /// mutation.
    /// </summary>
    public sealed class
        KvltNextTurnIngressSettlementService
    {
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
                IReadOnlyList<SceneRelease> releases,
                KvltSettledSceneStandingResult
                    frozenStanding)
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

            if (completedTurn ==
                int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedTurn),
                    "No representable next turn exists."
                );
            }

            if (float.IsNaN(
                    freshReleasePosition) ||
                float.IsInfinity(
                    freshReleasePosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(freshReleasePosition)
                );
            }

            if (float.IsNaN(
                    innerFieldEntryCeiling) ||
                float.IsInfinity(
                    innerFieldEntryCeiling))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(innerFieldEntryCeiling)
                );
            }

            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            if (frozenStanding == null)
            {
                throw new ArgumentNullException(
                    nameof(frozenStanding)
                );
            }

            if (frozenStanding.SceneId !=
                sceneId)
            {
                throw new ArgumentException(
                    "Ingress Standing snapshot belongs " +
                    "to another scene.",
                    nameof(frozenStanding)
                );
            }

            if (frozenStanding.SettledTurn !=
                completedTurn)
            {
                throw new ArgumentException(
                    "Next-turn ingress requires the " +
                    "final Standing snapshot from the " +
                    "just-completed turn.",
                    nameof(frozenStanding)
                );
            }

            int placementTurn =
                completedTurn + 1;

            List<SceneRelease>
                eligibleReleases =
                    GetOrderedEligibleReleases(
                        releases,
                        sceneId,
                        completedTurn
                    );

            List<SceneReleaseIngressEvaluation>
                evaluations =
                    new();

            foreach (
                SceneRelease release
                in eligibleReleases)
            {
                if (!frozenStanding.TryGet(
                        release.SourceOwnerEntityId,
                        out SceneStandingEvaluation
                            ownerStanding))
                {
                    /*
                     * Entry 26 deliberately emits an
                     * evaluation for every public owner,
                     * including an explicit no-Standing
                     * result.
                     *
                     * Missing owner Standing therefore
                     * means the chronology snapshots do
                     * not describe the same world.
                     */
                    throw new InvalidOperationException(
                        "Next-turn ingress has no frozen " +
                        "Scene Standing evaluation for " +
                        "release owner | " +
                        $"release={release.ReleaseId} | " +
                        $"owner=" +
                        $"{release.SourceOwnerEntityId}"
                    );
                }

                SceneReleaseIngressEvaluation
                    evaluation =
                        evaluator.Evaluate(
                            release,
                            ownerStanding,
                            freshReleasePosition,
                            innerFieldEntryCeiling,
                            placementTurn
                        );

                if (!ingress.TryEstablish(
                        release,
                        evaluation))
                {
                    throw new InvalidOperationException(
                        "Evaluated next-turn ingress " +
                        "could not be applied | " +
                        $"release={release.ReleaseId} | " +
                        $"placementTurn={placementTurn}"
                    );
                }

                evaluations.Add(
                    evaluation
                );
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
                        "Next-turn ingress release " +
                        "population cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!releaseIds.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Next-turn ingress release " +
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
                    /*
                     * Already resident Field.
                     * Ingress is a one-time operation.
                     */
                    continue;
                }

                /*
                 * Field-without-position is only valid
                 * between successful fettering on turn t
                 * and ingress at the start of t+1.
                 *
                 * Do not silently repair an older missed
                 * ingress.
                 */
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
                        SceneReleaseLifecycleState
                            .Fringe &&
                    transition.ToState ==
                        SceneReleaseLifecycleState
                            .Field &&
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
                    "Next-turn ingress identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}