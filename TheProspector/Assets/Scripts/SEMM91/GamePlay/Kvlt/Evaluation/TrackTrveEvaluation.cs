using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackTrveEvaluation
    {
        private readonly
            IdeaTrveEvaluation[]
                ideaEvaluations;

        public string SourceReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceTrackId { get; }

        public float Trve { get; }

        public bool HasTrve { get; }

        public IReadOnlyList<
            IdeaTrveEvaluation>
            IdeaEvaluations =>
                ideaEvaluations;

        public TrackTrveEvaluation(
            string sourceReleaseId,
            string sourceDemoTapeId,
            string sourceTrackId,
            float trve,
            IReadOnlyList<
                IdeaTrveEvaluation>
                ideaEvaluations)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceReleaseId))
            {
                throw new ArgumentException(
                    "TRVE evaluation requires " +
                    "a source Release ID.",
                    nameof(sourceReleaseId)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    sourceDemoTapeId))
            {
                throw new ArgumentException(
                    "TRVE evaluation requires " +
                    "a source DemoTape ID.",
                    nameof(sourceDemoTapeId)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    sourceTrackId))
            {
                throw new ArgumentException(
                    "TRVE evaluation requires " +
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

            SourceReleaseId =
                sourceReleaseId.Trim();

            SourceDemoTapeId =
                sourceDemoTapeId.Trim();

            SourceTrackId =
                sourceTrackId.Trim();

            Trve =
                trve;

            this.ideaEvaluations =
                new IdeaTrveEvaluation[
                    ideaEvaluations.Count
                ];

            bool hasTrve =
                false;

            for (int index = 0;
                 index < ideaEvaluations.Count;
                 index++)
            {
                IdeaTrveEvaluation evaluation =
                    ideaEvaluations[index] ??
                    throw new ArgumentException(
                        "Track TRVE evaluation cannot " +
                        "contain a null Idea result.",
                        nameof(ideaEvaluations)
                    );

                this.ideaEvaluations[index] =
                    evaluation;

                if (evaluation
                        .ActiveTrveContribution > 0f)
                {
                    hasTrve =
                        true;
                }
            }

            HasTrve =
                hasTrve;
        }
    }
}