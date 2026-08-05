using NUnit.Framework;
using SEMM91.Core.Tags;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class TagAdjacencyTests
    {
        [Test]
        public void AreAdjacent_ProfaneAndRaw_ReturnsTrueBothWays()
        {
            Assert.That(
                TagAdjacency.AreAdjacent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagAxis.Expressive,
                    TagPole.Negative
                ),
                Is.True
            );

            Assert.That(
                TagAdjacency.AreAdjacent(
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.True
            );
        }

        [Test]
        public void AreAdjacent_ColdAndSlow_CrossPolarityRelationshipReturnsTrue()
        {
            Assert.That(
                TagAdjacency.AreAdjacent(
                    TagAxis.Emotional,
                    TagPole.Negative,
                    TagAxis.Temporal,
                    TagPole.Positive
                ),
                Is.True
            );
        }

        [Test]
        public void AreAdjacent_ExactSameTag_ReturnsFalse()
        {
            Assert.That(
                TagAdjacency.AreAdjacent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.False
            );
        }

        [Test]
        public void AreAdjacent_AntiTagsWithoutAuthoredAdjacency_ReturnsFalse()
        {
            Assert.That(
                TagAdjacency.AreAdjacent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.False
            );
        }

        [Test]
        public void AreAdjacent_UnlistedRelationship_ReturnsFalse()
        {
            Assert.That(
                TagAdjacency.AreAdjacent(
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.False
            );
        }
    }
}