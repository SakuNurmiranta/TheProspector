using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class TrackSurfaceExtremityEvaluatorTests
    {
        [Test]
        public void SingleTag_UsesRecordedDegreeTimesCurrentAffinity()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "IDEA_1",
                        0,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            NormativeCentre current =
                CreateCentre(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    0.5f
                );

            TrackSurfaceExtremityEvaluation result =
                Evaluate(
                    track,
                    current
                );

            Assert.That(
                result.Surface,
                Is.EqualTo(1f)
            );

            Assert.That(
                result.Extremity,
                Is.EqualTo(2f)
            );
        }

        [Test]
        public void NegativeAffinity_ProducesNegativeSurface()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "IDEA_1",
                        0,
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        TagDegree.Transgressive
                    )
                );

            NormativeCentre current =
                CreateCentre(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Transgressive,
                    -1f
                );

            TrackSurfaceExtremityEvaluation result =
                Evaluate(
                    track,
                    current
                );

            Assert.That(
                result.Surface,
                Is.EqualTo(-3f)
            );

            Assert.That(
                result.Extremity,
                Is.EqualTo(3f)
            );
        }

        [Test]
        public void PairSurface_UsesDominantTagOnly()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        TagPole.Positive,
                        TagDegree.Transgressive
                    )
                );

            NormativeCentre current =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant,
                            0.5f
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            TagDegree.Transgressive,
                            1f
                        )
                    }
                );

            TrackSurfaceExtremityEvaluation result =
                Evaluate(
                    track,
                    current
                );

            Assert.That(
                result.Surface,
                Is.EqualTo(1f),
                "Submissive affinity must not add Surface."
            );
        }

        [Test]
        public void PairExtremity_IsMeanOfDominantAndSubmissiveDegrees()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        TagPole.Positive,
                        TagDegree.Transgressive
                    )
                );

            TrackSurfaceExtremityEvaluation result =
                Evaluate(
                    track,
                    NormativeCentre.Neutral
                );

            Assert.That(
                result.Extremity,
                Is.EqualTo(2.5f)
            );
        }

        [Test]
        public void TrackValues_AreMeanAcrossIdeas_NotTagOccurrences()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "SINGLE",
                        0,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak
                    ),
                    CreatePairIdea(
                        "PAIR",
                        1,
                        TagAxis.Expressive,
                        TagPole.Negative,
                        TagDegree.Transgressive,
                        TagPole.Positive,
                        TagDegree.Weak
                    )
                );

            NormativeCentre current =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Weak,
                            1f
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Expressive,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            0.5f
                        )
                    }
                );

            TrackSurfaceExtremityEvaluation result =
                Evaluate(
                    track,
                    current
                );

            // Idea Surface:
            // 1 * 1.0 = 1
            // 3 * 0.5 = 1.5
            //
            // Track Surface:
            // (1 + 1.5) / 2 = 1.25

            Assert.That(
                result.Surface,
                Is.EqualTo(1.25f)
            );

            // Idea Extremity:
            // 1
            // (3 + 1) / 2 = 2
            //
            // Track Extremity:
            // (1 + 2) / 2 = 1.5

            Assert.That(
                result.Extremity,
                Is.EqualTo(1.5f)
            );

            Assert.That(
                result.IdeaEvaluations.Count,
                Is.EqualTo(2)
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
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak
                    ),
                    CreateSingleIdea(
                        "SECOND",
                        1,
                        TagAxis.Expressive,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            TrackSurfaceExtremityEvaluation result =
                Evaluate(
                    track,
                    NormativeCentre.Neutral
                );

            Assert.That(
                result.SourceTrackId,
                Is.EqualTo("TRACK")
            );

            Assert.That(
                result.IdeaEvaluations[0]
                    .SourceIdeaId,
                Is.EqualTo("FIRST")
            );

            Assert.That(
                result.IdeaEvaluations[0]
                    .IdeaIndex,
                Is.EqualTo(0)
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

        [Test]
        public void Conveyance_DoesNotAttenuateSurfaceOrExtremity()
        {
            DemoTapeIdeaSnapshot idea =
                CreateSingleIdea(
                    "IDEA",
                    0,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    conveyance: 0.01f
                );

            DemoTapeTrackSnapshot track =
                new DemoTapeTrackSnapshot(
                    "TRACK",
                    "Track",
                    0.01f,
                    0.01f,
                    new[] { idea }
                );

            NormativeCentre current =
                CreateCentre(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    1f
                );

            TrackSurfaceExtremityEvaluation result =
                Evaluate(
                    track,
                    current
                );

            Assert.That(
                result.Surface,
                Is.EqualTo(3f)
            );

            Assert.That(
                result.Extremity,
                Is.EqualTo(3f)
            );
        }

        [Test]
        public void EmptyLegacyTrack_ReturnsZeroValues()
        {
            DemoTapeTrackSnapshot track =
                new DemoTapeTrackSnapshot(
                    "TRACK",
                    "Legacy Track",
                    0.5f,
                    0.5f
                );

            TrackSurfaceExtremityEvaluation result =
                Evaluate(
                    track,
                    NormativeCentre.Neutral
                );

            Assert.That(
                result.Surface,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.Extremity,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.IdeaEvaluations,
                Is.Empty
            );
        }

        private static TrackSurfaceExtremityEvaluation
            Evaluate(
                DemoTapeTrackSnapshot track,
                NormativeCentre current)
        {
            TrackEvaluationEnvironment environment =
                new TrackEvaluationEnvironment(
                    1,
                    current,
                    NormativeCentre.Neutral,
                    new SocietyNormativeProfile()
                );

            return new TrackSurfaceExtremityEvaluator()
                .Evaluate(
                    track,
                    environment
                );
        }

        private static NormativeCentre CreateCentre(
            TagAxis axis,
            TagPole pole,
            TagDegree degree,
            float affinity)
        {
            return new NormativeCentre(
                new[]
                {
                    new NormativeAffinityEntry(
                        axis,
                        pole,
                        degree,
                        affinity
                    )
                }
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
                TagAxis axis,
                TagPole pole,
                TagDegree degree,
                float conveyance = 0.75f)
        {
            return new DemoTapeIdeaSnapshot(
                id,
                index,
                "ASPECT",
                IdeaPayloadType.SingleTag,
                "AUTHOR",
                TagContainerType.Transient,
                conveyance,
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
                int index,
                TagAxis axis,
                TagPole dominantPole,
                TagDegree dominantDegree,
                TagPole submissivePole,
                TagDegree submissiveDegree)
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
                        axis,
                        dominantPole,
                        dominantDegree,
                        DemoTapeTagOccurrenceRole
                            .PairDominant
                    ),

                    new DemoTapeTagOccurrenceSnapshot(
                        axis,
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