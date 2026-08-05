using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class
        TrackNamingSemanticPositionTests
    {
        [Test]
        public void Constructor_InitializesNamingDegreeFromMechanicalDegree()
        {
            TrackNamingSemanticPosition position = new(
                "IDEA_1",
                "ASPECT_LYRICS",
                0,
                TrackNamingSemanticRole.PairDominant,
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Dominant
            );

            Assert.That(
                position.MechanicalDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                position.NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(position.IsPaired, Is.True);
        }

        [Test]
        public void WithNamingEffectiveDegree_DoesNotMutateMechanicalDegree()
        {
            TrackNamingSemanticPosition original = new(
                "IDEA_1",
                "ASPECT_LYRICS",
                0,
                TrackNamingSemanticRole.PairDominant,
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Dominant
            );

            TrackNamingSemanticPosition reinforced =
                original.WithNamingEffectiveDegree(
                    TagDegree.Transgressive
                );

            Assert.That(
                original.MechanicalDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                original.NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                reinforced.MechanicalDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                reinforced.NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Transgressive)
            );
        }

        [Test]
        public void MatchingTagsInDifferentRoles_RemainSeparatePositions()
        {
            TrackNamingSemanticPosition pairedSacred = new(
                "IDEA_PAIR",
                "ASPECT_LYRICS",
                0,
                TrackNamingSemanticRole.PairSubmissive,
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Weak
            );

            TrackNamingSemanticPosition solitarySacred = new(
                "IDEA_SOLITARY",
                "ASPECT_VOCALS",
                1,
                TrackNamingSemanticRole.Solitary,
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Dominant
            );

            Assert.That(
                pairedSacred.Axis,
                Is.EqualTo(solitarySacred.Axis)
            );

            Assert.That(
                pairedSacred.Pole,
                Is.EqualTo(solitarySacred.Pole)
            );

            Assert.That(
                pairedSacred.Role,
                Is.Not.EqualTo(solitarySacred.Role)
            );

            Assert.That(
                pairedSacred.SourceIdeaId,
                Is.Not.EqualTo(solitarySacred.SourceIdeaId)
            );
        }

        [Test]
        public void SolitaryPosition_IsNotMarkedPaired()
        {
            TrackNamingSemanticPosition position = new(
                "IDEA_1",
                "ASPECT_GUITAR",
                0,
                TrackNamingSemanticRole.Solitary,
                TagAxis.Expressive,
                TagPole.Negative,
                TagDegree.Dominant
            );

            Assert.That(position.IsPaired, Is.False);
        }
    }
}