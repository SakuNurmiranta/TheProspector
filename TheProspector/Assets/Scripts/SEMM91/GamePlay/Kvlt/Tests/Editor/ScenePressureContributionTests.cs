using System;
using NUnit.Framework;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Pressure;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class ScenePressureContributionTests
    {
        [Test]
        public void SolitaryTag_ContributesPositiveRecordedDegree()
        {
            ScenePressureContribution contribution =
                Create(
                    DemoTapeTagOccurrenceRole.Solitary,
                    TagDegree.Dominant,
                    1f
                );

            Assert.That(
                contribution.DegreeBasisContribution,
                Is.EqualTo(2f)
            );

            Assert.That(
                contribution.RawContribution,
                Is.EqualTo(2f)
            );
        }

        [Test]
        public void PairDominant_ContributesPositiveRecordedDegree()
        {
            ScenePressureContribution contribution =
                Create(
                    DemoTapeTagOccurrenceRole.PairDominant,
                    TagDegree.Transgressive,
                    0.5f
                );

            Assert.That(
                contribution.DegreeBasisContribution,
                Is.EqualTo(3f)
            );

            Assert.That(
                contribution.RawContribution,
                Is.EqualTo(1.5f)
            );
        }

        [Test]
        public void PairSubmissive_ContributesNegativeRecordedDegree()
        {
            ScenePressureContribution contribution =
                Create(
                    DemoTapeTagOccurrenceRole.PairSubmissive,
                    TagDegree.Weak,
                    2f
                );

            Assert.That(
                contribution.DegreeBasisContribution,
                Is.EqualTo(-1f)
            );

            Assert.That(
                contribution.RawContribution,
                Is.EqualTo(-2f)
            );
        }

        [TestCase(
            DemoTapeTagOccurrenceRole.Solitary)]
        [TestCase(
            DemoTapeTagOccurrenceRole.PairDominant)]
        [TestCase(
            DemoTapeTagOccurrenceRole.PairSubmissive)]
        public void DegreeZero_HasZeroSemanticContribution(
            DemoTapeTagOccurrenceRole role)
        {
            ScenePressureContribution contribution =
                Create(
                    role,
                    TagDegree.Neutral,
                    3f
                );

            Assert.That(
                contribution.DegreeBasisContribution,
                Is.EqualTo(0f)
            );

            Assert.That(
                contribution.RawContribution,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void Contribution_PreservesSemanticAndArtifactProvenance()
        {
            ScenePressureContribution contribution =
                new ScenePressureContribution(
                    "RELEASE_7",
                    "DEMO_4",
                    "OWNER_2",
                    "TRACK_3",
                    "IDEA_1",
                    DemoTapeTagOccurrenceRole.PairSubmissive,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak,
                    1f
                );

            Assert.That(
                contribution.SourceReleaseId,
                Is.EqualTo("RELEASE_7")
            );

            Assert.That(
                contribution.SourceDemoTapeId,
                Is.EqualTo("DEMO_4")
            );

            Assert.That(
                contribution.SourceOwnerEntityId,
                Is.EqualTo("OWNER_2")
            );

            Assert.That(
                contribution.SourceTrackId,
                Is.EqualTo("TRACK_3")
            );

            Assert.That(
                contribution.SourceIdeaId,
                Is.EqualTo("IDEA_1")
            );

            Assert.That(
                contribution.SourceRole,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole
                        .PairSubmissive
                )
            );

            Assert.That(
                contribution.Axis,
                Is.EqualTo(TagAxis.Symbolic)
            );

            Assert.That(
                contribution.Pole,
                Is.EqualTo(TagPole.Positive)
            );

            Assert.That(
                contribution.RecordedDegree,
                Is.EqualTo(TagDegree.Weak)
            );
        }

        [Test]
        public void NegativeOrNonFiniteWeight_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    Create(
                        DemoTapeTagOccurrenceRole.Solitary,
                        TagDegree.Weak,
                        -0.01f
                    )
            );

            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    Create(
                        DemoTapeTagOccurrenceRole.Solitary,
                        TagDegree.Weak,
                        float.NaN
                    )
            );

            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    Create(
                        DemoTapeTagOccurrenceRole.Solitary,
                        TagDegree.Weak,
                        float.PositiveInfinity
                    )
            );
        }

        [Test]
        public void MissingArtifactProvenance_IsRejected()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new ScenePressureContribution(
                        "",
                        "DEMO",
                        "OWNER",
                        "TRACK",
                        "IDEA",
                        DemoTapeTagOccurrenceRole.Solitary,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak,
                        1f
                    )
            );
        }

        private static ScenePressureContribution Create(
            DemoTapeTagOccurrenceRole role,
            TagDegree degree,
            float weight)
        {
            return new ScenePressureContribution(
                "RELEASE",
                "DEMO",
                "OWNER",
                "TRACK",
                "IDEA",
                role,
                TagAxis.Symbolic,
                TagPole.Negative,
                degree,
                weight
            );
        }
    }
}