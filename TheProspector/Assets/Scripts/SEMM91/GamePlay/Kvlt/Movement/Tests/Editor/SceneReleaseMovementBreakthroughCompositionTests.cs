using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class
        SceneReleaseMovementBreakthroughCompositionTests
    {
        private readonly
            SceneReleaseMovementEvaluator evaluator =
                new();

        [Test]
        public void
            NaturalDriftAndBreakthrough_AreSummedIntoFinalMovement()
        {
            SceneReleaseNaturalDriftEvaluation natural =
                NaturalDrift(
                    startPosition: 0.40f,
                    drift: -0.20f
                );

            SceneReleaseCanonBreakthroughMovementEvaluation
                breakthrough =
                    BreakthroughMovement(
                        startPosition: 0.40f,
                        drift: 0.75f
                    );

            SceneReleaseMovementEvaluation movement =
                evaluator.Evaluate(
                    natural,
                    breakthrough
                );

            Assert.That(
                movement.Components.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                movement.TryGetComponent(
                    SceneReleaseMovementComponentKind
                        .NaturalDrift,
                    out
                        SceneReleaseMovementComponent
                        naturalComponent
                ),
                Is.True
            );

            Assert.That(
                naturalComponent.Delta,
                Is.EqualTo(-0.20f)
            );

            Assert.That(
                movement.TryGetComponent(
                    SceneReleaseMovementComponentKind
                        .CanonBreakthrough,
                    out
                        SceneReleaseMovementComponent
                        breakthroughComponent
                ),
                Is.True
            );

            Assert.That(
                breakthroughComponent.Delta,
                Is.EqualTo(0.75f)
            );

            Assert.That(
                movement.TotalDelta,
                Is.EqualTo(0.55f)
                    .Within(0.0001f)
            );

            Assert.That(
                movement.ProjectedFieldPosition,
                Is.EqualTo(0.95f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            ZeroBreakthroughStillRecordsResolvedMovementComponent()
        {
            SceneReleaseMovementEvaluation movement =
                evaluator.Evaluate(
                    NaturalDrift(
                        0.40f,
                        -0.10f
                    ),
                    BreakthroughMovement(
                        0.40f,
                        0f
                    )
                );

            Assert.That(
                movement.Components.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                movement.TryGetComponent(
                    SceneReleaseMovementComponentKind
                        .CanonBreakthrough,
                    out
                        SceneReleaseMovementComponent
                        breakthrough
                ),
                Is.True
            );

            Assert.That(
                breakthrough.Delta,
                Is.EqualTo(0f)
            );

            Assert.That(
                movement.TotalDelta,
                Is.EqualTo(-0.10f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            MovementInputsMustShareFrozenReleaseState()
        {
            SceneReleaseNaturalDriftEvaluation natural =
                NaturalDrift(
                    0.40f,
                    0.10f
                );

            SceneReleaseCanonBreakthroughMovementEvaluation
                differentStart =
                    BreakthroughMovement(
                        0.50f,
                        0.20f
                    );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        natural,
                        differentStart
                    )
            );
        }

        [Test]
        public void
            CombinedMovementPreservesBothComponentKinds()
        {
            SceneReleaseMovementEvaluation movement =
                evaluator.Evaluate(
                    NaturalDrift(
                        0.20f,
                        0.10f
                    ),
                    BreakthroughMovement(
                        0.20f,
                        0.30f
                    )
                );

            Assert.That(
                movement.Components[0].Kind,
                Is.EqualTo(
                    SceneReleaseMovementComponentKind
                        .NaturalDrift
                )
            );

            Assert.That(
                movement.Components[1].Kind,
                Is.EqualTo(
                    SceneReleaseMovementComponentKind
                        .CanonBreakthrough
                )
            );
        }

        private static
            SceneReleaseNaturalDriftEvaluation
            NaturalDrift(
                float startPosition,
                float drift)
        {
            return new
                SceneReleaseNaturalDriftEvaluation(
                    "RELEASE",
                    "DEMO",
                    "OWNER",
                    "KVLT",
                    6,
                    startPosition,
                    3f,
                    2f,
                    2,
                    1f,
                    drift
                );
        }

        private static
            SceneReleaseCanonBreakthroughMovementEvaluation
            BreakthroughMovement(
                float startPosition,
                float drift)
        {
            /*
             * For composition tests the reduction has
             * already happened. Use one breakthrough
             * point and let the multiplier equal the
             * requested final drift.
             */
            if (drift == 0f)
            {
                return new
                    SceneReleaseCanonBreakthroughMovementEvaluation(
                        "RELEASE",
                        "DEMO",
                        "OWNER",
                        "KVLT",
                        6,
                        startPosition,
                        0,
                        0,
                        0,
                        0,
                        0,
                        1f,
                        0f
                    );
            }

            return new
                SceneReleaseCanonBreakthroughMovementEvaluation(
                    "RELEASE",
                    "DEMO",
                    "OWNER",
                    "KVLT",
                    6,
                    startPosition,
                    1,
                    1,
                    1,
                    0,
                    1,
                    drift,
                    drift
                );
        }
    }
}