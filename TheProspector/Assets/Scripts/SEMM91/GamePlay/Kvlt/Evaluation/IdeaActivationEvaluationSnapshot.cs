using System;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    /// <summary>
    /// Frozen evaluation-time view of the current
    /// legitimate activation attached to one recorded Idea
    /// on one SceneRelease.
    ///
    /// This is not activation history.
    /// Pending activation is deliberately excluded because
    /// it contributes no current TRVE.
    /// </summary>
    public sealed class IdeaActivationEvaluationSnapshot
    {
        public string SourceIdeaId { get; }

        /// <summary>
        /// Current legitimately active degree A.
        ///
        /// Range 0..3.
        /// Zero means no currently effective activation
        /// for TRVE evaluation.
        /// </summary>
        public int LegitimateActiveDegree { get; }

        public bool HasLegitimateActivation =>
            LegitimateActiveDegree > 0;

        public IdeaActivationEvaluationSnapshot(
            string sourceIdeaId,
            int legitimateActiveDegree)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceIdeaId))
            {
                throw new ArgumentException(
                    "Activation snapshot requires " +
                    "a source Idea ID.",
                    nameof(sourceIdeaId)
                );
            }

            if (legitimateActiveDegree < 0 ||
                legitimateActiveDegree > 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(legitimateActiveDegree),
                    legitimateActiveDegree,
                    "Legitimate activation degree " +
                    "must be in range 0..3."
                );
            }

            SourceIdeaId =
                sourceIdeaId.Trim();

            LegitimateActiveDegree =
                legitimateActiveDegree;
        }
    }
}