using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class TrackNamingSemanticProjectorTests
    {
        [Test]
        public void Project_EmptyTrack_ReturnsNoPositions()
        {
            Track track = CreateTrack();

            var positions =
                TrackNamingSemanticProjector.Project(track);

            Assert.That(positions, Is.Empty);
        }

        [Test]
        public void Project_SingleTagIdea_ProducesSolitaryPosition()
        {
            Track track = CreateTrack();

            Idea idea = CreateSingleIdea(
                "IDEA_RAW",
                "ASPECT_GUITAR",
                TagAxis.Expressive,
                TagPole.Negative,
                TagDegree.Dominant
            );

            track.AddIdea(idea);

            var positions =
                TrackNamingSemanticProjector.Project(track);

            Assert.That(positions.Count, Is.EqualTo(1));

            TrackNamingSemanticPosition position =
                positions[0];

            Assert.That(
                position.SourceIdeaId,
                Is.EqualTo("IDEA_RAW")
            );

            Assert.That(
                position.AspectId,
                Is.EqualTo("ASPECT_GUITAR")
            );

            Assert.That(
                position.IdeaIndex,
                Is.EqualTo(0)
            );

            Assert.That(
                position.Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.Solitary
                )
            );

            Assert.That(
                position.Axis,
                Is.EqualTo(TagAxis.Expressive)
            );

            Assert.That(
                position.Pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                position.MechanicalDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                position.NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                position.IsPaired,
                Is.False
            );
        }

        [Test]
        public void Project_TagPairIdea_ProducesDominantThenSubmissive()
        {
            Track track = CreateTrack();

            Idea pairIdea = CreatePairIdea(
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
            );

            track.AddIdea(pairIdea);

            var positions =
                TrackNamingSemanticProjector.Project(track);

            Assert.That(
                positions.Count,
                Is.EqualTo(2)
            );

            TrackNamingSemanticPosition dominant =
                positions[0];

            TrackNamingSemanticPosition submissive =
                positions[1];

            Assert.That(
                dominant.SourceIdeaId,
                Is.EqualTo("IDEA_PAIR")
            );

            Assert.That(
                dominant.AspectId,
                Is.EqualTo("ASPECT_LYRICS")
            );

            Assert.That(
                dominant.IdeaIndex,
                Is.EqualTo(0)
            );

            Assert.That(
                dominant.Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairDominant
                )
            );

            Assert.That(
                dominant.Axis,
                Is.EqualTo(TagAxis.Symbolic)
            );

            Assert.That(
                dominant.Pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                dominant.MechanicalDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                dominant.IsPaired,
                Is.True
            );

            Assert.That(
                submissive.SourceIdeaId,
                Is.EqualTo("IDEA_PAIR")
            );

            Assert.That(
                submissive.AspectId,
                Is.EqualTo("ASPECT_LYRICS")
            );

            Assert.That(
                submissive.IdeaIndex,
                Is.EqualTo(0)
            );

            Assert.That(
                submissive.Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairSubmissive
                )
            );

            Assert.That(
                submissive.Axis,
                Is.EqualTo(TagAxis.Symbolic)
            );

            Assert.That(
                submissive.Pole,
                Is.EqualTo(TagPole.Positive)
            );

            Assert.That(
                submissive.MechanicalDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                submissive.IsPaired,
                Is.True
            );
        }

        [Test]
        public void Project_MixedIdeas_PreservesCompleteOccurrenceOrder()
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
                    "IDEA_PROFANE",
                    "ASPECT_DRUMS",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            var positions =
                TrackNamingSemanticProjector.Project(track);

            Assert.That(
                positions.Count,
                Is.EqualTo(5)
            );

            Assert.That(
                positions[0].SourceIdeaId,
                Is.EqualTo("IDEA_PAIR")
            );

            Assert.That(
                positions[0].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairDominant
                )
            );

            Assert.That(
                positions[0].IdeaIndex,
                Is.EqualTo(0)
            );

            Assert.That(
                positions[1].SourceIdeaId,
                Is.EqualTo("IDEA_PAIR")
            );

            Assert.That(
                positions[1].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairSubmissive
                )
            );

            Assert.That(
                positions[1].IdeaIndex,
                Is.EqualTo(0)
            );

            Assert.That(
                positions[2].SourceIdeaId,
                Is.EqualTo("IDEA_SACRED")
            );

            Assert.That(
                positions[2].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.Solitary
                )
            );

            Assert.That(
                positions[2].IdeaIndex,
                Is.EqualTo(1)
            );

            Assert.That(
                positions[3].SourceIdeaId,
                Is.EqualTo("IDEA_RAW")
            );

            Assert.That(
                positions[3].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.Solitary
                )
            );

            Assert.That(
                positions[3].IdeaIndex,
                Is.EqualTo(2)
            );

            Assert.That(
                positions[4].SourceIdeaId,
                Is.EqualTo("IDEA_PROFANE")
            );

            Assert.That(
                positions[4].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.Solitary
                )
            );

            Assert.That(
                positions[4].IdeaIndex,
                Is.EqualTo(3)
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