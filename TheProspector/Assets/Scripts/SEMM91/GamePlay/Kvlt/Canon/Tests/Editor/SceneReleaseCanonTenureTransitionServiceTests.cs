using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Movement;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Canon.Tests.Editor
{
    public class
        SceneReleaseCanonTenureTransitionServiceTests
    {
        private readonly
            SceneReleaseCanonTenureTransitionService
            service =
                new();

        [Test]
        public void
            SameContinuousTenure_DoesNotEndRetention()
        {
            SceneRelease release =
                RetainedRelease(
                    "A",
                    "TENURE_A"
                );

            int transitionCount =
                release
                    .LifecycleTransitions
                    .Count;

            var result =
                service.Apply(
                    new[]
                    {
                        release
                    },
                    "TENURE_A",
                    "TENURE_A",
                    7
                );

            Assert.That(
                result,
                Is.Empty
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                release
                    .LifecycleTransitions
                    .Count,
                Is.EqualTo(
                    transitionCount
                )
            );
        }

        [Test]
        public void
            KeeperTenureChange_MovesRetainedReleaseToHistoricalCanon()
        {
            SceneRelease release =
                RetainedRelease(
                    "A",
                    "TENURE_A"
                );

            SceneReleaseCanonizationFreezeState
                freezeBefore =
                    release
                        .CanonizationFreezeState;

            float gravityBefore =
                release
                    .FrozenPostAssimilationGravity;

            var result =
                service.Apply(
                    new[]
                    {
                        release
                    },
                    "TENURE_A",
                    "TENURE_B",
                    7
                );

            Assert.That(
                result.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                )
            );

            Assert.That(
                release.IsCanonized,
                Is.True
            );

            Assert.That(
                release
                    .CanonizationFreezeState,
                Is.SameAs(
                    freezeBefore
                )
            );

            Assert.That(
                release
                    .FrozenPostAssimilationGravity,
                Is.EqualTo(
                    gravityBefore
                ).Within(0.0001f)
            );

            SceneReleaseLifecycleTransition
                transition =
                    release
                        .LifecycleTransitions[
                            release
                                .LifecycleTransitions
                                .Count - 1
                        ];

            Assert.That(
                transition.FromState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                transition.ToState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                )
            );

            Assert.That(
                transition.GlobalTurn,
                Is.EqualTo(7)
            );
        }

        [Test]
        public void
            SeveralEndingTenureReleasesTransitionInDeterministicIdOrder()
        {
            SceneRelease releaseA =
                RetainedRelease(
                    "A",
                    "TENURE_A"
                );

            SceneRelease releaseB =
                RetainedRelease(
                    "B",
                    "TENURE_A"
                );

            var result =
                service.Apply(
                    new[]
                    {
                        releaseB,
                        releaseA
                    },
                    "TENURE_A",
                    "TENURE_B",
                    7
                );

            Assert.That(
                result.Count,
                Is.EqualTo(2)
            );

            string expectedFirst =
                string.CompareOrdinal(
                    releaseA.ReleaseId,
                    releaseB.ReleaseId
                ) < 0
                    ? releaseA.ReleaseId
                    : releaseB.ReleaseId;

            string expectedSecond =
                expectedFirst ==
                releaseA.ReleaseId
                    ? releaseB.ReleaseId
                    : releaseA.ReleaseId;

            Assert.That(
                result[0].SceneReleaseId,
                Is.EqualTo(
                    expectedFirst
                )
            );

            Assert.That(
                result[1].SceneReleaseId,
                Is.EqualTo(
                    expectedSecond
                )
            );

            Assert.That(
                releaseA.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                )
            );

            Assert.That(
                releaseB.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                )
            );
        }

        [Test]
        public void
            RetainedReleaseFromAnotherTenure_IsNotTouched()
        {
            SceneRelease ending =
                RetainedRelease(
                    "A",
                    "TENURE_A"
                );

            SceneRelease other =
                RetainedRelease(
                    "B",
                    "TENURE_C"
                );

            var result =
                service.Apply(
                    new[]
                    {
                        ending,
                        other
                    },
                    "TENURE_A",
                    "TENURE_B",
                    7
                );

            Assert.That(
                result.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                ending.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                )
            );

            Assert.That(
                other.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );
        }

        [Test]
        public void
            HistoricalCanonNeverRevivesOnLaterKeeperTenure()
        {
            SceneRelease release =
                RetainedRelease(
                    "A",
                    "TENURE_A"
                );

            Assert.That(
                service.Apply(
                    new[]
                    {
                        release
                    },
                    "TENURE_A",
                    "TENURE_B",
                    7
                ).Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                )
            );

            int transitionCount =
                release
                    .LifecycleTransitions
                    .Count;

            /*
             * Later succession. This could correspond
             * to the original Keeper returning, but
             * their new reign has a new tenure ID.
             */
            var later =
                service.Apply(
                    new[]
                    {
                        release
                    },
                    "TENURE_B",
                    "TENURE_C",
                    9
                );

            Assert.That(
                later,
                Is.Empty
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                )
            );

            Assert.That(
                release
                    .LifecycleTransitions
                    .Count,
                Is.EqualTo(
                    transitionCount
                )
            );
        }

        private static SceneRelease
            RetainedRelease(
                string suffix,
                string tenureId)
        {
            DemoTape demo =
                Demo(
                    suffix
                );

            SceneRelease release =
                new(
                    "Release " + suffix,
                    demo.DemoTapeId,
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

            ApplyCovered(
                release,
                suffix
            );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.60f,
                    6,
                    out _
                ),
                Is.True
            );

            SceneReleaseCanonBreakthroughClaim
                breakthroughClaim =
                    new(
                        release.ReleaseId,
                        release.SourceDemoTapeId,
                        release.SourceOwnerEntityId,
                        release.HostedSceneNodeId,
                        "TRACK_" + suffix,
                        "IDEA_" + suffix,
                        0,
                        6,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak,
                        TagDegree.Weak,
                        false,
                        TagDegree.Neutral,
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        TagDegree.Weak,
                        true,
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
                            breakthroughClaim
                        }
                    );

            SceneReleaseNexusBoundaryEvaluation nexus =
                new(
                    breakthrough,
                    1f,
                    release
                        .FieldPositionState
                        .CurrentPosition,
                    SceneReleaseNexusBoundaryDisposition
                        .CanonCandidate
                );

            CanonPrecedentRecord precedent =
                new(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind.SceneRelease,
                    release.ReleaseId,
                    "TRACK_" + suffix,
                    "IDEA_" + suffix,
                    6
                );

            SceneReleaseCanonAssimilationPairEvaluation
                pair =
                    new(
                        release.ReleaseId,
                        release.SourceDemoTapeId,
                        release.SourceOwnerEntityId,
                        release.HostedSceneNodeId,
                        "TRACK_" + suffix,
                        "IDEA_" + suffix,
                        0,
                        6,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak,
                        TagDegree.Weak,
                        true,
                        TagDegree.Weak,
                        new[]
                        {
                            precedent
                        }
                    );

            SceneReleaseCanonAssimilationEvaluation
                assimilation =
                    new(
                        nexus,
                        tenureId,
                        new[]
                        {
                            pair
                        }
                    );

            SceneReleaseCanonAssimilationApplication
                application =
                    new(
                        assimilation,
                        Array.Empty<
                            SceneReleaseCanonAssimilationActivationRecord>()
                    );

            new SceneReleaseCanonFreezeService()
                .Apply(
                    release,
                    demo,
                    Environment(),
                    application
                );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            return release;
        }

        private static DemoTape Demo(
            string suffix)
        {
            DemoTapeIdeaSnapshot idea =
                new(
                    "IDEA_" + suffix,
                    0,
                    "ASPECT_" + suffix,
                    IdeaPayloadType.TagPair,
                    "AUTHOR",
                    TagContainerType.Transient,
                    1f,
                    new[]
                    {
                        new
                            DemoTapeTagOccurrenceSnapshot(
                                TagAxis.Symbolic,
                                TagPole.Negative,
                                TagDegree.Weak,
                                DemoTapeTagOccurrenceRole
                                    .PairDominant
                            ),

                        new
                            DemoTapeTagOccurrenceSnapshot(
                                TagAxis.Symbolic,
                                TagPole.Positive,
                                TagDegree.Weak,
                                DemoTapeTagOccurrenceRole
                                    .PairSubmissive
                            )
                    }
                );

            DemoTapeTrackSnapshot track =
                new(
                    "TRACK_" + suffix,
                    "Track " + suffix,
                    1f,
                    1f,
                    new[]
                    {
                        idea
                    }
                );

            return new DemoTape(
                "DEMO_" + suffix,
                "Demo " + suffix,
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
            TrackEvaluationEnvironment
            Environment()
        {
            NormativeCentre centre =
                new(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Weak,
                            1f
                        )
                    }
                );

            SocietyNormativeProfile society =
                new();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            return new TrackEvaluationEnvironment(
                6,
                centre,
                centre,
                society
            );
        }

        private static void ApplyCovered(
            SceneRelease release,
            string suffix)
        {
            string behavior =
                "PRAXIS_" + suffix;

            ActivationLegitimacyCandidate candidate =
                new(
                    ActivationAttemptRoute.Performance,
                    "HAPPENING_" + suffix,
                    "INTENT_" + suffix,
                    "PLAYER_" + suffix,
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    "TRACK_" + suffix,
                    "IDEA_" + suffix,
                    0,
                    behavior,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    TagDegree.Weak,
                    TagDegree.Weak
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT_" + suffix,
                    "KVLT",
                    behavior,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    6,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "TEST_" + suffix
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
    }
}