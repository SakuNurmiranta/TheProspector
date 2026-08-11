using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Score
{
    /// <summary>
    /// Authoritative append-only Score history.
    ///
    /// Lifetime Score and later YearInfluence are
    /// derived from retained events rather than stored
    /// as competing mutable totals.
    /// </summary>
    public sealed class ScoreLedger
    {
        private readonly
            List<ScoreEvent>
                history =
                    new();

        private readonly
            Dictionary<string, ScoreEvent>
                eventsById =
                    new(
                        StringComparer.Ordinal
                    );

        public IReadOnlyList<ScoreEvent>
            History =>
                history;

        public int Count =>
            history.Count;

        public bool TryRecord(
            ScoreEvent scoreEvent)
        {
            if (scoreEvent == null)
            {
                return false;
            }

            if (eventsById.ContainsKey(
                    scoreEvent.ScoreEventId))
            {
                return false;
            }

            history.Add(
                scoreEvent
            );

            eventsById.Add(
                scoreEvent.ScoreEventId,
                scoreEvent
            );

            return true;
        }

        public bool TryGetById(
            string scoreEventId,
            out ScoreEvent scoreEvent)
        {
            if (string.IsNullOrWhiteSpace(
                    scoreEventId))
            {
                scoreEvent = null;
                return false;
            }

            return eventsById.TryGetValue(
                scoreEventId,
                out scoreEvent
            );
        }

        public float GetLifetimeTotal(
            string beneficiaryEntityId)
        {
            RequireEntityId(
                beneficiaryEntityId
            );

            double total =
                0d;

            foreach (
                ScoreEvent scoreEvent
                in history)
            {
                if (scoreEvent.BeneficiaryEntityId !=
                    beneficiaryEntityId)
                {
                    continue;
                }

                total +=
                    scoreEvent.Amount;
            }

            return (float)total;
        }

        public float GetTotalDuringTurns(
            string beneficiaryEntityId,
            int firstTurnInclusive,
            int lastTurnInclusive)
        {
            RequireEntityId(
                beneficiaryEntityId
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
                    nameof(lastTurnInclusive),
                    lastTurnInclusive,
                    "Last Score turn cannot precede " +
                    "the first Score turn."
                );
            }

            double total =
                0d;

            foreach (
                ScoreEvent scoreEvent
                in history)
            {
                if (scoreEvent.BeneficiaryEntityId !=
                    beneficiaryEntityId)
                {
                    continue;
                }

                if (scoreEvent.GlobalTurn <
                        firstTurnInclusive ||
                    scoreEvent.GlobalTurn >
                        lastTurnInclusive)
                {
                    continue;
                }

                total +=
                    scoreEvent.Amount;
            }

            return (float)total;
        }

        public IReadOnlyList<ScoreEvent>
            GetForBeneficiary(
                string beneficiaryEntityId)
        {
            RequireEntityId(
                beneficiaryEntityId
            );

            List<ScoreEvent> matches =
                new();

            foreach (
                ScoreEvent scoreEvent
                in history)
            {
                if (scoreEvent.BeneficiaryEntityId ==
                    beneficiaryEntityId)
                {
                    matches.Add(
                        scoreEvent
                    );
                }
            }

            return matches.ToArray();
        }

        public IReadOnlyList<ScoreEvent>
            GetForSceneRelease(
                string sceneReleaseId)
        {
            RequireText(
                sceneReleaseId,
                nameof(sceneReleaseId)
            );

            List<ScoreEvent> matches =
                new();

            foreach (
                ScoreEvent scoreEvent
                in history)
            {
                if (scoreEvent.SceneReleaseId ==
                    sceneReleaseId)
                {
                    matches.Add(
                        scoreEvent
                    );
                }
            }

            return matches.ToArray();
        }

        public IReadOnlyList<ScoreEvent>
            GetForDemoTapeInScene(
                string demoTapeId,
                string sceneId)
        {
            RequireText(
                demoTapeId,
                nameof(demoTapeId)
            );

            RequireText(
                sceneId,
                nameof(sceneId)
            );

            List<ScoreEvent> matches =
                new();

            foreach (
                ScoreEvent scoreEvent
                in history)
            {
                if (scoreEvent.DemoTapeId ==
                        demoTapeId &&
                    scoreEvent.SceneId ==
                        sceneId)
                {
                    matches.Add(
                        scoreEvent
                    );
                }
            }

            return matches.ToArray();
        }

        private static void RequireEntityId(
            string entityId)
        {
            RequireText(
                entityId,
                nameof(entityId)
            );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Score lookup identity cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}