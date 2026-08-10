using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Pure classification of successful factual
    /// activation praxis against KVLT's current
    /// Accepted Transgression state.
    ///
    /// Does not create crises.
    /// Does not mutate SceneRelease.
    /// </summary>
    public sealed class
        ActivationLegitimacyClassificationService
    {
        private const string
            PublicPerformanceBehaviorTypeId =
                "PUBLIC_PERFORMANCE";

        public IReadOnlyList<
            ActivationLegitimacyAssessment>
            Classify(
                Happening happening,
                SceneReleaseActivationAttempt attempt,
                SceneReleaseActivationSource source,
                AcceptedTransgressionState
                    acceptedTransgressions)
        {
            if (happening == null)
            {
                throw new ArgumentNullException(
                    nameof(happening)
                );
            }

            if (attempt == null)
            {
                throw new ArgumentNullException(
                    nameof(attempt)
                );
            }

            if (source == null)
            {
                throw new ArgumentNullException(
                    nameof(source)
                );
            }

            if (acceptedTransgressions == null)
            {
                throw new ArgumentNullException(
                    nameof(acceptedTransgressions)
                );
            }

            if (attempt.HappeningId !=
                happening.HappeningId)
            {
                return Array.Empty<
                    ActivationLegitimacyAssessment>();
            }

            if (attempt.SceneReleaseId !=
                    source.Release.ReleaseId ||
                attempt.SourceDemoTapeId !=
                    source.DemoTape.DemoTapeId)
            {
                return Array.Empty<
                    ActivationLegitimacyAssessment>();
            }

            if (!happening.TryGetParticipantIntent(
                    attempt.SourceIntentId,
                    out HappeningParticipantIntent intent))
            {
                return Array.Empty<
                    ActivationLegitimacyAssessment>();
            }

            if (!happening.TryGetIntentResolution(
                    intent.IntentId,
                    out HappeningIntentResolution
                        resolution))
            {
                return Array.Empty<
                    ActivationLegitimacyAssessment>();
            }

            // Attempts survive blocked/failed outcomes
            // for Fringe grace, but institutional
            // legitimacy is only asked after praxis
            // actually succeeds.

            if (resolution.Outcome !=
                HappeningIntentOutcome.Succeeded)
            {
                return Array.Empty<
                    ActivationLegitimacyAssessment>();
            }

            return attempt.Route switch
            {
                ActivationAttemptRoute.Performance =>
                    ClassifyPerformance(
                        happening,
                        attempt,
                        source,
                        intent,
                        acceptedTransgressions
                    ),

                ActivationAttemptRoute.Hail =>
                    ClassifyHail(
                        happening,
                        attempt,
                        source,
                        intent,
                        acceptedTransgressions
                    ),

                _ =>
                    Array.Empty<
                        ActivationLegitimacyAssessment>()
            };
        }

        private static IReadOnlyList<
            ActivationLegitimacyAssessment>
            ClassifyPerformance(
                Happening happening,
                SceneReleaseActivationAttempt attempt,
                SceneReleaseActivationSource source,
                HappeningParticipantIntent participantIntent,
                AcceptedTransgressionState state)
        {
            if (participantIntent is not
                HappeningPerformIntent perform)
            {
                return Array.Empty<
                    ActivationLegitimacyAssessment>();
            }

            if (perform.TargetSceneReleaseId !=
                attempt.SceneReleaseId)
            {
                return Array.Empty<
                    ActivationLegitimacyAssessment>();
            }

            List<ActivationLegitimacyAssessment>
                results =
                    new();

            foreach (
                DemoTapeTrackSnapshot track
                in source.DemoTape.TrackSnapshots)
            {
                foreach (
                    DemoTapeIdeaSnapshot idea
                    in track.IdeaSnapshots)
                {
                    if (idea.PayloadType !=
                        IdeaPayloadType.TagPair)
                    {
                        continue;
                    }

                    DemoTapeTagOccurrenceSnapshot
                        dominant =
                            FindDominant(idea);

                    // Current Camp-1 runtime has no
                    // writable PairIntegrity loss yet.
                    // Formal persisted pair is therefore
                    // the current Intact-pair seam.

                    if (dominant.Degree ==
                        TagDegree.Neutral)
                    {
                        continue;
                    }

                    TagDegree appliedDegree =
                        MinDegree(
                            TagDegree.Weak,
                            dominant.Degree
                        );

                    ActivationLegitimacyCandidate
                        candidate =
                            new(
                                ActivationAttemptRoute
                                    .Performance,
                                happening.HappeningId,
                                perform.IntentId,
                                perform.ActorEntityId,
                                source.Release.ReleaseId,
                                source.DemoTape.DemoTapeId,
                                track.SourceTrackId,
                                idea.SourceIdeaId,
                                idea.IdeaIndex,
                                PublicPerformanceBehaviorTypeId,
                                dominant.Axis,
                                dominant.Pole,

                                // For public performance,
                                // praxis severity IS the
                                // low-order activation
                                // being enacted.
                                appliedDegree,

                                appliedDegree,
                                dominant.Degree
                            );

                    results.Add(
                        Assess(
                            candidate,
                            state
                        )
                    );
                }
            }

            return results.ToArray();
        }

        private static IReadOnlyList<
            ActivationLegitimacyAssessment>
            ClassifyHail(
                Happening happening,
                SceneReleaseActivationAttempt attempt,
                SceneReleaseActivationSource source,
                HappeningParticipantIntent participantIntent,
                AcceptedTransgressionState state)
        {
            if (participantIntent is not
                HappeningEnactBehaviorIntent intent ||
                !intent.HasIntendedHail)
            {
                return Array.Empty<
                    ActivationLegitimacyAssessment>();
            }

            BehaviorOccurrence behavior =
                FindBehaviorForIntent(
                    happening,
                    intent.IntentId
                );

            if (behavior == null)
            {
                return Array.Empty<
                    ActivationLegitimacyAssessment>();
            }

            HailOccurrence hail =
                FindHailForBehavior(
                    happening,
                    behavior.BehaviorOccurrenceId
                );

            if (hail == null)
            {
                return Array.Empty<
                    ActivationLegitimacyAssessment>();
            }

            if (hail.HailedAspectId !=
                attempt.HailedAspectId)
            {
                return Array.Empty<
                    ActivationLegitimacyAssessment>();
            }

            List<ActivationLegitimacyAssessment>
                results =
                    new();

            foreach (
                HailAnsweringPairCandidate pair
                in attempt.AnsweringPairCandidates)
            {
                TagDegree appliedDegree =
                    MinDegree(
                        behavior.DemonstratedDegree,
                        pair.RecordedDominantDegree
                    );

                ActivationLegitimacyCandidate candidate =
                    new(
                        ActivationAttemptRoute.Hail,
                        happening.HappeningId,
                        intent.IntentId,
                        intent.ActorEntityId,
                        source.Release.ReleaseId,
                        source.DemoTape.DemoTapeId,
                        pair.SourceTrackId,
                        pair.SourceIdeaId,
                        pair.IdeaIndex,
                        behavior.BehaviorTypeId,
                        behavior.Axis,
                        behavior.Pole,

                        // Critical:
                        // Accepted Transgression judges
                        // the factual praxis severity,
                        // NOT the degree capped by this
                        // particular recording.
                        behavior.DemonstratedDegree,

                        appliedDegree,
                        pair.RecordedDominantDegree,
                        behavior.BehaviorOccurrenceId,
                        hail.HailOccurrenceId,
                        hail.HailedAspectId
                    );

                results.Add(
                    Assess(
                        candidate,
                        state
                    )
                );
            }

            return results.ToArray();
        }

        private static
            ActivationLegitimacyAssessment
            Assess(
                ActivationLegitimacyCandidate candidate,
                AcceptedTransgressionState state)
        {
            if (state.TryGetCoveringPrecedent(
                    candidate.BehaviorTypeId,
                    candidate.Axis,
                    candidate.Pole,
                    candidate.PraxisDegree,
                    out AcceptedTransgressionRecord
                        precedent))
            {
                return new
                    ActivationLegitimacyAssessment(
                        candidate,
                        ActivationLegitimacyDisposition
                            .Covered,
                        precedent
                    );
            }

            return new ActivationLegitimacyAssessment(
                candidate,
                ActivationLegitimacyDisposition
                    .RequiresAllegianceCrisis,
                null
            );
        }

        private static BehaviorOccurrence
            FindBehaviorForIntent(
                Happening happening,
                string intentId)
        {
            foreach (
                BehaviorOccurrence behavior
                in happening.BehaviorOccurrences)
            {
                if (behavior.SourceIntentId ==
                    intentId)
                {
                    return behavior;
                }
            }

            return null;
        }

        private static HailOccurrence
            FindHailForBehavior(
                Happening happening,
                string behaviorOccurrenceId)
        {
            foreach (
                HailOccurrence hail
                in happening.HailOccurrences)
            {
                if (hail.BehaviorOccurrenceId ==
                    behaviorOccurrenceId)
                {
                    return hail;
                }
            }

            return null;
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
                "Formal Tag-pair snapshot has no " +
                "dominant occurrence | " +
                $"idea={idea.SourceIdeaId}"
            );
        }

        private static TagDegree MinDegree(
            TagDegree left,
            TagDegree right)
        {
            return
                (int)left <= (int)right
                    ? left
                    : right;
        }
    }
}