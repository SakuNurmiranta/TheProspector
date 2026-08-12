using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Immutable comparison of one canonical frontier
    /// degree against the frozen Canon at turn start
    /// and the Canon produced by current settlement.
    /// </summary>
    public sealed class
        CanonFrontierSupersessionEvaluation
    {
        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree
            CanonicalDegreeEstablished { get; }

        public TagDegree
            TurnStartCanonicalDegree { get; }

        public TagDegree
            PostSettlementCanonicalDegree { get; }

        public int SettledTurn { get; }

        public
            CanonFrontierSupersessionDisposition
            Disposition { get; }

        public bool WasEligibleForTurnStartPayout =>
            TurnStartCanonicalDegree ==
            CanonicalDegreeEstablished;

        public bool IsEligibleNextTurn =>
            PostSettlementCanonicalDegree ==
            CanonicalDegreeEstablished;

        public bool WasSupersededDuringSettlement =>
            Disposition ==
            CanonFrontierSupersessionDisposition
                .SupersededAfterSettlement;

        public CanonFrontierSupersessionEvaluation(
            TagAxis axis,
            TagPole pole,
            TagDegree canonicalDegreeEstablished,
            TagDegree turnStartCanonicalDegree,
            TagDegree postSettlementCanonicalDegree,
            int settledTurn,
            CanonFrontierSupersessionDisposition
                disposition)
        {
            if (canonicalDegreeEstablished ==
                TagDegree.Neutral)
            {
                throw new ArgumentException(
                    "Canonical frontier degree must " +
                    "be greater than Neutral.",
                    nameof(canonicalDegreeEstablished)
                );
            }

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            Axis =
                axis;

            Pole =
                pole;

            CanonicalDegreeEstablished =
                canonicalDegreeEstablished;

            TurnStartCanonicalDegree =
                turnStartCanonicalDegree;

            PostSettlementCanonicalDegree =
                postSettlementCanonicalDegree;

            SettledTurn =
                settledTurn;

            Disposition =
                disposition;
        }
    }
}