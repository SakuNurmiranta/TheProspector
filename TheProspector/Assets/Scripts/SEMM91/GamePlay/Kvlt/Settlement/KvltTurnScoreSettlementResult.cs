using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Score;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Immutable record of Score events produced by
    /// one TurnGravityScoreSettlement phase.
    ///
    /// The authoritative ScoreLedger remains the
    /// persistent history. This object only identifies
    /// the events appended by this phase invocation.
    /// </summary>
    public sealed class
        KvltTurnScoreSettlementResult
    {
        private readonly
            ScoreEvent[]
            scoreEvents;

        public string SceneId { get; }

        public int GlobalTurn { get; }

        public IReadOnlyList<ScoreEvent>
            ScoreEvents =>
            scoreEvents;

        public int EventCount =>
            scoreEvents.Length;

        public KvltTurnScoreSettlementResult(
            string sceneId,
            int globalTurn,
            IReadOnlyList<ScoreEvent>
                sourceScoreEvents)
        {
            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            if (sourceScoreEvents == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceScoreEvents)
                );
            }

            GlobalTurn =
                globalTurn;

            scoreEvents =
                new ScoreEvent[
                    sourceScoreEvents.Count
                ];

            for (int index = 0;
                 index < sourceScoreEvents.Count;
                 index++)
            {
                ScoreEvent scoreEvent =
                    sourceScoreEvents[index] ??
                    throw new ArgumentException(
                        "Turn Score result cannot " +
                        "contain null events.",
                        nameof(sourceScoreEvents)
                    );

                if (scoreEvent.GlobalTurn !=
                    globalTurn)
                {
                    throw new ArgumentException(
                        "Turn Score event belongs to " +
                        "another turn.",
                        nameof(sourceScoreEvents)
                    );
                }

                if (scoreEvent.SceneId !=
                    SceneId)
                {
                    throw new ArgumentException(
                        "Turn Score event belongs to " +
                        "another scene.",
                        nameof(sourceScoreEvents)
                    );
                }

                scoreEvents[index] =
                    scoreEvent;
            }
        }

        public int CountOf(
            ScoreEventKind kind)
        {
            if (!Enum.IsDefined(
                    typeof(ScoreEventKind),
                    kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind)
                );
            }

            int count =
                0;

            foreach (
                ScoreEvent scoreEvent
                in scoreEvents)
            {
                if (scoreEvent.Kind ==
                    kind)
                {
                    count++;
                }
            }

            return count;
        }

        public float AmountOf(
            ScoreEventKind kind)
        {
            if (!Enum.IsDefined(
                    typeof(ScoreEventKind),
                    kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind)
                );
            }

            double total =
                0d;

            foreach (
                ScoreEvent scoreEvent
                in scoreEvents)
            {
                if (scoreEvent.Kind ==
                    kind)
                {
                    total +=
                        scoreEvent.Amount;
                }
            }

            return (float)total;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Turn Score identity cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}