using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.Networking.DebugSnapshots;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class DemoTapeStructuralPairQueryTests
    {
        [Test]
        public void TryFindFirst_FormalOpposedPair_ReturnsCandidate()
        {
            FixedString64Bytes demoId =
                Fixed64("DEMO_A");

            NetworkList<DemoTapeIdeaSemanticDebugRow> ideas =
                new();

            NetworkList<DemoTapeTagSemanticDebugRow> tags =
                new();

            try
            {
                ideas.Add(CreatePairIdea(demoId));

                tags.Add(
                    CreateTag(
                        demoId,
                        0,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        DemoTapeTagOccurrenceRole.PairDominant
                    )
                );

                tags.Add(
                    CreateTag(
                        demoId,
                        1,
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        DemoTapeTagOccurrenceRole.PairSubmissive
                    )
                );

                bool found =
                    DemoTapeStructuralPairQuery.TryFindFirst(
                        demoId,
                        ideas,
                        tags,
                        out DemoTapeStructuralPairCandidate candidate
                    );

                Assert.That(found, Is.True);
                Assert.That(candidate.TrackIndex, Is.EqualTo(3));
                Assert.That(candidate.IdeaIndex, Is.EqualTo(0));
                Assert.That(candidate.Axis, Is.EqualTo(TagAxis.Symbolic));
                Assert.That(candidate.DominantPole, Is.EqualTo(TagPole.Negative));
                Assert.That(candidate.SubmissivePole, Is.EqualTo(TagPole.Positive));
            }
            finally
            {
                ideas.Dispose();
                tags.Dispose();
            }
        }

        [Test]
        public void TryFindFirst_SamePolePair_ReturnsFalse()
        {
            FixedString64Bytes demoId = Fixed64("DEMO_A");
            NetworkList<DemoTapeIdeaSemanticDebugRow> ideas = new();
            NetworkList<DemoTapeTagSemanticDebugRow> tags = new();

            try
            {
                ideas.Add(CreatePairIdea(demoId));

                tags.Add(
                    CreateTag(
                        demoId,
                        0,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        DemoTapeTagOccurrenceRole.PairDominant
                    )
                );

                tags.Add(
                    CreateTag(
                        demoId,
                        1,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        DemoTapeTagOccurrenceRole.PairSubmissive
                    )
                );

                bool found =
                    DemoTapeStructuralPairQuery.TryFindFirst(
                        demoId,
                        ideas,
                        tags,
                        out _
                    );

                Assert.That(found, Is.False);
            }
            finally
            {
                ideas.Dispose();
                tags.Dispose();
            }
        }

        [Test]
        public void TryFindFirst_DifferentAxes_ReturnsFalse()
        {
            FixedString64Bytes demoId = Fixed64("DEMO_A");
            NetworkList<DemoTapeIdeaSemanticDebugRow> ideas = new();
            NetworkList<DemoTapeTagSemanticDebugRow> tags = new();

            try
            {
                ideas.Add(CreatePairIdea(demoId));

                tags.Add(
                    CreateTag(
                        demoId,
                        0,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        DemoTapeTagOccurrenceRole.PairDominant
                    )
                );

                tags.Add(
                    CreateTag(
                        demoId,
                        1,
                        TagAxis.Expressive,
                        TagPole.Positive,
                        DemoTapeTagOccurrenceRole.PairSubmissive
                    )
                );

                bool found =
                    DemoTapeStructuralPairQuery.TryFindFirst(
                        demoId,
                        ideas,
                        tags,
                        out _
                    );

                Assert.That(found, Is.False);
            }
            finally
            {
                ideas.Dispose();
                tags.Dispose();
            }
        }

        [Test]
        public void TryFindFirst_ReversedPairRoles_ReturnsFalse()
        {
            FixedString64Bytes demoId = Fixed64("DEMO_A");
            NetworkList<DemoTapeIdeaSemanticDebugRow> ideas = new();
            NetworkList<DemoTapeTagSemanticDebugRow> tags = new();

            try
            {
                ideas.Add(CreatePairIdea(demoId));

                tags.Add(
                    CreateTag(
                        demoId,
                        0,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        DemoTapeTagOccurrenceRole.PairSubmissive
                    )
                );

                tags.Add(
                    CreateTag(
                        demoId,
                        1,
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        DemoTapeTagOccurrenceRole.PairDominant
                    )
                );

                bool found =
                    DemoTapeStructuralPairQuery.TryFindFirst(
                        demoId,
                        ideas,
                        tags,
                        out _
                    );

                Assert.That(found, Is.False);
            }
            finally
            {
                ideas.Dispose();
                tags.Dispose();
            }
        }

        [Test]
        public void TryFindFirst_OpposingSolitaryIdeas_ReturnsFalse()
        {
            FixedString64Bytes demoId = Fixed64("DEMO_A");
            NetworkList<DemoTapeIdeaSemanticDebugRow> ideas = new();
            NetworkList<DemoTapeTagSemanticDebugRow> tags = new();

            try
            {
                ideas.Add(CreateSingleIdea(demoId, 0));
                ideas.Add(CreateSingleIdea(demoId, 1));

                tags.Add(
                    CreateTag(
                        demoId,
                        0,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        DemoTapeTagOccurrenceRole.Solitary,
                        ideaIndex: 0
                    )
                );

                tags.Add(
                    CreateTag(
                        demoId,
                        0,
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        DemoTapeTagOccurrenceRole.Solitary,
                        ideaIndex: 1
                    )
                );

                bool found =
                    DemoTapeStructuralPairQuery.TryFindFirst(
                        demoId,
                        ideas,
                        tags,
                        out _
                    );

                Assert.That(found, Is.False);
            }
            finally
            {
                ideas.Dispose();
                tags.Dispose();
            }
        }

        [Test]
        public void TryFindFirst_DifferentDemoRowsDoNotJoin_ReturnsFalse()
        {
            FixedString64Bytes requestedDemoId = Fixed64("DEMO_A");
            NetworkList<DemoTapeIdeaSemanticDebugRow> ideas = new();
            NetworkList<DemoTapeTagSemanticDebugRow> tags = new();

            try
            {
                ideas.Add(CreatePairIdea(requestedDemoId));

                tags.Add(
                    CreateTag(
                        Fixed64("DEMO_B"),
                        0,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        DemoTapeTagOccurrenceRole.PairDominant
                    )
                );

                tags.Add(
                    CreateTag(
                        Fixed64("DEMO_B"),
                        1,
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        DemoTapeTagOccurrenceRole.PairSubmissive
                    )
                );

                bool found =
                    DemoTapeStructuralPairQuery.TryFindFirst(
                        requestedDemoId,
                        ideas,
                        tags,
                        out _
                    );

                Assert.That(found, Is.False);
            }
            finally
            {
                ideas.Dispose();
                tags.Dispose();
            }
        }

        private static DemoTapeIdeaSemanticDebugRow CreatePairIdea(
            FixedString64Bytes demoId)
        {
            return new DemoTapeIdeaSemanticDebugRow
            {
                OwnerClientId = 0,
                DemoIndex = 3,
                TrackIndex = 3,
                IdeaIndex = 0,
                DemoTapeId = demoId,
                SourceTrackId = Fixed64("TRACK_A"),
                SourceIdeaId = Fixed64("IDEA_PAIR"),
                AspectId = Fixed64("ASPECT_LYRICS"),
                PayloadTypeValue = (byte)IdeaPayloadType.TagPair,
                TagOccurrenceCount = 2
            };
        }

        private static DemoTapeIdeaSemanticDebugRow CreateSingleIdea(
            FixedString64Bytes demoId,
            int ideaIndex)
        {
            return new DemoTapeIdeaSemanticDebugRow
            {
                OwnerClientId = 0,
                DemoIndex = 3,
                TrackIndex = 3,
                IdeaIndex = ideaIndex,
                DemoTapeId = demoId,
                SourceTrackId = Fixed64("TRACK_A"),
                SourceIdeaId = Fixed64($"IDEA_{ideaIndex}"),
                AspectId = Fixed64("ASPECT_TEST"),
                PayloadTypeValue = (byte)IdeaPayloadType.SingleTag,
                TagOccurrenceCount = 1
            };
        }

        private static DemoTapeTagSemanticDebugRow CreateTag(
            FixedString64Bytes demoId,
            int tagIndex,
            TagAxis axis,
            TagPole pole,
            DemoTapeTagOccurrenceRole role,
            int ideaIndex = 0)
        {
            return new DemoTapeTagSemanticDebugRow
            {
                OwnerClientId = 0,
                DemoIndex = 3,
                TrackIndex = 3,
                IdeaIndex = ideaIndex,
                TagOccurrenceIndex = tagIndex,
                DemoTapeId = demoId,
                SourceTrackId = Fixed64("TRACK_A"),
                SourceIdeaId = Fixed64(
                    ideaIndex == 0
                        ? "IDEA_PAIR"
                        : $"IDEA_{ideaIndex}"
                ),
                AxisValue = (byte)axis,
                PoleValue = (byte)pole,
                DegreeValue = (byte)TagDegree.Dominant,
                RoleValue = (byte)role
            };
        }

        private static FixedString64Bytes Fixed64(string value)
        {
            FixedString64Bytes result = default;
            result.CopyFromTruncated(value);
            return result;
        }
    }
}