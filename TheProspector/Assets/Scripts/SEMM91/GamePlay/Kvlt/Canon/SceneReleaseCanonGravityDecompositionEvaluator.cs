using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Decomposes frozen post-assimilation release
    /// Gravity into active formal-pair claim shares.
    ///
    /// No Gravity formula is changed. This is an
    /// algebraic decomposition of the existing
    /// production result.
    /// </summary>
    public sealed class
        SceneReleaseCanonGravityDecompositionEvaluator
    {
        private const float Tolerance =
            0.0001f;

        public SceneReleaseCanonGravityDecompositionState
            Evaluate(
                SceneRelease release,
                SceneReleaseCanonFreezeApplication
                    freezeApplication,
                CanonSimultaneousMergeEvaluation
                    canonMerge)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (freezeApplication == null)
            {
                throw new ArgumentNullException(
                    nameof(freezeApplication)
                );
            }

            if (canonMerge == null)
            {
                throw new ArgumentNullException(
                    nameof(canonMerge)
                );
            }

            if (!release.IsCanonized ||
                release.CanonizationFreezeState == null)
            {
                throw new ArgumentException(
                    "Claim Gravity decomposition requires " +
                    "a frozen canonized SceneRelease.",
                    nameof(release)
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState
                    .CanonRetained)
            {
                throw new ArgumentException(
                    "Claim Gravity decomposition requires " +
                    "a CanonRetained release.",
                    nameof(release)
                );
            }

            SceneReleaseCanonizationFreezeState freeze =
                release.CanonizationFreezeState;

            SceneReleaseLegitimacyEvaluation final =
                freezeApplication.FinalLegitimacy;

            ValidateProvenance(
                release,
                freeze,
                final,
                freezeApplication,
                canonMerge
            );

            int trackCount =
                final.TrackEvaluations.Count;

            if (trackCount <= 0)
            {
                throw new InvalidOperationException(
                    "A canonized release cannot decompose " +
                    "Gravity without Track evaluations."
                );
            }

            List<
                SceneReleaseActivePairGravityContribution>
                activeContributions =
                    new();

            Dictionary<
                    PairKey,
                    SceneReleaseActivePairGravityContribution>
                activeByKey =
                    new();

            foreach (
                TrackLegitimacyEvaluation track
                in final.TrackEvaluations)
            {
                if (track == null)
                {
                    throw new InvalidOperationException(
                        "Final legitimacy contains null " +
                        "Track evaluation."
                    );
                }

                int ideaCount =
                    track.Trve.IdeaEvaluations.Count;

                if (ideaCount <= 0)
                {
                    if (track.GravityMagnitude >
                        Tolerance)
                    {
                        throw new InvalidOperationException(
                            "Track has Gravity without " +
                            "Idea evaluations."
                        );
                    }

                    continue;
                }

                float reconstructedTrackGravity =
                    0f;

                foreach (
                    IdeaTrveEvaluation idea
                    in track.Trve.IdeaEvaluations)
                {
                    /*
                     * Only active formal-pair TRVE
                     * claims contribute to T_track.
                     *
                     * Solitary, broken/ineligible and
                     * inactive pairs contribute zero.
                     */
                    if (!idea.IsFormalPair ||
                        !idea.IsTrveCapable ||
                        idea.ActiveTrveContribution <= 0f)
                    {
                        continue;
                    }

                    SceneReleaseActivePairGravityContribution
                        contribution =
                            new(
                                release.ReleaseId,
                                release.SourceDemoTapeId,
                                release.SourceOwnerEntityId,
                                release.HostedSceneNodeId,
                                track.SourceTrackId,
                                idea.SourceIdeaId,
                                idea.IdeaIndex,
                                freeze.CanonizedTurn,
                                idea.LegitimateActiveDegree,
                                idea.ActiveTrveContribution,
                                track.SignedResonance,
                                ideaCount,
                                trackCount
                            );

                    PairKey key =
                        new(
                            contribution.SourceTrackId,
                            contribution.SourceIdeaId,
                            contribution.IdeaIndex
                        );

                    if (!activeByKey.TryAdd(
                            key,
                            contribution))
                    {
                        throw new InvalidOperationException(
                            "Final legitimacy contains " +
                            "duplicate active pair identity."
                        );
                    }

                    activeContributions.Add(
                        contribution
                    );

                    reconstructedTrackGravity +=
                        contribution
                            .ClaimTrackGravityContribution;
                }

                if (Math.Abs(
                        reconstructedTrackGravity -
                        track.GravityMagnitude) >
                    Tolerance)
                {
                    throw new InvalidOperationException(
                        "Claim decomposition does not " +
                        "reconstruct Track Gravity | " +
                        $"track={track.SourceTrackId} | " +
                        $"expected={track.GravityMagnitude} | " +
                        $"actual={reconstructedTrackGravity}"
                    );
                }
            }

            float reconstructedReleaseGravity =
                0f;

            foreach (
                SceneReleaseActivePairGravityContribution
                    contribution
                in activeContributions)
            {
                reconstructedReleaseGravity +=
                    contribution
                        .ClaimReleaseGravityContribution;
            }

            if (Math.Abs(
                    reconstructedReleaseGravity -
                    final.Gravity) >
                Tolerance)
            {
                throw new InvalidOperationException(
                    "Claim decomposition does not " +
                    "reconstruct final G_release."
                );
            }

            if (Math.Abs(
                    reconstructedReleaseGravity -
                    freeze
                        .FrozenPostAssimilationGravity) >
                Tolerance)
            {
                throw new InvalidOperationException(
                    "Claim decomposition does not " +
                    "reconstruct frozen post-" +
                    "assimilation Gravity."
                );
            }

            List<
                SceneReleaseCanonFrontierGravityContribution>
                frontierContributions =
                    new();

            foreach (
                SceneReleaseCanonFrontierClaim frontier
                in canonMerge.FrontierClaims)
            {
                if (frontier.SceneReleaseId !=
                    release.ReleaseId)
                {
                    continue;
                }

                PairKey key =
                    new(
                        frontier.SourceTrackId,
                        frontier.SourceIdeaId,
                        frontier.IdeaIndex
                    );

                if (!activeByKey.TryGetValue(
                        key,
                        out
                            SceneReleaseActivePairGravityContribution
                            active))
                {
                    throw new InvalidOperationException(
                        "Canonical frontier claim has no " +
                        "matching final active pair " +
                        "Gravity contribution."
                    );
                }

                frontierContributions.Add(
                    new
                        SceneReleaseCanonFrontierGravityContribution(
                            frontier,
                            active
                        )
                );
            }

            return new
                SceneReleaseCanonGravityDecompositionState(
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    release.SourceOwnerEntityId,
                    release.HostedSceneNodeId,
                    freeze.CanonizedTurn,
                    freeze
                        .CanonizedUnderKeeperTenureId,
                    freeze
                        .FrozenPostAssimilationGravity,
                    activeContributions,
                    frontierContributions
                );
        }

        private static void ValidateProvenance(
            SceneRelease release,
            SceneReleaseCanonizationFreezeState freeze,
            SceneReleaseLegitimacyEvaluation final,
            SceneReleaseCanonFreezeApplication
                freezeApplication,
            CanonSimultaneousMergeEvaluation merge)
        {
            if (freezeApplication.FreezeState !=
                freeze)
            {
                throw new ArgumentException(
                    "Freeze application is not the " +
                    "authoritative freeze attached to " +
                    "this SceneRelease.",
                    nameof(freezeApplication)
                );
            }

            if (final.SourceReleaseId !=
                    release.ReleaseId ||
                final.SourceDemoTapeId !=
                    release.SourceDemoTapeId ||
                final.SourceOwnerEntityId !=
                    release.SourceOwnerEntityId ||
                final.SettledTurn !=
                    freeze.CanonizedTurn)
            {
                throw new ArgumentException(
                    "Final legitimacy provenance does " +
                    "not match frozen SceneRelease.",
                    nameof(freezeApplication)
                );
            }

            if (merge.SettledTurn !=
                    freeze.CanonizedTurn ||
                merge.KeeperTenureId !=
                    freeze
                        .CanonizedUnderKeeperTenureId)
            {
                throw new ArgumentException(
                    "Canon merge belongs to another " +
                    "settlement or Keeper tenure.",
                    nameof(merge)
                );
            }

            if (!merge.ContainsSuccessfulCandidate(
                    release.ReleaseId))
            {
                throw new ArgumentException(
                    "Frozen SceneRelease was not a " +
                    "successful candidate in the " +
                    "supplied Canon merge.",
                    nameof(merge)
                );
            }

            if (Math.Abs(
                    final.Gravity -
                    freeze
                        .FrozenPostAssimilationGravity) >
                Tolerance)
            {
                throw new InvalidOperationException(
                    "Final legitimacy Gravity differs " +
                    "from the authoritative frozen " +
                    "release Gravity."
                );
            }
        }

        private readonly struct PairKey :
            IEquatable<PairKey>
        {
            private readonly string trackId;

            private readonly string ideaId;

            private readonly int ideaIndex;

            public PairKey(
                string trackId,
                string ideaId,
                int ideaIndex)
            {
                this.trackId =
                    trackId;

                this.ideaId =
                    ideaId;

                this.ideaIndex =
                    ideaIndex;
            }

            public bool Equals(
                PairKey other)
            {
                return
                    string.Equals(
                        trackId,
                        other.trackId,
                        StringComparison.Ordinal
                    ) &&
                    string.Equals(
                        ideaId,
                        other.ideaId,
                        StringComparison.Ordinal
                    ) &&
                    ideaIndex ==
                    other.ideaIndex;
            }

            public override bool Equals(
                object obj)
            {
                return
                    obj is PairKey other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash =
                        trackId != null
                            ? StringComparer.Ordinal
                                .GetHashCode(trackId)
                            : 0;

                    hash =
                        (hash * 397) ^
                        (
                            ideaId != null
                                ? StringComparer.Ordinal
                                    .GetHashCode(ideaId)
                                : 0
                        );

                    return
                        (hash * 397) ^
                        ideaIndex;
                }
            }
        }
    }
}