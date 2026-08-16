using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SEMM91.GamePlay.Score.Tests.Editor
{
    public sealed class
        YearInfluenceEvaluatorTests
    {
        private readonly
            YearInfluenceEvaluator
            evaluator =
                new();

        [Test]
        public void
            CompletedYearIncludesOnlyInfluenceBearingGravity()
        {
            ScoreLedger ledger =
                new();

            Record(
                ledger,
                "RESONANCE",
                "PLAYER_A",
                ScoreEventKind
                    .FirstFetterResonance,
                20f,
                turn:
                    0
            );

            Record(
                ledger,
                "FIELD",
                "PLAYER_A",
                ScoreEventKind
                    .FieldGravity,
                1f,
                turn:
                    1
            );

            Record(
                ledger,
                "MANDATE",
                "PLAYER_A",
                ScoreEventKind
                    .HoleMandateGravity,
                40f,
                turn:
                    2,
                sourceOwner:
                    "OTHER_OWNER"
            );

            Record(
                ledger,
                "RETAINED",
                "PLAYER_A",
                ScoreEventKind
                    .CanonRetainedGravity,
                2f,
                turn:
                    2
            );

            Record(
                ledger,
                "INSTITUTIONAL",
                "PLAYER_A",
                ScoreEventKind
                    .InstitutionalGravity,
                3f,
                turn:
                    3
            );

            YearInfluenceEvaluation result =
                Evaluate(
                    ledger,
                    "PLAYER_A"
                );

            Assert.That(
                result.YearInfluence,
                Is.EqualTo(6f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.ContributingEvents.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                result.ContributingEvents
                    .Select(
                        scoreEvent =>
                            scoreEvent.Kind
                    ),
                Is.EquivalentTo(
                    new[]
                    {
                        ScoreEventKind.FieldGravity,
                        ScoreEventKind
                            .CanonRetainedGravity,
                        ScoreEventKind
                            .InstitutionalGravity
                    }
                )
            );
        }

        [Test]
        public void
            FinalWinterGravityIsIncludedBeforeYearInfluence()
        {
            ScoreLedger ledger =
                new();

            Record(
                ledger,
                "SPRING",
                "PLAYER_A",
                ScoreEventKind.FieldGravity,
                1f,
                turn:
                    0
            );

            Record(
                ledger,
                "WINTER",
                "PLAYER_A",
                ScoreEventKind
                    .CanonRetainedGravity,
                4f,
                turn:
                    3
            );

            YearInfluenceEvaluation result =
                Evaluate(
                    ledger,
                    "PLAYER_A"
                );

            Assert.That(
                result.FirstTurnInclusive,
                Is.EqualTo(0)
            );

            Assert.That(
                result.LastTurnInclusive,
                Is.EqualTo(3)
            );

            Assert.That(
                result.YearInfluence,
                Is.EqualTo(5f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.ContributingEvents[
                    result.ContributingEvents.Count - 1
                ].GlobalTurn,
                Is.EqualTo(3)
            );
        }

        [Test]
        public void
            EventsOutsideCompletedYearDoNotLeakIntoInfluence()
        {
            ScoreLedger ledger =
                new();

            Record(
                ledger,
                "OLD",
                "PLAYER_A",
                ScoreEventKind.FieldGravity,
                100f,
                turn:
                    3
            );

            Record(
                ledger,
                "CURRENT",
                "PLAYER_A",
                ScoreEventKind.FieldGravity,
                2f,
                turn:
                    4
            );

            Record(
                ledger,
                "CURRENT_WINTER",
                "PLAYER_A",
                ScoreEventKind
                    .InstitutionalGravity,
                3f,
                turn:
                    7
            );

            IReadOnlyList<
                    YearInfluenceEvaluation>
                results =
                    evaluator.Evaluate(
                        "KVLT",
                        lastTurnInclusive:
                            7,
                        turnsPerYear:
                            4,
                        new[]
                        {
                            "PLAYER_A"
                        },
                        ledger
                    );

            Assert.That(
                results[0].FirstTurnInclusive,
                Is.EqualTo(4)
            );

            Assert.That(
                results[0].LastTurnInclusive,
                Is.EqualTo(7)
            );

            Assert.That(
                results[0].YearInfluence,
                Is.EqualTo(5f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            ActiveParticipantWithNoGravityStillGetsZeroEvaluation()
        {
            ScoreLedger ledger =
                new();

            Record(
                ledger,
                "A_FIELD",
                "PLAYER_A",
                ScoreEventKind.FieldGravity,
                2f,
                turn:
                    2
            );

            IReadOnlyList<
                    YearInfluenceEvaluation>
                results =
                    evaluator.Evaluate(
                        "KVLT",
                        lastTurnInclusive:
                            3,
                        turnsPerYear:
                            4,
                        new[]
                        {
                            "PLAYER_B",
                            "PLAYER_A"
                        },
                        ledger
                    );

            Assert.That(
                results.Count,
                Is.EqualTo(2)
            );

            /*
             * Evaluator output is deterministic by
             * entity identity, not caller order.
             */
            Assert.That(
                results[0].BeneficiaryEntityId,
                Is.EqualTo(
                    "PLAYER_A"
                )
            );

            Assert.That(
                results[0].YearInfluence,
                Is.EqualTo(2f)
            );

            Assert.That(
                results[1].BeneficiaryEntityId,
                Is.EqualTo(
                    "PLAYER_B"
                )
            );

            Assert.That(
                results[1].YearInfluence,
                Is.EqualTo(0f)
            );

            Assert.That(
                results[1].ContributingEvents,
                Is.Empty
            );
        }

        private YearInfluenceEvaluation
            Evaluate(
                ScoreLedger ledger,
                string beneficiary)
        {
            return evaluator
                .Evaluate(
                    "KVLT",
                    lastTurnInclusive:
                        3,
                    turnsPerYear:
                        4,
                    new[]
                    {
                        beneficiary
                    },
                    ledger
                )[0];
        }

        private static void Record(
            ScoreLedger ledger,
            string suffix,
            string beneficiary,
            ScoreEventKind kind,
            float amount,
            int turn,
            string sourceOwner = null)
        {
            sourceOwner ??=
                beneficiary;

            Assert.That(
                ledger.TryRecord(
                    new ScoreEvent(
                        "SCORE_" + suffix,
                        beneficiary,
                        sourceOwner,
                        "KVLT",
                        "RELEASE_" + suffix,
                        "DEMO_" + suffix,
                        kind,
                        amount,
                        turn
                    )
                ),
                Is.True
            );
        }
    }
}