using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Score
{
    /// <summary>
    /// Immutable completed-year influence result for
    /// one competitive beneficiary in one scene.
    ///
    /// YearInfluence is deliberately narrower than
    /// lifetime Score. Only influence-bearing Gravity
    /// events participate.
    /// </summary>
    public sealed class YearInfluenceEvaluation
    {
        private readonly
            ScoreEvent[]
            contributingEvents;

        public string BeneficiaryEntityId { get; }

        public string SceneId { get; }

        public int FirstTurnInclusive { get; }

        public int LastTurnInclusive { get; }

        public IReadOnlyList<ScoreEvent>
            ContributingEvents =>
            contributingEvents;

        public float YearInfluence { get; }

        public YearInfluenceEvaluation(
            string beneficiaryEntityId,
            string sceneId,
            int firstTurnInclusive,
            int lastTurnInclusive,
            IReadOnlyList<ScoreEvent>
                sourceContributingEvents)
        {
            BeneficiaryEntityId =
                RequireText(
                    beneficiaryEntityId,
                    nameof(beneficiaryEntityId)
                );

            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (firstTurnInclusive < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(firstTurnInclusive)
                );
            }

            if (lastTurnInclusive <
                firstTurnInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lastTurnInclusive)
                );
            }

            if (sourceContributingEvents == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceContributingEvents)
                );
            }

            FirstTurnInclusive =
                firstTurnInclusive;

            LastTurnInclusive =
                lastTurnInclusive;

            contributingEvents =
                new ScoreEvent[
                    sourceContributingEvents.Count
                ];

            double total =
                0d;

            for (int index = 0;
                 index <
                 sourceContributingEvents.Count;
                 index++)
            {
                ScoreEvent scoreEvent =
                    sourceContributingEvents[index] ??
                    throw new ArgumentException(
                        "YearInfluence cannot contain " +
                        "a null Score event.",
                        nameof(
                            sourceContributingEvents
                        )
                    );

                if (scoreEvent.BeneficiaryEntityId !=
                    BeneficiaryEntityId)
                {
                    throw new ArgumentException(
                        "YearInfluence Score event " +
                        "belongs to another beneficiary.",
                        nameof(
                            sourceContributingEvents
                        )
                    );
                }

                if (scoreEvent.SceneId !=
                    SceneId)
                {
                    throw new ArgumentException(
                        "YearInfluence Score event " +
                        "belongs to another scene.",
                        nameof(
                            sourceContributingEvents
                        )
                    );
                }

                if (scoreEvent.GlobalTurn <
                        FirstTurnInclusive ||
                    scoreEvent.GlobalTurn >
                        LastTurnInclusive)
                {
                    throw new ArgumentException(
                        "YearInfluence Score event lies " +
                        "outside the completed year.",
                        nameof(
                            sourceContributingEvents
                        )
                    );
                }

                if (!YearInfluenceEvaluator
                        .IsInfluenceBearing(
                            scoreEvent.Kind
                        ))
                {
                    throw new ArgumentException(
                        "YearInfluence contains a " +
                        "non-influence-bearing Score " +
                        "event.",
                        nameof(
                            sourceContributingEvents
                        )
                    );
                }

                contributingEvents[index] =
                    scoreEvent;

                total +=
                    scoreEvent.Amount;
            }

            YearInfluence =
                (float)total;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "YearInfluence identity cannot " +
                    "be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}