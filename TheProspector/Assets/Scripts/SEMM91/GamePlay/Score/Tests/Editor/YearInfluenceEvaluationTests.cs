using NUnit.Framework;

namespace SEMM91.GamePlay.Score.Tests.Editor
{
    public sealed class
        YearInfluenceEvaluationTests
    {
        [Test]
        public void
            EvaluationSumsOnlyProvidedInfluenceEvents()
        {
            ScoreEvent field =
                Event(
                    "FIELD",
                    ScoreEventKind.FieldGravity,
                    1.25f,
                    turn:
                        1
                );

            ScoreEvent retained =
                Event(
                    "RETAINED",
                    ScoreEventKind
                        .CanonRetainedGravity,
                    2.5f,
                    turn:
                        3
                );

            YearInfluenceEvaluation evaluation =
                new(
                    "PLAYER_A",
                    "KVLT",
                    0,
                    3,
                    new[]
                    {
                        field,
                        retained
                    }
                );

            Assert.That(
                evaluation.YearInfluence,
                Is.EqualTo(3.75f)
                    .Within(0.0001f)
            );

            Assert.That(
                evaluation.ContributingEvents.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                evaluation.FirstTurnInclusive,
                Is.EqualTo(0)
            );

            Assert.That(
                evaluation.LastTurnInclusive,
                Is.EqualTo(3)
            );
        }

        private static ScoreEvent Event(
            string suffix,
            ScoreEventKind kind,
            float amount,
            int turn)
        {
            return new ScoreEvent(
                "SCORE_" + suffix,
                "PLAYER_A",
                "PLAYER_A",
                "KVLT",
                "RELEASE_" + suffix,
                "DEMO_" + suffix,
                kind,
                amount,
                turn
            );
        }
    }
}