using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackSocietyBackingEvaluation
    {
        private readonly
            IdeaSocietyBackingEvaluation[]
            ideaEvaluations;

        public string SourceTrackId { get; }

        public IReadOnlyList<
                IdeaSocietyBackingEvaluation>
            IdeaEvaluations =>
            ideaEvaluations;

        public TrackSocietyBackingEvaluation(
            string sourceTrackId,
            IReadOnlyList<
                    IdeaSocietyBackingEvaluation>
                ideaEvaluations)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceTrackId))
            {
                throw new ArgumentException(
                    "Society backing evaluation " +
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
                new IdeaSocietyBackingEvaluation[
                    ideaEvaluations.Count
                ];

            for (int index = 0;
                 index < ideaEvaluations.Count;
                 index++)
            {
                this.ideaEvaluations[index] =
                    ideaEvaluations[index] ??
                    throw new ArgumentException(
                        "Society backing evaluation " +
                        "cannot contain a null Idea result.",
                        nameof(ideaEvaluations)
                    );
            }
        }
    }
}