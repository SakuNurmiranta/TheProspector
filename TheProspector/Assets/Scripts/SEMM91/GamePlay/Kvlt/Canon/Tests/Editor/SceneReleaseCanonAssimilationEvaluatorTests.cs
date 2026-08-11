using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Canon.Tests.Editor
{
    public class
        SceneReleaseCanonAssimilationEvaluatorTests
    {
        private readonly
            SceneReleaseCanonAssimilationEvaluator
                evaluator =
                    new();

        [Test]
        public void
            CanonCandidate_UsesPreviouslySettledCanonCeiling()
        {
            DemoTape demo =
                DemoWithPair(
                    TagDegree.Transgressive
                );

            SceneRelease release =
                CandidateRelease(
                    demo
                );

            CanonState canon =
                Canon(
                    TagDegree.Dominant
                );

            var result =
                evaluator.Evaluate(
                    release,
                    demo,
                    NexusCandidate(
                        release
                    ),
                    canon
                );

            Assert.That(
                result.PairEvaluations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.PairEvaluations[0]
                    .RecordedDominantDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                result.PairEvaluations[0]
                    .ExistingActivationDegree,
                Is.EqualTo(
                    TagDegree.Neutral
                )
            );

            Assert.That(
                result.PairEvaluations[0]
                    .ExistingCanonicalDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                result.PairEvaluations[0]
                    .FinalActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                result.RaisedPairCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            TiedCanonicalCeilingProvenance_IsPreserved()
        {
            DemoTape demo =
                DemoWithPair(
                    TagDegree.Transgressive
                );

            SceneRelease release =
                CandidateRelease(
                    demo
                );

            CanonState canon =
                new();

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    CanonProvenanceKind.SceneRelease,
                    "OLD_RELEASE_A"
                ),
                Is.True
            );

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    CanonProvenanceKind.SceneRelease,
                    "OLD_RELEASE_B"
                ),
                Is.True
            );

            var result =
                evaluator.Evaluate(
                    release,
                    demo,
                    NexusCandidate(
                        release
                    ),
                    canon
                );

            Assert.That(
                result.PairEvaluations[0]
                    .CanonicalPrecedents.Count,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void
            SolitaryTags_AreNotAssimilationTargets()
        {
            DemoTape demo =
                DemoWithPairAndSolitary();

            SceneRelease release =
                CandidateRelease(
                    demo
                );

            CanonState canon =
                Canon(
                    TagDegree.Dominant
                );

            var result =
                evaluator.Evaluate(
                    release,
                    demo,
                    NexusCandidate(
                        release
                    ),
                    canon
                );

            Assert.That(
                result.PairEvaluations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.PairEvaluations[0]
                    .SourceIdeaId,
                Is.EqualTo("PAIR")
            );
        }

        [Test]
        public void
            NonCandidate_IsRejected()
        {
            DemoTape demo =
                DemoWithPair(
                    TagDegree.Dominant
                );

            SceneRelease release =
                CandidateRelease(
                    demo
                );

            SceneReleaseNexusBoundaryEvaluation
                notCandidate =
                    NexusEvaluation(
                        release,
                        qualifying: false
                    );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        demo,
                        notCandidate,
                        new CanonState()
                    )
            );
        }

        private static DemoTape DemoWithPair(
            TagDegree dominantDegree)
        {
            return Demo(
                new[]
                {
                    PairIdea(
                        "PAIR",
                        0,
                        dominantDegree
                    )
                }
            );
        }

        private static DemoTape
            DemoWithPairAndSolitary()
        {
            DemoTapeIdeaSnapshot solitary =
                new(
                    "SOLITARY",
                    1,
                    "ASPECT_SOLITARY",
                    IdeaPayloadType.SingleTag,
                    "AUTHOR",
                    TagContainerType.Transient,
                    1f,
                    new[]
                    {
                        new DemoTapeTagOccurrenceSnapshot(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            DemoTapeTagOccurrenceRole
                                .Solitary
                        )
                    }
                );

            return Demo(
                new[]
                {
                    PairIdea(
                        "PAIR",
                        0,
                        TagDegree.Transgressive
                    ),
                    solitary
                }
            );
        }

        private static DemoTape Demo(
            DemoTapeIdeaSnapshot[] ideas)
        {
            DemoTapeTrackSnapshot track =
                new(
                    "TRACK",
                    "Track",
                    1f,
                    1f,
                    ideas
                );

            return new DemoTape(
                "DEMO",
                "Demo",
                "SET",
                "Set",
                1,
                1,
                1f,
                new[]
                {
                    track
                }
            );
        }

        private static
            DemoTapeIdeaSnapshot
            PairIdea(
                string id,
                int index,
                TagDegree dominantDegree)
        {
            return new DemoTapeIdeaSnapshot(
                id,
                index,
                "ASPECT_" + id,
                IdeaPayloadType.TagPair,
                "AUTHOR",
                TagContainerType.Transient,
                1f,
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
                        TagDegree.Weak,
                        DemoTapeTagOccurrenceRole
                            .PairSubmissive
                    )
                }
            );
        }

        private static SceneRelease CandidateRelease(
            DemoTape demo)
        {
            SceneRelease release =
                new(
                    "Release",
                    demo.DemoTapeId,
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            Assert.That(
                release.TryFetter(5),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    0.50f,
                    5
                ),
                Is.True
            );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.60f,
                    6,
                    out _
                ),
                Is.True
            );

            return release;
        }

        private static CanonState Canon(
            TagDegree degree)
        {
            CanonState canon =
                new();

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    degree,
                    CanonProvenanceKind.ScenarioSeed,
                    "CANON"
                ),
                Is.True
            );

            return canon;
        }

        private static
            SceneReleaseNexusBoundaryEvaluation
            NexusCandidate(
                SceneRelease release)
        {
            return NexusEvaluation(
                release,
                qualifying: true
            );
        }

        private static
            SceneReleaseNexusBoundaryEvaluation
            NexusEvaluation(
                SceneRelease release,
                bool qualifying)
        {
            SceneReleaseCanonBreakthroughClaim claim =
                new(
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    release.SourceOwnerEntityId,
                    release.HostedSceneNodeId,
                    "TRACK",
                    "PAIR",
                    0,
                    6,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    qualifying
                        ? TagDegree.Dominant
                        : TagDegree.Weak,
                    true,
                    TagDegree.Weak,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak,
                    true
                );

            SceneReleaseCanonBreakthroughEvaluation
                breakthrough =
                    new(
                        release.ReleaseId,
                        release.SourceDemoTapeId,
                        release.SourceOwnerEntityId,
                        release.HostedSceneNodeId,
                        6,
                        0.50f,
                        new[]
                        {
                            claim
                        }
                    );

            return new
                SceneReleaseNexusBoundaryEvaluation(
                    breakthrough,
                    nexusBoundary: 1f,
                    fieldPosition:
                        release.FieldPositionState
                            .CurrentPosition,
                    qualifying
                        ? SceneReleaseNexusBoundaryDisposition
                            .CanonCandidate
                        : SceneReleaseNexusBoundaryDisposition
                            .RemainsField
                );
        }
    }
}