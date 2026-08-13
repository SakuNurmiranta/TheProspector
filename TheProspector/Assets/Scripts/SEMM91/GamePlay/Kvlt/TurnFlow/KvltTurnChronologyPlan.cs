using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.TurnFlow
{
    /// <summary>
    /// Immutable authoritative chronology for closing
    /// one zero-based global turn.
    ///
    /// Example with TurnsPerYear=4:
    ///
    /// 0 Spring
    /// 1 Summer
    /// 2 Fall
    /// 3 Winter / year end
    /// 4 next-year Spring
    /// </summary>
    public sealed class KvltTurnChronologyPlan
    {
        private readonly
            KvltTurnPhase[]
            phases;

        public int CompletedTurn { get; }

        public int TurnsPerYear { get; }

        /// <summary>
        /// One-based year number.
        /// </summary>
        public int YearNumber { get; }

        /// <summary>
        /// Zero-based position inside the year.
        /// </summary>
        public int TurnIndexInYear { get; }

        public bool IsYearEnd { get; }

        public int NextTurn =>
            CompletedTurn + 1;

        public int NextTurnIndexInYear =>
            NextTurn %
            TurnsPerYear;

        public int NextYearNumber =>
            (NextTurn /
             TurnsPerYear) +
            1;

        public IReadOnlyList<KvltTurnPhase>
            Phases =>
            phases;

        internal KvltTurnChronologyPlan(
            int completedTurn,
            int turnsPerYear,
            IReadOnlyList<KvltTurnPhase>
                sourcePhases)
        {
            if (completedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedTurn)
                );
            }

            if (turnsPerYear <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnsPerYear)
                );
            }

            if (sourcePhases == null ||
                sourcePhases.Count == 0)
            {
                throw new ArgumentException(
                    "Turn chronology requires at least " +
                    "one phase.",
                    nameof(sourcePhases)
                );
            }

            CompletedTurn =
                completedTurn;

            TurnsPerYear =
                turnsPerYear;

            TurnIndexInYear =
                completedTurn %
                turnsPerYear;

            YearNumber =
                (completedTurn /
                 turnsPerYear) +
                1;

            IsYearEnd =
                TurnIndexInYear ==
                turnsPerYear - 1;

            phases =
                new KvltTurnPhase[
                    sourcePhases.Count
                ];

            HashSet<KvltTurnPhase> seen =
                new();

            int previousValue =
                int.MinValue;

            for (int index = 0;
                 index < sourcePhases.Count;
                 index++)
            {
                KvltTurnPhase phase =
                    sourcePhases[index];

                if (!Enum.IsDefined(
                        typeof(KvltTurnPhase),
                        phase))
                {
                    throw new ArgumentException(
                        "Turn chronology contains an " +
                        "undefined phase.",
                        nameof(sourcePhases)
                    );
                }

                if (!seen.Add(phase))
                {
                    throw new ArgumentException(
                        "Turn chronology cannot contain " +
                        "duplicate phases.",
                        nameof(sourcePhases)
                    );
                }

                int currentValue =
                    (int)phase;

                if (currentValue <=
                    previousValue)
                {
                    throw new ArgumentException(
                        "Turn chronology phases must be " +
                        "strictly ordered.",
                        nameof(sourcePhases)
                    );
                }

                phases[index] =
                    phase;

                previousValue =
                    currentValue;
            }
        }

        public bool Includes(
            KvltTurnPhase phase)
        {
            return IndexOf(phase) >= 0;
        }

        public int IndexOf(
            KvltTurnPhase phase)
        {
            for (int index = 0;
                 index < phases.Length;
                 index++)
            {
                if (phases[index] ==
                    phase)
                {
                    return index;
                }
            }

            return -1;
        }

        public bool TryGetSeason(
            out KvltTurnSeason season)
        {
            if (TurnsPerYear != 4)
            {
                season =
                    default;

                return false;
            }

            season =
                (KvltTurnSeason)
                TurnIndexInYear;

            return true;
        }

        public bool TryGetNextSeason(
            out KvltTurnSeason season)
        {
            if (TurnsPerYear != 4)
            {
                season =
                    default;

                return false;
            }

            season =
                (KvltTurnSeason)
                NextTurnIndexInYear;

            return true;
        }
    }
}