using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackSurfaceExtremityEvaluator
    {
        public TrackSurfaceExtremityEvaluation Evaluate(
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

            List<IdeaSurfaceExtremityEvaluation>
                ideaEvaluations =
                    new();

            float surfaceTotal = 0f;
            float extremityTotal = 0f;

            foreach (DemoTapeIdeaSnapshot idea
                     in track.IdeaSnapshots)
            {
                IdeaSurfaceExtremityEvaluation
                    ideaEvaluation =
                        EvaluateIdea(
                            idea,
                            environment
                        );

                ideaEvaluations.Add(
                    ideaEvaluation
                );

                surfaceTotal +=
                    ideaEvaluation
                        .SurfaceContribution;

                extremityTotal +=
                    ideaEvaluation
                        .Extremity;
            }

            if (ideaEvaluations.Count == 0)
            {
                return new TrackSurfaceExtremityEvaluation(
                    track.SourceTrackId,
                    0f,
                    0f,
                    ideaEvaluations
                );
            }

            float count =
                ideaEvaluations.Count;

            return new TrackSurfaceExtremityEvaluation(
                track.SourceTrackId,
                surfaceTotal / count,
                extremityTotal / count,
                ideaEvaluations
            );
        }

        private static
            IdeaSurfaceExtremityEvaluation
            EvaluateIdea(
                DemoTapeIdeaSnapshot idea,
                TrackEvaluationEnvironment environment)
        {
            return idea.PayloadType switch
            {
                IdeaPayloadType.SingleTag =>
                    EvaluateSingleTagIdea(
                        idea,
                        environment
                    ),

                IdeaPayloadType.TagPair =>
                    EvaluateTagPairIdea(
                        idea,
                        environment
                    ),

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(idea.PayloadType),
                        idea.PayloadType,
                        "Unsupported Idea payload type."
                    )
            };
        }

        private static
            IdeaSurfaceExtremityEvaluation
            EvaluateSingleTagIdea(
                DemoTapeIdeaSnapshot idea,
                TrackEvaluationEnvironment environment)
        {
            DemoTapeTagOccurrenceSnapshot tag =
                idea.TagOccurrences[0];

            float degree =
                (float)(int)tag.Degree;

            float affinity =
                environment.CurrentNormativeCentre
                    .GetAffinity(
                        tag.Axis,
                        tag.Pole,
                        tag.Degree
                    );

            return new IdeaSurfaceExtremityEvaluation(
                idea.SourceIdeaId,
                idea.IdeaIndex,
                degree * affinity,
                degree
            );
        }

        private static
            IdeaSurfaceExtremityEvaluation
            EvaluateTagPairIdea(
                DemoTapeIdeaSnapshot idea,
                TrackEvaluationEnvironment environment)
        {
            DemoTapeTagOccurrenceSnapshot dominant =
                idea.TagOccurrences[0];

            DemoTapeTagOccurrenceSnapshot submissive =
                idea.TagOccurrences[1];

            float dominantDegree =
                (float)(int)dominant.Degree;

            float submissiveDegree =
                (float)(int)submissive.Degree;

            float dominantAffinity =
                environment.CurrentNormativeCentre
                    .GetAffinity(
                        dominant.Axis,
                        dominant.Pole,
                        dominant.Degree
                    );

            float surface =
                dominantDegree *
                dominantAffinity;

            float extremity =
                (
                    dominantDegree +
                    submissiveDegree
                ) / 2f;

            return new IdeaSurfaceExtremityEvaluation(
                idea.SourceIdeaId,
                idea.IdeaIndex,
                surface,
                extremity
            );
        }
    }
}