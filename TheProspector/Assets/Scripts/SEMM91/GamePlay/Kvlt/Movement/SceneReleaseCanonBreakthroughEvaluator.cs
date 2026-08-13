using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Evaluates every formal recorded pair against
    /// previously settled Canon.
    ///
    /// Recorded degree creates only potential novelty.
    /// Realized breakthrough requires authoritative
    /// legitimate activation AND current active TRVE.
    ///
    /// Breakthrough may be evaluated at two explicit
    /// points in Peak-2 chronology:
    ///
    /// - PreMovement, for breakthrough movement;
    /// - PostHappening, for year-end canonization
    ///   screening after same-turn movement is already
    ///   immutable.
    /// </summary>
    public sealed class
        SceneReleaseCanonBreakthroughEvaluator
    {
        /// <summary>
        /// Historical/default contract.
        ///
        /// Existing callers remain explicitly
        /// pre-movement unless they opt into another
        /// phase through the overload below.
        /// </summary>
        public SceneReleaseCanonBreakthroughEvaluation
            Evaluate(
                SceneRelease release,
                DemoTape demoTape,
                SceneReleaseLegitimacyEvaluation
                    legitimacy,
                CanonState frozenCanon)
        {
            return Evaluate(
                release,
                demoTape,
                legitimacy,
                frozenCanon,
                SceneReleaseCanonBreakthroughEvaluationPhase
                    .PreMovement
            );
        }

        public SceneReleaseCanonBreakthroughEvaluation
            Evaluate(
                SceneRelease release,
                DemoTape demoTape,
                SceneReleaseLegitimacyEvaluation
                    legitimacy,
                CanonState frozenCanon,
                SceneReleaseCanonBreakthroughEvaluationPhase
                    phase)
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

            if (legitimacy == null)
            {
                throw new ArgumentNullException(
                    nameof(legitimacy)
                );
            }

            if (frozenCanon == null)
            {
                throw new ArgumentNullException(
                    nameof(frozenCanon)
                );
            }

            if (!Enum.IsDefined(
                    typeof(
                        SceneReleaseCanonBreakthroughEvaluationPhase
                    ),
                    phase))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(phase)
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new ArgumentException(
                    "Canon breakthrough is evaluated " +
                    "only for current Field releases.",
                    nameof(release)
                );
            }

            if (!release.HasFieldPosition ||
                release.FieldPositionState == null)
            {
                throw new ArgumentException(
                    "Canon breakthrough evaluation " +
                    "requires Field Position.",
                    nameof(release)
                );
            }

            if (release.SourceDemoTapeId !=
                demoTape.DemoTapeId)
            {
                throw new ArgumentException(
                    "SceneRelease and DemoTape identity " +
                    "do not match.",
                    nameof(demoTape)
                );
            }

            if (legitimacy.SourceReleaseId !=
                    release.ReleaseId ||
                legitimacy.SourceDemoTapeId !=
                    demoTape.DemoTapeId ||
                legitimacy.SourceOwnerEntityId !=
                    release.SourceOwnerEntityId)
            {
                throw new ArgumentException(
                    "Legitimacy evaluation provenance " +
                    "does not match SceneRelease."
                );
            }

            if (legitimacy.SettledTurn <
                release.FieldPositionState
                    .EstablishedTurn)
            {
                throw new ArgumentException(
                    "Canon breakthrough evaluation " +
                    "predates Field establishment.",
                    nameof(legitimacy)
                );
            }

            ValidatePhaseTiming(
                release,
                legitimacy,
                phase
            );

            Dictionary<
                    string,
                    TrackLegitimacyEvaluation>
                trackEvaluations =
                    BuildTrackEvaluationLookup(
                        release,
                        demoTape,
                        legitimacy
                    );

            List<
                SceneReleaseCanonBreakthroughClaim>
                claims =
                    new();

            foreach (
                DemoTapeTrackSnapshot track
                in demoTape.TrackSnapshots)
            {
                TrackLegitimacyEvaluation
                    trackEvaluation =
                        trackEvaluations[
                            track.SourceTrackId
                        ];

                foreach (
                    DemoTapeIdeaSnapshot idea
                    in track.IdeaSnapshots)
                {
                    /*
                     * Peak-2 canonical breakthrough
                     * belongs only to formal directed
                     * TRVE claims.
                     *
                     * Solitary rhetoric does not
                     * become Canon merely because it
                     * appears on a successful cassette.
                     */
                    if (idea.PayloadType ==
                        IdeaPayloadType.SingleTag)
                    {
                        continue;
                    }

                    if (idea.PayloadType !=
                        IdeaPayloadType.TagPair)
                    {
                        throw new InvalidOperationException(
                            "Unsupported recorded Idea " +
                            "payload during Canon " +
                            "breakthrough evaluation."
                        );
                    }

                    DemoTapeTagOccurrenceSnapshot
                        dominant =
                            FindOccurrence(
                                idea,
                                DemoTapeTagOccurrenceRole
                                    .PairDominant
                            );

                    DemoTapeTagOccurrenceSnapshot
                        submissive =
                            FindOccurrence(
                                idea,
                                DemoTapeTagOccurrenceRole
                                    .PairSubmissive
                            );

                    IdeaTrveEvaluation ideaTrve =
                        FindIdeaTrveEvaluation(
                            trackEvaluation,
                            idea
                        );

                    if (!ideaTrve.IsFormalPair)
                    {
                        throw new InvalidOperationException(
                            "Formal recorded pair has " +
                            "non-pair TRVE evaluation | " +
                            $"idea={idea.SourceIdeaId}"
                        );
                    }

                    if (ideaTrve.DominantDegree !=
                        (int)dominant.Degree)
                    {
                        throw new InvalidOperationException(
                            "Recorded dominant degree and " +
                            "TRVE evaluation disagree | " +
                            $"idea={idea.SourceIdeaId}"
                        );
                    }

                    TagDegree authoritativeActivation =
                        TagDegree.Neutral;

                    if (release.TryGetPairActivationState(
                            track.SourceTrackId,
                            idea.SourceIdeaId,
                            idea.IdeaIndex,
                            out
                                SceneReleasePairActivationState
                                activationState))
                    {
                        if (activationState
                                .RecordedDominantDegree !=
                            dominant.Degree)
                        {
                            throw new InvalidOperationException(
                                "SceneRelease activation " +
                                "recorded degree disagrees " +
                                "with DemoTape | " +
                                $"idea={idea.SourceIdeaId}"
                            );
                        }

                        authoritativeActivation =
                            activationState
                                .CurrentActivationDegree;
                    }

                    if ((int)authoritativeActivation !=
                        ideaTrve.LegitimateActiveDegree)
                    {
                        throw new InvalidOperationException(
                            "Legitimacy evaluation is stale " +
                            "relative to authoritative " +
                            "SceneRelease activation | " +
                            $"idea={idea.SourceIdeaId}"
                        );
                    }

                    bool hasCanonicalPrecedent =
                        frozenCanon
                            .TryGetCanonicalDegree(
                                dominant.Axis,
                                dominant.Pole,
                                out
                                    TagDegree
                                    canonicalDegree
                            );

                    if (!hasCanonicalPrecedent)
                    {
                        canonicalDegree =
                            TagDegree.Neutral;
                    }

                    bool currentlyTrve =
                        ideaTrve.IsTrveCapable &&
                        ideaTrve
                            .ActiveTrveContribution >
                        0f;

                    claims.Add(
                        new
                            SceneReleaseCanonBreakthroughClaim(
                                release.ReleaseId,
                                demoTape.DemoTapeId,
                                release.SourceOwnerEntityId,
                                release.HostedSceneNodeId,
                                track.SourceTrackId,
                                idea.SourceIdeaId,
                                idea.IdeaIndex,
                                legitimacy.SettledTurn,
                                dominant.Axis,
                                dominant.Pole,
                                dominant.Degree,
                                authoritativeActivation,
                                hasCanonicalPrecedent,
                                canonicalDegree,
                                submissive.Axis,
                                submissive.Pole,
                                submissive.Degree,
                                currentlyTrve
                            )
                    );
                }
            }

            /*
             * StartFieldPosition retains its historical
             * property name.
             *
             * For PreMovement it is literally the
             * movement-start position.
             *
             * For PostHappening it is the already-settled
             * position at which year-end screening was
             * performed.
             */
            return new
                SceneReleaseCanonBreakthroughEvaluation(
                    release.ReleaseId,
                    demoTape.DemoTapeId,
                    release.SourceOwnerEntityId,
                    release.HostedSceneNodeId,
                    legitimacy.SettledTurn,
                    release.FieldPositionState
                        .CurrentPosition,
                    claims
                );
        }

        private static void ValidatePhaseTiming(
            SceneRelease release,
            SceneReleaseLegitimacyEvaluation
                legitimacy,
            SceneReleaseCanonBreakthroughEvaluationPhase
                phase)
        {
            int lastMovementTurn =
                release.FieldPositionState
                    .LastMovementTurn;

            switch (phase)
            {
                case
                    SceneReleaseCanonBreakthroughEvaluationPhase
                        .PreMovement:
                {
                    /*
                     * Preserve the Camp-3 movement
                     * invariant exactly.
                     */
                    if (lastMovementTurn >=
                        legitimacy.SettledTurn)
                    {
                        throw new InvalidOperationException(
                            "Pre-movement Canon breakthrough " +
                            "evaluation cannot occur after " +
                            "movement has already settled " +
                            "for this turn."
                        );
                    }

                    return;
                }

                case
                    SceneReleaseCanonBreakthroughEvaluationPhase
                        .PostHappening:
                {
                    /*
                     * Post-Happening screening is allowed
                     * after same-turn movement.
                     *
                     * A release may also have had no
                     * movement transition this turn
                     * (for example dormant TRVE that was
                     * activated during the Happening).
                     *
                     * What is forbidden is evaluating a
                     * semantic snapshot against position
                     * state from a future turn.
                     */
                    if (lastMovementTurn >
                        legitimacy.SettledTurn)
                    {
                        throw new InvalidOperationException(
                            "Post-Happening Canon " +
                            "breakthrough evaluation cannot " +
                            "use future Field movement."
                        );
                    }

                    return;
                }

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(phase)
                    );
            }
        }

        private static Dictionary<
                string,
                TrackLegitimacyEvaluation>
            BuildTrackEvaluationLookup(
                SceneRelease release,
                DemoTape demoTape,
                SceneReleaseLegitimacyEvaluation
                    legitimacy)
        {
            Dictionary<
                    string,
                    TrackLegitimacyEvaluation>
                result =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                TrackLegitimacyEvaluation track
                in legitimacy.TrackEvaluations)
            {
                if (track == null)
                {
                    throw new ArgumentException(
                        "Release legitimacy evaluation " +
                        "contains null Track result."
                    );
                }

                if (track.SourceReleaseId !=
                        release.ReleaseId ||
                    track.SourceDemoTapeId !=
                        demoTape.DemoTapeId ||
                    track.SettledTurn !=
                        legitimacy.SettledTurn)
                {
                    throw new ArgumentException(
                        "Track legitimacy provenance " +
                        "does not match release."
                    );
                }

                if (!result.TryAdd(
                        track.SourceTrackId,
                        track))
                {
                    throw new ArgumentException(
                        "Release legitimacy contains " +
                        "duplicate Track identity."
                    );
                }
            }

            foreach (
                DemoTapeTrackSnapshot track
                in demoTape.TrackSnapshots)
            {
                if (!result.ContainsKey(
                        track.SourceTrackId))
                {
                    throw new ArgumentException(
                        "DemoTape Track has no matching " +
                        "legitimacy evaluation | " +
                        $"track={track.SourceTrackId}"
                    );
                }
            }

            return result;
        }

        private static IdeaTrveEvaluation
            FindIdeaTrveEvaluation(
                TrackLegitimacyEvaluation track,
                DemoTapeIdeaSnapshot idea)
        {
            IdeaTrveEvaluation found =
                null;

            foreach (
                IdeaTrveEvaluation candidate
                in track.Trve.IdeaEvaluations)
            {
                if (candidate.SourceIdeaId !=
                        idea.SourceIdeaId ||
                    candidate.IdeaIndex !=
                        idea.IdeaIndex)
                {
                    continue;
                }

                if (found != null)
                {
                    throw new InvalidOperationException(
                        "TRVE evaluation contains " +
                        "duplicate Idea identity | " +
                        $"idea={idea.SourceIdeaId}"
                    );
                }

                found =
                    candidate;
            }

            return found ??
                throw new InvalidOperationException(
                    "Recorded Idea has no matching " +
                    "TRVE evaluation | " +
                    $"idea={idea.SourceIdeaId}"
                );
        }

        private static
            DemoTapeTagOccurrenceSnapshot
            FindOccurrence(
                DemoTapeIdeaSnapshot idea,
                DemoTapeTagOccurrenceRole role)
        {
            foreach (
                DemoTapeTagOccurrenceSnapshot occurrence
                in idea.TagOccurrences)
            {
                if (occurrence.Role ==
                    role)
                {
                    return occurrence;
                }
            }

            throw new InvalidOperationException(
                "Formal recorded pair is missing " +
                $"required occurrence | " +
                $"idea={idea.SourceIdeaId} | " +
                $"role={role}"
            );
        }
    }
}