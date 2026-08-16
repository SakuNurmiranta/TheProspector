using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.TurnFlow
{
    /// <summary>
    /// Produces the authoritative phase plan for one
    /// completed turn.
    ///
    /// It does not execute any gameplay mutation.
    /// </summary>
    public sealed class KvltTurnChronologyPlanner
    {
        public KvltTurnChronologyPlan Plan(
            int completedTurn,
            int turnsPerYear)
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

            bool yearEnd =
                completedTurn %
                turnsPerYear ==
                turnsPerYear - 1;

            List<KvltTurnPhase> phases =
                new()
                {
                    KvltTurnPhase
                        .OrdinaryActions,

                    KvltTurnPhase
                        .ExistingFieldSettlement,

                    KvltTurnPhase
                        .SharedHappeningWindow,

                    KvltTurnPhase
                        .HappeningSettlement
                };

            if (yearEnd)
            {
                phases.Add(
                    KvltTurnPhase
                        .YearEndCanonSettlement
                );
            }

            /*
             * Gravity-bearing Score belongs to every
             * completed turn.
             *
             * At year end it occurs after Canon so a
             * newly canonized release can receive its
             * first retained payout immediately.
             */
            phases.Add(
                KvltTurnPhase
                    .TurnGravityScoreSettlement
            );

            if (yearEnd)
            {
                phases.Add(
                    KvltTurnPhase
                        .YearInfluenceKeeperSuccession
                );

                phases.Add(
                    KvltTurnPhase
                        .KeeperTenureTransitionResolution
                );
            }

            /*
             * Releases fettered during Happening
             * settlement do not become Field objects
             * until the following turn begins.
             */
            phases.Add(
                KvltTurnPhase
                    .NextTurnIngress
            );

            phases.Add(
                KvltTurnPhase
                    .NextStatePublication
            );

            return new KvltTurnChronologyPlan(
                completedTurn,
                turnsPerYear,
                phases
            );
        }
    }
}