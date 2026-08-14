using System;
using NUnit.Framework;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltNextTurnIngressSettlementServiceTests
    {
        private const int Turn =
            8;

        private const int NextTurn =
            9;

        private const string SceneId =
            "KVLT";

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
            FreshFetterWithoutStandingEntersAtFreshPositionOnNextTurn()
        {
            SceneRelease release =
                FreshFetter(
                    "Fresh",
                    "OWNER_A",
                    Turn
                );

            KvltSettledSceneStandingResult standing =
                Snapshot(
                    NoStanding(
                        "OWNER_A"
                    )
                );

            KvltNextTurnIngressSettlementResult
                result =
                    service.Settle(
                        SceneId,
                        Turn,
                        FreshPosition,
                        EntryCeiling,
                        new[]
                        {
                            release
                        },
                        standing
                    );

            Assert.That(
                result.IngressedCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.PlacementTurn,
                Is.EqualTo(
                    NextTurn
                )
            );

            Assert.That(
                release.HasFieldPosition,
                Is.True
            );

            Assert.That(
                release.FieldPositionState
                    .EstablishedTurn,
                Is.EqualTo(
                    NextTurn
                )
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(
                    FreshPosition
                ).Within(0.0001f)
            );

            Assert.That(
                result.IngressEvaluations[0]
                    .HasStandingBasis,
                Is.False
            );
        }

        [Test]
        public void
            StandingBelowFreshPositionCannotPushIngressOutward()
        {
            SceneRelease release =
                FreshFetter(
                    "Low Standing",
                    "OWNER_A",
                    Turn
                );

            KvltSettledSceneStandingResult standing =
                Snapshot(
                    Standing(
                        "OWNER_A",
                        position:
                            0.10f
                    )
                );

            KvltNextTurnIngressSettlementResult
                result =
                    service.Settle(
                        SceneId,
                        Turn,
                        FreshPosition,
                        EntryCeiling,
                        new[]
                        {
                            release
                        },
                        standing
                    );

            SceneReleaseIngressEvaluation
                evaluation =
                    result.IngressEvaluations[0];

            Assert.That(
                evaluation.HasStandingBasis,
                Is.True
            );

            Assert.That(
                evaluation.StandingBasisPosition,
                Is.EqualTo(0.10f)
                    .Within(0.0001f)
            );

            /*
             * Final ingress rule:
             *
             * max(FreshReleasePosition,
             *     BandSceneStanding)
             */
            Assert.That(
                evaluation.UncappedPosition,
                Is.EqualTo(
                    FreshPosition
                ).Within(0.0001f)
            );

            Assert.That(
                evaluation.AppliedInitialPosition,
                Is.EqualTo(
                    FreshPosition
                ).Within(0.0001f)
            );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(
                    FreshPosition
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            StrongStandingIsCappedAndExistingResidentFieldIsUntouched()
        {
            SceneRelease fresh =
                FreshFetter(
                    "Privileged",
                    "OWNER_A",
                    Turn
                );

            SceneRelease resident =
                FreshFetter(
                    "Resident",
                    "OWNER_B",
                    Turn - 1
                );

            Assert.That(
                resident.TryEstablishFieldPosition(
                    0.40f,
                    Turn
                ),
                Is.True
            );

            int residentTransitionCount =
                resident.FieldPositionState
                    .Transitions.Count;

            KvltSettledSceneStandingResult standing =
                Snapshot(
                    Standing(
                        "OWNER_A",
                        position:
                            0.95f
                    ),

                    Standing(
                        "OWNER_B",
                        position:
                            0.40f
                    )
                );

            KvltNextTurnIngressSettlementResult
                result =
                    service.Settle(
                        SceneId,
                        Turn,
                        FreshPosition,
                        EntryCeiling,
                        new[]
                        {
                            resident,
                            fresh
                        },
                        standing
                    );

            Assert.That(
                result.IngressedCount,
                Is.EqualTo(1)
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
                evaluation.UncappedPosition,
                Is.EqualTo(0.95f)
                    .Within(0.0001f)
            );

            Assert.That(
                evaluation.AppliedInitialPosition,
                Is.EqualTo(
                    EntryCeiling
                ).Within(0.0001f)
            );

            Assert.That(
                evaluation.WasCapped,
                Is.True
            );

            Assert.That(
                fresh.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(
                    EntryCeiling
                ).Within(0.0001f)
            );

            Assert.That(
                resident.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.40f)
                    .Within(0.0001f)
            );

            Assert.That(
                resident.FieldPositionState
                    .Transitions.Count,
                Is.EqualTo(
                    residentTransitionCount
                )
            );
        }

        [Test]
        public void
            MissedPriorTurnIngressIsRejectedAsStaleChronology()
        {
            SceneRelease stale =
                FreshFetter(
                    "Stale",
                    "OWNER_A",
                    Turn - 1
                );

            Assert.That(
                stale.HasFieldPosition,
                Is.False
            );

            KvltSettledSceneStandingResult standing =
                Snapshot(
                    NoStanding(
                        "OWNER_A"
                    )
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
                        },
                        standing
                    )
            );

            Assert.That(
                stale.HasFieldPosition,
                Is.False
            );
        }

        private static SceneRelease FreshFetter(
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

        private static
            KvltSettledSceneStandingResult
            Snapshot(
                params SceneStandingEvaluation[]
                    evaluations)
        {
            return new
                KvltSettledSceneStandingResult(
                    SceneId,
                    Turn,
                    evaluations
                );
        }

        private static SceneStandingEvaluation
            NoStanding(
                string owner)
        {
            return new SceneStandingEvaluation(
                owner,
                SceneId,
                Turn,
                Array.Empty<
                    SceneStandingContribution>()
            );
        }

        private static SceneStandingEvaluation
            Standing(
                string owner,
                float position)
        {
            SceneStandingContribution
                contribution =
                    new(
                        "HISTORICAL_" + owner,
                        "DEMO_HISTORICAL_" + owner,
                        owner,
                        SceneId,
                        SceneStandingContributionKind
                            .ActiveField,
                        position,
                        weight:
                            1f,
                        basisTurn:
                            Turn
                    );

            return new SceneStandingEvaluation(
                owner,
                SceneId,
                Turn,
                new[]
                {
                    contribution
                }
            );
        }
    }
}