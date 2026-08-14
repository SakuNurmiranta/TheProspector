using System.Reflection;
using NUnit.Framework;
using SEMM91;

namespace SEMM91.GamePlay.Kvlt.TurnFlow.Tests.Editor
{
    public sealed class
        GameCoordinatorTurnChronologyIntegrationTests
    {
        [TestCase(0, false, 1, 0)]
        [TestCase(1, false, 2, 1)]
        [TestCase(2, false, 3, 2)]
        [TestCase(3, true,  4, 3)]
        [TestCase(4, false, 5, 0)]
        [TestCase(5, false, 6, 1)]
        [TestCase(6, false, 7, 2)]
        [TestCase(7, true,  8, 3)]
        public void
            CoordinatorPlansClosureFromCurrentGlobalTurn(
                int currentGlobalTurn,
                bool expectedYearEnd,
                int expectedNextTurn,
                int expectedTurnIndexInYear)
        {
            MethodInfo method =
                typeof(GameCoordinator)
                    .GetMethod(
                        "PlanPeak2TurnClosure",
                        BindingFlags.NonPublic |
                        BindingFlags.Static
                    );

            Assert.That(
                method,
                Is.Not.Null,
                "GameCoordinator must route turn " +
                "closure through the Peak-2 chronology " +
                "planner."
            );

            object rawResult =
                method.Invoke(
                    null,
                    new object[]
                    {
                        currentGlobalTurn
                    }
                );

            Assert.That(
                rawResult,
                Is.InstanceOf<
                    KvltTurnChronologyPlan>()
            );

            KvltTurnChronologyPlan plan =
                (KvltTurnChronologyPlan)
                rawResult;

            /*
             * Most important integration assertion:
             *
             * the current replicated globalTurn is the
             * turn being CLOSED, not the already
             * incremented next turn.
             */
            Assert.That(
                plan.CompletedTurn,
                Is.EqualTo(
                    currentGlobalTurn
                )
            );

            Assert.That(
                plan.NextTurn,
                Is.EqualTo(
                    expectedNextTurn
                )
            );

            Assert.That(
                plan.IsYearEnd,
                Is.EqualTo(
                    expectedYearEnd
                )
            );

            Assert.That(
                plan.TurnIndexInYear,
                Is.EqualTo(
                    expectedTurnIndexInYear
                )
            );

            Assert.That(
                plan.Includes(
                    KvltTurnPhase
                        .YearEndCanonSettlement
                ),
                Is.EqualTo(
                    expectedYearEnd
                )
            );

            Assert.That(
                plan.Includes(
                    KvltTurnPhase
                        .YearInfluenceKeeperSuccession
                ),
                Is.EqualTo(
                    expectedYearEnd
                )
            );
        }
    }
}