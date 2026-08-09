using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class CanonStateTests
    {
        [Test]
        public void SeededWorldState_StartsWithEmptyKvltCanon()
        {
            SeededWorldState world =
                new SeededWorldState(
                    new CollectiveRegistry()
                );

            Assert.That(
                world.KvltCanon,
                Is.Not.Null
            );

            Assert.That(
                world.KvltCanon.Records.Count,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void Canon_DistinguishesAbsentFromDegreeZero()
        {
            CanonState canon =
                new CanonState();

            bool existedBefore =
                canon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out _
                );

            Assert.That(
                existedBefore,
                Is.False
            );

            bool recorded =
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Neutral,
                    CanonProvenanceKind.ScenarioSeed,
                    "FIRST_WAVE_PROFANE_0"
                );

            Assert.That(
                recorded,
                Is.True
            );

            bool existsAfter =
                canon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out TagDegree degree
                );

            Assert.That(
                existsAfter,
                Is.True
            );

            Assert.That(
                degree,
                Is.EqualTo(TagDegree.Neutral)
            );
        }

        [Test]
        public void Canon_HigherPrecedentRaisesCeiling_LowerDoesNotLowerIt()
        {
            CanonState canon =
                new CanonState();

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind.ScenarioSeed,
                    "SOURCE_1"
                ),
                Is.True
            );

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    CanonProvenanceKind.SceneRelease,
                    "RELEASE_2",
                    "TRACK_2",
                    "IDEA_2",
                    4
                ),
                Is.True
            );

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind.SceneRelease,
                    "RELEASE_3",
                    "TRACK_3",
                    "IDEA_3",
                    5
                ),
                Is.False
            );

            Assert.That(
                canon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out TagDegree degree
                ),
                Is.True
            );

            Assert.That(
                degree,
                Is.EqualTo(TagDegree.Dominant)
            );
        }

        [Test]
        public void Canon_EqualCeilingPreservesProvenance_AndPolesRemainIndependent()
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Dominant,
                CanonProvenanceKind.SceneRelease,
                "RELEASE_A",
                "TRACK_A",
                "IDEA_A",
                8
            );

            bool tiedRecorded =
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    CanonProvenanceKind.SceneRelease,
                    "RELEASE_B",
                    "TRACK_B",
                    "IDEA_B",
                    8
                );

            bool oppositeRecorded =
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak,
                    CanonProvenanceKind.SceneRelease,
                    "RELEASE_C",
                    "TRACK_C",
                    "IDEA_C",
                    9
                );

            Assert.That(
                tiedRecorded,
                Is.True
            );

            Assert.That(
                oppositeRecorded,
                Is.True
            );

            Assert.That(
                canon.Records.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                canon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out TagDegree negativeDegree
                ),
                Is.True
            );

            Assert.That(
                negativeDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                canon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    out TagDegree positiveDegree
                ),
                Is.True
            );

            Assert.That(
                positiveDegree,
                Is.EqualTo(TagDegree.Weak)
            );
        }
    }
}