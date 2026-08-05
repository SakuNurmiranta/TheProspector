using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Core.Recordings.Tests.Editor
{
    public sealed class
        DemoTapeTrackSnapshotFactoryTests
    {
        [Test]
        public void Create_EmptyTrack_PreservesTrackMetadata()
        {
            Track track = CreateTrack();

            DemoTapeTrackSnapshot snapshot =
                DemoTapeTrackSnapshotFactory.Create(
                    track,
                    0.65f
                );

            Assert.That(
                snapshot.SourceTrackId,
                Is.EqualTo("TRACK_TEST")
            );

            Assert.That(
                snapshot.DisplayName,
                Is.EqualTo("Generated Title")
            );

            Assert.That(
                snapshot.SourceConveyance,
                Is.EqualTo(0.8f)
            );

            Assert.That(
                snapshot.RecordedConveyance,
                Is.EqualTo(0.65f)
            );

            Assert.That(
                snapshot.IdeaSnapshots,
                Is.Empty
            );
        }

        [Test]
        public void Create_SingleTagIdea_PreservesCompleteSemantics()
        {
            Track track = CreateTrack();

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_RAW",
                    "ASPECT_GUITAR",
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagDegree.Dominant
                )
            );

            DemoTapeTrackSnapshot snapshot =
                DemoTapeTrackSnapshotFactory.Create(
                    track,
                    0.7f
                );

            Assert.That(
                snapshot.IdeaCount,
                Is.EqualTo(1)
            );

            DemoTapeIdeaSnapshot idea =
                snapshot.IdeaSnapshots[0];

            Assert.That(
                idea.SourceIdeaId,
                Is.EqualTo("IDEA_RAW")
            );

            Assert.That(
                idea.IdeaIndex,
                Is.EqualTo(0)
            );

            Assert.That(
                idea.AspectId,
                Is.EqualTo("ASPECT_GUITAR")
            );

            Assert.That(
                idea.PayloadType,
                Is.EqualTo(
                    IdeaPayloadType.SingleTag
                )
            );

            Assert.That(
                idea.SourceEntityId,
                Is.EqualTo("ENTITY_AUTHOR")
            );

            Assert.That(
                idea.SourceContainerType,
                Is.EqualTo(
                    TagContainerType.Transient
                )
            );

            Assert.That(
                idea.Conveyance,
                Is.EqualTo(0.75f)
            );

            Assert.That(
                idea.TagOccurrences.Count,
                Is.EqualTo(1)
            );

            DemoTapeTagOccurrenceSnapshot tag =
                idea.TagOccurrences[0];

            Assert.That(
                tag.Axis,
                Is.EqualTo(TagAxis.Expressive)
            );

            Assert.That(
                tag.Pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                tag.Degree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                tag.Role,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole.Solitary
                )
            );
        }

        [Test]
        public void Create_TagPairIdea_PreservesPairDirection()
        {
            Track track = CreateTrack();

            track.AddIdea(
                CreatePairIdea(
                    "IDEA_PAIR",
                    "ASPECT_LYRICS"
                )
            );

            DemoTapeTrackSnapshot snapshot =
                DemoTapeTrackSnapshotFactory.Create(
                    track,
                    0.7f
                );

            DemoTapeIdeaSnapshot idea =
                snapshot.IdeaSnapshots[0];

            Assert.That(
                idea.PayloadType,
                Is.EqualTo(
                    IdeaPayloadType.TagPair
                )
            );

            Assert.That(
                idea.TagOccurrences.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                idea.TagOccurrences[0].Role,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole
                        .PairDominant
                )
            );

            Assert.That(
                idea.TagOccurrences[0].Pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                idea.TagOccurrences[0].Degree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                idea.TagOccurrences[1].Role,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole
                        .PairSubmissive
                )
            );

            Assert.That(
                idea.TagOccurrences[1].Pole,
                Is.EqualTo(TagPole.Positive)
            );

            Assert.That(
                idea.TagOccurrences[1].Degree,
                Is.EqualTo(TagDegree.Weak)
            );
        }

        [Test]
        public void Create_MixedIdeas_PreservesIdeaOrder()
        {
            Track track = CreateTrack();

            track.AddIdea(
                CreatePairIdea(
                    "IDEA_PAIR",
                    "ASPECT_LYRICS"
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

            DemoTapeTrackSnapshot snapshot =
                DemoTapeTrackSnapshotFactory.Create(
                    track,
                    0.7f
                );

            Assert.That(
                snapshot.IdeaCount,
                Is.EqualTo(3)
            );

            Assert.That(
                snapshot.IdeaSnapshots[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_PAIR")
            );

            Assert.That(
                snapshot.IdeaSnapshots[0]
                    .IdeaIndex,
                Is.EqualTo(0)
            );

            Assert.That(
                snapshot.IdeaSnapshots[1]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_SACRED")
            );

            Assert.That(
                snapshot.IdeaSnapshots[1]
                    .IdeaIndex,
                Is.EqualTo(1)
            );

            Assert.That(
                snapshot.IdeaSnapshots[2]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_RAW")
            );

            Assert.That(
                snapshot.IdeaSnapshots[2]
                    .IdeaIndex,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void Create_LaterTrackMutation_DoesNotChangeSnapshot()
        {
            Track track = CreateTrack();

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_ORIGINAL",
                    "ASPECT_GUITAR",
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagDegree.Dominant
                )
            );

            DemoTapeTrackSnapshot snapshot =
                DemoTapeTrackSnapshotFactory.Create(
                    track,
                    0.7f
                );

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_LATER",
                    "ASPECT_DRUMS",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            track.TrySetGeneratedDisplayName(
                "Later Rehearsal Title"
            );

            Assert.That(
                snapshot.IdeaCount,
                Is.EqualTo(1)
            );

            Assert.That(
                snapshot.IdeaSnapshots[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_ORIGINAL")
            );

            Assert.That(
                snapshot.DisplayName,
                Is.EqualTo("Generated Title")
            );
        }

        [Test]
        public void Create_NullTrack_Throws()
        {
            Assert.Throws<
                ArgumentNullException>(
                () =>
                    DemoTapeTrackSnapshotFactory.Create(
                        null,
                        0.7f
                    )
            );
        }

        private static Track CreateTrack()
        {
            return new Track(
                "TRACK_TEST",
                "Generated Title",
                0.8f,
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
                conveyance: 0.75f,
                sourceEntityId: "ENTITY_AUTHOR",
                sourceContainerType:
                    TagContainerType.Transient
            );
        }

        private static Idea CreatePairIdea(
            string ideaId,
            string aspectId)
        {
            TagPair pair = new TagPair(
                dominantTag: new TagInstance(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                ),
                submissiveTag: new TagInstance(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                )
            );

            return new Idea(
                ideaId: ideaId,
                aspectId: aspectId,
                tagPair: pair,
                conveyance: 0.75f,
                sourceEntityId: "ENTITY_AUTHOR",
                sourceContainerType:
                    TagContainerType.Transient
            );
        }
    }
}