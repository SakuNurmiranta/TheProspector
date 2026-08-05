using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;

namespace SEMM91.Tests.Editor.Recordings
{
    public sealed class
        DemoTapeSemanticSnapshotTests
    {
        [Test]
        public void TagOccurrenceSnapshot_PreservesSemanticValues()
        {
            DemoTapeTagOccurrenceSnapshot occurrence =
                new DemoTapeTagOccurrenceSnapshot(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    DemoTapeTagOccurrenceRole
                        .PairDominant
                );

            Assert.That(
                occurrence.Axis,
                Is.EqualTo(TagAxis.Symbolic)
            );

            Assert.That(
                occurrence.Pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                occurrence.Degree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                occurrence.Role,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole
                        .PairDominant
                )
            );
        }

        [Test]
        public void IdeaSnapshot_SingleTag_PreservesMetadata()
        {
            DemoTapeIdeaSnapshot snapshot =
                CreateSingleIdeaSnapshot(
                    "IDEA_SINGLE",
                    2
                );

            Assert.That(
                snapshot.SourceIdeaId,
                Is.EqualTo("IDEA_SINGLE")
            );

            Assert.That(
                snapshot.IdeaIndex,
                Is.EqualTo(2)
            );

            Assert.That(
                snapshot.AspectId,
                Is.EqualTo("ASPECT_GUITAR")
            );

            Assert.That(
                snapshot.PayloadType,
                Is.EqualTo(
                    IdeaPayloadType.SingleTag
                )
            );

            Assert.That(
                snapshot.SourceEntityId,
                Is.EqualTo("ENTITY_AUTHOR")
            );

            Assert.That(
                snapshot.SourceContainerType,
                Is.EqualTo(
                    TagContainerType.Transient
                )
            );

            Assert.That(
                snapshot.TagOccurrences.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                snapshot.TagOccurrences[0].Role,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole.Solitary
                )
            );
        }

        [Test]
        public void IdeaSnapshot_TagPair_PreservesDirection()
        {
            DemoTapeIdeaSnapshot snapshot =
                CreatePairIdeaSnapshot(
                    "IDEA_PAIR",
                    0
                );

            Assert.That(
                snapshot.PayloadType,
                Is.EqualTo(
                    IdeaPayloadType.TagPair
                )
            );

            Assert.That(
                snapshot.TagOccurrences.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                snapshot.TagOccurrences[0].Role,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole
                        .PairDominant
                )
            );

            Assert.That(
                snapshot.TagOccurrences[0].Pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                snapshot.TagOccurrences[1].Role,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole
                        .PairSubmissive
                )
            );

            Assert.That(
                snapshot.TagOccurrences[1].Pole,
                Is.EqualTo(TagPole.Positive)
            );
        }

        [Test]
        public void IdeaSnapshot_ReversedPairRoles_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new DemoTapeIdeaSnapshot(
                    "IDEA_PAIR",
                    0,
                    "ASPECT_LYRICS",
                    IdeaPayloadType.TagPair,
                    "ENTITY_AUTHOR",
                    TagContainerType.Transient,
                    0.75f,
                    new[]
                    {
                        CreateOccurrence(
                            DemoTapeTagOccurrenceRole
                                .PairSubmissive
                        ),
                        CreateOccurrence(
                            DemoTapeTagOccurrenceRole
                                .PairDominant
                        )
                    }
                )
            );
        }

        [Test]
        public void IdeaSnapshot_SingleTagWithPairRole_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new DemoTapeIdeaSnapshot(
                    "IDEA_SINGLE",
                    0,
                    "ASPECT_GUITAR",
                    IdeaPayloadType.SingleTag,
                    "ENTITY_AUTHOR",
                    TagContainerType.Transient,
                    0.75f,
                    new[]
                    {
                        CreateOccurrence(
                            DemoTapeTagOccurrenceRole
                                .PairDominant
                        )
                    }
                )
            );
        }

        [Test]
        public void TrackSnapshot_LegacyConstructor_HasNoSemanticIdeas()
        {
            DemoTapeTrackSnapshot snapshot =
                new DemoTapeTrackSnapshot(
                    "TRACK_LEGACY",
                    "Old Track",
                    0.5f,
                    0.4f
                );

            Assert.That(
                snapshot.IdeaSnapshots,
                Is.Empty
            );

            Assert.That(
                snapshot.IdeaCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void TrackSnapshot_PreservesOrderedIdeaSnapshots()
        {
            DemoTapeTrackSnapshot snapshot =
                new DemoTapeTrackSnapshot(
                    "TRACK_RECORDED",
                    "Generated Title",
                    0.8f,
                    0.7f,
                    new[]
                    {
                        CreatePairIdeaSnapshot(
                            "IDEA_PAIR",
                            0
                        ),
                        CreateSingleIdeaSnapshot(
                            "IDEA_SINGLE",
                            1
                        )
                    }
                );

            Assert.That(
                snapshot.IdeaCount,
                Is.EqualTo(2)
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
                Is.EqualTo("IDEA_SINGLE")
            );

            Assert.That(
                snapshot.IdeaSnapshots[1]
                    .IdeaIndex,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void TrackSnapshot_CopiesInputCollection()
        {
            DemoTapeIdeaSnapshot[] input =
            {
                CreateSingleIdeaSnapshot(
                    "IDEA_ORIGINAL",
                    0
                )
            };

            DemoTapeTrackSnapshot snapshot =
                new DemoTapeTrackSnapshot(
                    "TRACK_RECORDED",
                    "Generated Title",
                    0.8f,
                    0.7f,
                    input
                );

            input[0] =
                CreateSingleIdeaSnapshot(
                    "IDEA_REPLACEMENT",
                    0
                );

            Assert.That(
                snapshot.IdeaSnapshots[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_ORIGINAL")
            );
        }

        private static DemoTapeIdeaSnapshot
            CreateSingleIdeaSnapshot(
                string ideaId,
                int ideaIndex)
        {
            return new DemoTapeIdeaSnapshot(
                ideaId,
                ideaIndex,
                "ASPECT_GUITAR",
                IdeaPayloadType.SingleTag,
                "ENTITY_AUTHOR",
                TagContainerType.Transient,
                0.75f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        TagAxis.Expressive,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        DemoTapeTagOccurrenceRole.Solitary
                    )
                }
            );
        }

        private static DemoTapeIdeaSnapshot
            CreatePairIdeaSnapshot(
                string ideaId,
                int ideaIndex)
        {
            return new DemoTapeIdeaSnapshot(
                ideaId,
                ideaIndex,
                "ASPECT_LYRICS",
                IdeaPayloadType.TagPair,
                "ENTITY_AUTHOR",
                TagContainerType.Transient,
                0.75f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        DemoTapeTagOccurrenceRole
                            .PairDominant
                    ),
                    new DemoTapeTagOccurrenceSnapshot(
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        TagDegree.Weak,
                        DemoTapeTagOccurrenceRole
                            .PairSubmissive
                    )
                }
            );
        }

        private static
            DemoTapeTagOccurrenceSnapshot
            CreateOccurrence(
                DemoTapeTagOccurrenceRole role)
        {
            return new DemoTapeTagOccurrenceSnapshot(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Weak,
                role
            );
        }
    }
}