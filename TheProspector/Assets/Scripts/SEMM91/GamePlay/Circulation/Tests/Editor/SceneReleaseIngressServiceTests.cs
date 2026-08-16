using NUnit.Framework;

namespace SEMM91.GamePlay.Circulation.Tests
{
    public class SceneReleaseIngressServiceTests
    {
        private readonly
            SceneReleaseIngressEvaluator evaluator =
                new();

        private readonly
            SceneReleaseIngressService service =
                new();

        [Test]
        public void
            FetteredRelease_EstablishesEvaluatedFieldPosition()
        {
            SceneRelease release =
                Release();

            Assert.That(
                release.TryFetter(6),
                Is.True
            );

            SceneReleaseIngressEvaluation evaluation =
                evaluator.Evaluate(
                    release,
                    null,
                    0.20f,
                    0.70f,
                    6
                );

            Assert.That(
                service.TryEstablish(
                    release,
                    evaluation
                ),
                Is.True
            );

            Assert.That(
                release.HasFieldPosition,
                Is.True
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.20f)
            );

            Assert.That(
                release.FieldPositionState
                    .EstablishedTurn,
                Is.EqualTo(6)
            );

            Assert.That(
                release.FieldPositionState
                    .Transitions.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            FringeRelease_CannotApplyIngressEvaluation()
        {
            SceneRelease release =
                Release();

            /*
             * Build the evaluation from a proper
             * Field release so we are testing the
             * mutation boundary rather than evaluator
             * validation.
             */
            SceneRelease source =
                Release();

            Assert.That(
                source.TryFetter(6),
                Is.True
            );

            SceneReleaseIngressEvaluation evaluation =
                evaluator.Evaluate(
                    source,
                    null,
                    0.20f,
                    0.70f,
                    6
                );

            /*
             * Evaluation provenance points at source,
             * so the unrelated Fringe release cannot
             * consume it.
             */
            Assert.That(
                service.TryEstablish(
                    release,
                    evaluation
                ),
                Is.False
            );

            Assert.That(
                release.HasFieldPosition,
                Is.False
            );
        }

        [Test]
        public void
            ExistingFieldPosition_CannotBeReestablished()
        {
            SceneRelease release =
                Release();

            Assert.That(
                release.TryFetter(6),
                Is.True
            );

            SceneReleaseIngressEvaluation first =
                evaluator.Evaluate(
                    release,
                    null,
                    0.20f,
                    0.70f,
                    6
                );

            Assert.That(
                service.TryEstablish(
                    release,
                    first
                ),
                Is.True
            );

            SceneReleaseIngressEvaluation second =
                evaluator.Evaluate(
                    release,
                    null,
                    0.50f,
                    0.70f,
                    7
                );

            Assert.That(
                service.TryEstablish(
                    release,
                    second
                ),
                Is.False
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.20f)
            );

            Assert.That(
                release.FieldPositionState
                    .Transitions.Count,
                Is.EqualTo(1)
            );
        }

        private static SceneRelease Release()
        {
            return new SceneRelease(
                "Test Release",
                "DEMO",
                "OWNER",
                "KVLT_SCENE",
                4,
                1f
            );
        }
    }
}