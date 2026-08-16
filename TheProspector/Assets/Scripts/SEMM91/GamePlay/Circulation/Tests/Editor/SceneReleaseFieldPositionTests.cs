using NUnit.Framework;

namespace SEMM91.GamePlay.Circulation.Tests
{
    public class
        SceneReleaseFieldPositionTests
    {
        [Test]
        public void
            NewFringeRelease_HasNoFieldPosition()
        {
            SceneRelease release =
                Release();

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Fringe
                )
            );

            Assert.That(
                release.HasFieldPosition,
                Is.False
            );

            Assert.That(
                release.FieldPositionState,
                Is.Null
            );
        }

        [Test]
        public void
            FringeRelease_CannotReceiveFieldPosition()
        {
            SceneRelease release =
                Release();

            Assert.That(
                release.TryEstablishFieldPosition(
                    0.25f,
                    5
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
            FetteredRelease_CanReceiveInitialFieldPositionOnce()
        {
            SceneRelease release =
                Release();

            Assert.That(
                release.TryFetter(6),
                Is.True
            );

            /*
             * Current state may be Field, but the
             * historical placement turn cannot precede
             * the actual fettering transition.
             */
            Assert.That(
                release.TryEstablishFieldPosition(
                    0.10f,
                    5
                ),
                Is.False
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    0.25f,
                    6
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
                Is.EqualTo(0.25f)
            );

            Assert.That(
                release.FieldPositionState
                    .PeakInwardPosition,
                Is.EqualTo(0.25f)
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

            Assert.That(
                release.FieldPositionState
                    .Transitions[0]
                    .Kind,
                Is.EqualTo(
                    SceneReleaseFieldPositionTransitionKind
                        .InitialFieldPlacement
                )
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    0.75f,
                    7
                ),
                Is.False
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.25f)
            );
        }

        [Test]
        public void
            InitialPlacementAndFirstMovementMayOccurOnSameTurn()
        {
            SceneRelease release =
                FieldReleaseAt(
                    0.25f,
                    turn: 5
                );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.10f,
                    5,
                    out
                        SceneReleaseFieldPositionTransition
                        movement
                ),
                Is.True
            );

            Assert.That(
                movement.PreviousPosition,
                Is.EqualTo(0.25f)
            );

            Assert.That(
                movement.NewPosition,
                Is.EqualTo(0.35f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.35f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            PositiveMovement_IsInwardAndRaisesPeakPosition()
        {
            SceneRelease release =
                FieldReleaseAt(
                    0.20f,
                    turn: 5
                );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.30f,
                    6,
                    out
                        SceneReleaseFieldPositionTransition
                        transition
                ),
                Is.True
            );

            Assert.That(
                transition.AppliedDelta,
                Is.EqualTo(0.30f)
                    .Within(0.0001f)
            );

            Assert.That(
                transition.PreviousPosition,
                Is.EqualTo(0.20f)
            );

            Assert.That(
                transition.NewPosition,
                Is.EqualTo(0.50f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.50f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.FieldPositionState
                    .PeakInwardPosition,
                Is.EqualTo(0.50f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            OutwardMovement_DoesNotErasePeakInwardPosition()
        {
            SceneRelease release =
                FieldReleaseAt(
                    0.20f,
                    turn: 5
                );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.40f,
                    6,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.60f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.TryApplyFieldMovement(
                    -0.50f,
                    7,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.10f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.FieldPositionState
                    .PeakInwardPosition,
                Is.EqualTo(0.60f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            StableOrbit_RecordsZeroMovement()
        {
            SceneRelease release =
                FieldReleaseAt(
                    0.40f,
                    turn: 5
                );

            Assert.That(
                release.TryApplyFieldMovement(
                    0f,
                    6,
                    out
                        SceneReleaseFieldPositionTransition
                        transition
                ),
                Is.True
            );

            Assert.That(
                transition.AppliedDelta,
                Is.EqualTo(0f)
            );

            Assert.That(
                transition.PreviousPosition,
                Is.EqualTo(0.40f)
            );

            Assert.That(
                transition.NewPosition,
                Is.EqualTo(0.40f)
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.40f)
            );

            Assert.That(
                release.FieldPositionState
                    .Transitions.Count,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void
            FieldMovement_IsStrictlyChronological()
        {
            SceneRelease release =
                FieldReleaseAt(
                    0.25f,
                    turn: 5
                );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.10f,
                    6,
                    out _
                ),
                Is.True
            );

            /*
             * Same settlement turn cannot mutate
             * position twice.
             */
            Assert.That(
                release.TryApplyFieldMovement(
                    0.20f,
                    6,
                    out _
                ),
                Is.False
            );

            /*
             * Nor can an earlier settlement be
             * inserted after turn 6 was recorded.
             */
            Assert.That(
                release.TryApplyFieldMovement(
                    0.50f,
                    5,
                    out _
                ),
                Is.False
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.35f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.20f,
                    7,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.55f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.FieldPositionState
                    .LastMovementTurn,
                Is.EqualTo(7)
            );
        }

        [Test]
        public void
            FailedToFetterRelease_CannotAcquireOrMoveFieldPosition()
        {
            SceneRelease release =
                Release();

            Assert.That(
                release.TryFailToFetter(5),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    0.25f,
                    5
                ),
                Is.False
            );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.10f,
                    6,
                    out _
                ),
                Is.False
            );

            Assert.That(
                release.HasFieldPosition,
                Is.False
            );
        }

        private static SceneRelease
            FieldReleaseAt(
                float position,
                int turn)
        {
            SceneRelease release =
                Release();

            Assert.That(
                release.TryFetter(turn),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    position,
                    turn
                ),
                Is.True
            );

            return release;
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