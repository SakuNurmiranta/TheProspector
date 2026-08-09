using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Kvlt.Normative;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackSurfaceExtremityEvaluator
    {
        public TrackSurfaceExtremityEvaluation Evaluate(
            DemoTapeTrackSnapshot track,
            TrackEvaluationEnvironment environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(
                    nameof(environment)
                );
            }

            return EvaluateAgainstNormativeCentre(
                track,
                environment.CurrentNormativeCentre
            );
        }

        public TrackSurfaceExtremityEvaluation
            EvaluateAgainstNormativeCentre(
                DemoTapeTrackSnapshot track,
                NormativeCentre normativeCentre)
        {
            if (track == null)
            {
                throw new ArgumentNullException(
                    nameof(track)
                );
            }

            if (normativeCentre == null)
            {
                throw new ArgumentNullException(
                    nameof(normativeCentre)
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
                            normativeCentre
                        );

                ideaEvaluations.Add(
                    ideaEvaluation
                );

                surfaceTotal +=
                    ideaEvaluation.SurfaceContribution;

                extremityTotal +=
                    ideaEvaluation.Extremity;
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
                NormativeCentre normativeCentre)
        {
            return idea.PayloadType switch
            {
                IdeaPayloadType.SingleTag =>
                    EvaluateSingleTagIdea(
                        idea,
                        normativeCentre
                    ),

                IdeaPayloadType.TagPair =>
                    EvaluateTagPairIdea(
                        idea,
                        normativeCentre
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
                NormativeCentre normativeCentre)
        {
            DemoTapeTagOccurrenceSnapshot tag =
                idea.TagOccurrences[0];

            float degree =
                (float)(int)tag.Degree;

            float affinity =
                normativeCentre.GetAffinity(
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
                NormativeCentre normativeCentre)
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
                normativeCentre.GetAffinity(
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