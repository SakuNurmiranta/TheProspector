using System;

namespace SEMM91.GamePlay.Events
{
    public sealed class HappeningIntentResolution
    {
        public string IntentId { get; }

        public HappeningIntentOutcome Outcome { get; }

        public int ResolvedTurn { get; }

        public string OpposingIntentId { get; }

        public bool WasBlocked =>
            Outcome ==
            HappeningIntentOutcome.Blocked;

        public HappeningIntentResolution(
            string intentId,
            HappeningIntentOutcome outcome,
            int resolvedTurn,
            string opposingIntentId = null)
        {
            if (string.IsNullOrWhiteSpace(
                    intentId))
            {
                throw new ArgumentException(
                    "Intent resolution requires " +
                    "an Intent ID.",
                    nameof(intentId)
                );
            }

            if (!Enum.IsDefined(
                    typeof(HappeningIntentOutcome),
                    outcome))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(outcome)
                );
            }

            if (resolvedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resolvedTurn)
                );
            }

            bool hasOpposition =
                !string.IsNullOrWhiteSpace(
                    opposingIntentId
                );

            if (outcome ==
                    HappeningIntentOutcome.Blocked &&
                !hasOpposition)
            {
                throw new ArgumentException(
                    "A blocked Intent must identify " +
                    "the opposing Intent that blocked it.",
                    nameof(opposingIntentId)
                );
            }

            if (outcome !=
                    HappeningIntentOutcome.Blocked &&
                hasOpposition)
            {
                throw new ArgumentException(
                    "Only a blocked Intent records " +
                    "an opposing Intent in its resolution.",
                    nameof(opposingIntentId)
                );
            }

            IntentId =
                intentId.Trim();

            Outcome =
                outcome;

            ResolvedTurn =
                resolvedTurn;

            OpposingIntentId =
                hasOpposition
                    ? opposingIntentId.Trim()
                    : null;
        }
    }
}