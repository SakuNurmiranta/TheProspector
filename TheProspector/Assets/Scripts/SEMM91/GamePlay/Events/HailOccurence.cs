using System;

namespace SEMM91.GamePlay.Events
{
    /// <summary>
    /// Durable factual declaration associated with
    /// an actually occurring Behavior.
    ///
    /// Hail identity remains separate from the
    /// physical/social Behavior itself.
    /// </summary>
    public sealed class HailOccurrence
    {
        public string HailOccurrenceId { get; }

        public string BehaviorOccurrenceId { get; }

        public string DeclarerEntityId { get; }

        public string HailedAspectId { get; }

        public HailOccurrence(
            string hailOccurrenceId,
            string behaviorOccurrenceId,
            string declarerEntityId,
            string hailedAspectId)
        {
            HailOccurrenceId =
                RequireText(
                    hailOccurrenceId,
                    nameof(hailOccurrenceId)
                );

            BehaviorOccurrenceId =
                RequireText(
                    behaviorOccurrenceId,
                    nameof(behaviorOccurrenceId)
                );

            DeclarerEntityId =
                RequireText(
                    declarerEntityId,
                    nameof(declarerEntityId)
                );

            HailedAspectId =
                RequireText(
                    hailedAspectId,
                    nameof(hailedAspectId)
                );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "HailOccurrence provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}