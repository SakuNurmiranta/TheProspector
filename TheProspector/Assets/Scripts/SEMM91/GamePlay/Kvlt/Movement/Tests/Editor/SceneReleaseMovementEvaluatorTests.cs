using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class SceneReleaseMovementEvaluatorTests
    {
        private readonly
            SceneReleaseMovementEvaluator evaluator =
                new();

        [Test]
        public void
            NaturalDrift_BecomesCompleteMovementEvaluation()
        {
            SceneReleaseNaturalDriftEvaluation drift =
                NaturalDrift(
                    startPosition: 0.40f,
                    drift: 0.25f
                );

            SceneReleaseMovementEvaluation movement =
                evaluator.Evaluate(
                    drift
                );

            Assert.That(
                movement.SceneReleaseId,
                Is.EqualTo("RELEASE")
            );

            Assert.That(
                movement.StartFieldPosition,
                Is.EqualTo(0.40f)
            );

            Assert.That(
                movement.Components.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                movement.Components[0].Kind,
                Is.EqualTo(
                    SceneReleaseMovementComponentKind
                        .NaturalDrift
                )
            );

            Assert.That(
                movement.TotalDelta,
                Is.EqualTo(0.25f)
            );

            Assert.That(
                movement.ProjectedFieldPosition,
                Is.EqualTo(0.65f)
                    .Within(0.0001f)
            );

            Assert.That(
                movement.TryGetComponent(
                    SceneReleaseMovementComponentKind
                        .NaturalDrift,
                    out
                        SceneReleaseMovementComponent
                        component
                ),
                Is.True
            );

            Assert.That(
                component.Delta,
                Is.EqualTo(0.25f)
            );
        }

        [Test]
        public void
            StableNaturalDrift_StillProducesZeroDeltaMovementEvaluation()
        {
            SceneReleaseMovementEvaluation movement =
                evaluator.Evaluate(
                    NaturalDrift(
                        startPosition: 0.40f,
                        drift: 0f
                    )
                );

            Assert.That(
                movement.TotalDelta,
                Is.EqualTo(0f)
            );

            Assert.That(
                movement.ProjectedFieldPosition,
                Is.EqualTo(0.40f)
            );

            Assert.That(
                movement.Components.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            MovementEvaluationRejectsDuplicateMovementPolicy()
        {
            SceneReleaseMovementComponent first =
                new SceneReleaseMovementComponent(
                    "RELEASE",
                    "KVLT",
                    6,
                    SceneReleaseMovementComponentKind
                        .NaturalDrift,
                    0.1f,
                    "SOURCE_A"
                );

            SceneReleaseMovementComponent second =
                new SceneReleaseMovementComponent(
                    "RELEASE",
                    "KVLT",
                    6,
                    SceneReleaseMovementComponentKind
                        .NaturalDrift,
                    0.2f,
                    "SOURCE_B"
                );

            Assert.Throws<
                ArgumentException>(
                () =>
                    new SceneReleaseMovementEvaluation(
                        "RELEASE",
                        "DEMO",
                        "OWNER",
                        "KVLT",
                        6,
                        0.4f,
                        new[]
                        {
                            first,
                            second
                        }
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
    }
}