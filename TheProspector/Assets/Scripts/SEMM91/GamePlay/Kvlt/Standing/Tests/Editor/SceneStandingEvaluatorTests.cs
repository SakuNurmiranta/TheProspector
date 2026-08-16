using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Kvlt.Standing.Tests.Editor
{
    public class SceneStandingEvaluatorTests
    {
        private readonly
            SceneStandingEvaluator evaluator =
                new();

        [Test]
        public void
            EmptyRememberedCorpus_HasNoSceneStanding()
        {
            SceneStandingEvaluation evaluation =
                evaluator.Evaluate(
                    "OWNER",
                    "KVLT",
                    10,
                    Array.Empty<
                        SceneStandingContribution>()
                );

            Assert.That(
                evaluation.HasStanding,
                Is.False
            );

            Assert.That(
                evaluation.Standing,
                Is.Null
            );

            Assert.That(
                evaluation.TotalWeight,
                Is.EqualTo(0f)
            );

            Assert.That(
                evaluation.Contributions,
                Is.Empty
            );
        }

        [Test]
        public void
            SingleActiveFieldRelease_StandingEqualsItsPosition()
        {
            SceneStandingEvaluation evaluation =
                evaluator.Evaluate(
                    "OWNER",
                    "KVLT",
                    10,
                    new[]
                    {
                        Contribution(
                            "ACTIVE",
                            SceneStandingContributionKind
                                .ActiveField,
                            0.40f,
                            1f,
                            10
                        )
                    }
                );

            Assert.That(
                evaluation.HasStanding,
                Is.True
            );

            Assert.That(
                evaluation.Standing,
                Is.EqualTo(0.40f)
                    .Within(0.0001f)
            );

            Assert.That(
                evaluation.TotalWeight,
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void
            MixedRememberedCorpus_UsesWeightedHistoricalCentroid()
        {
            /*
             * Active:
             *   0.40 × 1 = 0.40
             *
             * Canon:
             *   1.00 × 2 = 2.00
             *
             * Rejected:
             *  -0.50 × 1 = -0.50
             *
             * weighted sum = 1.90
             * total weight = 4
             *
             * SS = 0.475
             */

            SceneStandingEvaluation evaluation =
                evaluator.Evaluate(
                    "OWNER",
                    "KVLT",
                    12,
                    new[]
                    {
                        Contribution(
                            "ACTIVE",
                            SceneStandingContributionKind
                                .ActiveField,
                            0.40f,
                            1f,
                            12
                        ),

                        Contribution(
                            "CANON",
                            SceneStandingContributionKind
                                .CanonLegacy,
                            1.00f,
                            2f,
                            9
                        ),

                        Contribution(
                            "REJECTED",
                            SceneStandingContributionKind
                                .RejectionScar,
                            -0.50f,
                            1f,
                            11
                        )
                    }
                );

            Assert.That(
                evaluation.Standing,
                Is.EqualTo(0.475f)
                    .Within(0.0001f)
            );

            Assert.That(
                evaluation.TotalWeight,
                Is.EqualTo(4f)
            );

            Assert.That(
                evaluation.Contributions.Count,
                Is.EqualTo(3)
            );
        }

        [Test]
        public void
            DuplicateSceneReleaseContribution_IsRejected()
        {
            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        "OWNER",
                        "KVLT",
                        10,
                        new[]
                        {
                            Contribution(
                                "SAME_RELEASE",
                                SceneStandingContributionKind
                                    .ActiveField,
                                0.4f,
                                1f,
                                10
                            ),

                            Contribution(
                                "SAME_RELEASE",
                                SceneStandingContributionKind
                                    .CanonLegacy,
                                1f,
                                2f,
                                8
                            )
                        }
                    )
            );
        }

        [Test]
        public void
            ContributionFromDifferentOwnerOrScene_IsRejected()
        {
            SceneStandingContribution
                foreignOwner =
                    new SceneStandingContribution(
                        "RELEASE_A",
                        "DEMO_A",
                        "OTHER_OWNER",
                        "KVLT",
                        SceneStandingContributionKind
                            .ActiveField,
                        0.4f,
                        1f,
                        10
                    );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        "OWNER",
                        "KVLT",
                        10,
                        new[]
                        {
                            foreignOwner
                        }
                    )
            );

            SceneStandingContribution
                foreignScene =
                    new SceneStandingContribution(
                        "RELEASE_B",
                        "DEMO_B",
                        "OWNER",
                        "OTHER_SCENE",
                        SceneStandingContributionKind
                            .ActiveField,
                        0.4f,
                        1f,
                        10
                    );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        "OWNER",
                        "KVLT",
                        10,
                        new[]
                        {
                            foreignScene
                        }
                    )
            );
        }

        [Test]
        public void
            FutureContributionCannotEnterEarlierSettledStanding()
        {
            SceneStandingContribution future =
                Contribution(
                    "FUTURE",
                    SceneStandingContributionKind
                        .ActiveField,
                    0.4f,
                    1f,
                    11
                );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        "OWNER",
                        "KVLT",
                        10,
                        new[]
                        {
                            future
                        }
                    )
            );
        }

        private static SceneStandingContribution
            Contribution(
                string releaseId,
                SceneStandingContributionKind kind,
                float position,
                float weight,
                int basisTurn)
        {
            return new SceneStandingContribution(
                releaseId,
                "DEMO_" + releaseId,
                "OWNER",
                "KVLT",
                kind,
                position,
                weight,
                basisTurn
            );
        }
    }
}