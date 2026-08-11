using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Builds the complete NewCanon Assimilation plan
    /// for one successful simultaneous Canon candidate.
    ///
    /// Assimilation occurs only after the simultaneous
    /// Canon merge and therefore reads NewCanon rather
    /// than Canon_t.
    ///
    /// Only TRVE-capable formal pairs may assimilate.
    /// Solitary and structurally/contextually
    /// ineligible material remains inactive.
    /// </summary>
    public sealed class
        SceneReleaseCanonAssimilationEvaluator
    {
        private const float PositionTolerance =
            0.0001f;

        public SceneReleaseCanonAssimilationEvaluation
            Evaluate(
                SceneRelease release,
                DemoTape demoTape,
                SceneReleaseNexusBoundaryEvaluation
                    nexusEvaluation,
                CanonSimultaneousMergeEvaluation
                    canonMerge)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (demoTape == null)
            {
                throw new ArgumentNullException(
                    nameof(demoTape)
                );
            }

            if (nexusEvaluation == null)
            {
                throw new ArgumentNullException(
                    nameof(nexusEvaluation)
                );
            }

            if (canonMerge == null)
            {
                throw new ArgumentNullException(
                    nameof(canonMerge)
                );
            }

            if (!nexusEvaluation.IsCanonCandidate)
            {
                throw new ArgumentException(
                    "Canon Assimilation requires a " +
                    "Canon candidate.",
                    nameof(nexusEvaluation)
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new ArgumentException(
                    "Canon candidate must still be an " +
                    "active Field release.",
                    nameof(release)
                );
            }

            if (!release.HasFieldPosition ||
                release.FieldPositionState == null)
            {
                throw new ArgumentException(
                    "Canon Assimilation requires Field " +
                    "Position.",
                    nameof(release)
                );
            }

            ValidateIdentity(
                release,
                demoTape,
                nexusEvaluation
            );

            if (canonMerge.SettledTurn !=
                nexusEvaluation.SettledTurn)
            {
                throw new ArgumentException(
                    "NewCanon belongs to a different " +
                    "settlement turn.",
                    nameof(canonMerge)
                );
            }

            if (!canonMerge.ContainsSuccessfulCandidate(
                    release.ReleaseId))
            {
                throw new ArgumentException(
                    "SceneRelease did not participate " +
                    "in the successful simultaneous " +
                    "Canon merge.",
                    nameof(canonMerge)
                );
            }

            if (!NearlyEqual(
                    release.FieldPositionState
                        .CurrentPosition,
                    nexusEvaluation.FieldPosition))
            {
                throw new InvalidOperationException(
                    "Canon candidate position changed " +
                    "after Nexus-boundary evaluation."
                );
            }

            if (release.FieldPositionState
                    .LastMovementTurn !=
                nexusEvaluation.SettledTurn)
            {
                throw new InvalidOperationException(
                    "Canon Assimilation requires the " +
                    "same settled movement turn as the " +
                    "Nexus candidate."
                );
            }

            Dictionary<
                    PairKey,
                    SceneReleaseCanonBreakthroughClaim>
                breakthroughClaims =
                    BuildBreakthroughClaimLookup(
                        nexusEvaluation
                            .BreakthroughEvaluation
                            .Claims
                    );

            List<
                SceneReleaseCanonAssimilationPairEvaluation>
                results =
                    new();

            foreach (
                DemoTapeTrackSnapshot track
                in demoTape.TrackSnapshots)
            {
                foreach (
                    DemoTapeIdeaSnapshot idea
                    in track.IdeaSnapshots)
                {
                    if (idea.PayloadType ==
                        IdeaPayloadType.SingleTag)
                    {
                        continue;
                    }

                    if (idea.PayloadType !=
                        IdeaPayloadType.TagPair)
                    {
                        throw new InvalidOperationException(
                            "Unsupported Idea payload in " +
                            "Canon Assimilation."
                        );
                    }

                    PairKey key =
                        new(
                            track.SourceTrackId,
                            idea.SourceIdeaId,
                            idea.IdeaIndex
                        );

                    if (!breakthroughClaims.TryGetValue(
                            key,
                            out
                                SceneReleaseCanonBreakthroughClaim
                                semanticClaim))
                    {
                        throw new InvalidOperationException(
                            "Formal DemoTape pair has no " +
                            "matching frozen Canon " +
                            "Breakthrough semantic claim."
                        );
                    }

                    /*
                     * Revision rule:
                     *
                     * Broken or otherwise ineligible
                     * formal pairs remain inactive even
                     * if NewCanon recognizes their
                     * dominant Tag polarity.
                     */
                    if (!semanticClaim.IsTrveCapable)
                    {
                        continue;
                    }

                    DemoTapeTagOccurrenceSnapshot dominant =
                        FindDominant(
                            idea
                        );

                    if (dominant.Axis !=
                            semanticClaim.DominantAxis ||
                        dominant.Pole !=
                            semanticClaim.DominantPole ||
                        dominant.Degree !=
                            semanticClaim
                                .RecordedDominantDegree)
                    {
                        throw new InvalidOperationException(
                            "Frozen breakthrough semantic " +
                            "claim disagrees with immutable " +
                            "DemoTape pair."
                        );
                    }

                    TagDegree existingActivation =
                        TagDegree.Neutral;

                    if (release.TryGetPairActivationState(
                            track.SourceTrackId,
                            idea.SourceIdeaId,
                            idea.IdeaIndex,
                            out
                                SceneReleasePairActivationState
                                state))
                    {
                        if (state.RecordedDominantDegree !=
                            dominant.Degree)
                        {
                            throw new InvalidOperationException(
                                "SceneRelease activation " +
                                "state disagrees with " +
                                "immutable DemoTape."
                            );
                        }

                        existingActivation =
                            state.CurrentActivationDegree;
                    }

                    bool hasNewCanon =
                        canonMerge.NewCanon
                            .TryGetCanonicalDegree(
                                dominant.Axis,
                                dominant.Pole,
                                out
                                    TagDegree
                                    newCanonicalDegree
                            );

                    List<CanonPrecedentRecord>
                        precedents =
                            new();

                    if (hasNewCanon)
                    {
                        foreach (
                            CanonPrecedentRecord record
                            in canonMerge.NewCanon.Records)
                        {
                            if (record.Axis ==
                                    dominant.Axis &&
                                record.Pole ==
                                    dominant.Pole &&
                                record.Degree ==
                                    newCanonicalDegree)
                            {
                                precedents.Add(
                                    record
                                );
                            }
                        }

                        if (precedents.Count == 0)
                        {
                            throw new InvalidOperationException(
                                "NewCanon reports a degree " +
                                "without matching precedent " +
                                "provenance."
                            );
                        }
                    }
                    else
                    {
                        newCanonicalDegree =
                            TagDegree.Neutral;
                    }

                    results.Add(
                        new
                            SceneReleaseCanonAssimilationPairEvaluation(
                                release.ReleaseId,
                                demoTape.DemoTapeId,
                                release.SourceOwnerEntityId,
                                release.HostedSceneNodeId,
                                track.SourceTrackId,
                                idea.SourceIdeaId,
                                idea.IdeaIndex,
                                nexusEvaluation.SettledTurn,
                                dominant.Axis,
                                dominant.Pole,
                                dominant.Degree,
                                existingActivation,
                                hasNewCanon,
                                newCanonicalDegree,
                                precedents
                            )
                    );
                }
            }

            return new
                SceneReleaseCanonAssimilationEvaluation(
                    nexusEvaluation,
                    canonMerge.KeeperTenureId,
                    results
                );
        }

        private static Dictionary<
                PairKey,
                SceneReleaseCanonBreakthroughClaim>
            BuildBreakthroughClaimLookup(
                IReadOnlyList<
                    SceneReleaseCanonBreakthroughClaim>
                    claims)
        {
            if (claims == null)
            {
                throw new ArgumentNullException(
                    nameof(claims)
                );
            }

            Dictionary<
                    PairKey,
                    SceneReleaseCanonBreakthroughClaim>
                result =
                    new();

            foreach (
                SceneReleaseCanonBreakthroughClaim claim
                in claims)
            {
                if (claim == null)
                {
                    throw new ArgumentException(
                        "Breakthrough evaluation contains " +
                        "null claim.",
                        nameof(claims)
                    );
                }

                PairKey key =
                    new(
                        claim.SourceTrackId,
                        claim.SourceIdeaId,
                        claim.IdeaIndex
                    );

                if (!result.TryAdd(
                        key,
                        claim))
                {
                    throw new InvalidOperationException(
                        "Breakthrough evaluation contains " +
                        "duplicate formal-pair identity."
                    );
                }
            }

            return result;
        }

        private static void ValidateIdentity(
            SceneRelease release,
            DemoTape demoTape,
            SceneReleaseNexusBoundaryEvaluation nexus)
        {
            if (release.SourceDemoTapeId !=
                demoTape.DemoTapeId)
            {
                throw new ArgumentException(
                    "SceneRelease and DemoTape identity " +
                    "do not match.",
                    nameof(demoTape)
                );
            }

            if (nexus.SceneReleaseId !=
                    release.ReleaseId ||
                nexus.SourceDemoTapeId !=
                    release.SourceDemoTapeId ||
                nexus.SourceOwnerEntityId !=
                    release.SourceOwnerEntityId ||
                nexus.SceneId !=
                    release.HostedSceneNodeId)
            {
                throw new ArgumentException(
                    "Nexus candidate provenance does " +
                    "not match SceneRelease.",
                    nameof(nexus)
                );
            }
        }

        private static
            DemoTapeTagOccurrenceSnapshot
            FindDominant(
                DemoTapeIdeaSnapshot idea)
        {
            foreach (
                DemoTapeTagOccurrenceSnapshot occurrence
                in idea.TagOccurrences)
            {
                if (occurrence.Role ==
                    DemoTapeTagOccurrenceRole
                        .PairDominant)
                {
                    return occurrence;
                }
            }

            throw new InvalidOperationException(
                "Formal Tag-pair has no dominant " +
                "occurrence."
            );
        }

        private static bool NearlyEqual(
            float left,
            float right)
        {
            return
                Math.Abs(left - right) <=
                PositionTolerance;
        }

        private readonly struct PairKey :
            IEquatable<PairKey>
        {
            private readonly string
                trackId;

            private readonly string
                ideaId;

            private readonly int
                ideaIndex;

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
                            ? StringComparer
                                .Ordinal
                                .GetHashCode(trackId)
                            : 0;

                    hash =
                        (hash * 397) ^
                        (
                            ideaId != null
                                ? StringComparer
                                    .Ordinal
                                    .GetHashCode(ideaId)
                                : 0
                        );

                    hash =
                        (hash * 397) ^
                        ideaIndex;

                    return hash;
                }
            }
        }
    }
}