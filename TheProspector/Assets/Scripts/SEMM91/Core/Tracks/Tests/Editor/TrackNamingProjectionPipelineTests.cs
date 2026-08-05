using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class
        TrackNamingProjectionPipelineTests
    {
        [Test]
        public void Resolve_EmptyTrack_ReturnsEmptySelection()
        {
            Track track = CreateTrack();

            TrackNamingPositionSelection result =
                TrackNamingProjectionPipeline.Resolve(track);

            Assert.That(
                result.SelectedOccurrences,
                Is.Empty
            );

            Assert.That(
                result.OverflowOccurrences,
                Is.Empty
            );

            Assert.That(
                result.SelectedPositionCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void Resolve_Peak1Fixture_AppliesExactOverflowReinforcement()
        {
            Track track = CreateTrack();

            track.AddIdea(
                CreatePairIdea(
                    "IDEA_PAIR",
                    "ASPECT_LYRICS",
                    new TagInstance(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    ),
                    new TagInstance(
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        TagDegree.Weak
                    )
                )
            );

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_SACRED",
                    "ASPECT_VOCALS",
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                )
            );

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_RAW",
                    "ASPECT_GUITAR",
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagDegree.Dominant
                )
            );

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_PROFANE_OVERFLOW",
                    "ASPECT_DRUMS",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            TrackNamingPositionSelection result =
                TrackNamingProjectionPipeline.Resolve(track);

            Assert.That(
                result.SelectedPositionCount,
                Is.EqualTo(3)
            );

            Assert.That(
                result.SelectedOccurrences.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                result.OverflowOccurrences,
                Is.Empty
            );

            TrackNamingSemanticPosition profane =
                result.SelectedOccurrences[0];

            Assert.That(
                profane.SourceIdeaId,
                Is.EqualTo("IDEA_PAIR")
            );

            Assert.That(
                profane.Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairDominant
                )
            );

            Assert.That(
                profane.MechanicalDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                profane.NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Transgressive)
            );

            Assert.That(
                result.SelectedOccurrences[1]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                result.SelectedOccurrences[2]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Dominant)
            );
        }

        [Test]
        public void Resolve_UnmatchedExactTag_AppliesAdjacencyAfterward()
        {
            Track track = CreateTrack();

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_SACRED",
                    "ASPECT_0",
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                )
            );

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_PROFANE",
                    "ASPECT_1",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_RAW",
                    "ASPECT_2",
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            // Meaning is adjacent to Sacred. It has no exact
            // match among the selected occurrences.
            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_MEANING_OVERFLOW",
                    "ASPECT_3",
                    TagAxis.Interpretive,
                    TagPole.Positive,
                    TagDegree.Transgressive
                )
            );

            TrackNamingPositionSelection result =
                TrackNamingProjectionPipeline.Resolve(track);

            Assert.That(
                result.SelectedOccurrences[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_SACRED")
            );

            Assert.That(
                result.SelectedOccurrences[0]
                    .MechanicalDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                result.SelectedOccurrences[0]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                result.OverflowOccurrences,
                Is.Empty
            );
        }

        [Test]
        public void Resolve_DoesNotMutateAuthoritativeTagDegrees()
        {
            Track track = CreateTrack();

            Idea profane = CreateSingleIdea(
                "IDEA_PROFANE",
                "ASPECT_0",
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Dominant
            );

            track.AddIdea(profane);

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_1",
                    "ASPECT_1",
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                )
            );

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_2",
                    "ASPECT_2",
                    TagAxis.Expressive,
                    TagPole.Positive,
                    TagDegree.Weak
                )
            );

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_PROFANE_OVERFLOW",
                    "ASPECT_3",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            TrackNamingPositionSelection result =
                TrackNamingProjectionPipeline.Resolve(track);

            Assert.That(
                result.SelectedOccurrences[0]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Transgressive)
            );

            Assert.That(
                profane.TagInstance.degree,
                Is.EqualTo(TagDegree.Dominant)
            );
        }

        private static Track CreateTrack()
        {
            return new Track(
                "TRACK_TEST",
                "Untitled Track",
                0.0f,
                0
            );
        }

        private static Idea CreateSingleIdea(
            string ideaId,
            string aspectId,
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            return new Idea(
                ideaId: ideaId,
                aspectId: aspectId,
                tagInstance: new TagInstance(
                    axis,
                    pole,
                    degree
                ),
                conveyance: 1.0f,
                sourceEntityId: "ENTITY_TEST",
                sourceContainerType:
                    TagContainerType.Transient
            );
        }

        private static Idea CreatePairIdea(
            string ideaId,
            string aspectId,
            TagInstance dominantTag,
            TagInstance submissiveTag)
        {
            TagPair pair = new(
                dominantTag: dominantTag,
                submissiveTag: submissiveTag
            );

            return new Idea(
                ideaId: ideaId,
                aspectId: aspectId,
                tagPair: pair,
                conveyance: 1.0f,
                sourceEntityId: "ENTITY_TEST",
                sourceContainerType:
                    TagContainerType.Transient
            );
        }
    }
}