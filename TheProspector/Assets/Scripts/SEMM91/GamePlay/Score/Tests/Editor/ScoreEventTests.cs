using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Score.Tests.Editor
{
    public class ScoreEventTests
    {
        [Test]
        public void OrdinaryReleaseScore_PreservesExactProvenance()
        {
            ScoreEvent scoreEvent =
                new ScoreEvent(
                    "SCORE_1",
                    "PLAYER_A",
                    "PLAYER_A",
                    "KVLT",
                    "RELEASE_A",
                    "DEMO_A",
                    ScoreEventKind.FieldGravity,
                    1.25f,
                    7
                );

            Assert.That(
                scoreEvent.ScoreEventId,
                Is.EqualTo("SCORE_1")
            );

            Assert.That(
                scoreEvent.BeneficiaryEntityId,
                Is.EqualTo("PLAYER_A")
            );

            Assert.That(
                scoreEvent.SourceOwnerEntityId,
                Is.EqualTo("PLAYER_A")
            );

            Assert.That(
                scoreEvent.SceneId,
                Is.EqualTo("KVLT")
            );

            Assert.That(
                scoreEvent.SceneReleaseId,
                Is.EqualTo("RELEASE_A")
            );

            Assert.That(
                scoreEvent.DemoTapeId,
                Is.EqualTo("DEMO_A")
            );

            Assert.That(
                scoreEvent.Amount,
                Is.EqualTo(1.25f)
            );

            Assert.That(
                scoreEvent.GlobalTurn,
                Is.EqualTo(7)
            );
        }

        [Test]
        public void ScoreAmount_MustBeFiniteAndNonNegative()
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    Event(
                        ScoreEventKind.FieldGravity,
                        -0.01f
                    )
            );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    Event(
                        ScoreEventKind.FieldGravity,
                        float.NaN
                    )
            );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    Event(
                        ScoreEventKind.FieldGravity,
                        float.PositiveInfinity
                    )
            );

            Assert.DoesNotThrow(
                () =>
                    Event(
                        ScoreEventKind.FieldGravity,
                        0f
                    )
            );
        }

        [Test]
        public void OrdinaryReleaseScore_MustBenefitReleaseOwner()
        {
            Assert.Throws<
                ArgumentException>(
                () =>
                    new ScoreEvent(
                        "SCORE",
                        "KEEPER",
                        "OWNER",
                        "KVLT",
                        "RELEASE",
                        "DEMO",
                        ScoreEventKind.FieldGravity,
                        1f,
                        1
                    )
            );

            Assert.Throws<
                ArgumentException>(
                () =>
                    new ScoreEvent(
                        "SCORE",
                        "HELPER",
                        "OWNER",
                        "KVLT",
                        "RELEASE",
                        "DEMO",
                        ScoreEventKind
                            .FirstFetterResonance,
                        1f,
                        1
                    )
            );
        }

        [Test]
        public void HoleMandateCredit_RequiresDistinctBeneficiary()
        {
            Assert.DoesNotThrow(
                () =>
                    new ScoreEvent(
                        "MANDATE",
                        "KEEPER",
                        "OWNER",
                        "KVLT",
                        "RELEASE",
                        "DEMO",
                        ScoreEventKind
                            .HoleMandateGravity,
                        1f,
                        4
                    )
            );

            Assert.Throws<
                ArgumentException>(
                () =>
                    new ScoreEvent(
                        "INVALID",
                        "OWNER",
                        "OWNER",
                        "KVLT",
                        "RELEASE",
                        "DEMO",
                        ScoreEventKind
                            .HoleMandateGravity,
                        1f,
                        4
                    )
            );
        }

        private static ScoreEvent Event(
            ScoreEventKind kind,
            float amount)
        {
            return new ScoreEvent(
                "SCORE",
                "OWNER",
                "OWNER",
                "KVLT",
                "RELEASE",
                "DEMO",
                kind,
                amount,
                1
            );
        }
    }
}