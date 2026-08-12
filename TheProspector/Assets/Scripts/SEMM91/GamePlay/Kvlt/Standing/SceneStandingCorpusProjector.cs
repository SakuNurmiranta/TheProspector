using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;

namespace SEMM91.GamePlay.Kvlt.Standing
{
    /// <summary>
    /// Projects authoritative settled SceneRelease
    /// lifecycle/history into the remembered corpus
    /// consumed by Scene Standing.
    ///
    /// This class does not calculate the centroid.
    /// It builds provenance-bearing contributions and
    /// delegates that calculation to the established
    /// SceneStandingEvaluator.
    /// </summary>
    public sealed class
        SceneStandingCorpusProjector
    {
        private readonly
            SceneStandingEvaluator evaluator =
                new();

        public SceneStandingEvaluation Project(
            string sourceOwnerEntityId,
            string sceneId,
            int settledTurn,
            IReadOnlyList<SceneRelease> releases,
            SceneStandingProjectionPolicy policy)
        {
            string owner =
                RequireText(
                    sourceOwnerEntityId,
                    nameof(sourceOwnerEntityId)
                );

            string scene =
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

            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            if (policy == null)
            {
                throw new ArgumentNullException(
                    nameof(policy)
                );
            }

            HashSet<string> seenReleaseIds =
                new(
                    StringComparer.Ordinal
                );

            List<SceneStandingContribution>
                contributions =
                    new();

            foreach (
                SceneRelease release
                in releases)
            {
                if (release == null)
                {
                    throw new ArgumentException(
                        "Scene Standing corpus cannot " +
                        "contain null SceneRelease.",
                        nameof(releases)
                    );
                }

                if (!seenReleaseIds.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Scene Standing corpus contains " +
                        "duplicate SceneRelease identity.",
                        nameof(releases)
                    );
                }

                /*
                 * Settlement may supply the complete
                 * scene corpus. Standing is projected
                 * only for the requested owner/scene.
                 */
                if (!string.Equals(
                        release.SourceOwnerEntityId,
                        owner,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        release.HostedSceneNodeId,
                        scene,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                switch (release.LifecycleState)
                {
                    case SceneReleaseLifecycleState
                        .Fringe:

                    case SceneReleaseLifecycleState
                        .FailedToFetter:

                        /*
                         * Neither state entered the
                         * remembered successfully-
                         * fettered corpus.
                         */
                        continue;

                    case SceneReleaseLifecycleState
                        .Field:

                        contributions.Add(
                            ProjectActiveField(
                                release,
                                settledTurn
                            )
                        );

                        break;

                    case SceneReleaseLifecycleState
                        .CanonRetained:

                    case SceneReleaseLifecycleState
                        .HistoricalCanon:

                        contributions.Add(
                            ProjectCanonLegacy(
                                release,
                                settledTurn,
                                policy
                            )
                        );

                        break;

                    case SceneReleaseLifecycleState
                        .Rejected:

                        contributions.Add(
                            ProjectRejectionScar(
                                release,
                                settledTurn,
                                policy
                            )
                        );

                        break;

                    default:

                        throw new InvalidOperationException(
                            "Scene Standing projection " +
                            "does not recognize lifecycle " +
                            $"state {release.LifecycleState}."
                        );
                }
            }

            /*
             * Standing mathematics are order-
             * independent, but deterministic ordering
             * matters for logs, replication and tests.
             */
            contributions.Sort(
                CompareReleaseIdentity
            );

            return evaluator.Evaluate(
                owner,
                scene,
                settledTurn,
                contributions
            );
        }

        private static SceneStandingContribution
            ProjectActiveField(
                SceneRelease release,
                int settledTurn)
        {
            SceneReleaseFieldPositionState position =
                release.FieldPositionState ??
                throw new InvalidOperationException(
                    "Field SceneRelease has no Field " +
                    "Position | release=" +
                    release.ReleaseId
                );

            int basisTurn =
                position.LastMovementTurn >= 0
                    ? position.LastMovementTurn
                    : position.EstablishedTurn;

            if (basisTurn > settledTurn)
            {
                throw new InvalidOperationException(
                    "Field SceneRelease position belongs " +
                    "to a future settlement | release=" +
                    release.ReleaseId
                );
            }

            return new SceneStandingContribution(
                release.ReleaseId,
                release.SourceDemoTapeId,
                release.SourceOwnerEntityId,
                release.HostedSceneNodeId,
                SceneStandingContributionKind
                    .ActiveField,
                position.CurrentPosition,
                1f,
                basisTurn
            );
        }

        private static SceneStandingContribution
            ProjectCanonLegacy(
                SceneRelease release,
                int settledTurn,
                SceneStandingProjectionPolicy policy)
        {
            if (!release.IsCanonized ||
                release.CanonizationFreezeState == null)
            {
                throw new InvalidOperationException(
                    "Canonical lifecycle release has no " +
                    "canonization freeze | release=" +
                    release.ReleaseId
                );
            }

            if (release.CanonizedTurn >
                settledTurn)
            {
                throw new InvalidOperationException(
                    "Canonical SceneRelease originates " +
                    "from a future settlement | release=" +
                    release.ReleaseId
                );
            }

            SceneReleaseFieldPositionState position =
                release.FieldPositionState ??
                throw new InvalidOperationException(
                    "Canonical SceneRelease has no " +
                    "historical Field Position | release=" +
                    release.ReleaseId
                );

            SceneReleaseCanonGravityDecompositionState
                decomposition =
                    release
                        .CanonGravityDecompositionState ??
                    throw new InvalidOperationException(
                        "Canonical SceneRelease has no " +
                        "frozen Canon Gravity " +
                        "decomposition | release=" +
                        release.ReleaseId
                    );

            int precedentMagnitude =
                CalculateUniqueFrontierMagnitude(
                    release,
                    decomposition
                );

            float weight =
                policy.CalculateCanonLegacyWeight(
                    precedentMagnitude
                );

            return new SceneStandingContribution(
                release.ReleaseId,
                release.SourceDemoTapeId,
                release.SourceOwnerEntityId,
                release.HostedSceneNodeId,
                SceneStandingContributionKind
                    .CanonLegacy,
                /*
                 * Canonized releases cannot move after
                 * freeze, so CurrentPosition is the
                 * durable central historical anchor.
                 */
                position.CurrentPosition,
                weight,
                release.CanonizedTurn
            );
        }

        private static SceneStandingContribution
            ProjectRejectionScar(
                SceneRelease release,
                int settledTurn,
                SceneStandingProjectionPolicy policy)
        {
            SceneReleaseRejectionState rejection =
                release.RejectionState ??
                throw new InvalidOperationException(
                    "Rejected SceneRelease has no " +
                    "rejection history | release=" +
                    release.ReleaseId
                );

            if (rejection.RejectedTurn >
                settledTurn)
            {
                throw new InvalidOperationException(
                    "Rejected SceneRelease originates " +
                    "from a future settlement | release=" +
                    release.ReleaseId
                );
            }

            return new SceneStandingContribution(
                release.ReleaseId,
                release.SourceDemoTapeId,
                release.SourceOwnerEntityId,
                release.HostedSceneNodeId,
                SceneStandingContributionKind
                    .RejectionScar,
                rejection.RejectedPosition,
                policy.CalculateRejectionScarWeight(
                    rejection
                ),
                rejection.RejectedTurn
            );
        }

        private static int
            CalculateUniqueFrontierMagnitude(
                SceneRelease release,
                SceneReleaseCanonGravityDecompositionState
                    decomposition)
        {
            Dictionary<
                    (TagAxis Axis, TagPole Pole),
                    int>
                magnitudeByPolarity =
                    new();

            foreach (
                SceneReleaseCanonFrontierGravityContribution
                    frontier
                in decomposition.FrontierContributions)
            {
                SceneReleaseCanonFrontierClaim claim =
                    frontier.FrontierClaim;

                if (claim.SceneReleaseId !=
                    release.ReleaseId)
                {
                    throw new InvalidOperationException(
                        "Canon Gravity decomposition " +
                        "contains foreign frontier " +
                        "provenance."
                    );
                }

                int magnitude =
                    claim
                        .BreakthroughClaim
                        .RealizedBreakthroughDifferential;

                if (magnitude <= 0)
                {
                    throw new InvalidOperationException(
                        "Canonical frontier has no " +
                        "realized breakthrough magnitude."
                    );
                }

                var key =
                    (
                        claim.Axis,
                        claim.Pole
                    );

                /*
                 * Coincident/tied frontier provenance
                 * on one polarity represents one
                 * institutional degree frontier.
                 *
                 * Provenance multiplicity therefore
                 * must not inflate Standing weight.
                 */
                if (!magnitudeByPolarity.TryGetValue(
                        key,
                        out int existing) ||
                    magnitude > existing)
                {
                    magnitudeByPolarity[key] =
                        magnitude;
                }
            }

            int total =
                0;

            foreach (
                int magnitude
                in magnitudeByPolarity.Values)
            {
                checked
                {
                    total +=
                        magnitude;
                }
            }

            return total;
        }

        private static int CompareReleaseIdentity(
            SceneStandingContribution left,
            SceneStandingContribution right)
        {
            return string.CompareOrdinal(
                left.SourceSceneReleaseId,
                right.SourceSceneReleaseId
            );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Scene Standing projection identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}