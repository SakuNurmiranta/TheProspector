using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    /// <summary>
    /// Immutable evaluation-time activation view for one
    /// Track on one specific SceneRelease.
    ///
    /// Missing Idea activation means effective A = 0.
    /// </summary>
    public sealed class TrackActivationEvaluationSnapshot
    {
        private readonly
            IdeaActivationEvaluationSnapshot[]
                ideaActivations;

        private readonly Dictionary<
            string,
            IdeaActivationEvaluationSnapshot>
                activationByIdeaId =
                    new();

        public string SourceReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceTrackId { get; }

        public IReadOnlyList<
            IdeaActivationEvaluationSnapshot>
            IdeaActivations =>
                ideaActivations;

        public TrackActivationEvaluationSnapshot(
            string sourceReleaseId,
            string sourceDemoTapeId,
            string sourceTrackId,
            IReadOnlyList<
                IdeaActivationEvaluationSnapshot>
                sourceIdeaActivations)
        {
            SourceReleaseId =
                RequireText(
                    sourceReleaseId,
                    nameof(sourceReleaseId)
                );

            SourceDemoTapeId =
                RequireText(
                    sourceDemoTapeId,
                    nameof(sourceDemoTapeId)
                );

            SourceTrackId =
                RequireText(
                    sourceTrackId,
                    nameof(sourceTrackId)
                );

            if (sourceIdeaActivations == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceIdeaActivations)
                );
            }

            ideaActivations =
                new IdeaActivationEvaluationSnapshot[
                    sourceIdeaActivations.Count
                ];

            for (int index = 0;
                 index < sourceIdeaActivations.Count;
                 index++)
            {
                IdeaActivationEvaluationSnapshot activation =
                    sourceIdeaActivations[index] ??
                    throw new ArgumentException(
                        "Track activation snapshot " +
                        "cannot contain a null Idea activation.",
                        nameof(sourceIdeaActivations)
                    );

                if (activationByIdeaId.ContainsKey(
                        activation.SourceIdeaId))
                {
                    throw new ArgumentException(
                        "Track activation snapshot contains " +
                        "duplicate Idea activation | " +
                        $"idea={activation.SourceIdeaId}",
                        nameof(sourceIdeaActivations)
                    );
                }

                ideaActivations[index] =
                    activation;

                activationByIdeaId.Add(
                    activation.SourceIdeaId,
                    activation
                );
            }
        }

        public int GetLegitimateActiveDegree(
            string sourceIdeaId)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceIdeaId))
            {
                throw new ArgumentException(
                    "Activation lookup requires " +
                    "a source Idea ID.",
                    nameof(sourceIdeaId)
                );
            }

            if (activationByIdeaId.TryGetValue(
                    sourceIdeaId,
                    out IdeaActivationEvaluationSnapshot
                        activation))
            {
                return activation
                    .LegitimateActiveDegree;
            }

            return 0;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Activation provenance cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}