using System;
using NUnit.Framework;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class
        SceneReleaseCanonBreakthroughMovementEvaluatorTests
    {
        private readonly
            SceneReleaseCanonBreakthroughMovementEvaluator
                evaluator =
                    new();

        [Test]
        public void
            SingleBreakthrough_UsesItsFullDifferential()
        {
            var result =
                evaluator.Evaluate(
                    Breakthrough(
                        Claim(3)
                    ),
                    breakthroughDriftMultiplier: 0.5f
                );

            Assert.That(
                result.QualifyingClaimCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.HighestDifferential,
                Is.EqualTo(3)
            );

            Assert.That(
                result.HighestDifferentialClaimCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.LowerQualifyingClaimCount,
                Is.EqualTo(0)
            );

            Assert.That(
                result.ReleaseBreakthroughValue,
                Is.EqualTo(3)
            );

            Assert.That(
                result.BreakthroughDrift,
                Is.EqualTo(1.5f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            LowerBreakthroughsEachAddOne()
        {
            var result =
                evaluator.Evaluate(
                    Breakthrough(
                        Claim(3),
                        Claim(2),
                        Claim(2)
                    ),
                    1f
                );

            Assert.That(
                result.HighestDifferential,
                Is.EqualTo(3)
            );

            Assert.That(
                result.HighestDifferentialClaimCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.LowerQualifyingClaimCount,
                Is.EqualTo(2)
            );

            Assert.That(
                result.ReleaseBreakthroughValue,
                Is.EqualTo(5)
            );

            Assert.That(
                result.BreakthroughDrift,
                Is.EqualTo(5f)
            );
        }

        [Test]
        public void
            CoHighestBreakthroughsEachContributeFullValue()
        {
            var result =
                evaluator.Evaluate(
                    Breakthrough(
                        Claim(3),
                        Claim(3),
                        Claim(2)
                    ),
                    1f
                );

            Assert.That(
                result.HighestDifferential,
                Is.EqualTo(3)
            );

            Assert.That(
                result.HighestDifferentialClaimCount,
                Is.EqualTo(2)
            );

            Assert.That(
                result.LowerQualifyingClaimCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.ReleaseBreakthroughValue,
                Is.EqualTo(7)
            );
        }

        [Test]
        public void
            EqualSecondLevelExample_PreservesMemorableRule()
        {
            var result =
                evaluator.Evaluate(
                    Breakthrough(
                        Claim(2),
                        Claim(2),
                        Claim(1),
                        Claim(1)
                    ),
                    1f
                );

            Assert.That(
                result.ReleaseBreakthroughValue,
                Is.EqualTo(6)
            );
        }

        [Test]
        public void
            NoQualifyingClaims_ProducesZeroBreakthroughDrift()
        {
            var result =
                evaluator.Evaluate(
                    Breakthrough(),
                    5f
                );

            Assert.That(
                result.HasBreakthrough,
                Is.False
            );

            Assert.That(
                result.QualifyingClaimCount,
                Is.EqualTo(0)
            );

            Assert.That(
                result.ReleaseBreakthroughValue,
                Is.EqualTo(0)
            );

            Assert.That(
                result.BreakthroughDrift,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void
            InvalidMultiplier_IsRejected()
        {
            SceneReleaseCanonBreakthroughEvaluation
                breakthrough =
                    Breakthrough(
                        Claim(1)
                    );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    evaluator.Evaluate(
                        breakthrough,
                        -1f
                    )
            );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    evaluator.Evaluate(
                        breakthrough,
                        float.NaN
                    )
            );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    evaluator.Evaluate(
                        breakthrough,
                        float.PositiveInfinity
                    )
            );
        }

        private static
            SceneReleaseCanonBreakthroughEvaluation
            Breakthrough(
                params
                    SceneReleaseCanonBreakthroughClaim[]
                    claims)
        {
            return new
                SceneReleaseCanonBreakthroughEvaluation(
                    "RELEASE",
                    "DEMO",
                    "OWNER",
                    "KVLT",
                    6,
                    0.40f,
                    claims
                );
        }

        private static
            SceneReleaseCanonBreakthroughClaim
            Claim(
                int realizedDifferential)
        {
            /*
             * Canon degree 0 and current TRVE=true
             * make ActiveDegree itself the realized
             * differential.
             */
            TagDegree activeDegree =
                (TagDegree)realizedDifferential;

            return new
                SceneReleaseCanonBreakthroughClaim(
                    "RELEASE",
                    "DEMO",
                    "OWNER",
                    "KVLT",
                    "TRACK_" + Guid.NewGuid(),
                    "IDEA_" + Guid.NewGuid(),
                    0,
                    6,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    activeDegree,
                    activeDegree,
                    false,
                    TagDegree.Neutral,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak,
                    true
                );
        }
    }
}