using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackTrveEvaluator
    {
        public TrackTrveEvaluation Evaluate(
            DemoTapeTrackSnapshot track,
            TrackTrveCapabilityEvaluation capability,
            TrackActivationEvaluationSnapshot activation)
        {
            if (track == null)
            {
                throw new ArgumentNullException(
                    nameof(track)
                );
            }

            if (capability == null)
            {
                throw new ArgumentNullException(
                    nameof(capability)
                );
            }

            if (activation == null)
            {
                throw new ArgumentNullException(
                    nameof(activation)
                );
            }

            ValidateTrackIdentity(
                track,
                capability,
                activation
            );

            ValidateActivationReferences(
                track,
                activation
            );

            List<IdeaTrveEvaluation> results =
                new();

            float trveTotal =
                0f;

            for (int index = 0;
                 index < track.IdeaSnapshots.Count;
                 index++)
            {
                DemoTapeIdeaSnapshot idea =
                    track.IdeaSnapshots[index];

                IdeaTrveCapabilityEvaluation
                    ideaCapability =
                        capability
                            .IdeaEvaluations[index];

                ValidateIdeaIdentity(
                    idea,
                    ideaCapability
                );

                IdeaTrveEvaluation evaluation =
                    EvaluateIdea(
                        idea,
                        ideaCapability,
                        activation
                    );

                results.Add(
                    evaluation
                );

                trveTotal +=
                    evaluation
                        .ActiveTrveContribution;
            }

            float trackTrve =
                results.Count == 0
                    ? 0f
                    : trveTotal / results.Count;

            return new TrackTrveEvaluation(
                activation.SourceReleaseId,
                activation.SourceDemoTapeId,
                track.SourceTrackId,
                trackTrve,
                results
            );
        }

        private static IdeaTrveEvaluation
            EvaluateIdea(
                DemoTapeIdeaSnapshot idea,
                IdeaTrveCapabilityEvaluation capability,
                TrackActivationEvaluationSnapshot activation)
        {
            int activeDegree =
                activation
                    .GetLegitimateActiveDegree(
                        idea.SourceIdeaId
                    );

            if (idea.PayloadType ==
                IdeaPayloadType.SingleTag)
            {
                if (activeDegree > 0)
                {
                    throw new InvalidOperationException(
                        "Solitary Idea cannot have " +
                        "legitimate TRVE activation | " +
                        $"idea={idea.SourceIdeaId} | " +
                        $"activation={activeDegree}"
                    );
                }

                if (capability.IsFormalPair)
                {
                    throw new InvalidOperationException(
                        "Solitary Idea has a formal-pair " +
                        "TRVE capability result | " +
                        $"idea={idea.SourceIdeaId}"
                    );
                }

                return new IdeaTrveEvaluation(
                    idea.SourceIdeaId,
                    idea.IdeaIndex,
                    false,
                    PairIntegrity.NotApplicable,
                    false,
                    0,
                    0f,
                    0f,
                    0,
                    0f,
                    0f
                );
            }

            if (idea.PayloadType !=
                IdeaPayloadType.TagPair)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idea.PayloadType),
                    idea.PayloadType,
                    "Unsupported Idea payload type."
                );
            }

            if (!capability.IsFormalPair)
            {
                throw new InvalidOperationException(
                    "Formal Tag-Pair has a non-pair " +
                    "TRVE capability result | " +
                    $"idea={idea.SourceIdeaId}"
                );
            }

            DemoTapeTagOccurrenceSnapshot dominant =
                FindDominantOccurrence(
                    idea
                );

            int recordedDominantDegree =
                (int)dominant.Degree;

            if (capability.DominantDegree !=
                recordedDominantDegree)
            {
                throw new InvalidOperationException(
                    "TRVE capability dominant degree " +
                    "does not match recorded semantics | " +
                    $"idea={idea.SourceIdeaId} | " +
                    $"recorded={recordedDominantDegree} | " +
                    $"capability={capability.DominantDegree}"
                );
            }

            if (activeDegree >
                recordedDominantDegree)
            {
                throw new InvalidOperationException(
                    "Legitimate activation exceeds " +
                    "recorded dominant degree | " +
                    $"idea={idea.SourceIdeaId} | " +
                    $"recorded={recordedDominantDegree} | " +
                    $"activation={activeDegree}"
                );
            }

            float activationFraction =
                recordedDominantDegree > 0
                    ? (float)activeDegree /
                      recordedDominantDegree
                    : 0f;

            float activeContribution =
                capability.IsTrveCapable &&
                activeDegree > 0
                    ? capability
                          .ContextualTrvePotential *
                      activationFraction
                    : 0f;

            return new IdeaTrveEvaluation(
                idea.SourceIdeaId,
                idea.IdeaIndex,
                true,
                capability.PairIntegrity,
                capability.IsTrveCapable,
                recordedDominantDegree,
                capability.SocietyBacking,
                capability.ContextualTrvePotential,
                activeDegree,
                activationFraction,
                activeContribution
            );
        }

        private static void ValidateTrackIdentity(
            DemoTapeTrackSnapshot track,
            TrackTrveCapabilityEvaluation capability,
            TrackActivationEvaluationSnapshot activation)
        {
            if (capability.SourceTrackId !=
                track.SourceTrackId)
            {
                throw new ArgumentException(
                    "TRVE capability evaluation refers " +
                    "to a different Track.",
                    nameof(capability)
                );
            }

            if (activation.SourceTrackId !=
                track.SourceTrackId)
            {
                throw new ArgumentException(
                    "Activation evaluation refers " +
                    "to a different Track.",
                    nameof(activation)
                );
            }

            if (capability.IdeaEvaluations.Count !=
                track.IdeaSnapshots.Count)
            {
                throw new ArgumentException(
                    "TRVE capability evaluation contains " +
                    "a different number of Ideas.",
                    nameof(capability)
                );
            }
        }

        private static void ValidateIdeaIdentity(
            DemoTapeIdeaSnapshot idea,
            IdeaTrveCapabilityEvaluation capability)
        {
            if (idea.SourceIdeaId !=
                capability.SourceIdeaId ||
                idea.IdeaIndex !=
                capability.IdeaIndex)
            {
                throw new ArgumentException(
                    "TRVE capability Idea identity/order " +
                    "does not match recorded Track.",
                    nameof(capability)
                );
            }
        }

        private static void ValidateActivationReferences(
            DemoTapeTrackSnapshot track,
            TrackActivationEvaluationSnapshot activation)
        {
            HashSet<string> trackIdeaIds =
                new();

            foreach (DemoTapeIdeaSnapshot idea
                     in track.IdeaSnapshots)
            {
                trackIdeaIds.Add(
                    idea.SourceIdeaId
                );
            }

            foreach (IdeaActivationEvaluationSnapshot
                     ideaActivation
                     in activation.IdeaActivations)
            {
                if (!trackIdeaIds.Contains(
                        ideaActivation.SourceIdeaId))
                {
                    throw new ArgumentException(
                        "Activation snapshot references " +
                        "an Idea that does not belong " +
                        "to the Track | " +
                        $"idea={ideaActivation.SourceIdeaId}",
                        nameof(activation)
                    );
                }
            }
        }

        private static
            DemoTapeTagOccurrenceSnapshot
            FindDominantOccurrence(
                DemoTapeIdeaSnapshot idea)
        {
            foreach (DemoTapeTagOccurrenceSnapshot
                     occurrence
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
                "Formal Tag-Pair snapshot has no " +
                "dominant occurrence | " +
                $"idea={idea.SourceIdeaId}"
            );
        }
    }
}