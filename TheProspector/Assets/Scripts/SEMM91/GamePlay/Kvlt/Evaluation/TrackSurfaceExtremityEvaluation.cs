using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackSurfaceExtremityEvaluation
    {
        private readonly
            IdeaSurfaceExtremityEvaluation[]
                ideaEvaluations;

        public string SourceTrackId { get; }

        public float Surface { get; }

        public float Extremity { get; }

        public IReadOnlyList<
            IdeaSurfaceExtremityEvaluation>
            IdeaEvaluations =>
                ideaEvaluations;

        public TrackSurfaceExtremityEvaluation(
            string sourceTrackId,
            float surface,
            float extremity,
            IReadOnlyList<
                IdeaSurfaceExtremityEvaluation>
                ideaEvaluations)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceTrackId))
            {
                throw new ArgumentException(
                    "Track evaluation requires a source Track ID.",
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

            Surface = surface;
            Extremity = extremity;

            this.ideaEvaluations =
                new IdeaSurfaceExtremityEvaluation[
                    ideaEvaluations.Count
                ];

            for (int index = 0;
                 index < ideaEvaluations.Count;
                 index++)
            {
                this.ideaEvaluations[index] =
                    ideaEvaluations[index] ??
                    throw new ArgumentException(
                        "Track evaluation cannot contain " +
                        "a null Idea evaluation.",
                        nameof(ideaEvaluations)
                    );
            }
        }
    }
}