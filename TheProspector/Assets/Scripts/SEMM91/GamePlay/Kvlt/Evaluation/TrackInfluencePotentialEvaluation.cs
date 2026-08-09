using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackInfluencePotentialEvaluation
    {
        private readonly
            IdeaInfluencePotentialEvaluation[]
                ideaEvaluations;

        public string SourceTrackId { get; }

        public float CanonSignedResonance { get; }

        public float CanonResonance { get; }

        public float MaximumTrve { get; }

        public float MaximumTrveFraction { get; }

        public float SignedInfluencePotential { get; }

        public float InfluencePotential { get; }

        public IReadOnlyList<
            IdeaInfluencePotentialEvaluation>
            IdeaEvaluations =>
                ideaEvaluations;

        public TrackInfluencePotentialEvaluation(
            string sourceTrackId,
            float canonSignedResonance,
            float maximumTrve,
            float signedInfluencePotential,
            IReadOnlyList<
                IdeaInfluencePotentialEvaluation>
                ideaEvaluations)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceTrackId))
            {
                throw new ArgumentException(
                    "Influence Potential evaluation " +
                    "requires a source Track ID.",
                    nameof(sourceTrackId)
                );
            }

            if (maximumTrve < 0f ||
                maximumTrve > 6f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumTrve),
                    maximumTrve,
                    "Maximum Track TRVE must be " +
                    "in range 0..6."
                );
            }

            if (ideaEvaluations == null)
            {
                throw new ArgumentNullException(
                    nameof(ideaEvaluations)
                );
            }

            SourceTrackId =
                sourceTrackId.Trim();

            CanonSignedResonance =
                canonSignedResonance;

            CanonResonance =
                Math.Abs(
                    canonSignedResonance
                );

            MaximumTrve =
                maximumTrve;

            MaximumTrveFraction =
                maximumTrve / 6f;

            SignedInfluencePotential =
                signedInfluencePotential;

            InfluencePotential =
                Math.Abs(
                    signedInfluencePotential
                );

            this.ideaEvaluations =
                new IdeaInfluencePotentialEvaluation[
                    ideaEvaluations.Count
                ];

            for (int index = 0;
                 index < ideaEvaluations.Count;
                 index++)
            {
                this.ideaEvaluations[index] =
                    ideaEvaluations[index] ??
                    throw new ArgumentException(
                        "Influence Potential evaluation " +
                        "cannot contain a null Idea result.",
                        nameof(ideaEvaluations)
                    );
            }
        }
    }
}