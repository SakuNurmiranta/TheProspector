using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Movement;
using SEMM91.GamePlay.Kvlt.Transgression;

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
            SimultaneousFrontierFromOtherRelease_IsUsedForAssimilation()
        {
            Fixture fixture =
                BuildFixture(
                    dormantPairIsTrveCapable: true,
                    includeSolitary: false
                );

            SceneReleaseCanonAssimilationEvaluation result =
                evaluator.Evaluate(
                    fixture.ReleaseA,
                    fixture.DemoA,
                    fixture.NexusA,
                    fixture.Merge
                );

            SceneReleaseCanonAssimilationPairEvaluation
                dormant =
                    FindPair(
                        result,
                        "A_DORMANT"
                    );

            /*
             * Canon_t had no Physical precedent.
             *
             * Release B simultaneously established
             * Physical2 in NewCanon.
             */
            Assert.That(
                fixture.Merge.PreviousCanon
                    .TryGetCanonicalDegree(
                        TagAxis.Physical,
                        TagPole.Negative,
                        out _
                    ),
                Is.False
            );

            Assert.That(
                fixture.Merge.NewCanon
                    .TryGetCanonicalDegree(
                        TagAxis.Physical,
                        TagPole.Negative,
                        out TagDegree newDegree
                    ),
                Is.True
            );

            Assert.That(
                newDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                dormant.ExistingActivationDegree,
                Is.EqualTo(
                    TagDegree.Neutral
                )
            );

            Assert.That(
                dormant.NewCanonicalDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                dormant.FinalActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                dormant.RaisesActivation,
                Is.True
            );
        }

        [Test]
        public void
            RecordedMaterialAboveNewCanon_RemainsUnrealized()
        {
            Fixture fixture =
                BuildFixture(
                    dormantPairIsTrveCapable: true,
                    includeSolitary: false
                );

            SceneReleaseCanonAssimilationEvaluation result =
                evaluator.Evaluate(
                    fixture.ReleaseA,
                    fixture.DemoA,
                    fixture.NexusA,
                    fixture.Merge
                );

            SceneReleaseCanonAssimilationPairEvaluation
                dormant =
                    FindPair(
                        result,
                        "A_DORMANT"
                    );

            /*
             * Recorded Physical3
             * NewCanon Physical2
             *
             * Assimilation reaches only A2.
             */
            Assert.That(
                dormant.RecordedDominantDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                dormant.FinalActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                (int)dormant.FinalActivationDegree,
                Is.LessThan(
                    (int)dormant.RecordedDominantDegree
                )
            );
        }

        [Test]
        public void
            IneligibleFormalPair_IsNotAssimilated()
        {
            Fixture fixture =
                BuildFixture(
                    dormantPairIsTrveCapable: false,
                    includeSolitary: false
                );

            SceneReleaseCanonAssimilationEvaluation result =
                evaluator.Evaluate(
                    fixture.ReleaseA,
                    fixture.DemoA,
                    fixture.NexusA,
                    fixture.Merge
                );

            /*
             * The physical pair exists on the tape and
             * NewCanon recognizes Physical2, but its
             * frozen semantic claim says it is not
             * TRVE-capable.
             */
            Assert.That(
                HasPair(
                    result,
                    "A_DORMANT"
                ),
                Is.False
            );

            Assert.That(
                fixture.ReleaseA
                    .TryGetPairActivationState(
                        "TRACK_A",
                        "A_DORMANT",
                        1,
                        out _
                    ),
                Is.False
            );
        }

        [Test]
        public void
            SolitaryTag_RemainsOutsideAssimilation()
        {
            Fixture fixture =
                BuildFixture(
                    dormantPairIsTrveCapable: true,
                    includeSolitary: true
                );

            SceneReleaseCanonAssimilationEvaluation result =
                evaluator.Evaluate(
                    fixture.ReleaseA,
                    fixture.DemoA,
                    fixture.NexusA,
                    fixture.Merge
                );

            Assert.That(
                result.PairEvaluations.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                HasPair(
                    result,
                    "A_HEADLINE"
                ),
                Is.True
            );

            Assert.That(
                HasPair(
                    result,
                    "A_DORMANT"
                ),
                Is.True
            );

            Assert.That(
                HasPair(
                    result,
                    "A_SOLITARY"
                ),
                Is.False
            );
        }

        [Test]
        public void
            CandidateOutsideMergeCannotAssimilateAgainstItsNewCanon()
        {
            Fixture fixture =
                BuildFixture(
                    dormantPairIsTrveCapable: true,
                    includeSolitary: false
                );

            DemoTape outsiderDemo =
                DemoForA(
                    includeSolitary: false
                );

            SceneRelease outsider =
                FieldRelease(
                    "OUTSIDER",
                    outsiderDemo.DemoTapeId
                );

            ApplyCovered(
                outsider,
                "TRACK_A",
                "A_HEADLINE",
                0,
                "OUTSIDER_PLAYER",
                TagAxis.Symbolic,
                TagDegree.Dominant,
                TagDegree.Dominant
            );

            SceneReleaseNexusBoundaryEvaluation
                outsiderNexus =
                    NexusForA(
                        outsider,
                        dormantPairIsTrveCapable: true
                    );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        outsider,
                        outsiderDemo,
                        outsiderNexus,
                        fixture.Merge
                    )
            );
        }

        private static Fixture BuildFixture(
            bool dormantPairIsTrveCapable,
            bool includeSolitary)
        {
            CanonState previousCanon =
                new();

            Assert.That(
                previousCanon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind.ScenarioSeed,
                    "OLD_SYMBOLIC"
                ),
                Is.True
            );

            DemoTape demoA =
                DemoForA(
                    includeSolitary
                );

            SceneRelease releaseA =
                FieldRelease(
                    "A",
                    demoA.DemoTapeId
                );

            ApplyCovered(
                releaseA,
                "TRACK_A",
                "A_HEADLINE",
                0,
                "PLAYER_A",
                TagAxis.Symbolic,
                TagDegree.Dominant,
                TagDegree.Dominant
            );

            SceneReleaseNexusBoundaryEvaluation nexusA =
                NexusForA(
                    releaseA,
                    dormantPairIsTrveCapable
                );

            SceneRelease releaseB =
                FieldRelease(
                    "B",
                    "DEMO_B"
                );

            ApplyCovered(
                releaseB,
                "TRACK_B",
                "B_HEADLINE",
                0,
                "PLAYER_B",
                TagAxis.Physical,
                TagDegree.Dominant,
                TagDegree.Dominant
            );

            SceneReleaseNexusBoundaryEvaluation nexusB =
                NexusForB(
                    releaseB
                );

            CanonSimultaneousMergeEvaluation merge =
                new CanonSimultaneousMergeEvaluator()
                    .Evaluate(
                        previousCanon,
                        new[]
                        {
                            new SceneReleaseCanonMergeCandidate(
                                releaseA,
                                nexusA
                            ),

                            new SceneReleaseCanonMergeCandidate(
                                releaseB,
                                nexusB
                            )
                        },
                        "TENURE_A"
                    );

            return new Fixture(
                releaseA,
                demoA,
                nexusA,
                merge
            );
        }

        private static SceneRelease
            FieldRelease(
                string suffix,
                string demoId)
        {
            SceneRelease release =
                new(
                    "Release " + suffix,
                    demoId,
                    "OWNER_" + suffix,
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

        private static DemoTape DemoForA(
            bool includeSolitary)
        {
            DemoTapeIdeaSnapshot headline =
                Pair(
                    "A_HEADLINE",
                    0,
                    TagAxis.Symbolic,
                    TagDegree.Dominant
                );

            DemoTapeIdeaSnapshot dormant =
                Pair(
                    "A_DORMANT",
                    1,
                    TagAxis.Physical,
                    TagDegree.Transgressive
                );

            if (!includeSolitary)
            {
                return Demo(
                    "DEMO_A",
                    "TRACK_A",
                    new[]
                    {
                        headline,
                        dormant
                    }
                );
            }

            DemoTapeIdeaSnapshot solitary =
                new(
                    "A_SOLITARY",
                    2,
                    "ASPECT_A_SOLITARY",
                    IdeaPayloadType.SingleTag,
                    "AUTHOR",
                    TagContainerType.Transient,
                    1f,
                    new[]
                    {
                        new DemoTapeTagOccurrenceSnapshot(
                            TagAxis.Physical,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            DemoTapeTagOccurrenceRole
                                .Solitary
                        )
                    }
                );

            return Demo(
                "DEMO_A",
                "TRACK_A",
                new[]
                {
                    headline,
                    dormant,
                    solitary
                }
            );
        }

        private static DemoTape Demo(
            string demoId,
            string trackId,
            DemoTapeIdeaSnapshot[] ideas)
        {
            DemoTapeTrackSnapshot track =
                new(
                    trackId,
                    "Track",
                    1f,
                    1f,
                    ideas
                );

            return new DemoTape(
                demoId,
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

        private static DemoTapeIdeaSnapshot Pair(
            string ideaId,
            int ideaIndex,
            TagAxis axis,
            TagDegree dominantDegree)
        {
            return new DemoTapeIdeaSnapshot(
                ideaId,
                ideaIndex,
                "ASPECT_" + ideaId,
                IdeaPayloadType.TagPair,
                "AUTHOR",
                TagContainerType.Transient,
                1f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        axis,
                        TagPole.Negative,
                        dominantDegree,
                        DemoTapeTagOccurrenceRole
                            .PairDominant
                    ),

                    new DemoTapeTagOccurrenceSnapshot(
                        axis,
                        TagPole.Positive,
                        TagDegree.Weak,
                        DemoTapeTagOccurrenceRole
                            .PairSubmissive
                    )
                }
            );
        }

        private static
            SceneReleaseNexusBoundaryEvaluation
            NexusForA(
                SceneRelease release,
                bool dormantPairIsTrveCapable)
        {
            SceneReleaseCanonBreakthroughClaim headline =
                Claim(
                    release,
                    "TRACK_A",
                    "A_HEADLINE",
                    0,
                    TagAxis.Symbolic,
                    recorded:
                        TagDegree.Dominant,
                    active:
                        TagDegree.Dominant,
                    hasOldCanon:
                        true,
                    oldCanon:
                        TagDegree.Weak,
                    isCurrentTrve:
                        true,
                    isTrveCapable:
                        true
                );

            SceneReleaseCanonBreakthroughClaim dormant =
                Claim(
                    release,
                    "TRACK_A",
                    "A_DORMANT",
                    1,
                    TagAxis.Physical,
                    recorded:
                        TagDegree.Transgressive,
                    active:
                        TagDegree.Neutral,
                    hasOldCanon:
                        false,
                    oldCanon:
                        TagDegree.Neutral,
                    isCurrentTrve:
                        false,
                    isTrveCapable:
                        dormantPairIsTrveCapable
                );

            return Nexus(
                release,
                new[]
                {
                    headline,
                    dormant
                }
            );
        }

        private static
            SceneReleaseNexusBoundaryEvaluation
            NexusForB(
                SceneRelease release)
        {
            SceneReleaseCanonBreakthroughClaim headline =
                Claim(
                    release,
                    "TRACK_B",
                    "B_HEADLINE",
                    0,
                    TagAxis.Physical,
                    recorded:
                        TagDegree.Dominant,
                    active:
                        TagDegree.Dominant,
                    hasOldCanon:
                        false,
                    oldCanon:
                        TagDegree.Neutral,
                    isCurrentTrve:
                        true,
                    isTrveCapable:
                        true
                );

            return Nexus(
                release,
                new[]
                {
                    headline
                }
            );
        }

        private static
            SceneReleaseNexusBoundaryEvaluation
            Nexus(
                SceneRelease release,
                SceneReleaseCanonBreakthroughClaim[]
                    claims)
        {
            SceneReleaseCanonBreakthroughEvaluation
                breakthrough =
                    new(
                        release.ReleaseId,
                        release.SourceDemoTapeId,
                        release.SourceOwnerEntityId,
                        release.HostedSceneNodeId,
                        6,
                        0.50f,
                        claims
                    );

            Assert.That(
                breakthrough.HasQualifyingBreakthrough,
                Is.True
            );

            return new
                SceneReleaseNexusBoundaryEvaluation(
                    breakthrough,
                    1f,
                    release.FieldPositionState
                        .CurrentPosition,
                    SceneReleaseNexusBoundaryDisposition
                        .CanonCandidate
                );
        }

        private static
            SceneReleaseCanonBreakthroughClaim
            Claim(
                SceneRelease release,
                string trackId,
                string ideaId,
                int ideaIndex,
                TagAxis axis,
                TagDegree recorded,
                TagDegree active,
                bool hasOldCanon,
                TagDegree oldCanon,
                bool isCurrentTrve,
                bool isTrveCapable)
        {
            return new
                SceneReleaseCanonBreakthroughClaim(
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    release.SourceOwnerEntityId,
                    release.HostedSceneNodeId,
                    trackId,
                    ideaId,
                    ideaIndex,
                    6,
                    axis,
                    TagPole.Negative,
                    recorded,
                    active,
                    hasOldCanon,
                    oldCanon,
                    axis,
                    TagPole.Positive,
                    TagDegree.Weak,
                    isCurrentTrve,
                    isTrveCapable
                );
        }

        private static void ApplyCovered(
            SceneRelease release,
            string trackId,
            string ideaId,
            int ideaIndex,
            string actorId,
            TagAxis axis,
            TagDegree appliedDegree,
            TagDegree recordedDegree)
        {
            string behavior =
                "PRAXIS_" +
                ideaId;

            ActivationLegitimacyCandidate candidate =
                new(
                    ActivationAttemptRoute.Performance,
                    "HAPPENING_" + ideaId,
                    "INTENT_" + ideaId,
                    actorId,
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    trackId,
                    ideaId,
                    ideaIndex,
                    behavior,
                    axis,
                    TagPole.Negative,
                    appliedDegree,
                    appliedDegree,
                    recordedDegree
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT_" + ideaId,
                    "KVLT",
                    behavior,
                    axis,
                    TagPole.Negative,
                    appliedDegree,
                    6,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "TEST"
                );

            ActivationLegitimacyAssessment assessment =
                new(
                    candidate,
                    ActivationLegitimacyDisposition
                        .Covered,
                    precedent
                );

            Assert.That(
                new SceneReleaseActivationStateService()
                    .ApplyCovered(
                        release,
                        assessment,
                        6
                    ),
                Is.True
            );
        }

        private static
            SceneReleaseCanonAssimilationPairEvaluation
            FindPair(
                SceneReleaseCanonAssimilationEvaluation
                    evaluation,
                string ideaId)
        {
            foreach (
                SceneReleaseCanonAssimilationPairEvaluation
                    pair
                in evaluation.PairEvaluations)
            {
                if (pair.SourceIdeaId ==
                    ideaId)
                {
                    return pair;
                }
            }

            Assert.Fail(
                "Assimilation pair not found | " +
                $"idea={ideaId}"
            );

            return null;
        }

        private static bool HasPair(
            SceneReleaseCanonAssimilationEvaluation
                evaluation,
            string ideaId)
        {
            foreach (
                SceneReleaseCanonAssimilationPairEvaluation
                    pair
                in evaluation.PairEvaluations)
            {
                if (pair.SourceIdeaId ==
                    ideaId)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class Fixture
        {
            public SceneRelease ReleaseA { get; }

            public DemoTape DemoA { get; }

            public SceneReleaseNexusBoundaryEvaluation
                NexusA { get; }

            public CanonSimultaneousMergeEvaluation
                Merge { get; }

            public Fixture(
                SceneRelease releaseA,
                DemoTape demoA,
                SceneReleaseNexusBoundaryEvaluation
                    nexusA,
                CanonSimultaneousMergeEvaluation merge)
            {
                ReleaseA =
                    releaseA;

                DemoA =
                    demoA;

                NexusA =
                    nexusA;

                Merge =
                    merge;
            }
        }
    }
}