using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class TrackSensationalExtremityEvaluatorTests
    {
        [Test]
        public void SolitaryTag_QualifiesAgainstPresentAntiTagNorm()
        {
            SocietyNormativeProfile society =
                SocietyWith(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Transgressive
                );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "PROFANE",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            TrackSensationalExtremityEvaluation result =
                Evaluate(
                    track,
                    society
                );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(2)
            );

            Assert.That(
                result.HasSocietyTransgressiveOccurrence,
                Is.True
            );

            Assert.That(
                result.OccurrenceEvaluations[0]
                    .IsSocietyTransgressive,
                Is.True
            );
        }

        [TestCase(TagDegree.Neutral)]
        [TestCase(TagDegree.Transgressive)]
        public void SocietyAntiTagDegree_DoesNotChangeQualification(
            TagDegree societyDegree)
        {
            SocietyNormativeProfile society =
                SocietyWith(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    societyDegree
                );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "PROFANE",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive
                    )
                );

            TrackSensationalExtremityEvaluation result =
                Evaluate(
                    track,
                    society
                );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(3)
            );

            Assert.That(
                result.HasSocietyTransgressiveOccurrence,
                Is.True
            );
        }

        [Test]
        public void SamePolaritySocietySupport_DoesNotCancelOpposedNorm()
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Weak
            );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "PROFANE",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            TrackSensationalExtremityEvaluation result =
                Evaluate(
                    track,
                    society
                );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(2)
            );

            Assert.That(
                result.HasSocietyTransgressiveOccurrence,
                Is.True
            );
        }

        [Test]
        public void SamePolarityDegreeExcess_AloneDoesNotQualify()
        {
            SocietyNormativeProfile society =
                SocietyWith(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "PROFANE",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive
                    )
                );

            TrackSensationalExtremityEvaluation result =
                Evaluate(
                    track,
                    society
                );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(0)
            );

            Assert.That(
                result.HasSocietyTransgressiveOccurrence,
                Is.False
            );
        }

        [Test]
        public void MissingAntiTagNorm_DoesNotQualify()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "PROFANE",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive
                    )
                );

            TrackSensationalExtremityEvaluation result =
                Evaluate(
                    track,
                    new SocietyNormativeProfile()
                );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(0)
            );

            Assert.That(
                result.HasSocietyTransgressiveOccurrence,
                Is.False
            );
        }

        [Test]
        public void AdjacentSocietyNorm_DoesNotQualify()
        {
            // Meaning is adjacent to Sacred,
            // but adjacency has no role in E_s.
            SocietyNormativeProfile society =
                SocietyWith(
                    TagAxis.Interpretive,
                    TagPole.Positive,
                    TagDegree.Transgressive
                );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "SACRED",
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        TagDegree.Transgressive
                    )
                );

            TrackSensationalExtremityEvaluation result =
                Evaluate(
                    track,
                    society
                );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(0)
            );

            Assert.That(
                result.HasSocietyTransgressiveOccurrence,
                Is.False
            );
        }

        [Test]
        public void PairSubmissiveOccurrence_CanDetermineSensationalExtremity()
        {
            SocietyNormativeProfile society =
                SocietyWith(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Transgressive
                );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        TagAxis.Interpretive,
                        TagPole.Positive,
                        TagDegree.Dominant,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive
                    )
                );

            TrackSensationalExtremityEvaluation result =
                Evaluate(
                    track,
                    society
                );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(3)
            );

            Assert.That(
                result.OccurrenceEvaluations[1]
                    .SourceRole,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole
                        .PairSubmissive
                )
            );

            Assert.That(
                result.OccurrenceEvaluations[1]
                    .IsSocietyTransgressive,
                Is.True
            );
        }

        [Test]
        public void TrackUsesMaximumQualifyingRecordedDegree()
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Neutral
            );

            society.SetNormativeDegree(
                TagAxis.Expressive,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "ONE",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak,
                        0
                    ),

                    CreateSingleIdea(
                        "TWO",
                        TagAxis.Expressive,
                        TagPole.Negative,
                        TagDegree.Transgressive,
                        1
                    )
                );

            TrackSensationalExtremityEvaluation result =
                Evaluate(
                    track,
                    society
                );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(3)
            );
        }

        [Test]
        public void QualifyingDegreeZero_IsPreservedEvenThoughEsIsZero()
        {
            SocietyNormativeProfile society =
                SocietyWith(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "PROFANE_ZERO",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Neutral
                    )
                );

            TrackSensationalExtremityEvaluation result =
                Evaluate(
                    track,
                    society
                );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(0)
            );

            Assert.That(
                result.HasSocietyTransgressiveOccurrence,
                Is.True
            );

            Assert.That(
                result.OccurrenceEvaluations[0]
                    .CandidateMagnitude,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void EmptyLegacyTrack_HasZeroSensationalExtremity()
        {
            DemoTapeTrackSnapshot track =
                new DemoTapeTrackSnapshot(
                    "TRACK",
                    "Legacy Track",
                    0.5f,
                    0.5f
                );

            TrackSensationalExtremityEvaluation result =
                Evaluate(
                    track,
                    new SocietyNormativeProfile()
                );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(0)
            );

            Assert.That(
                result.HasSocietyTransgressiveOccurrence,
                Is.False
            );

            Assert.That(
                result.OccurrenceEvaluations,
                Is.Empty
            );
        }

        private static TrackSensationalExtremityEvaluation
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

            return new TrackSensationalExtremityEvaluator()
                .Evaluate(
                    track,
                    environment
                );
        }

        private static SocietyNormativeProfile SocietyWith(
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                axis,
                pole,
                degree
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
                string id,
                TagAxis axis,
                TagPole pole,
                TagDegree degree,
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
                        axis,
                        pole,
                        degree,
                        DemoTapeTagOccurrenceRole.Solitary
                    )
                }
            );
        }

        private static DemoTapeIdeaSnapshot
            CreatePairIdea(
                string id,
                TagAxis dominantAxis,
                TagPole dominantPole,
                TagDegree dominantDegree,
                TagAxis submissiveAxis,
                TagPole submissivePole,
                TagDegree submissiveDegree)
        {
            return new DemoTapeIdeaSnapshot(
                id,
                0,
                "ASPECT",
                IdeaPayloadType.TagPair,
                "AUTHOR",
                TagContainerType.Transient,
                0.75f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        dominantAxis,
                        dominantPole,
                        dominantDegree,
                        DemoTapeTagOccurrenceRole
                            .PairDominant
                    ),

                    new DemoTapeTagOccurrenceSnapshot(
                        submissiveAxis,
                        submissivePole,
                        submissiveDegree,
                        DemoTapeTagOccurrenceRole
                            .PairSubmissive
                    )
                }
            );
        }
    }
}