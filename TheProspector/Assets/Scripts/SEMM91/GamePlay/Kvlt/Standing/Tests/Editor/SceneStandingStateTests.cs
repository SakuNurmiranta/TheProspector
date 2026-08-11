using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Kvlt.Standing.Tests.Editor
{
    public class SceneStandingStateTests
    {
        private readonly
            SceneStandingEvaluator evaluator =
                new();

        [Test]
        public void
            NewState_HasNoSettledStanding()
        {
            SceneStandingState state =
                new SceneStandingState(
                    "OWNER",
                    "KVLT"
                );

            Assert.That(
                state.HasSettledEvaluation,
                Is.False
            );

            Assert.That(
                state.HasCurrentStanding,
                Is.False
            );

            Assert.That(
                state.CurrentStanding,
                Is.Null
            );

            Assert.That(
                state.LastSettledTurn,
                Is.EqualTo(-1)
            );

            Assert.That(
                state.History,
                Is.Empty
            );
        }

        [Test]
        public void
            SettledEvaluation_BecomesCurrentStanding()
        {
            SceneStandingState state =
                new SceneStandingState(
                    "OWNER",
                    "KVLT"
                );

            SceneStandingEvaluation evaluation =
                Standing(
                    turn: 5,
                    position: 0.25f
                );

            Assert.That(
                state.TryRecord(
                    evaluation
                ),
                Is.True
            );

            Assert.That(
                state.CurrentEvaluation,
                Is.SameAs(evaluation)
            );

            Assert.That(
                state.HasCurrentStanding,
                Is.True
            );

            Assert.That(
                state.CurrentStanding,
                Is.EqualTo(0.25f)
                    .Within(0.0001f)
            );

            Assert.That(
                state.LastSettledTurn,
                Is.EqualTo(5)
            );
        }

        [Test]
        public void
            LaterSettlementReplacesCurrentButPreservesHistory()
        {
            SceneStandingState state =
                new SceneStandingState(
                    "OWNER",
                    "KVLT"
                );

            SceneStandingEvaluation first =
                Standing(
                    turn: 5,
                    position: 0.25f
                );

            SceneStandingEvaluation second =
                Standing(
                    turn: 6,
                    position: 0.60f
                );

            Assert.That(
                state.TryRecord(first),
                Is.True
            );

            Assert.That(
                state.TryRecord(second),
                Is.True
            );

            Assert.That(
                state.History.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                state.History[0],
                Is.SameAs(first)
            );

            Assert.That(
                state.History[1],
                Is.SameAs(second)
            );

            Assert.That(
                state.CurrentEvaluation,
                Is.SameAs(second)
            );

            Assert.That(
                state.CurrentStanding,
                Is.EqualTo(0.60f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            StandingHistoryRejectsWrongIdentityAndNonChronologicalSettlement()
        {
            SceneStandingState state =
                new SceneStandingState(
                    "OWNER",
                    "KVLT"
                );

            Assert.That(
                state.TryRecord(
                    Standing(
                        6,
                        0.4f
                    )
                ),
                Is.True
            );

            Assert.That(
                state.TryRecord(
                    Standing(
                        6,
                        0.5f
                    )
                ),
                Is.False
            );

            Assert.That(
                state.TryRecord(
                    Standing(
                        5,
                        0.1f
                    )
                ),
                Is.False
            );

            SceneStandingEvaluation
                wrongOwner =
                    evaluator.Evaluate(
                        "OTHER_OWNER",
                        "KVLT",
                        7,
                        Array.Empty<
                            SceneStandingContribution>()
                    );

            Assert.That(
                state.TryRecord(
                    wrongOwner
                ),
                Is.False
            );

            SceneStandingEvaluation
                wrongScene =
                    evaluator.Evaluate(
                        "OWNER",
                        "OTHER_SCENE",
                        7,
                        Array.Empty<
                            SceneStandingContribution>()
                    );

            Assert.That(
                state.TryRecord(
                    wrongScene
                ),
                Is.False
            );

            Assert.That(
                state.History.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                state.LastSettledTurn,
                Is.EqualTo(6)
            );
        }

        private SceneStandingEvaluation Standing(
            int turn,
            float position)
        {
            return evaluator.Evaluate(
                "OWNER",
                "KVLT",
                turn,
                new[]
                {
                    new SceneStandingContribution(
                        "RELEASE_" + turn,
                        "DEMO_" + turn,
                        "OWNER",
                        "KVLT",
                        SceneStandingContributionKind
                            .ActiveField,
                        position,
                        1f,
                        turn
                    )
                }
            );
        }
    }
}