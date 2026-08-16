using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Kvlt.TurnFlow.Tests.Editor
{
    public class
        KvltTurnChronologyPlannerTests
    {
        private readonly
            KvltTurnChronologyPlanner planner =
                new();

        [Test]
        public void
            TurnZeroIsYearOneSpringNotYearEnd()
        {
            KvltTurnChronologyPlan plan =
                planner.Plan(
                    completedTurn: 0,
                    turnsPerYear: 4
                );

            Assert.That(
                plan.YearNumber,
                Is.EqualTo(1)
            );

            Assert.That(
                plan.TurnIndexInYear,
                Is.EqualTo(0)
            );

            Assert.That(
                plan.IsYearEnd,
                Is.False
            );

            Assert.That(
                plan.TryGetSeason(
                    out KvltTurnSeason season
                ),
                Is.True
            );

            Assert.That(
                season,
                Is.EqualTo(
                    KvltTurnSeason.Spring
                )
            );
        }

        [Test]
        public void
            TurnThreeIsYearOneWinterAndYearEnd()
        {
            KvltTurnChronologyPlan plan =
                planner.Plan(
                    completedTurn: 3,
                    turnsPerYear: 4
                );

            Assert.That(
                plan.YearNumber,
                Is.EqualTo(1)
            );

            Assert.That(
                plan.TurnIndexInYear,
                Is.EqualTo(3)
            );

            Assert.That(
                plan.IsYearEnd,
                Is.True
            );

            Assert.That(
                plan.TryGetSeason(
                    out KvltTurnSeason season
                ),
                Is.True
            );

            Assert.That(
                season,
                Is.EqualTo(
                    KvltTurnSeason.Winter
                )
            );

            Assert.That(
                plan.NextTurn,
                Is.EqualTo(4)
            );

            Assert.That(
                plan.NextYearNumber,
                Is.EqualTo(2)
            );

            Assert.That(
                plan.TryGetNextSeason(
                    out KvltTurnSeason nextSeason
                ),
                Is.True
            );

            Assert.That(
                nextSeason,
                Is.EqualTo(
                    KvltTurnSeason.Spring
                )
            );
        }

        [Test]
        public void
            TurnFourBeginsYearTwoSpring()
        {
            KvltTurnChronologyPlan plan =
                planner.Plan(
                    completedTurn: 4,
                    turnsPerYear: 4
                );

            Assert.That(
                plan.YearNumber,
                Is.EqualTo(2)
            );

            Assert.That(
                plan.TurnIndexInYear,
                Is.EqualTo(0)
            );

            Assert.That(
                plan.IsYearEnd,
                Is.False
            );

            Assert.That(
                plan.TryGetSeason(
                    out KvltTurnSeason season
                ),
                Is.True
            );

            Assert.That(
                season,
                Is.EqualTo(
                    KvltTurnSeason.Spring
                )
            );
        }

        [Test]
        public void
            OrdinaryTurnOmitsYearEndInstitutionalPhases()
        {
            KvltTurnChronologyPlan plan =
                planner.Plan(
                    1,
                    4
                );

            Assert.That(
                plan.Includes(
                    KvltTurnPhase
                        .YearEndCanonSettlement
                ),
                Is.False
            );

            Assert.That(
                plan.Includes(
                    KvltTurnPhase
                        .YearInfluenceKeeperSuccession
                ),
                Is.False
            );

            Assert.That(
                plan.Includes(
                    KvltTurnPhase
                        .KeeperTenureTransitionResolution
                ),
                Is.False
            );

            Assert.That(
                plan.Includes(
                    KvltTurnPhase
                        .TurnGravityScoreSettlement
                ),
                Is.True
            );
        }

        [Test]
        public void
            FieldSettlementPrecedesSharedHappeningAndHappeningSettlement()
        {
            KvltTurnChronologyPlan plan =
                planner.Plan(
                    2,
                    4
                );

            Assert.That(
                plan.IndexOf(
                    KvltTurnPhase
                        .ExistingFieldSettlement
                ),
                Is.LessThan(
                    plan.IndexOf(
                        KvltTurnPhase
                            .SharedHappeningWindow
                    )
                )
            );

            Assert.That(
                plan.IndexOf(
                    KvltTurnPhase
                        .SharedHappeningWindow
                ),
                Is.LessThan(
                    plan.IndexOf(
                        KvltTurnPhase
                            .HappeningSettlement
                    )
                )
            );
        }

        [Test]
        public void
            YearEndCanonOccursAfterHappeningsButBeforeGravityAndKeeperSelection()
        {
            KvltTurnChronologyPlan plan =
                planner.Plan(
                    3,
                    4
                );

            int happening =
                plan.IndexOf(
                    KvltTurnPhase
                        .HappeningSettlement
                );

            int canon =
                plan.IndexOf(
                    KvltTurnPhase
                        .YearEndCanonSettlement
                );

            int gravity =
                plan.IndexOf(
                    KvltTurnPhase
                        .TurnGravityScoreSettlement
                );

            int succession =
                plan.IndexOf(
                    KvltTurnPhase
                        .YearInfluenceKeeperSuccession
                );

            Assert.That(
                happening,
                Is.LessThan(canon)
            );

            Assert.That(
                canon,
                Is.LessThan(gravity)
            );

            Assert.That(
                gravity,
                Is.LessThan(succession)
            );
        }

        [Test]
        public void
            NextTurnIngressOccursAfterCompletedTurnGravityAndTenureResolution()
        {
            KvltTurnChronologyPlan plan =
                planner.Plan(
                    3,
                    4
                );

            int gravity =
                plan.IndexOf(
                    KvltTurnPhase
                        .TurnGravityScoreSettlement
                );

            int tenure =
                plan.IndexOf(
                    KvltTurnPhase
                        .KeeperTenureTransitionResolution
                );

            int ingress =
                plan.IndexOf(
                    KvltTurnPhase
                        .NextTurnIngress
                );

            int publication =
                plan.IndexOf(
                    KvltTurnPhase
                        .NextStatePublication
                );

            Assert.That(
                gravity,
                Is.LessThan(ingress)
            );

            Assert.That(
                tenure,
                Is.LessThan(ingress)
            );

            Assert.That(
                ingress,
                Is.LessThan(publication)
            );
        }

        [Test]
        public void
            ConfigurableYearLengthStillUsesCompletedTurnBoundary()
        {
            KvltTurnChronologyPlan endYearOne =
                planner.Plan(
                    completedTurn: 2,
                    turnsPerYear: 3
                );

            Assert.That(
                endYearOne.IsYearEnd,
                Is.True
            );

            Assert.That(
                endYearOne.YearNumber,
                Is.EqualTo(1)
            );

            Assert.That(
                endYearOne.NextTurn,
                Is.EqualTo(3)
            );

            Assert.That(
                endYearOne.NextYearNumber,
                Is.EqualTo(2)
            );

            Assert.That(
                endYearOne.TryGetSeason(
                    out _
                ),
                Is.False
            );

            KvltTurnChronologyPlan
                nextYear =
                    planner.Plan(
                        completedTurn: 3,
                        turnsPerYear: 3
                    );

            Assert.That(
                nextYear.YearNumber,
                Is.EqualTo(2)
            );

            Assert.That(
                nextYear.TurnIndexInYear,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void
            InvalidTurnOrYearLengthIsRejected()
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    planner.Plan(
                        -1,
                        4
                    )
            );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    planner.Plan(
                        0,
                        0
                    )
            );
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void
            EverySeasonIncludesSharedHappeningAndSettlement(
                int completedTurn)
        {
            KvltTurnChronologyPlan plan =
                planner.Plan(
                    completedTurn,
                    turnsPerYear: 4
                );

            Assert.That(
                plan.Includes(
                    KvltTurnPhase
                        .SharedHappeningWindow
                ),
                Is.True
            );

            Assert.That(
                plan.Includes(
                    KvltTurnPhase
                        .HappeningSettlement
                ),
                Is.True
            );

            Assert.That(
                plan.IndexOf(
                    KvltTurnPhase
                        .ExistingFieldSettlement
                ),
                Is.LessThan(
                    plan.IndexOf(
                        KvltTurnPhase
                            .SharedHappeningWindow
                    )
                )
            );

            Assert.That(
                plan.IndexOf(
                    KvltTurnPhase
                        .SharedHappeningWindow
                ),
                Is.LessThan(
                    plan.IndexOf(
                        KvltTurnPhase
                            .HappeningSettlement
                    )
                )
            );
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, true)]
        public void
            OnlyWinterIncludesCanonization(
                int completedTurn,
                bool expectedCanonization)
        {
            KvltTurnChronologyPlan plan =
                planner.Plan(
                    completedTurn,
                    turnsPerYear: 4
                );

            Assert.That(
                plan.Includes(
                    KvltTurnPhase
                        .YearEndCanonSettlement
                ),
                Is.EqualTo(
                    expectedCanonization
                )
            );

            /*
             * Canonization, when present, must occur only
             * AFTER the ordinary every-turn Happening
             * settlement.
             */
            if (expectedCanonization)
            {
                Assert.That(
                    plan.IndexOf(
                        KvltTurnPhase
                            .HappeningSettlement
                    ),
                    Is.LessThan(
                        plan.IndexOf(
                            KvltTurnPhase
                                .YearEndCanonSettlement
                        )
                    )
                );
            }
        }
    }
}