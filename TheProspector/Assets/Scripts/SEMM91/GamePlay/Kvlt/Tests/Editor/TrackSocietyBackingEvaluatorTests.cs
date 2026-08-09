using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class TrackSocietyBackingEvaluatorTests
    {
        [Test]
        public void SolitaryIdea_HasNoSocietyBacking()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "SINGLE",
                        0,
                        TagDegree.Transgressive
                    )
                );

            TrackSocietyBackingEvaluation result =
                Evaluate(
                    track,
                    new SocietyNormativeProfile()
                );

            IdeaSocietyBackingEvaluation idea =
                result.IdeaEvaluations[0];

            Assert.That(
                idea.IsFormalPair,
                Is.False
            );

            Assert.That(
                idea.SocietyBacking,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void MissingSocietyNorm_ProducesZeroBacking()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        TagDegree.Dominant,
                        TagDegree.Transgressive
                    )
                );

            TrackSocietyBackingEvaluation result =
                Evaluate(
                    track,
                    new SocietyNormativeProfile()
                );

            IdeaSocietyBackingEvaluation idea =
                result.IdeaEvaluations[0];

            Assert.That(
                idea.RecordedSubmissiveSemanticForce,
                Is.EqualTo(3f)
            );

            Assert.That(
                idea.SocietyNormativeForce,
                Is.EqualTo(0f)
            );

            Assert.That(
                idea.SocietyBacking,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void SocietyCannotAmplifyRecordedSubmissiveForce()
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            IdeaSocietyBackingEvaluation idea =
                Evaluate(
                    track,
                    society
                ).IdeaEvaluations[0];

            Assert.That(
                idea.RecordedSubmissiveSemanticForce,
                Is.EqualTo(1f)
            );

            Assert.That(
                idea.SocietyNormativeForce,
                Is.EqualTo(3f)
            );

            Assert.That(
                idea.SocietyBacking,
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void WeakSocietyNorm_CapsStrongerRecordedSubmissiveForce()
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Weak
            );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        TagDegree.Dominant,
                        TagDegree.Transgressive
                    )
                );

            IdeaSocietyBackingEvaluation idea =
                Evaluate(
                    track,
                    society
                ).IdeaEvaluations[0];

            Assert.That(
                idea.RecordedSubmissiveSemanticForce,
                Is.EqualTo(3f)
            );

            Assert.That(
                idea.SocietyNormativeForce,
                Is.EqualTo(1f)
            );

            Assert.That(
                idea.SocietyBacking,
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void DegreeZeroRecordedSubmissive_HasHalfSemanticForce()
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        TagDegree.Dominant,
                        TagDegree.Neutral
                    )
                );

            IdeaSocietyBackingEvaluation idea =
                Evaluate(
                    track,
                    society
                ).IdeaEvaluations[0];

            Assert.That(
                idea.RecordedSubmissiveSemanticForce,
                Is.EqualTo(0.5f)
            );

            Assert.That(
                idea.SocietyBacking,
                Is.EqualTo(0.5f)
            );
        }

        [Test]
        public void DegreeZeroSocietyNorm_HasHalfNormativeForce()
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Neutral
            );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        TagDegree.Dominant,
                        TagDegree.Transgressive
                    )
                );

            IdeaSocietyBackingEvaluation idea =
                Evaluate(
                    track,
                    society
                ).IdeaEvaluations[0];

            Assert.That(
                idea.SocietyNormativeForce,
                Is.EqualTo(0.5f)
            );

            Assert.That(
                idea.SocietyBacking,
                Is.EqualTo(0.5f)
            );
        }

        [Test]
        public void SocietyBacking_UsesSubmissiveTagNotDominantTag()
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            // Society values Profane, but not Sacred.
            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Transgressive
            );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        TagDegree.Dominant,
                        TagDegree.Transgressive
                    )
                );

            IdeaSocietyBackingEvaluation idea =
                Evaluate(
                    track,
                    society
                ).IdeaEvaluations[0];

            Assert.That(
                idea.SocietyBacking,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void Evaluation_PreservesIdeaIdentityAndOrder()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "FIRST",
                        0,
                        TagDegree.Weak
                    ),
                    CreatePairIdea(
                        TagDegree.Dominant,
                        TagDegree.Weak,
                        "SECOND",
                        1
                    )
                );

            TrackSocietyBackingEvaluation result =
                Evaluate(
                    track,
                    new SocietyNormativeProfile()
                );

            Assert.That(
                result.SourceTrackId,
                Is.EqualTo("TRACK")
            );

            Assert.That(
                result.IdeaEvaluations.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                result.IdeaEvaluations[0]
                    .SourceIdeaId,
                Is.EqualTo("FIRST")
            );

            Assert.That(
                result.IdeaEvaluations[1]
                    .SourceIdeaId,
                Is.EqualTo("SECOND")
            );

            Assert.That(
                result.IdeaEvaluations[1]
                    .IdeaIndex,
                Is.EqualTo(1)
            );
        }

        private static TrackSocietyBackingEvaluation
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

            return new TrackSocietyBackingEvaluator()
                .Evaluate(
                    track,
                    environment
                );
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
                string id,
                int index,
                TagDegree degree)
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
                        degree,
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