using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackResonanceEvaluator
    {
        public TrackResonanceEvaluation Evaluate(
            TrackSurfaceExtremityEvaluation
                surfaceExtremity)
        {
            if (surfaceExtremity == null)
            {
                throw new ArgumentNullException(
                    nameof(surfaceExtremity)
                );
            }

            List<IdeaResonanceEvaluation>
                ideaEvaluations =
                    new();

            float signedTotal =
                0f;

            foreach (IdeaSurfaceExtremityEvaluation
                         idea
                     in surfaceExtremity
                         .IdeaEvaluations)
            {
                float extremityFactor =
                    idea.Extremity / 3f;

                float signedContribution =
                    idea.SurfaceContribution *
                    extremityFactor;

                IdeaResonanceEvaluation evaluation =
                    new IdeaResonanceEvaluation(
                        idea.SourceIdeaId,
                        idea.IdeaIndex,
                        idea.SurfaceContribution,
                        idea.Extremity,
                        extremityFactor,
                        signedContribution
                    );

                ideaEvaluations.Add(
                    evaluation
                );

                signedTotal +=
                    signedContribution;
            }

            float signedResonance =
                ideaEvaluations.Count == 0
                    ? 0f
                    : signedTotal /
                      ideaEvaluations.Count;

            return new TrackResonanceEvaluation(
                surfaceExtremity.SourceTrackId,
                signedResonance,
                ideaEvaluations
            );
        }
    }
}