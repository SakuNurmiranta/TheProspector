using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Kvlt.Standing.Tests.Editor
{
    public class SceneStandingContributionTests
    {
        [Test]
        public void
            ActiveFieldContribution_UsesUnitWeight()
        {
            SceneStandingContribution contribution =
                Contribution(
                    SceneStandingContributionKind
                        .ActiveField,
                    position: 0.42f,
                    weight: 1f
                );

            Assert.That(
                contribution.Position,
                Is.EqualTo(0.42f)
            );

            Assert.That(
                contribution.Weight,
                Is.EqualTo(1f)
            );

            Assert.That(
                contribution.Kind,
                Is.EqualTo(
                    SceneStandingContributionKind
                        .ActiveField
                )
            );
        }

        [Test]
        public void
            ActiveFieldContribution_RejectsNonUnitWeight()
        {
            Assert.Throws<
                ArgumentException>(
                () =>
                    Contribution(
                        SceneStandingContributionKind
                            .ActiveField,
                        0.5f,
                        2f
                    )
            );
        }

        [Test]
        public void
            HistoricalContributions_AcceptExplicitPositiveWeights()
        {
            SceneStandingContribution canon =
                Contribution(
                    SceneStandingContributionKind
                        .CanonLegacy,
                    1f,
                    2.5f
                );

            SceneStandingContribution rejection =
                Contribution(
                    SceneStandingContributionKind
                        .RejectionScar,
                    -0.2f,
                    1.75f
                );

            Assert.That(
                canon.Weight,
                Is.EqualTo(2.5f)
            );

            Assert.That(
                rejection.Weight,
                Is.EqualTo(1.75f)
            );
        }

        [Test]
        public void
            InvalidPositionWeightOrTurn_IsRejected()
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    Contribution(
                        SceneStandingContributionKind
                            .CanonLegacy,
                        float.NaN,
                        1f
                    )
            );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    Contribution(
                        SceneStandingContributionKind
                            .CanonLegacy,
                        1f,
                        0f
                    )
            );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    Contribution(
                        SceneStandingContributionKind
                            .CanonLegacy,
                        1f,
                        -1f
                    )
            );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    new SceneStandingContribution(
                        "RELEASE",
                        "DEMO",
                        "OWNER",
                        "KVLT",
                        SceneStandingContributionKind
                            .CanonLegacy,
                        1f,
                        1f,
                        -1
                    )
            );
        }

        private static SceneStandingContribution
            Contribution(
                SceneStandingContributionKind kind,
                float position,
                float weight)
        {
            return new SceneStandingContribution(
                "RELEASE",
                "DEMO",
                "OWNER",
                "KVLT",
                kind,
                position,
                weight,
                5
            );
        }
    }
}