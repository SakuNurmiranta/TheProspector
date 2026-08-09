using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackTrveCapabilityEvaluation
    {
        private readonly
            IdeaTrveCapabilityEvaluation[]
                ideaEvaluations;

        public string SourceTrackId { get; }

        public IReadOnlyList<
            IdeaTrveCapabilityEvaluation>
            IdeaEvaluations =>
                ideaEvaluations;

        public bool HasTrveCapableIdea
        {
            get;
        }

        public TrackTrveCapabilityEvaluation(
            string sourceTrackId,
            IReadOnlyList<
                IdeaTrveCapabilityEvaluation>
                ideaEvaluations)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceTrackId))
            {
                throw new ArgumentException(
                    "TRVE capability evaluation " +
                    "requires a source Track ID.",
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

            this.ideaEvaluations =
                new IdeaTrveCapabilityEvaluation[
                    ideaEvaluations.Count
                ];

            bool hasCapableIdea = false;

            for (int index = 0;
                 index < ideaEvaluations.Count;
                 index++)
            {
                IdeaTrveCapabilityEvaluation evaluation =
                    ideaEvaluations[index] ??
                    throw new ArgumentException(
                        "TRVE capability evaluation " +
                        "cannot contain a null Idea result.",
                        nameof(ideaEvaluations)
                    );

                this.ideaEvaluations[index] =
                    evaluation;

                if (evaluation.IsTrveCapable)
                {
                    hasCapableIdea = true;
                }
            }

            HasTrveCapableIdea =
                hasCapableIdea;
        }
    }
}