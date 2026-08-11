using System;
using NUnit.Framework;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Circulation.Tests
{
    public class SceneReleaseIngressEvaluatorTests
    {
        private readonly
            SceneReleaseIngressEvaluator evaluator =
                new();

        private readonly
            SceneStandingEvaluator standingEvaluator =
                new();

        [Test]
        public void
            NoStandingHistory_UsesConfiguredBaseline()
        {
            SceneRelease release =
                FieldRelease(
                    fetterTurn: 6
                );

            SceneReleaseIngressEvaluation result =
                evaluator.Evaluate(
                    release,
                    frozenStanding: null,
                    noStandingEntryPosition: 0.20f,
                    innerFieldEntryCeiling: 0.70f,
                    placementTurn: 6
                );

            Assert.That(
                result.HasStandingBasis,
                Is.False
            );

            Assert.That(
                result.StandingBasisTurn,
                Is.Null
            );

            Assert.That(
                result.StandingBasisPosition,
                Is.Null
            );

            Assert.That(
                result.UncappedPosition,
                Is.EqualTo(0.20f)
            );

            Assert.That(
                result.AppliedInitialPosition,
                Is.EqualTo(0.20f)
            );

            Assert.That(
                result.WasCapped,
                Is.False
            );
        }

        [Test]
        public void
            SettledEmptyCorpus_StillUsesNoHistoryBaseline()
        {
            SceneRelease release =
                FieldRelease(
                    fetterTurn: 6
                );

            SceneStandingEvaluation standing =
                standingEvaluator.Evaluate(
                    "OWNER",
                    "KVLT_SCENE",
                    5,
                    Array.Empty<
                        SceneStandingContribution>()
                );

            Assert.That(
                standing.HasStanding,
                Is.False
            );

            SceneReleaseIngressEvaluation result =
                evaluator.Evaluate(
                    release,
                    standing,
                    0.20f,
                    0.70f,
                    6
                );

            Assert.That(
                result.HasStandingBasis,
                Is.False
            );

            Assert.That(
                result.AppliedInitialPosition,
                Is.EqualTo(0.20f)
            );
        }

        [Test]
        public void
            HistoricalStanding_IsUsedDirectlyAsCandidatePosition()
        {
            SceneRelease release =
                FieldRelease(
                    fetterTurn: 6
                );

            SceneStandingEvaluation standing =
                Standing(
                    settledTurn: 5,
                    position: 0.45f
                );

            SceneReleaseIngressEvaluation result =
                evaluator.Evaluate(
                    release,
                    standing,
                    0.20f,
                    0.70f,
                    6
                );

            Assert.That(
                result.HasStandingBasis,
                Is.True
            );

            Assert.That(
                result.StandingBasisTurn,
                Is.EqualTo(5)
            );

            Assert.That(
                result.StandingBasisPosition,
                Is.EqualTo(0.45f)
            );

            Assert.That(
                result.UncappedPosition,
                Is.EqualTo(0.45f)
            );

            Assert.That(
                result.AppliedInitialPosition,
                Is.EqualTo(0.45f)
            );

            Assert.That(
                result.WasCapped,
                Is.False
            );
        }

        [Test]
        public void
            StandingBeyondInnerEntryCeiling_IsCapped()
        {
            SceneRelease release =
                FieldRelease(
                    fetterTurn: 6
                );

            SceneStandingEvaluation standing =
                Standing(
                    settledTurn: 5,
                    position: 0.95f
                );

            SceneReleaseIngressEvaluation result =
                evaluator.Evaluate(
                    release,
                    standing,
                    0.20f,
                    0.70f,
                    6
                );

            Assert.That(
                result.UncappedPosition,
                Is.EqualTo(0.95f)
            );

            Assert.That(
                result.AppliedInitialPosition,
                Is.EqualTo(0.70f)
            );

            Assert.That(
                result.WasCapped,
                Is.True
            );
        }

        [Test]
        public void
            NoHistoryBaselineCannotExceedEntryCeiling()
        {
            SceneRelease release =
                FieldRelease(
                    fetterTurn: 6
                );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        null,
                        noStandingEntryPosition: 0.80f,
                        innerFieldEntryCeiling: 0.70f,
                        placementTurn: 6
                    )
            );
        }

        [Test]
        public void
            SameTurnOrFutureStanding_CannotFeedIngress()
        {
            SceneRelease release =
                FieldRelease(
                    fetterTurn: 6
                );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        Standing(
                            settledTurn: 6,
                            position: 0.50f
                        ),
                        0.20f,
                        0.70f,
                        6
                    )
            );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        Standing(
                            settledTurn: 7,
                            position: 0.50f
                        ),
                        0.20f,
                        0.70f,
                        6
                    )
            );
        }

        [Test]
        public void
            StandingFromDifferentOwnerOrScene_IsRejected()
        {
            SceneRelease release =
                FieldRelease(
                    fetterTurn: 6
                );

            SceneStandingEvaluation wrongOwner =
                standingEvaluator.Evaluate(
                    "OTHER_OWNER",
                    "KVLT_SCENE",
                    5,
                    Array.Empty<
                        SceneStandingContribution>()
                );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        wrongOwner,
                        0.20f,
                        0.70f,
                        6
                    )
            );

            SceneStandingEvaluation wrongScene =
                standingEvaluator.Evaluate(
                    "OWNER",
                    "OTHER_SCENE",
                    5,
                    Array.Empty<
                        SceneStandingContribution>()
                );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        wrongScene,
                        0.20f,
                        0.70f,
                        6
                    )
            );
        }

        private SceneStandingEvaluation Standing(
            int settledTurn,
            float position)
        {
            return standingEvaluator.Evaluate(
                "OWNER",
                "KVLT_SCENE",
                settledTurn,
                new[]
                {
                    new SceneStandingContribution(
                        "OLD_RELEASE",
                        "OLD_DEMO",
                        "OWNER",
                        "KVLT_SCENE",
                        SceneStandingContributionKind
                            .ActiveField,
                        position,
                        1f,
                        settledTurn
                    )
                }
            );
        }

        private static SceneRelease FieldRelease(
            int fetterTurn)
        {
            SceneRelease release =
                new SceneRelease(
                    "Test Release",
                    "DEMO",
                    "OWNER",
                    "KVLT_SCENE",
                    4,
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
    }
}