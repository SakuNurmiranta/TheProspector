using System;
using NUnit.Framework;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class
        SceneReleaseOuterBoundaryEvaluatorTests
    {
        private readonly
            SceneReleaseOuterBoundaryEvaluator evaluator =
                new();

        [Test]
        public void
            PositionInsideBoundary_RemainsField()
        {
            SceneRelease release =
                FieldReleaseAt(
                    0.40f
                );

            SceneReleaseOuterBoundaryEvaluation
                result =
                    evaluator.Evaluate(
                        release,
                        outerBoundary: 0f,
                        settledTurn: 6
                    );

            Assert.That(
                result.Disposition,
                Is.EqualTo(
                    SceneReleaseOuterBoundaryDisposition
                        .RemainsField
                )
            );

            Assert.That(
                result.CrossedOuterBoundary,
                Is.False
            );

            Assert.That(
                result.SignedDistanceFromBoundary,
                Is.EqualTo(0.40f)
            );

            Assert.That(
                result.OutwardOvershoot,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void
            PositionExactlyOnBoundary_RemainsField()
        {
            SceneRelease release =
                FieldReleaseAt(
                    0f
                );

            SceneReleaseOuterBoundaryEvaluation
                result =
                    evaluator.Evaluate(
                        release,
                        0f,
                        6
                    );

            Assert.That(
                result.Disposition,
                Is.EqualTo(
                    SceneReleaseOuterBoundaryDisposition
                        .RemainsField
                )
            );

            Assert.That(
                result.SignedDistanceFromBoundary,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void
            PositionBeyondOuterBoundary_IsRejected()
        {
            SceneRelease release =
                FieldReleaseAt(
                    -0.25f
                );

            SceneReleaseOuterBoundaryEvaluation
                result =
                    evaluator.Evaluate(
                        release,
                        0f,
                        6
                    );

            Assert.That(
                result.Disposition,
                Is.EqualTo(
                    SceneReleaseOuterBoundaryDisposition
                        .Rejected
                )
            );

            Assert.That(
                result.CrossedOuterBoundary,
                Is.True
            );

            Assert.That(
                result.SignedDistanceFromBoundary,
                Is.EqualTo(-0.25f)
            );

            Assert.That(
                result.OutwardOvershoot,
                Is.EqualTo(0.25f)
            );
        }

        [Test]
        public void
            BoundaryEvaluationRejectsNonFieldOrMissingPosition()
        {
            SceneRelease fringe =
                new SceneRelease(
                    "Fringe",
                    "DEMO",
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        fringe,
                        0f,
                        6
                    )
            );

            SceneRelease field =
                new SceneRelease(
                    "Field",
                    "DEMO_2",
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            Assert.That(
                field.TryFetter(5),
                Is.True
            );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        field,
                        0f,
                        6
                    )
            );
        }

        private static SceneRelease FieldReleaseAt(
            float position)
        {
            SceneRelease release =
                new SceneRelease(
                    "Test Release",
                    "DEMO",
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            Assert.That(
                release.TryFetter(5),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    position,
                    5
                ),
                Is.True
            );

            return release;
        }
    }
}