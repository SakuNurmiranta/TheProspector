using NUnit.Framework;
using SEMM91.GamePlay.Collectives;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Society;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Society.Tests.Editor
{
    public class SocietyNormativeProfileTests
    {
        [Test]
        public void NewProfile_ContainsNoNorms()
        {
            SocietyNormativeProfile profile =
                new SocietyNormativeProfile();

            Assert.That(
                profile.Count,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void MissingNorm_HasZeroForce()
        {
            SocietyNormativeProfile profile =
                new SocietyNormativeProfile();

            Assert.That(
                profile.TryGetNormativeDegree(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    out _
                ),
                Is.False
            );

            Assert.That(
                profile.GetNormativeForce(
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void DegreeZeroNorm_Exists_AndHasHalfForce()
        {
            SocietyNormativeProfile profile =
                new SocietyNormativeProfile();

            profile.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Neutral
            );

            Assert.That(
                profile.TryGetNormativeDegree(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    out TagDegree degree
                ),
                Is.True
            );

            Assert.That(
                degree,
                Is.EqualTo(TagDegree.Neutral)
            );

            Assert.That(
                profile.GetNormativeForce(
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.EqualTo(0.5f)
            );
        }

        [TestCase(TagDegree.Weak, 1f)]
        [TestCase(TagDegree.Dominant, 2f)]
        [TestCase(TagDegree.Transgressive, 3f)]
        public void PositiveDegrees_MapToExpectedForce(
            TagDegree degree,
            float expectedForce)
        {
            SocietyNormativeProfile profile =
                new SocietyNormativeProfile();

            profile.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                degree
            );

            Assert.That(
                profile.GetNormativeForce(
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.EqualTo(expectedForce)
            );
        }

        [Test]
        public void OppositePoles_AreIndependent()
        {
            SocietyNormativeProfile profile =
                new SocietyNormativeProfile();

            profile.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Weak
            );

            profile.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            Assert.That(
                profile.GetNormativeForce(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(1f)
            );

            Assert.That(
                profile.GetNormativeForce(
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.EqualTo(3f)
            );
        }

        [Test]
        public void RemoveNorm_MakesPolarityNormativelyAbsent()
        {
            SocietyNormativeProfile profile =
                new SocietyNormativeProfile();

            profile.SetNormativeDegree(
                TagAxis.Interpretive,
                TagPole.Positive,
                TagDegree.Dominant
            );

            Assert.That(
                profile.RemoveNorm(
                    TagAxis.Interpretive,
                    TagPole.Positive
                ),
                Is.True
            );

            Assert.That(
                profile.TryGetNormativeDegree(
                    TagAxis.Interpretive,
                    TagPole.Positive,
                    out _
                ),
                Is.False
            );

            Assert.That(
                profile.GetNormativeForce(
                    TagAxis.Interpretive,
                    TagPole.Positive
                ),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void SeededWorldState_OwnsSocietyNormativeProfile()
        {
            SeededWorldState world =
                new SeededWorldState(
                    new CollectiveRegistry()
                );

            Assert.That(
                world.SocietyNorms,
                Is.Not.Null
            );

            Assert.That(
                world.SocietyNorms.Count,
                Is.EqualTo(0)
            );
        }
    }
}