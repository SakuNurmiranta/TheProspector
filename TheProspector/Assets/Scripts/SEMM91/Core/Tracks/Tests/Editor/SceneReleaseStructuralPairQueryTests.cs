using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.Networking.DebugSnapshots;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class
        SceneReleaseStructuralPairQueryTests
    {
        private NetworkList<
            DomainSnapshotReplicator
                .KeeperReleaseDebugRow>
            releaseRows;

        private NetworkList<
            DemoTapeIdeaSemanticDebugRow>
            ideaRows;

        private NetworkList<
            DemoTapeTagSemanticDebugRow>
            tagRows;

        [SetUp]
        public void SetUp()
        {
            releaseRows =
                new NetworkList<
                    DomainSnapshotReplicator
                        .KeeperReleaseDebugRow>();

            ideaRows =
                new NetworkList<
                    DemoTapeIdeaSemanticDebugRow>();

            tagRows =
                new NetworkList<
                    DemoTapeTagSemanticDebugRow>();
        }

        [TearDown]
        public void TearDown()
        {
            releaseRows.Dispose();
            ideaRows.Dispose();
            tagRows.Dispose();
        }

        [Test]
        public void TryFindFirst_MatchingReleaseAndDemo_ReturnsCandidate()
        {
            FixedString64Bytes releaseId =
                Fixed64("RELEASE_A");

            FixedString64Bytes demoId =
                Fixed64("DEMO_A");

            AddRelease(
                releaseId,
                demoId
            );

            AddPairSemantics(
                demoId
            );

            bool found =
                SceneReleaseStructuralPairQuery
                    .TryFindFirst(
                        releaseId,
                        releaseRows,
                        ideaRows,
                        tagRows,
                        out DemoTapeStructuralPairCandidate
                            candidate
                    );

            Assert.That(found, Is.True);

            Assert.That(
                candidate.DemoTapeId,
                Is.EqualTo(demoId)
            );

            Assert.That(
                candidate.Axis,
                Is.EqualTo(TagAxis.Symbolic)
            );
        }

        [Test]
        public void TryFindFirst_MissingRelease_ReturnsFalse()
        {
            AddPairSemantics(
                Fixed64("DEMO_A")
            );

            bool found =
                SceneReleaseStructuralPairQuery
                    .TryFindFirst(
                        Fixed64("UNKNOWN_RELEASE"),
                        releaseRows,
                        ideaRows,
                        tagRows,
                        out _
                    );

            Assert.That(found, Is.False);
        }

        [Test]
        public void TryFindFirst_ReleaseTargetsDifferentDemo_ReturnsFalse()
        {
            FixedString64Bytes releaseId =
                Fixed64("RELEASE_A");

            AddRelease(
                releaseId,
                Fixed64("DEMO_B")
            );

            AddPairSemantics(
                Fixed64("DEMO_A")
            );

            bool found =
                SceneReleaseStructuralPairQuery
                    .TryFindFirst(
                        releaseId,
                        releaseRows,
                        ideaRows,
                        tagRows,
                        out _
                    );

            Assert.That(found, Is.False);
        }

        [Test]
        public void TryFindFirst_UnreleasedDemoSemanticsAreNotReachedThroughAnotherRelease()
        {
            FixedString64Bytes releasedDemoId =
                Fixed64("DEMO_RELEASED");

            FixedString64Bytes unreleasedDemoId =
                Fixed64("DEMO_UNRELEASED");

            FixedString64Bytes releaseId =
                Fixed64("RELEASE_A");

            AddRelease(
                releaseId,
                releasedDemoId
            );

            AddPairSemantics(
                unreleasedDemoId
            );

            bool found =
                SceneReleaseStructuralPairQuery
                    .TryFindFirst(
                        releaseId,
                        releaseRows,
                        ideaRows,
                        tagRows,
                        out _
                    );

            Assert.That(found, Is.False);
        }

        private void AddRelease(
            FixedString64Bytes releaseId,
            FixedString64Bytes demoId)
        {
            releaseRows.Add(
                new DomainSnapshotReplicator
                    .KeeperReleaseDebugRow
                {
                    ReleaseId = releaseId,
                    SourceDemoTapeId = demoId
                }
            );
        }

        private void AddPairSemantics(
            FixedString64Bytes demoId)
        {
            FixedString64Bytes trackId =
                Fixed64("TRACK_A");

            FixedString64Bytes ideaId =
                Fixed64("IDEA_PAIR");

            ideaRows.Add(
                new DemoTapeIdeaSemanticDebugRow
                {
                    OwnerClientId = 0,
                    DemoIndex = 0,
                    TrackIndex = 0,
                    IdeaIndex = 0,

                    DemoTapeId = demoId,
                    SourceTrackId = trackId,
                    SourceIdeaId = ideaId,

                    PayloadTypeValue =
                        (byte)IdeaPayloadType.TagPair,

                    TagOccurrenceCount = 2
                }
            );

            tagRows.Add(
                CreateTag(
                    demoId,
                    trackId,
                    ideaId,
                    tagIndex: 0,
                    pole: TagPole.Negative,
                    role:
                    DemoTapeTagOccurrenceRole
                        .PairDominant
                )
            );

            tagRows.Add(
                CreateTag(
                    demoId,
                    trackId,
                    ideaId,
                    tagIndex: 1,
                    pole: TagPole.Positive,
                    role:
                    DemoTapeTagOccurrenceRole
                        .PairSubmissive
                )
            );
        }

        private static
            DemoTapeTagSemanticDebugRow
            CreateTag(
                FixedString64Bytes demoId,
                FixedString64Bytes trackId,
                FixedString64Bytes ideaId,
                int tagIndex,
                TagPole pole,
                DemoTapeTagOccurrenceRole role)
        {
            return new DemoTapeTagSemanticDebugRow
            {
                OwnerClientId = 0,
                DemoIndex = 0,
                TrackIndex = 0,
                IdeaIndex = 0,

                TagOccurrenceIndex =
                    tagIndex,

                DemoTapeId = demoId,
                SourceTrackId = trackId,
                SourceIdeaId = ideaId,

                AxisValue =
                    (byte)TagAxis.Symbolic,

                PoleValue =
                    (byte)pole,

                DegreeValue =
                    (byte)TagDegree.Dominant,

                RoleValue =
                    (byte)role
            };
        }

        private static FixedString64Bytes
            Fixed64(string value)
        {
            FixedString64Bytes result =
                default;

            result.CopyFromTruncated(value);

            return result;
        }
    }
}