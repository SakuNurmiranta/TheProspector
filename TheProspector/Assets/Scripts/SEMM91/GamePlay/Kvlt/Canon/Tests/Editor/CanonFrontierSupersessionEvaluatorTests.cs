using NUnit.Framework;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Canon.Tests.Editor
{
    public class
        CanonFrontierSupersessionEvaluatorTests
    {
        private readonly
            CanonFrontierSupersessionEvaluator evaluator =
                new();

        [Test]
        public void
            EqualPostSettlementDegreeLeavesFrontierCurrent()
        {
            CanonState start =
                CanonAt(
                    TagDegree.Dominant
                );

            CanonState next =
                start.CreateCopy();

            Assert.That(
                next.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    CanonProvenanceKind.SceneRelease,
                    "EQUAL_RELEASE",
                    "EQUAL_TRACK",
                    "EQUAL_IDEA",
                    8
                ),
                Is.True
            );

            CanonFrontierSupersessionEvaluation
                result =
                    evaluator.Evaluate(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        start,
                        next,
                        8
                    );

            Assert.That(
                result.Disposition,
                Is.EqualTo(
                    CanonFrontierSupersessionDisposition
                        .RemainsCurrent
                )
            );

            Assert.That(
                result.WasEligibleForTurnStartPayout,
                Is.True
            );

            Assert.That(
                result.IsEligibleNextTurn,
                Is.True
            );

            Assert.That(
                result.WasSupersededDuringSettlement,
                Is.False
            );
        }

        [Test]
        public void
            StrictlyHigherPostSettlementDegreeSupersedesProspectively()
        {
            CanonState start =
                CanonAt(
                    TagDegree.Dominant
                );

            CanonState next =
                start.CreateCopy();

            Assert.That(
                next.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    CanonProvenanceKind.SceneRelease,
                    "HIGHER_RELEASE",
                    "HIGHER_TRACK",
                    "HIGHER_IDEA",
                    8
                ),
                Is.True
            );

            CanonFrontierSupersessionEvaluation
                result =
                    evaluator.Evaluate(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        start,
                        next,
                        8
                    );

            Assert.That(
                result.Disposition,
                Is.EqualTo(
                    CanonFrontierSupersessionDisposition
                        .SupersededAfterSettlement
                )
            );

            /*
             * Crucial prospective rule:
             *
             * the old frontier was still valid at
             * Scene_t, so turn-t payout survives.
             */
            Assert.That(
                result.WasEligibleForTurnStartPayout,
                Is.True
            );

            Assert.That(
                result.IsEligibleNextTurn,
                Is.False
            );

            Assert.That(
                result.WasSupersededDuringSettlement,
                Is.True
            );
        }

        [Test]
        public void
            FrontierAlreadyBelowTurnStartCanonIsAlreadySuperseded()
        {
            CanonState start =
                CanonAt(
                    TagDegree.Transgressive
                );

            CanonState next =
                start.CreateCopy();

            CanonFrontierSupersessionEvaluation
                result =
                    evaluator.Evaluate(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        start,
                        next,
                        9
                    );

            Assert.That(
                result.Disposition,
                Is.EqualTo(
                    CanonFrontierSupersessionDisposition
                        .AlreadySuperseded
                )
            );

            Assert.That(
                result.WasEligibleForTurnStartPayout,
                Is.False
            );

            Assert.That(
                result.IsEligibleNextTurn,
                Is.False
            );

            Assert.That(
                result.WasSupersededDuringSettlement,
                Is.False
            );
        }

        [Test]
        public void
            PostSettlementCanonCannotDecrease()
        {
            CanonState start =
                CanonAt(
                    TagDegree.Transgressive
                );

            CanonState invalidNext =
                CanonAt(
                    TagDegree.Dominant
                );

            Assert.Throws<
                System.ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        start,
                        invalidNext,
                        9
                    )
            );
        }

        [Test]
        public void
            TurnStartCanonCannotBeBelowEstablishedHistoricalFrontier()
        {
            CanonState invalidStart =
                CanonAt(
                    TagDegree.Weak
                );

            CanonState next =
                CanonAt(
                    TagDegree.Dominant
                );

            Assert.Throws<
                System.ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        invalidStart,
                        next,
                        8
                    )
            );
        }

        private static CanonState CanonAt(
            TagDegree degree)
        {
            CanonState canon =
                new();

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    degree,
                    CanonProvenanceKind.ScenarioSeed,
                    "CANON_" + degree
                ),
                Is.True
            );

            return canon;
        }
    }
}