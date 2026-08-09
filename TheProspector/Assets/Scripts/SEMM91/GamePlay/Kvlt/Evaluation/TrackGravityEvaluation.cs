using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackGravityEvaluation
    {
        private readonly
            IdeaGravityEvaluation[]
                ideaEvaluations;

        public string SourceReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceTrackId { get; }

        public float TrveFraction { get; }

        public float SignedGravity { get; }

        public float Gravity { get; }

        public IReadOnlyList<
            IdeaGravityEvaluation>
            IdeaEvaluations =>
                ideaEvaluations;

        public TrackGravityEvaluation(
            string sourceReleaseId,
            string sourceDemoTapeId,
            string sourceTrackId,
            float trveFraction,
            float signedGravity,
            IReadOnlyList<
                IdeaGravityEvaluation>
                ideaEvaluations)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceReleaseId))
            {
                throw new ArgumentException(
                    "Gravity evaluation requires " +
                    "a source Release ID.",
                    nameof(sourceReleaseId)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    sourceDemoTapeId))
            {
                throw new ArgumentException(
                    "Gravity evaluation requires " +
                    "a source DemoTape ID.",
                    nameof(sourceDemoTapeId)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    sourceTrackId))
            {
                throw new ArgumentException(
                    "Gravity evaluation requires " +
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

            TrveFraction =
                trveFraction;

            SignedGravity =
                signedGravity;

            Gravity =
                Math.Abs(
                    signedGravity
                );

            this.ideaEvaluations =
                new IdeaGravityEvaluation[
                    ideaEvaluations.Count
                ];

            for (int index = 0;
                 index < ideaEvaluations.Count;
                 index++)
            {
                this.ideaEvaluations[index] =
                    ideaEvaluations[index] ??
                    throw new ArgumentException(
                        "Gravity evaluation cannot " +
                        "contain a null Idea result.",
                        nameof(ideaEvaluations)
                    );
            }
        }
    }
}