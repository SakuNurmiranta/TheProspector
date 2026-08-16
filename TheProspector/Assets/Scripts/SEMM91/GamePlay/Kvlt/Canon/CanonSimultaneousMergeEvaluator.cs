using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Builds NewCanon simultaneously from every
    /// successful Nexus candidate in one settlement.
    ///
    /// All breakthrough claims compare against the
    /// same PreviousCanon. No same-pass claim can
    /// become precedent for another.
    /// </summary>
    public sealed class
        CanonSimultaneousMergeEvaluator
    {
        public CanonSimultaneousMergeEvaluation
            Evaluate(
                CanonState previousCanon,
                IReadOnlyList<
                    SceneReleaseCanonMergeCandidate>
                    candidates,
                string keeperTenureId)
        {
            if (previousCanon == null)
            {
                throw new ArgumentNullException(
                    nameof(previousCanon)
                );
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(
                    nameof(candidates)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    keeperTenureId))
            {
                throw new ArgumentException(
                    "Canon merge requires current " +
                    "Keeper tenure identity.",
                    nameof(keeperTenureId)
                );
            }

            if (candidates.Count == 0)
            {
                throw new ArgumentException(
                    "Canon merge requires at least one " +
                    "successful Nexus candidate.",
                    nameof(candidates)
                );
            }

            int settledTurn =
                candidates[0]
                    ?.SettledTurn ??
                throw new ArgumentException(
                    "Canon merge candidate cannot be null.",
                    nameof(candidates)
                );

            HashSet<string> seenReleaseIds =
                new(
                    StringComparer.Ordinal
                );

            List<ClaimSource> qualifying =
                new();

            foreach (
                SceneReleaseCanonMergeCandidate candidate
                in candidates)
            {
                if (candidate == null)
                {
                    throw new ArgumentException(
                        "Canon merge candidate cannot " +
                        "be null.",
                        nameof(candidates)
                    );
                }

                if (candidate.SettledTurn !=
                    settledTurn)
                {
                    throw new ArgumentException(
                        "Simultaneous Canon candidates " +
                        "must belong to one settlement.",
                        nameof(candidates)
                    );
                }

                if (!seenReleaseIds.Add(
                        candidate.SceneReleaseId))
                {
                    throw new ArgumentException(
                        "SceneRelease appears more than " +
                        "once in simultaneous Canon merge.",
                        nameof(candidates)
                    );
                }

                foreach (
                    SceneReleaseCanonBreakthroughClaim claim
                    in candidate
                        .NexusEvaluation
                        .QualifyingBreakthroughClaims)
                {
                    ValidateAgainstPreviousCanon(
                        previousCanon,
                        claim
                    );

                    SceneReleaseActivationHistoryRecord
                        activation =
                            FindFrontierActivation(
                                candidate.Release,
                                claim
                            );

                    qualifying.Add(
                        new ClaimSource(
                            candidate.Release,
                            claim,
                            activation
                        )
                    );
                }
            }

            if (qualifying.Count == 0)
            {
                throw new InvalidOperationException(
                    "Successful Canon candidates contain " +
                    "no qualifying realized breakthrough."
                );
            }

            /*
             * PASS 1:
             *
             * Determine the highest simultaneously
             * established degree for every Tag
             * polarity. Nothing mutates Canon here.
             */
            Dictionary<
                    TagDirectionKey,
                    TagDegree>
                highestByDirection =
                    new();

            foreach (
                ClaimSource source
                in qualifying)
            {
                TagDirectionKey key =
                    new(
                        source.Claim.DominantAxis,
                        source.Claim.DominantPole
                    );

                TagDegree degree =
                    source.Claim
                        .ActiveDominantDegree;

                if (!highestByDirection.TryGetValue(
                        key,
                        out TagDegree current) ||
                    degree > current)
                {
                    highestByDirection[key] =
                        degree;
                }
            }

            /*
             * PASS 2:
             *
             * Only claims tied at the resulting
             * highest degree actually establish the
             * NewCanon frontier.
             */
            List<ClaimSource> winning =
                new();

            foreach (
                ClaimSource source
                in qualifying)
            {
                TagDirectionKey key =
                    new(
                        source.Claim.DominantAxis,
                        source.Claim.DominantPole
                    );

                if (source.Claim.ActiveDominantDegree ==
                    highestByDirection[key])
                {
                    winning.Add(
                        source
                    );
                }
            }

            winning.Sort(
                CompareClaimSources
            );

            /*
             * PASS 3:
             *
             * Reproduce Canon_t and append only the
             * winning simultaneous frontier claims.
             */
            CanonState newCanon =
                previousCanon.CreateCopy();

            List<
                SceneReleaseCanonFrontierClaim>
                frontierClaims =
                    new();

            foreach (
                ClaimSource source
                in winning)
            {
                SceneReleaseCanonBreakthroughClaim claim =
                    source.Claim;

                bool recorded =
                    newCanon.TryRecordPrecedent(
                        claim.DominantAxis,
                        claim.DominantPole,
                        claim.ActiveDominantDegree,
                        CanonProvenanceKind.SceneRelease,
                        source.Release.ReleaseId,
                        claim.SourceTrackId,
                        claim.SourceIdeaId,
                        settledTurn
                    );

                if (!recorded)
                {
                    throw new InvalidOperationException(
                        "Validated simultaneous Canon " +
                        "frontier could not be recorded | " +
                        $"release={source.Release.ReleaseId} | " +
                        $"track={claim.SourceTrackId} | " +
                        $"idea={claim.SourceIdeaId}"
                    );
                }

                frontierClaims.Add(
                    new SceneReleaseCanonFrontierClaim(
                        source.Release,
                        claim,
                        source.Activation,
                        keeperTenureId,
                        settledTurn
                    )
                );
            }
            
            List<string> successfulCandidateIds =
                new(
                    seenReleaseIds
                );

            successfulCandidateIds.Sort(
                StringComparer.Ordinal
            );
            
            return new
                CanonSimultaneousMergeEvaluation(
                    previousCanon,
                    newCanon,
                    settledTurn,
                    keeperTenureId,
                    successfulCandidateIds,
                    qualifying.Count,
                    frontierClaims
                );
        }

        private static void
            ValidateAgainstPreviousCanon(
                CanonState previousCanon,
                SceneReleaseCanonBreakthroughClaim claim)
        {
            if (claim == null)
            {
                throw new ArgumentException(
                    "Nexus candidate contains null " +
                    "breakthrough claim."
                );
            }

            if (!claim.IsQualifyingBreakthrough)
            {
                throw new ArgumentException(
                    "Nexus qualifying collection " +
                    "contains a non-qualifying claim."
                );
            }

            TagDegree previousDegree =
                TagDegree.Neutral;

            previousCanon.TryGetCanonicalDegree(
                claim.DominantAxis,
                claim.DominantPole,
                out previousDegree
            );

            int expectedDifferential =
                Math.Max(
                    0,
                    (int)claim.ActiveDominantDegree -
                    (int)previousDegree
                );

            if (expectedDifferential !=
                claim.RealizedBreakthroughDifferential)
            {
                throw new InvalidOperationException(
                    "Canon breakthrough was not " +
                    "evaluated against the supplied " +
                    "previously settled Canon."
                );
            }

            if (expectedDifferential <= 0)
            {
                throw new InvalidOperationException(
                    "Qualifying breakthrough does not " +
                    "actually exceed previous Canon."
                );
            }
        }

        private static
            SceneReleaseActivationHistoryRecord
            FindFrontierActivation(
                SceneRelease release,
                SceneReleaseCanonBreakthroughClaim claim)
        {
            SceneReleaseActivationHistoryRecord found =
                null;

            foreach (
                SceneReleaseActivationHistoryRecord record
                in release.ActivationHistory)
            {
                if (!record.RaisedActivation)
                {
                    continue;
                }

                ActivationLegitimacyCandidate candidate =
                    record.Candidate;

                if (candidate.SourceTrackId !=
                        claim.SourceTrackId ||
                    candidate.SourceIdeaId !=
                        claim.SourceIdeaId ||
                    candidate.IdeaIndex !=
                        claim.IdeaIndex ||
                    record.NewActivationDegree !=
                        claim.ActiveDominantDegree)
                {
                    continue;
                }

                if (found != null)
                {
                    throw new InvalidOperationException(
                        "More than one activation history " +
                        "record claims to establish the " +
                        "canonical frontier degree."
                    );
                }

                found =
                    record;
            }

            return found ??
                throw new InvalidOperationException(
                    "Realized Canon breakthrough has no " +
                    "activation history establishing its " +
                    "active frontier degree | " +
                    $"release={release.ReleaseId} | " +
                    $"track={claim.SourceTrackId} | " +
                    $"idea={claim.SourceIdeaId}"
                );
        }

        private static int CompareClaimSources(
            ClaimSource left,
            ClaimSource right)
        {
            int axis =
                ((int)left.Claim.DominantAxis)
                    .CompareTo(
                        (int)right.Claim.DominantAxis
                    );

            if (axis != 0)
            {
                return axis;
            }

            int pole =
                ((int)left.Claim.DominantPole)
                    .CompareTo(
                        (int)right.Claim.DominantPole
                    );

            if (pole != 0)
            {
                return pole;
            }

            int release =
                string.CompareOrdinal(
                    left.Release.ReleaseId,
                    right.Release.ReleaseId
                );

            if (release != 0)
            {
                return release;
            }

            int track =
                string.CompareOrdinal(
                    left.Claim.SourceTrackId,
                    right.Claim.SourceTrackId
                );

            if (track != 0)
            {
                return track;
            }

            int ideaIndex =
                left.Claim.IdeaIndex.CompareTo(
                    right.Claim.IdeaIndex
                );

            if (ideaIndex != 0)
            {
                return ideaIndex;
            }

            return string.CompareOrdinal(
                left.Claim.SourceIdeaId,
                right.Claim.SourceIdeaId
            );
        }

        private readonly struct TagDirectionKey :
            IEquatable<TagDirectionKey>
        {
            public TagAxis Axis { get; }

            public TagPole Pole { get; }

            public TagDirectionKey(
                TagAxis axis,
                TagPole pole)
            {
                Axis =
                    axis;

                Pole =
                    pole;
            }

            public bool Equals(
                TagDirectionKey other)
            {
                return
                    Axis == other.Axis &&
                    Pole == other.Pole;
            }

            public override bool Equals(
                object obj)
            {
                return
                    obj is TagDirectionKey other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return
                        ((int)Axis * 397) ^
                        (int)Pole;
                }
            }
        }

        private sealed class ClaimSource
        {
            public SceneRelease Release { get; }

            public
                SceneReleaseCanonBreakthroughClaim
                Claim { get; }

            public
                SceneReleaseActivationHistoryRecord
                Activation { get; }

            public ClaimSource(
                SceneRelease release,
                SceneReleaseCanonBreakthroughClaim
                    claim,
                SceneReleaseActivationHistoryRecord
                    activation)
            {
                Release =
                    release;

                Claim =
                    claim;

                Activation =
                    activation;
            }
        }
    }
}