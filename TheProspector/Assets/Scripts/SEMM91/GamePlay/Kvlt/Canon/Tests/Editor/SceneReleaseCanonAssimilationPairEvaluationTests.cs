using NUnit.Framework;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Canon.Tests.Editor
{
    public class
        SceneReleaseCanonAssimilationPairEvaluationTests
    {
        [Test]
        public void
            DormantRecordedDegreeThree_CanonTwo_RaisesToTwo()
        {
            var result =
                Pair(
                    recorded: TagDegree.Transgressive,
                    existing: TagDegree.Neutral,
                    canon: TagDegree.Dominant
                );

            Assert.That(
                result.CanonicalActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                result.FinalActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                result.RaisesActivation,
                Is.True
            );
        }

        [Test]
        public void
            CanonAboveRecording_IsCappedByRecordedDegree()
        {
            var result =
                Pair(
                    recorded: TagDegree.Weak,
                    existing: TagDegree.Neutral,
                    canon: TagDegree.Transgressive
                );

            Assert.That(
                result.CanonicalActivationDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            Assert.That(
                result.FinalActivationDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );
        }

        [Test]
        public void
            ExistingActivationAboveCanon_IsNeverLowered()
        {
            var result =
                Pair(
                    recorded: TagDegree.Transgressive,
                    existing: TagDegree.Transgressive,
                    canon: TagDegree.Dominant
                );

            Assert.That(
                result.CanonicalActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                result.FinalActivationDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                result.RaisesActivation,
                Is.False
            );
        }

        [Test]
        public void
            NoCanonicalPrecedent_LeavesActivationUntouched()
        {
            var result =
                new
                    SceneReleaseCanonAssimilationPairEvaluation(
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
                        TagDegree.Transgressive,
                        TagDegree.Neutral,
                        false,
                        TagDegree.Neutral,
                        System.Array.Empty<
                            CanonPrecedentRecord>()
                    );

            Assert.That(
                result.CanonicalActivationDegree,
                Is.EqualTo(
                    TagDegree.Neutral
                )
            );

            Assert.That(
                result.FinalActivationDegree,
                Is.EqualTo(
                    TagDegree.Neutral
                )
            );

            Assert.That(
                result.RaisesActivation,
                Is.False
            );
        }

        private static
            SceneReleaseCanonAssimilationPairEvaluation
            Pair(
                TagDegree recorded,
                TagDegree existing,
                TagDegree canon)
        {
            CanonPrecedentRecord precedent =
                new(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    canon,
                    CanonProvenanceKind.ScenarioSeed,
                    "CANON"
                );

            return new
                SceneReleaseCanonAssimilationPairEvaluation(
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
                    existing,
                    true,
                    canon,
                    new[]
                    {
                        precedent
                    }
                );
        }
    }
}