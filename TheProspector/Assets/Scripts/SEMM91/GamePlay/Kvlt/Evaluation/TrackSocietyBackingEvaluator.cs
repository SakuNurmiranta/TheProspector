using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackSocietyBackingEvaluator
    {
        public TrackSocietyBackingEvaluation Evaluate(
            DemoTapeTrackSnapshot track,
            TrackEvaluationEnvironment environment)
        {
            if (track == null)
            {
                throw new ArgumentNullException(
                    nameof(track)
                );
            }

            if (environment == null)
            {
                throw new ArgumentNullException(
                    nameof(environment)
                );
            }

            List<IdeaSocietyBackingEvaluation>
                ideaEvaluations =
                    new();

            foreach (DemoTapeIdeaSnapshot idea
                     in track.IdeaSnapshots)
            {
                ideaEvaluations.Add(
                    EvaluateIdea(
                        idea,
                        environment
                    )
                );
            }

            return new TrackSocietyBackingEvaluation(
                track.SourceTrackId,
                ideaEvaluations
            );
        }

        private static
            IdeaSocietyBackingEvaluation
            EvaluateIdea(
                DemoTapeIdeaSnapshot idea,
                TrackEvaluationEnvironment environment)
        {
            if (idea.PayloadType ==
                IdeaPayloadType.SingleTag)
            {
                return new IdeaSocietyBackingEvaluation(
                    idea.SourceIdeaId,
                    idea.IdeaIndex,
                    false,
                    0f,
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

            DemoTapeTagOccurrenceSnapshot
                submissive =
                    FindSubmissiveOccurrence(
                        idea
                    );

            float recordedSemanticForce =
                ResolveSemanticForce(
                    submissive.Degree
                );

            float societyForce =
                environment
                    .GetSocietyNormativeForce(
                        submissive.Axis,
                        submissive.Pole
                    );

            float backing =
                Math.Min(
                    recordedSemanticForce,
                    societyForce
                );

            return new IdeaSocietyBackingEvaluation(
                idea.SourceIdeaId,
                idea.IdeaIndex,
                true,
                recordedSemanticForce,
                societyForce,
                backing
            );
        }

        private static
            DemoTapeTagOccurrenceSnapshot
            FindSubmissiveOccurrence(
                DemoTapeIdeaSnapshot idea)
        {
            foreach (DemoTapeTagOccurrenceSnapshot
                     occurrence
                     in idea.TagOccurrences)
            {
                if (occurrence.Role ==
                    DemoTapeTagOccurrenceRole
                        .PairSubmissive)
                {
                    return occurrence;
                }
            }

            throw new InvalidOperationException(
                "Formal Tag-Pair snapshot has no " +
                "submissive occurrence | " +
                $"idea={idea.SourceIdeaId}"
            );
        }

        private static float ResolveSemanticForce(
            TagDegree degree)
        {
            return degree switch
            {
                TagDegree.Neutral => 0.5f,
                TagDegree.Weak => 1f,
                TagDegree.Dominant => 2f,
                TagDegree.Transgressive => 3f,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(degree),
                        degree,
                        "Unsupported Tag degree."
                    )
            };
        }
    }
}