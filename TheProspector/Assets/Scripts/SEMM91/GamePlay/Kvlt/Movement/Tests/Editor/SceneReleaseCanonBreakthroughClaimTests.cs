using System;
using NUnit.Framework;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class
        SceneReleaseCanonBreakthroughClaimTests
    {
        [Test]
        public void
            RecordedAboveCanonButInactive_IsPotentialOnly()
        {
            SceneReleaseCanonBreakthroughClaim claim =
                Claim(
                    recorded: TagDegree.Transgressive,
                    active: TagDegree.Neutral,
                    canon: TagDegree.Weak,
                    currentlyTrve: false
                );

            Assert.That(
                claim.PotentialNoveltyDifferential,
                Is.EqualTo(2)
            );

            Assert.That(
                claim.RealizedBreakthroughDifferential,
                Is.EqualTo(0)
            );

            Assert.That(
                claim.HasPotentialNovelty,
                Is.True
            );

            Assert.That(
                claim.IsQualifyingBreakthrough,
                Is.False
            );
        }

        [Test]
        public void
            ActiveTrveAboveCanon_ProducesRealizedDifferential()
        {
            SceneReleaseCanonBreakthroughClaim claim =
                Claim(
                    recorded: TagDegree.Transgressive,
                    active: TagDegree.Dominant,
                    canon: TagDegree.Weak,
                    currentlyTrve: true
                );

            Assert.That(
                claim.PotentialNoveltyDifferential,
                Is.EqualTo(2)
            );

            Assert.That(
                claim.RealizedBreakthroughDifferential,
                Is.EqualTo(1)
            );

            Assert.That(
                claim.IsQualifyingBreakthrough,
                Is.True
            );
        }

        [Test]
        public void
            ActiveDegreeCannotExceedRecording()
        {
            Assert.Throws<
                ArgumentException>(
                () =>
                    Claim(
                        recorded: TagDegree.Weak,
                        active: TagDegree.Dominant,
                        canon: TagDegree.Neutral,
                        currentlyTrve: true
                    )
            );
        }

        private static
            SceneReleaseCanonBreakthroughClaim
            Claim(
                TagDegree recorded,
                TagDegree active,
                TagDegree canon,
                bool currentlyTrve)
        {
            return new
                SceneReleaseCanonBreakthroughClaim(
                    "RELEASE",
                    "DEMO",
                    "OWNER",
                    "KVLT",
                    "TRACK",
                    "IDEA",
                    0,
                    6,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    recorded,
                    active,
                    true,
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak,
                    currentlyTrve
                );
        }
    }
}