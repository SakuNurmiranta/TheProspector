using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class TrackTrveCapabilityEvaluatorTests
    {
        [Test]
        public void SolitaryIdea_CanNeverBeTrveCapable()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea()
                );

            TrackTrveCapabilityEvaluation result =
                Evaluate(
                    track,
                    Society(
                        TagDegree.Transgressive
                    )
                );

            IdeaTrveCapabilityEvaluation idea =
                result.IdeaEvaluations[0];

            Assert.That(
                idea.IsFormalPair,
                Is.False
            );

            Assert.That(
                idea.PairIntegrity,
                Is.EqualTo(
                    PairIntegrity.NotApplicable
                )
            );

            Assert.That(
                idea.IsTrveCapable,
                Is.False
            );

            Assert.That(
                idea.ContextualTrvePotential,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void ValidBackedPair_IsTrveCapable()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            IdeaTrveCapabilityEvaluation idea =
                Evaluate(
                    track,
                    Society(
                        TagDegree.Transgressive
                    )
                ).IdeaEvaluations[0];

            Assert.That(
                idea.PairIntegrity,
                Is.EqualTo(
                    PairIntegrity.Intact
                )
            );

            Assert.That(
                idea.IsTrveCapable,
                Is.True
            );

            Assert.That(
                idea.DominantDegree,
                Is.EqualTo(2)
            );

            Assert.That(
                idea.SocietyBacking,
                Is.EqualTo(1f)
            );

            Assert.That(
                idea.ContextualTrvePotential,
                Is.EqualTo(3f)
            );
        }

        [Test]
        public void MissingSocietyBacking_MakesPairIncapableWithoutDamagingIntegrity()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        TagDegree.Dominant,
                        TagDegree.Transgressive
                    )
                );

            IdeaTrveCapabilityEvaluation idea =
                Evaluate(
                    track,
                    new SocietyNormativeProfile()
                ).IdeaEvaluations[0];

            Assert.That(
                idea.PairIntegrity,
                Is.EqualTo(
                    PairIntegrity.Intact
                )
            );

            Assert.That(
                idea.SocietyBacking,
                Is.EqualTo(0f)
            );

            Assert.That(
                idea.IsTrveCapable,
                Is.False
            );

            Assert.That(
                idea.ContextualTrvePotential,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void DominantDegreeZero_CannotProduceActiveTransgression()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        TagDegree.Neutral,
                        TagDegree.Transgressive
                    )
                );

            IdeaTrveCapabilityEvaluation idea =
                Evaluate(
                    track,
                    Society(
                        TagDegree.Transgressive
                    )
                ).IdeaEvaluations[0];

            Assert.That(
                idea.PairIntegrity,
                Is.EqualTo(
                    PairIntegrity.Intact
                )
            );

            Assert.That(
                idea.SocietyBacking,
                Is.EqualTo(3f)
            );

            Assert.That(
                idea.DominantDegree,
                Is.EqualTo(0)
            );

            Assert.That(
                idea.IsTrveCapable,
                Is.False
            );

            Assert.That(
                idea.ContextualTrvePotential,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void DegreeZeroSubmissiveForce_CanStillSupportTrveCapability()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        TagDegree.Weak,
                        TagDegree.Neutral
                    )
                );

            IdeaTrveCapabilityEvaluation idea =
                Evaluate(
                    track,
                    Society(
                        TagDegree.Transgressive
                    )
                ).IdeaEvaluations[0];

            Assert.That(
                idea.SocietyBacking,
                Is.EqualTo(0.5f)
            );

            Assert.That(
                idea.IsTrveCapable,
                Is.True
            );

            Assert.That(
                idea.ContextualTrvePotential,
                Is.EqualTo(1.5f)
            );
        }

        [Test]
        public void MaximumContextualPotential_IsSix()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        TagDegree.Transgressive,
                        TagDegree.Transgressive
                    )
                );

            IdeaTrveCapabilityEvaluation idea =
                Evaluate(
                    track,
                    Society(
                        TagDegree.Transgressive
                    )
                ).IdeaEvaluations[0];

            Assert.That(
                idea.ContextualTrvePotential,
                Is.EqualTo(6f)
            );
        }

        [Test]
        public void TrackReportsWhetherAnyIdeaIsTrveCapable()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "FIRST",
                        0
                    ),
                    CreatePairIdea(
                        TagDegree.Dominant,
                        TagDegree.Weak,
                        "SECOND",
                        1
                    )
                );

            TrackTrveCapabilityEvaluation result =
                Evaluate(
                    track,
                    Society(
                        TagDegree.Weak
                    )
                );

            Assert.That(
                result.HasTrveCapableIdea,
                Is.True
            );

            Assert.That(
                result.IdeaEvaluations.Count,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void TrackWithNoCapablePair_ReportsFalse()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "FIRST",
                        0
                    ),
                    CreatePairIdea(
                        TagDegree.Neutral,
                        TagDegree.Transgressive,
                        "SECOND",
                        1
                    )
                );

            TrackTrveCapabilityEvaluation result =
                Evaluate(
                    track,
                    Society(
                        TagDegree.Transgressive
                    )
                );

            Assert.That(
                result.HasTrveCapableIdea,
                Is.False
            );
        }

        private static TrackTrveCapabilityEvaluation
            Evaluate(
                DemoTapeTrackSnapshot track,
                SocietyNormativeProfile society)
        {
            TrackEvaluationEnvironment environment =
                new TrackEvaluationEnvironment(
                    1,
                    NormativeCentre.Neutral,
                    NormativeCentre.Neutral,
                    society
                );

            TrackSocietyBackingEvaluation backing =
                new TrackSocietyBackingEvaluator()
                    .Evaluate(
                        track,
                        environment
                    );

            return new TrackTrveCapabilityEvaluator()
                .Evaluate(
                    track,
                    backing
                );
        }

        private static SocietyNormativeProfile Society(
            TagDegree sacredDegree)
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                sacredDegree
            );

            return society;
        }

        private static DemoTapeTrackSnapshot CreateTrack(
            params DemoTapeIdeaSnapshot[] ideas)
        {
            return new DemoTapeTrackSnapshot(
                "TRACK",
                "Track",
                0.75f,
                0.75f,
                ideas
            );
        }

        private static DemoTapeIdeaSnapshot
            CreateSingleIdea(
                string id = "SINGLE",
                int index = 0)
        {
            return new DemoTapeIdeaSnapshot(
                id,
                index,
                "ASPECT",
                IdeaPayloadType.SingleTag,
                "AUTHOR",
                TagContainerType.Transient,
                0.75f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive,
                        DemoTapeTagOccurrenceRole.Solitary
                    )
                }
            );
        }

        private static DemoTapeIdeaSnapshot
            CreatePairIdea(
                TagDegree dominantDegree,
                TagDegree submissiveDegree,
                string id = "PAIR",
                int index = 0)
        {
            return new DemoTapeIdeaSnapshot(
                id,
                index,
                "ASPECT",
                IdeaPayloadType.TagPair,
                "AUTHOR",
                TagContainerType.Transient,
                0.75f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        dominantDegree,
                        DemoTapeTagOccurrenceRole
                            .PairDominant
                    ),

                    new DemoTapeTagOccurrenceSnapshot(
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        submissiveDegree,
                        DemoTapeTagOccurrenceRole
                            .PairSubmissive
                    )
                }
            );
        }
    }
}