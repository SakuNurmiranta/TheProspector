using System;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Pure initial Field-position policy.
    ///
    /// Scene Standing is already expressed in the
    /// Field-position coordinate, so Standing is used
    /// directly as the candidate ingress coordinate.
    ///
    /// Inward privilege is capped by the configured
    /// Inner-Field Entry Ceiling.
    /// </summary>
    public sealed class SceneReleaseIngressEvaluator
    {
        public SceneReleaseIngressEvaluation Evaluate(
            SceneRelease release,
            SceneStandingEvaluation frozenStanding,
            float noStandingEntryPosition,
            float innerFieldEntryCeiling,
            int placementTurn)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new ArgumentException(
                    "Only a fettered Field release " +
                    "can receive Field ingress.",
                    nameof(release)
                );
            }

            if (placementTurn <
                release.ReleasedTurn)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(placementTurn)
                );
            }

            if (!IsFinite(
                    noStandingEntryPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(noStandingEntryPosition)
                );
            }

            if (!IsFinite(
                    innerFieldEntryCeiling))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(innerFieldEntryCeiling)
                );
            }

            /*
             * An unknown band's baseline entry cannot
             * itself violate the inward entry ceiling.
             */
            if (noStandingEntryPosition >
                innerFieldEntryCeiling)
            {
                throw new ArgumentException(
                    "No-history entry position cannot " +
                    "be inward of the Inner-Field " +
                    "Entry Ceiling.",
                    nameof(noStandingEntryPosition)
                );
            }

            bool hasStandingBasis =
                false;

            int? standingBasisTurn =
                null;

            float? standingBasisPosition =
                null;

            float uncappedPosition =
                noStandingEntryPosition;

            if (frozenStanding != null)
            {
                if (frozenStanding
                        .SourceOwnerEntityId !=
                    release.SourceOwnerEntityId)
                {
                    throw new ArgumentException(
                        "Frozen Scene Standing belongs " +
                        "to a different release owner.",
                        nameof(frozenStanding)
                    );
                }

                if (frozenStanding.SceneId !=
                    release.HostedSceneNodeId)
                {
                    throw new ArgumentException(
                        "Frozen Scene Standing belongs " +
                        "to a different scene.",
                        nameof(frozenStanding)
                    );
                }

                /*
                 * Standing generated during this same
                 * settlement cannot feed the ingress
                 * that helped generate it.
                 */
                if (frozenStanding.SettledTurn >=
                    placementTurn)
                {
                    throw new ArgumentException(
                        "SceneRelease ingress requires " +
                        "Standing from an earlier " +
                        "settled turn.",
                        nameof(frozenStanding)
                    );
                }

                if (frozenStanding.HasStanding)
                {
                    if (!frozenStanding
                            .Standing.HasValue)
                    {
                        throw new InvalidOperationException(
                            "Standing evaluation claims " +
                            "Standing but contains no " +
                            "Standing value."
                        );
                    }

                    hasStandingBasis =
                        true;

                    standingBasisTurn =
                        frozenStanding.SettledTurn;

                    standingBasisPosition =
                        frozenStanding.Standing.Value;

                    /*
                     * Identity projection:
                     *
                     * Scene Standing is a historical
                     * centroid in this same Field
                     * coordinate, so no additional
                     * invented transform is required.
                     */
                    uncappedPosition =
                        frozenStanding.Standing.Value;
                }
            }

            float appliedPosition =
                Math.Min(
                    uncappedPosition,
                    innerFieldEntryCeiling
                );

            return new SceneReleaseIngressEvaluation(
                release.ReleaseId,
                release.SourceOwnerEntityId,
                release.HostedSceneNodeId,
                placementTurn,
                hasStandingBasis,
                standingBasisTurn,
                standingBasisPosition,
                noStandingEntryPosition,
                innerFieldEntryCeiling,
                uncappedPosition,
                appliedPosition
            );
        }

        private static bool IsFinite(
            float value)
        {
            return
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}