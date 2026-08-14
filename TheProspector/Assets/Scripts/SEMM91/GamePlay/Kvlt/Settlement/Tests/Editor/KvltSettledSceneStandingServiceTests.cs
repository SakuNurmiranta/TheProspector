using NUnit.Framework;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltSettledSceneStandingServiceTests
    {
        private const int Turn =
            8;

        private readonly
            KvltSettledSceneStandingService
            service =
                new();

        [Test]
        public void
            NewlyFetteredOwnerGetsExplicitNoStandingSnapshot()
        {
            SceneRelease release =
                new(
                    "Fresh Release",
                    "DEMO_FRESH",
                    "OWNER_A",
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
                release.HasFieldPosition,
                Is.False
            );

            KvltSettledSceneStandingResult result =
                service.Settle(
                    "KVLT",
                    Turn,
                    new[]
                    {
                        release
                    },
                    Policy()
                );

            Assert.That(
                result.OwnerCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.TryGet(
                    "OWNER_A",
                    out SceneStandingEvaluation
                        standing
                ),
                Is.True
            );

            Assert.That(
                standing.HasStanding,
                Is.False
            );

            Assert.That(
                standing.Standing,
                Is.Null
            );

            Assert.That(
                standing.Contributions,
                Is.Empty
            );

            /*
             * This explicit turn-t evaluation can
             * safely become the frozen basis for
             * placement on turn t+1.
             */
            Assert.That(
                standing.SettledTurn,
                Is.EqualTo(
                    Turn
                )
            );
        }

        [Test]
        public void
            SameTurnRejectionIsRememberedAsStandingScar()
        {
            SceneRelease release =
                Resident(
                    "Release Rejected",
                    "OWNER_A",
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

            new
                KvltExistingFieldBoundarySettlementService()
                .Settle(
                    "KVLT",
                    Turn,
                    outerBoundary:
                        0.10f,
                    new[]
                    {
                        release
                    }
                );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Rejected
                )
            );

            KvltSettledSceneStandingResult result =
                service.Settle(
                    "KVLT",
                    Turn,
                    new[]
                    {
                        release
                    },
                    Policy()
                );

            Assert.That(
                result.TryGet(
                    "OWNER_A",
                    out SceneStandingEvaluation
                        standing
                ),
                Is.True
            );

            Assert.That(
                standing.HasStanding,
                Is.True
            );

            Assert.That(
                standing.Contributions.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                standing.Contributions[0].Kind,
                Is.EqualTo(
                    SceneStandingContributionKind
                        .RejectionScar
                )
            );

            Assert.That(
                standing.Standing,
                Is.EqualTo(0.05f)
                    .Within(0.0001f)
            );

            Assert.That(
                standing.Contributions[0].BasisTurn,
                Is.EqualTo(
                    Turn
                )
            );
        }

        [Test]
        public void
            OwnerOrderingIsDeterministicAndUsesFinalResidentPositions()
        {
            SceneRelease ownerB =
                Resident(
                    "Release B",
                    "OWNER_B",
                    initialPosition:
                        0.60f
                );

            SceneRelease ownerA =
                Resident(
                    "Release A",
                    "OWNER_A",
                    initialPosition:
                        0.30f
                );

            Assert.That(
                ownerB.TryApplyFieldMovement(
                    0.10f,
                    Turn,
                    out _
                ),
                Is.True
            );

            Assert.That(
                ownerA.TryApplyFieldMovement(
                    0.20f,
                    Turn,
                    out _
                ),
                Is.True
            );

            /*
             * Supply reverse owner order deliberately.
             */
            KvltSettledSceneStandingResult result =
                service.Settle(
                    "KVLT",
                    Turn,
                    new[]
                    {
                        ownerB,
                        ownerA
                    },
                    Policy()
                );

            Assert.That(
                result.Evaluations.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                result.Evaluations[0]
                    .SourceOwnerEntityId,
                Is.EqualTo(
                    "OWNER_A"
                )
            );

            Assert.That(
                result.Evaluations[1]
                    .SourceOwnerEntityId,
                Is.EqualTo(
                    "OWNER_B"
                )
            );

            Assert.That(
                result.Evaluations[0].Standing,
                Is.EqualTo(0.50f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.Evaluations[1].Standing,
                Is.EqualTo(0.70f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.Evaluations[0]
                    .Contributions[0].Kind,
                Is.EqualTo(
                    SceneStandingContributionKind
                        .ActiveField
                )
            );
        }

        private static SceneRelease Resident(
            string releaseName,
            string ownerEntityId,
            float initialPosition)
        {
            SceneRelease release =
                new(
                    releaseName,
                    "DEMO_" + releaseName,
                    ownerEntityId,
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

        private static
            SceneStandingProjectionPolicy
            Policy()
        {
            return new SceneStandingProjectionPolicy(
                canonLegacyBaseWeight:
                    1f,
                canonLegacyBreakthroughDegreeWeight:
                    1f,
                rejectionScarBaseWeight:
                    1f,
                rejectionPeakPenetrationWeight:
                    0f,
                rejectionOutwardOvershootWeight:
                    0f
            );
        }
    }
}