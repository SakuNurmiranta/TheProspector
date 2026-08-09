using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackGravityEvaluator
    {
        public TrackGravityEvaluation Evaluate(
            TrackResonanceEvaluation resonance,
            TrackTrveEvaluation trve)
        {
            if (resonance == null)
            {
                throw new ArgumentNullException(
                    nameof(resonance)
                );
            }

            if (trve == null)
            {
                throw new ArgumentNullException(
                    nameof(trve)
                );
            }

            if (resonance.SourceTrackId !=
                trve.SourceTrackId)
            {
                throw new ArgumentException(
                    "Resonance and TRVE evaluations " +
                    "must refer to the same Track.",
                    nameof(trve)
                );
            }

            if (resonance.IdeaEvaluations.Count !=
                trve.IdeaEvaluations.Count)
            {
                throw new ArgumentException(
                    "Resonance and TRVE evaluations " +
                    "contain different Idea counts.",
                    nameof(trve)
                );
            }

            if (trve.Trve < 0f ||
                trve.Trve > 6f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(trve),
                    trve.Trve,
                    "Track TRVE must be in range 0..6."
                );
            }

            float trveFraction =
                trve.Trve / 6f;

            List<IdeaGravityEvaluation>
                ideaEvaluations =
                    new();

            for (int index = 0;
                 index <
                 resonance.IdeaEvaluations.Count;
                 index++)
            {
                IdeaResonanceEvaluation resonanceIdea =
                    resonance.IdeaEvaluations[index];

                IdeaTrveEvaluation trveIdea =
                    trve.IdeaEvaluations[index];

                if (resonanceIdea.SourceIdeaId !=
                        trveIdea.SourceIdeaId ||
                    resonanceIdea.IdeaIndex !=
                        trveIdea.IdeaIndex)
                {
                    throw new ArgumentException(
                        "Resonance and TRVE Idea " +
                        "identity/order does not match.",
                        nameof(trve)
                    );
                }

                float signedContribution =
                    resonanceIdea
                        .SignedResonanceContribution *
                    trveFraction;

                ideaEvaluations.Add(
                    new IdeaGravityEvaluation(
                        resonanceIdea.SourceIdeaId,
                        resonanceIdea.IdeaIndex,
                        resonanceIdea
                            .SignedResonanceContribution,
                        trveFraction,
                        signedContribution
                    )
                );
            }

            float signedGravity =
                resonance.SignedResonance *
                trveFraction;

            return new TrackGravityEvaluation(
                trve.SourceReleaseId,
                trve.SourceDemoTapeId,
                trve.SourceTrackId,
                trveFraction,
                signedGravity,
                ideaEvaluations
            );
        }
    }
}