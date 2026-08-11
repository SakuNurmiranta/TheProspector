using NUnit.Framework;

namespace SEMM91.GamePlay.Circulation.Tests.Editor
{
    public class SceneReleaseRejectionTests
    {
        [Test]
        public void
            FieldReleaseBeyondOuterBoundary_BecomesRejected()
        {
            SceneRelease release =
                FieldRelease(
                    initialPosition: 0.40f
                );

            Assert.That(
                release.TryApplyFieldMovement(
                    -0.70f,
                    6,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(-0.30f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.TryReject(
                    6,
                    outerBoundary: 0f
                ),
                Is.True
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Rejected
                )
            );

            Assert.That(
                release.HasRejectionState,
                Is.True
            );

            Assert.That(
                release.RejectionState.RejectedTurn,
                Is.EqualTo(6)
            );

            Assert.That(
                release.RejectionState
                    .RejectedPosition,
                Is.EqualTo(-0.30f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.RejectionState
                    .OuterBoundary,
                Is.EqualTo(0f)
            );

            Assert.That(
                release.RejectionState
                    .OutwardOvershoot,
                Is.EqualTo(0.30f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.RejectionState
                    .PeakInwardPosition,
                Is.EqualTo(0.40f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            ExactlyOnOuterBoundary_RemainsField()
        {
            SceneRelease release =
                FieldRelease(
                    initialPosition: 0.40f
                );

            Assert.That(
                release.TryApplyFieldMovement(
                    -0.40f,
                    6,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.TryReject(
                    6,
                    outerBoundary: 0f
                ),
                Is.False
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                release.RejectionState,
                Is.Null
            );
        }

        [Test]
        public void
            RejectionIsDistinctFromFailedToFetter()
        {
            SceneRelease failed =
                new SceneRelease(
                    "Failed",
                    "DEMO_FAILED",
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            Assert.That(
                failed.TryFailToFetter(5),
                Is.True
            );

            Assert.That(
                failed.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .FailedToFetter
                )
            );

            Assert.That(
                failed.TryReject(
                    6,
                    0f
                ),
                Is.False
            );

            Assert.That(
                failed.HasRejectionState,
                Is.False
            );

            SceneRelease rejected =
                FieldRelease(
                    initialPosition: 0.20f
                );

            Assert.That(
                rejected.TryApplyFieldMovement(
                    -0.30f,
                    6,
                    out _
                ),
                Is.True
            );

            Assert.That(
                rejected.TryReject(
                    6,
                    0f
                ),
                Is.True
            );

            Assert.That(
                rejected.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Rejected
                )
            );

            Assert.That(
                rejected.HasFieldPosition,
                Is.True
            );

            Assert.That(
                rejected.HasRejectionState,
                Is.True
            );
        }

        [Test]
        public void
            RejectionRetainsDeepestHistoricalPenetration()
        {
            SceneRelease release =
                FieldRelease(
                    initialPosition: 0.20f
                );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.60f,
                    6,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.TryApplyFieldMovement(
                    -1.10f,
                    7,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.TryReject(
                    7,
                    0f
                ),
                Is.True
            );

            Assert.That(
                release.RejectionState
                    .RejectedPosition,
                Is.EqualTo(-0.30f)
                    .Within(0.0001f)
            );

            Assert.That(
                release.RejectionState
                    .PeakInwardPosition,
                Is.EqualTo(0.80f)
                    .Within(0.0001f)
            );
        }

        private static SceneRelease FieldRelease(
            float initialPosition)
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
                    initialPosition,
                    5
                ),
                Is.True
            );

            return release;
        }
    }
}