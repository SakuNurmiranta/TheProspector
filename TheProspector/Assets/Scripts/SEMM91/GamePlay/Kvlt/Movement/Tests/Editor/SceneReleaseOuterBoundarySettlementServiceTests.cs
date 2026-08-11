using System;
using NUnit.Framework;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class
        SceneReleaseOuterBoundarySettlementServiceTests
    {
        private readonly
            SceneReleaseOuterBoundaryEvaluator evaluator =
                new();

        private readonly
            SceneReleaseOuterBoundarySettlementService
                settlement =
                    new();

        [Test]
        public void
            CrossingReleaseIsRejectedWhileSurvivorRemainsField()
        {
            SceneRelease survivor =
                FieldRelease(
                    "SURVIVOR",
                    0.30f
                );

            SceneRelease rejected =
                FieldRelease(
                    "REJECTED",
                    -0.20f
                );

            SceneReleaseOuterBoundaryEvaluation
                survivorEvaluation =
                    evaluator.Evaluate(
                        survivor,
                        0f,
                        6
                    );

            SceneReleaseOuterBoundaryEvaluation
                rejectedEvaluation =
                    evaluator.Evaluate(
                        rejected,
                        0f,
                        6
                    );

            var applications =
                settlement.Apply(
                    new[]
                    {
                        rejected,
                        survivor
                    },
                    new[]
                    {
                        rejectedEvaluation,
                        survivorEvaluation
                    }
                );

            Assert.That(
                applications.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                survivor.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                rejected.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Rejected
                )
            );

            Assert.That(
                applications[0].SceneReleaseId,
                Is.EqualTo(
                    rejected.ReleaseId
                )
            );
        }

        [Test]
        public void
            InvalidBatchRejectsBeforeAnyTerminalMutation()
        {
            SceneRelease releaseA =
                FieldRelease(
                    "A",
                    -0.10f
                );

            SceneRelease releaseB =
                FieldRelease(
                    "B",
                    -0.20f
                );

            SceneReleaseOuterBoundaryEvaluation validA =
                evaluator.Evaluate(
                    releaseA,
                    0f,
                    6
                );

            SceneReleaseOuterBoundaryEvaluation staleB =
                evaluator.Evaluate(
                    releaseB,
                    0f,
                    6
                );

            /*
             * Mutate B after the evaluation so its
             * frozen position is stale.
             */
            Assert.That(
                releaseB.TryApplyFieldMovement(
                    0.50f,
                    7,
                    out _
                ),
                Is.True
            );

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    settlement.Apply(
                        new[]
                        {
                            releaseA,
                            releaseB
                        },
                        new[]
                        {
                            validA,
                            staleB
                        }
                    )
            );

            /*
             * A qualified for rejection but must not
             * have been mutated because the complete
             * batch failed validation first.
             */
            Assert.That(
                releaseA.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                releaseA.RejectionState,
                Is.Null
            );

            Assert.That(
                releaseB.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );
        }

        [Test]
        public void
            RejectedReleaseLeavesFutureFieldBoundaryPopulation()
        {
            SceneRelease release =
                FieldRelease(
                    "A",
                    -0.10f
                );

            SceneReleaseOuterBoundaryEvaluation evaluation =
                evaluator.Evaluate(
                    release,
                    0f,
                    6
                );

            Assert.That(
                settlement.Apply(
                    new[]
                    {
                        release
                    },
                    new[]
                    {
                        evaluation
                    }
                ).Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Rejected
                )
            );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        0f,
                        7
                    )
            );
        }

        [Test]
        public void
            RejectionTransitionPreservesHistoricalFieldPosition()
        {
            SceneRelease release =
                FieldRelease(
                    "A",
                    0.60f
                );

            Assert.That(
                release.TryApplyFieldMovement(
                    -0.90f,
                    6,
                    out _
                ),
                Is.True
            );

            SceneReleaseOuterBoundaryEvaluation evaluation =
                evaluator.Evaluate(
                    release,
                    0f,
                    6
                );

            var applications =
                settlement.Apply(
                    new[]
                    {
                        release
                    },
                    new[]
                    {
                        evaluation
                    }
                );

            Assert.That(
                applications.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.HasFieldPosition,
                Is.True
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(-0.30f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.FieldPositionState
                    .PeakInwardPosition,
                Is.EqualTo(0.60f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.RejectionState
                    .PeakInwardPosition,
                Is.EqualTo(0.60f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.LifecycleTransitions[
                    release.LifecycleTransitions.Count - 1
                ].ToState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Rejected
                )
            );
        }

        private static SceneRelease FieldRelease(
            string suffix,
            float position)
        {
            SceneRelease release =
                new SceneRelease(
                    "Release " + suffix,
                    "DEMO_" + suffix,
                    "OWNER_" + suffix,
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