using System;
using NUnit.Framework;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class
        SceneReleaseMovementSettlementServiceTests
    {
        private readonly
            SceneReleaseMovementEvaluator evaluator =
                new();

        private readonly
            SceneReleaseMovementSettlementService
                settlement =
                    new();

        [Test]
        public void
            ValidFrozenMovement_IsAppliedToAuthoritativePosition()
        {
            SceneRelease release =
                FieldRelease(
                    "A",
                    0.40f
                );

            SceneReleaseMovementEvaluation movement =
                Movement(
                    release,
                    0.40f,
                    0.25f,
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
                        movement
                    }
                );

            Assert.That(
                applications.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.65f)
                    .Within(0.0001f)
            );

            Assert.That(
                applications[0]
                    .PositionTransition
                    .PreviousPosition,
                Is.EqualTo(0.40f)
            );

            Assert.That(
                applications[0]
                    .PositionTransition
                    .AppliedDelta,
                Is.EqualTo(0.25f)
                    .Within(0.0001f)
            );

            Assert.That(
                applications[0]
                    .PositionTransition
                    .NewPosition,
                Is.EqualTo(0.65f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            StableOrbit_IsAppliedAndRecorded()
        {
            SceneRelease release =
                FieldRelease(
                    "A",
                    0.40f
                );

            SceneReleaseMovementEvaluation movement =
                Movement(
                    release,
                    0.40f,
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
                        movement
                    }
                );

            Assert.That(
                applications.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.40f)
            );

            Assert.That(
                release.FieldPositionState
                    .LastMovementTurn,
                Is.EqualTo(6)
            );

            Assert.That(
                release.FieldPositionState
                    .Transitions.Count,
                Is.EqualTo(2)
            );
        }

        [Test]
public void
    SeveralFrozenMovementsApplyFromTheirOwnStartPositions()
{
    SceneRelease releaseA =
        FieldRelease(
            "A",
            0.20f
        );

    SceneRelease releaseB =
        FieldRelease(
            "B",
            0.80f
        );

    SceneReleaseMovementEvaluation movementA =
        Movement(
            releaseA,
            0.20f,
            0.30f,
            6
        );

    SceneReleaseMovementEvaluation movementB =
        Movement(
            releaseB,
            0.80f,
            -0.40f,
            6
        );

    /*
     * Deliberately provide both collections in
     * reverse construction order.
     */
    var applications =
        settlement.Apply(
            new[]
            {
                releaseB,
                releaseA
            },
            new[]
            {
                movementB,
                movementA
            }
        );

    Assert.That(
        releaseA.FieldPositionState
            .CurrentPosition,
        Is.EqualTo(0.50f)
            .Within(0.0001f)
    );

    Assert.That(
        releaseB.FieldPositionState
            .CurrentPosition,
        Is.EqualTo(0.40f)
            .Within(0.0001f)
    );

    /*
     * Application history is deterministic by
     * SceneReleaseId, independent of input
     * collection order.
     *
     * ReleaseIds are generated GUIDs, so the test
     * must derive the expected lexical order rather
     * than assume release A sorts before release B.
     */
    string expectedFirstId;
    string expectedSecondId;

    if (string.CompareOrdinal(
            releaseA.ReleaseId,
            releaseB.ReleaseId) < 0)
    {
        expectedFirstId =
            releaseA.ReleaseId;

        expectedSecondId =
            releaseB.ReleaseId;
    }
    else
    {
        expectedFirstId =
            releaseB.ReleaseId;

        expectedSecondId =
            releaseA.ReleaseId;
    }

    Assert.That(
        applications[0].SceneReleaseId,
        Is.EqualTo(expectedFirstId)
    );

    Assert.That(
        applications[1].SceneReleaseId,
        Is.EqualTo(expectedSecondId)
    );
}

        [Test]
        public void
            OneInvalidEvaluationRejectsBatchBeforeAnyReleaseMoves()
        {
            SceneRelease releaseA =
                FieldRelease(
                    "A",
                    0.20f
                );

            SceneRelease releaseB =
                FieldRelease(
                    "B",
                    0.80f
                );

            SceneReleaseMovementEvaluation valid =
                Movement(
                    releaseA,
                    0.20f,
                    0.30f,
                    6
                );

            /*
             * Evaluation claims B's start position was
             * 0.70 even though authoritative frozen
             * position is still 0.80.
             */
            SceneReleaseMovementEvaluation stale =
                Movement(
                    releaseB,
                    0.70f,
                    -0.20f,
                    6
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
                            valid,
                            stale
                        }
                    )
            );

            /*
             * A was valid but MUST NOT have moved,
             * because the complete batch failed
             * validation before mutation began.
             */
            Assert.That(
                releaseA.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.20f)
            );

            Assert.That(
                releaseB.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.80f)
            );

            Assert.That(
                releaseA.FieldPositionState
                    .LastMovementTurn,
                Is.EqualTo(-1)
            );

            Assert.That(
                releaseB.FieldPositionState
                    .LastMovementTurn,
                Is.EqualTo(-1)
            );
        }

        [Test]
        public void
            PreviouslyMovedReleaseRejectsDuplicateSettlementTurn()
        {
            SceneRelease release =
                FieldRelease(
                    "A",
                    0.40f
                );

            SceneReleaseMovementEvaluation first =
                Movement(
                    release,
                    0.40f,
                    0.10f,
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
                        first
                    }
                ).Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.50f)
                    .Within(0.0001f)
            );

            SceneReleaseMovementEvaluation duplicate =
                Movement(
                    release,
                    0.50f,
                    0.20f,
                    6
                );

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    settlement.Apply(
                        new[]
                        {
                            release
                        },
                        new[]
                        {
                            duplicate
                        }
                    )
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.50f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            MovementEvaluationMustMatchExactReleaseProvenance()
        {
            SceneRelease release =
                FieldRelease(
                    "A",
                    0.40f
                );

            SceneReleaseMovementComponent component =
                new SceneReleaseMovementComponent(
                    release.ReleaseId,
                    release.HostedSceneNodeId,
                    6,
                    SceneReleaseMovementComponentKind
                        .NaturalDrift,
                    0.10f,
                    "SOURCE"
                );

            SceneReleaseMovementEvaluation
                wrongDemo =
                    new SceneReleaseMovementEvaluation(
                        release.ReleaseId,
                        "OTHER_DEMO",
                        release.SourceOwnerEntityId,
                        release.HostedSceneNodeId,
                        6,
                        0.40f,
                        new[]
                        {
                            component
                        }
                    );

            Assert.Throws<
                ArgumentException>(
                () =>
                    settlement.Apply(
                        new[]
                        {
                            release
                        },
                        new[]
                        {
                            wrongDemo
                        }
                    )
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.40f)
            );

            Assert.That(
                release.FieldPositionState
                    .LastMovementTurn,
                Is.EqualTo(-1)
            );
        }

        private SceneReleaseMovementEvaluation
            Movement(
                SceneRelease release,
                float frozenStartPosition,
                float drift,
                int turn)
        {
            return evaluator.Evaluate(
                new SceneReleaseNaturalDriftEvaluation(
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    release.SourceOwnerEntityId,
                    release.HostedSceneNodeId,
                    turn,
                    frozenStartPosition,
                    3f,
                    2f,
                    2,
                    1f,
                    drift
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