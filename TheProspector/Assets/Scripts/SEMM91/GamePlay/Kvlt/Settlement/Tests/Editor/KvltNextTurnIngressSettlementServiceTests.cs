using System;
using NUnit.Framework;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltNextTurnIngressSettlementServiceTests
    {
        private const string SceneId =
            "KVLT";

        private const int Turn =
            8;

        private const int NextTurn =
            9;

        private const float FreshPosition =
            0.20f;

        private const float EntryCeiling =
            0.70f;

        private readonly
            KvltNextTurnIngressSettlementService
            service =
                new();

        [Test]
        public void
            BandWithoutResidentReleaseEntersAtFreshPosition()
        {
            SceneRelease fresh =
                FreshFetter(
                    "Fresh",
                    "OWNER_A",
                    Turn
                );

            KvltNextTurnIngressSettlementResult result =
                service.Settle(
                    SceneId,
                    Turn,
                    FreshPosition,
                    EntryCeiling,
                    new[]
                    {
                        fresh
                    }
                );

            Assert.That(
                result.IngressedCount,
                Is.EqualTo(1)
            );

            Assert.That(
                fresh.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(FreshPosition)
                    .Within(0.0001f)
            );

            Assert.That(
                result.IngressEvaluations[0]
                    .HasBandScenePositionBasis,
                Is.False
            );
        }

        [Test]
        public void
            ResidentReleaseMeanDeterminesBandScenePosition()
        {
            SceneRelease residentA =
                Resident(
                    "Resident A",
                    "OWNER_A",
                    0.40f
                );

            SceneRelease residentB =
                Resident(
                    "Resident B",
                    "OWNER_A",
                    0.60f
                );

            SceneRelease fresh =
                FreshFetter(
                    "Fresh",
                    "OWNER_A",
                    Turn
                );

            KvltNextTurnIngressSettlementResult result =
                service.Settle(
                    SceneId,
                    Turn,
                    FreshPosition,
                    EntryCeiling,
                    new[]
                    {
                        residentB,
                        fresh,
                        residentA
                    }
                );

            Assert.That(
                result.TryGet(
                    fresh.ReleaseId,
                    out SceneReleaseIngressEvaluation
                        evaluation
                ),
                Is.True
            );

            Assert.That(
                evaluation.BandScenePositionBasis,
                Is.EqualTo(0.50f)
                    .Within(0.0001f)
            );

            Assert.That(
                fresh.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.50f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            LowBandPositionCannotPushFreshReleaseBelowBaseline()
        {
            SceneRelease resident =
                Resident(
                    "Resident",
                    "OWNER_A",
                    0.10f
                );

            SceneRelease fresh =
                FreshFetter(
                    "Fresh",
                    "OWNER_A",
                    Turn
                );

            KvltNextTurnIngressSettlementResult result =
                service.Settle(
                    SceneId,
                    Turn,
                    FreshPosition,
                    EntryCeiling,
                    new[]
                    {
                        resident,
                        fresh
                    }
                );

            Assert.That(
                result.IngressEvaluations[0]
                    .BandScenePositionBasis,
                Is.EqualTo(0.10f)
                    .Within(0.0001f)
            );

            Assert.That(
                fresh.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(FreshPosition)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            StrongBandPositionIsCappedAtEntryCeiling()
        {
            SceneRelease resident =
                Resident(
                    "Resident",
                    "OWNER_A",
                    0.95f
                );

            SceneRelease fresh =
                FreshFetter(
                    "Fresh",
                    "OWNER_A",
                    Turn
                );

            KvltNextTurnIngressSettlementResult result =
                service.Settle(
                    SceneId,
                    Turn,
                    FreshPosition,
                    EntryCeiling,
                    new[]
                    {
                        resident,
                        fresh
                    }
                );

            Assert.That(
                result.IngressEvaluations[0]
                    .BandScenePositionBasis,
                Is.EqualTo(0.95f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.IngressEvaluations[0]
                    .WasCapped,
                Is.True
            );

            Assert.That(
                fresh.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(EntryCeiling)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            SeveralFreshReleasesUseSamePrePlacementBandSnapshot()
        {
            SceneRelease resident =
                Resident(
                    "Resident",
                    "OWNER_A",
                    0.95f
                );

            SceneRelease freshA =
                FreshFetter(
                    "Fresh A",
                    "OWNER_A",
                    Turn
                );

            SceneRelease freshB =
                FreshFetter(
                    "Fresh B",
                    "OWNER_A",
                    Turn
                );

            KvltNextTurnIngressSettlementResult result =
                service.Settle(
                    SceneId,
                    Turn,
                    FreshPosition,
                    EntryCeiling,
                    new[]
                    {
                        freshB,
                        resident,
                        freshA
                    }
                );

            Assert.That(
                result.TryGet(
                    freshA.ReleaseId,
                    out SceneReleaseIngressEvaluation
                        evaluationA
                ),
                Is.True
            );

            Assert.That(
                result.TryGet(
                    freshB.ReleaseId,
                    out SceneReleaseIngressEvaluation
                        evaluationB
                ),
                Is.True
            );

            /*
             * Both use the original resident mean.
             * The first newly placed release cannot
             * alter the second one's provenance.
             */
            Assert.That(
                evaluationA.BandScenePositionBasis,
                Is.EqualTo(0.95f)
                    .Within(0.0001f)
            );

            Assert.That(
                evaluationB.BandScenePositionBasis,
                Is.EqualTo(0.95f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            MissedPriorTurnPlacementIsRejectedAsStaleChronology()
        {
            SceneRelease stale =
                FreshFetter(
                    "Stale",
                    "OWNER_A",
                    Turn - 1
                );

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    service.Settle(
                        SceneId,
                        Turn,
                        FreshPosition,
                        EntryCeiling,
                        new[]
                        {
                            stale
                        }
                    )
            );

            Assert.That(
                stale.HasFieldPosition,
                Is.False
            );
        }

        private static SceneRelease
            FreshFetter(
                string displayName,
                string owner,
                int fetterTurn)
        {
            SceneRelease release =
                new(
                    displayName,
                    "DEMO_" + displayName,
                    owner,
                    SceneId,
                    fetterTurn,
                    1f
                );

            Assert.That(
                release.TryFetter(
                    fetterTurn
                ),
                Is.True
            );

            return release;
        }

        private static SceneRelease Resident(
            string displayName,
            string owner,
            float position)
        {
            SceneRelease release =
                new(
                    displayName,
                    "DEMO_" + displayName,
                    owner,
                    SceneId,
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
                    position,
                    Turn
                ),
                Is.True
            );

            return release;
        }
    }
}