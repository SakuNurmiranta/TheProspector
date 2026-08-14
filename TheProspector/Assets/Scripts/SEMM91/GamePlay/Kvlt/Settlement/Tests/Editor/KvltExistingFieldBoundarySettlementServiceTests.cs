using NUnit.Framework;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltExistingFieldBoundarySettlementServiceTests
    {
        private const int Turn =
            8;

        private const float OuterBoundary =
            0.10f;

        private readonly
            KvltExistingFieldBoundarySettlementService
            service =
                new();

        [Test]
        public void
            ResidentReleaseCrossingOuterBoundaryIsRejected()
        {
            SceneRelease release =
                ResidentRelease(
                    "RELEASE_REJECTED",
                    initialPosition:
                        0.20f
                );

            Assert.That(
                release.TryApplyFieldMovement(
                    -0.15f,
                    Turn,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.05f)
                    .Within(0.0001f)
            );

            KvltExistingFieldBoundarySettlementResult
                result =
                    service.Settle(
                        "KVLT",
                        Turn,
                        OuterBoundary,
                        new[]
                        {
                            release
                        }
                    );

            Assert.That(
                result.BoundaryEvaluations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.RejectionApplications.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.RejectedCount,
                Is.EqualTo(1)
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Rejected
                )
            );

            Assert.That(
                release.RejectionState,
                Is.Not.Null
            );

            Assert.That(
                release.RejectionState.RejectedTurn,
                Is.EqualTo(
                    Turn
                )
            );

            Assert.That(
                release.RejectionState
                    .RejectedPosition,
                Is.EqualTo(0.05f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            ResidentReleaseAtOuterBoundaryRemainsField()
        {
            SceneRelease release =
                ResidentRelease(
                    "RELEASE_SURVIVES",
                    initialPosition:
                        0.20f
                );

            Assert.That(
                release.TryApplyFieldMovement(
                    -0.10f,
                    Turn,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(
                    OuterBoundary
                ).Within(0.0001f)
            );

            KvltExistingFieldBoundarySettlementResult
                result =
                    service.Settle(
                        "KVLT",
                        Turn,
                        OuterBoundary,
                        new[]
                        {
                            release
                        }
                    );

            Assert.That(
                result.BoundaryEvaluations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.BoundaryEvaluations[0]
                    .CrossedOuterBoundary,
                Is.False
            );

            Assert.That(
                result.RejectionApplications,
                Is.Empty
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
            NewlyFetteredPreIngressReleaseIsNotBoundaryEvaluated()
        {
            SceneRelease release =
                new(
                    "Freshly Fettered",
                    "DEMO_FRESH",
                    "OWNER",
                    "KVLT",
                    Turn,
                    1f
                );

            Assert.That(
                release.TryFetter(
                    Turn
                ),
                Is.True
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                release.HasFieldPosition,
                Is.False
            );

            KvltExistingFieldBoundarySettlementResult
                result =
                    service.Settle(
                        "KVLT",
                        Turn,
                        OuterBoundary,
                        new[]
                        {
                            release
                        }
                    );

            Assert.That(
                result.BoundaryEvaluations,
                Is.Empty
            );

            Assert.That(
                result.RejectionApplications,
                Is.Empty
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                release.HasFieldPosition,
                Is.False
            );
        }

        private static SceneRelease
            ResidentRelease(
                string releaseName,
                float initialPosition)
        {
            SceneRelease release =
                new(
                    releaseName,
                    "DEMO_" + releaseName,
                    "OWNER",
                    "KVLT",
                    Turn - 2,
                    1f
                );

            Assert.That(
                release.TryFetter(
                    Turn - 1
                ),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    initialPosition,
                    Turn - 1
                ),
                Is.True
            );

            return release;
        }
    }
}