using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackResonanceEvaluation
    {
        private readonly
            IdeaResonanceEvaluation[]
                ideaEvaluations;

        public string SourceTrackId { get; }

        public float SignedResonance { get; }

        public float Resonance { get; }

        public IReadOnlyList<
            IdeaResonanceEvaluation>
            IdeaEvaluations =>
                ideaEvaluations;

        public TrackResonanceEvaluation(
            string sourceTrackId,
            float signedResonance,
            IReadOnlyList<
                IdeaResonanceEvaluation>
                ideaEvaluations)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceTrackId))
            {
                throw new ArgumentException(
                    "Resonance evaluation requires " +
                    "a source Track ID.",
                    nameof(sourceTrackId)
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

            SignedResonance =
                signedResonance;

            Resonance =
                Math.Abs(
                    signedResonance
                );

            this.ideaEvaluations =
                new IdeaResonanceEvaluation[
                    ideaEvaluations.Count
                ];

            for (int index = 0;
                 index < ideaEvaluations.Count;
                 index++)
            {
                this.ideaEvaluations[index] =
                    ideaEvaluations[index] ??
                    throw new ArgumentException(
                        "Resonance evaluation cannot " +
                        "contain a null Idea result.",
                        nameof(ideaEvaluations)
                    );
            }
        }
    }
}