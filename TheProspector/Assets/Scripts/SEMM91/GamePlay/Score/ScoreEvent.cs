using System;

namespace SEMM91.GamePlay.Score
{
    /// <summary>
    /// Immutable provenance-bearing competitive
    /// Score event.
    ///
    /// Score records competitive benefit only.
    /// It does not participate in scene physics.
    /// </summary>
    public sealed class ScoreEvent
    {
        public string ScoreEventId { get; }

        public string BeneficiaryEntityId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public string SceneReleaseId { get; }

        public string DemoTapeId { get; }

        public ScoreEventKind Kind { get; }

        public float Amount { get; }

        public int GlobalTurn { get; }

        public ScoreEvent(
            string scoreEventId,
            string beneficiaryEntityId,
            string sourceOwnerEntityId,
            string sceneId,
            string sceneReleaseId,
            string demoTapeId,
            ScoreEventKind kind,
            float amount,
            int globalTurn)
        {
            ScoreEventId =
                RequireText(
                    scoreEventId,
                    nameof(scoreEventId)
                );

            BeneficiaryEntityId =
                RequireText(
                    beneficiaryEntityId,
                    nameof(beneficiaryEntityId)
                );

            SourceOwnerEntityId =
                RequireText(
                    sourceOwnerEntityId,
                    nameof(sourceOwnerEntityId)
                );

            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            SceneReleaseId =
                RequireText(
                    sceneReleaseId,
                    nameof(sceneReleaseId)
                );

            DemoTapeId =
                RequireText(
                    demoTapeId,
                    nameof(demoTapeId)
                );

            if (!Enum.IsDefined(
                    typeof(ScoreEventKind),
                    kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind)
                );
            }

            if (float.IsNaN(amount) ||
                float.IsInfinity(amount) ||
                amount < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "Peak-2 Score event amount must " +
                    "be finite and non-negative."
                );
            }

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn),
                    globalTurn,
                    "Score event turn cannot be negative."
                );
            }

            /*
             * Ordinary creator scoring always belongs
             * to the release owner.
             *
             * Hole Mandate Gravity is the distinct
             * institutional Keeper credit. If creator
             * and Keeper are the same entity, the Bible
             * says Gravity scores only once, through the
             * ordinary FieldGravity event.
             */
            switch (kind)
            {
                case ScoreEventKind.FirstFetterResonance:
                case ScoreEventKind.FieldGravity:
                case ScoreEventKind.CanonRetainedGravity:

                    if (BeneficiaryEntityId !=
                        SourceOwnerEntityId)
                    {
                        throw new ArgumentException(
                            "Ordinary release Score must " +
                            "benefit the release owner.",
                            nameof(beneficiaryEntityId)
                        );
                    }

                    break;

                case ScoreEventKind.HoleMandateGravity:

                    if (BeneficiaryEntityId ==
                        SourceOwnerEntityId)
                    {
                        throw new ArgumentException(
                            "Hole Mandate institutional " +
                            "credit exists only for a " +
                            "distinct Keeper."
                        );
                    }

                    break;
            }

            Kind =
                kind;

            Amount =
                amount;

            GlobalTurn =
                globalTurn;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Score provenance cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}