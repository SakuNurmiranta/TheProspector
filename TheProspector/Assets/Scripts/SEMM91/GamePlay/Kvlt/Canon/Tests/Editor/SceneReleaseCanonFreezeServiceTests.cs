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
        SceneReleaseCanonFreezeServiceTests
    {
        private readonly
            SceneReleaseCanonFreezeService service =
                new();

        [Test]
        public void
            FreezeRecalculatesAndStoresPostAssimilationGravity()
        {
            Fixture fixture =
                BuildAppliedAssimilation();

            SceneReleaseCanonFreezeApplication result =
                service.Apply(
                    fixture.Release,
                    fixture.Demo,
                    fixture.Environment,
                    fixture.AssimilationApplication
                );

            Assert.That(
                result.FinalLegitimacy.Gravity,
                Is.GreaterThan(
                    fixture.PreAssimilationLegitimacy
                        .Gravity
                )
            );

            Assert.That(
                fixture.Release
                    .FrozenPostAssimilationGravity,
                Is.EqualTo(
                    result.FinalLegitimacy.Gravity
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            FreezeStoresCanonizedTurnAndKeeperTenure()
        {
            Fixture fixture =
                BuildAppliedAssimilation();

            service.Apply(
                fixture.Release,
                fixture.Demo,
                fixture.Environment,
                fixture.AssimilationApplication
            );

            Assert.That(
                fixture.Release.IsCanonized,
                Is.True
            );

            Assert.That(
                fixture.Release.IsActivationFrozen,
                Is.True
            );

            Assert.That(
                fixture.Release.CanonizedTurn,
                Is.EqualTo(6)
            );

            Assert.That(
                fixture.Release
                    .CanonizedUnderKeeperTenureId,
                Is.EqualTo("TENURE_A")
            );

            Assert.That(
                fixture.Release
                    .CanonizationFreezeState
                    .FrozenScenePosition,
                Is.EqualTo(
                    fixture.Release
                        .FieldPositionState
                        .CurrentPosition
                ).Within(0.0001f)
            );

            /*
             * R4 has not introduced CanonRetained
             * lifecycle yet.
             */
            Assert.That(
                fixture.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );
        }

        [Test]
        public void
            FreezeCapturesFinalAuthoritativePairActivation()
        {
            Fixture fixture =
                BuildAppliedAssimilation();

            service.Apply(
                fixture.Release,
                fixture.Demo,
                fixture.Environment,
                fixture.AssimilationApplication
            );

            SceneReleaseCanonizationFreezeState freeze =
                fixture.Release
                    .CanonizationFreezeState;

            Assert.That(
                freeze.FrozenPairActivations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                freeze.TryGetFrozenPairActivation(
                    "TRACK",
                    "IDEA",
                    0,
                    out
                    SceneReleaseFrozenPairActivation
                        activation
                ),
                Is.True
            );

            Assert.That(
                activation.RecordedDominantDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                activation.FinalActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );
        }

        [Test]
        public void
            CoveredHappeningCannotModifyActivationAfterFreeze()
        {
            Fixture fixture =
                BuildAppliedAssimilation();

            service.Apply(
                fixture.Release,
                fixture.Demo,
                fixture.Environment,
                fixture.AssimilationApplication
            );

            int historyBefore =
                fixture.Release
                    .ActivationHistory.Count;

            bool applied =
                ApplyCovered(
                    fixture.Release,
                    TagDegree.Dominant,
                    TagDegree.Dominant
                );

            Assert.That(
                applied,
                Is.False
            );

            Assert.That(
                fixture.Release
                    .ActivationHistory.Count,
                Is.EqualTo(historyBefore)
            );

            fixture.Release
                .TryGetPairActivationState(
                    "TRACK",
                    "IDEA",
                    0,
                    out
                    SceneReleasePairActivationState
                        state
                );

            Assert.That(
                state.CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );
        }

        [Test]
        public void
            NewPendingActivationCannotBeStoredAfterFreeze()
        {
            Fixture fixture =
                BuildAppliedAssimilation();

            service.Apply(
                fixture.Release,
                fixture.Demo,
                fixture.Environment,
                fixture.AssimilationApplication
            );

            ActivationLegitimacyCandidate candidate =
                Candidate(
                    fixture.Release,
                    TagDegree.Dominant,
                    TagDegree.Dominant
                );

            PendingActivationCandidate pending =
                new(
                    candidate,
                    "LATE_CRISIS",
                    7
                );

            Assert.That(
                new SceneReleaseActivationStateService()
                    .StorePending(
                        fixture.Release,
                        pending,
                        out _
                    ),
                Is.False
            );

            Assert.That(
                fixture.Release.PendingActivations,
                Is.Empty
            );
        }

        [Test]
        public void
            CanonAssimilationCannotRunAgainAfterFreeze()
        {
            Fixture fixture =
                BuildAppliedAssimilation();

            service.Apply(
                fixture.Release,
                fixture.Demo,
                fixture.Environment,
                fixture.AssimilationApplication
            );

            Assert.Throws<
                System.InvalidOperationException>(
                () =>
                    new
                            SceneReleaseCanonAssimilationService()
                        .Apply(
                            fixture.Release,
                            fixture.AssimilationApplication
                                .Evaluation
                        )
            );
        }

        private static Fixture
            BuildAppliedAssimilation()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                new(
                    "Canon Candidate",
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

            /*
             * Before NewCanon assimilation:
             *
             * Recorded D2
             * Current A1
             * Old Canon D0
             *
             * This is already a valid realized
             * breakthrough and therefore sufficient
             * for Nexus candidacy.
             */
            Assert.That(
                ApplyCovered(
                    release,
                    TagDegree.Weak,
                    TagDegree.Dominant
                ),
                Is.True
            );

            TrackEvaluationEnvironment environment =
                Environment();

            SceneReleaseLegitimacyEvaluation
                preAssimilation =
                    new
                            SceneReleaseLegitimacyEvaluator()
                        .Evaluate(
                            release,
                            demo,
                            environment
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
                        "TRACK",
                        "IDEA",
                        0,
                        6,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
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
                    release.FieldPositionState
                        .CurrentPosition,
                    SceneReleaseNexusBoundaryDisposition
                        .CanonCandidate
                );

            /*
             * NewCanon now recognizes D2.
             *
             * That frontier may have been established
             * by another simultaneous release; R4
             * consumes the already-produced NewCanon
             * assimilation plan rather than rebuilding
             * the merge.
             */
            CanonPrecedentRecord
                newCanonPrecedent =
                    new(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        CanonProvenanceKind.SceneRelease,
                        "OTHER_FRONTIER_RELEASE",
                        "OTHER_TRACK",
                        "OTHER_IDEA",
                        6
                    );

            SceneReleaseCanonAssimilationPairEvaluation
                pair =
                    new(
                        release.ReleaseId,
                        release.SourceDemoTapeId,
                        release.SourceOwnerEntityId,
                        release.HostedSceneNodeId,
                        "TRACK",
                        "IDEA",
                        0,
                        6,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        TagDegree.Weak,
                        true,
                        TagDegree.Dominant,
                        new[]
                        {
                            newCanonPrecedent
                        }
                    );

            SceneReleaseCanonAssimilationEvaluation
                assimilation =
                    new(
                        nexus,
                        "TENURE_A",
                        new[]
                        {
                            pair
                        }
                    );

            SceneReleaseCanonAssimilationApplication
                assimilationApplication =
                    new
                            SceneReleaseCanonAssimilationService()
                        .Apply(
                            release,
                            assimilation
                        );

            release.TryGetPairActivationState(
                "TRACK",
                "IDEA",
                0,
                out
                SceneReleasePairActivationState
                    assimilatedState
            );

            Assert.That(
                assimilatedState
                    .CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            return new Fixture(
                release,
                demo,
                environment,
                preAssimilation,
                assimilationApplication
            );
        }

        private static DemoTape Demo()
        {
            DemoTapeIdeaSnapshot idea =
                new(
                    "IDEA",
                    0,
                    "ASPECT",
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
                                TagDegree.Dominant,
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
                    "TRACK",
                    "Track",
                    1f,
                    1f,
                    new[]
                    {
                        idea
                    }
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
                            TagDegree.Dominant,
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

        private static bool ApplyCovered(
            SceneRelease release,
            TagDegree appliedDegree,
            TagDegree recordedDegree)
        {
            ActivationLegitimacyCandidate candidate =
                Candidate(
                    release,
                    appliedDegree,
                    recordedDegree
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT_" + appliedDegree,
                    "KVLT",
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    0,
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

            return new
                    SceneReleaseActivationStateService()
                .ApplyCovered(
                    release,
                    assessment,
                    6
                );
        }

        private static
            ActivationLegitimacyCandidate
            Candidate(
                SceneRelease release,
                TagDegree appliedDegree,
                TagDegree recordedDegree)
        {
            return new ActivationLegitimacyCandidate(
                ActivationAttemptRoute.Performance,
                "HAPPENING",
                "INTENT",
                "ACTOR",
                release.ReleaseId,
                release.SourceDemoTapeId,
                "TRACK",
                "IDEA",
                0,
                "PUBLIC_PERFORMANCE",
                TagAxis.Symbolic,
                TagPole.Negative,
                appliedDegree,
                appliedDegree,
                recordedDegree
            );
        }

        private sealed class Fixture
        {
            public SceneRelease Release { get; }

            public DemoTape Demo { get; }

            public TrackEvaluationEnvironment
                Environment { get; }

            public SceneReleaseLegitimacyEvaluation
                PreAssimilationLegitimacy { get; }

            public
                SceneReleaseCanonAssimilationApplication
                AssimilationApplication { get; }

            public Fixture(
                SceneRelease release,
                DemoTape demo,
                TrackEvaluationEnvironment environment,
                SceneReleaseLegitimacyEvaluation
                    preAssimilationLegitimacy,
                SceneReleaseCanonAssimilationApplication
                    assimilationApplication)
            {
                Release =
                    release;

                Demo =
                    demo;

                Environment =
                    environment;

                PreAssimilationLegitimacy =
                    preAssimilationLegitimacy;

                AssimilationApplication =
                    assimilationApplication;
            }
        }

        [Test]
        public void
            FreezeRecordsFieldToCanonRetainedTransition()
        {
            Fixture fixture =
                BuildAppliedAssimilation();

            service.Apply(
                fixture.Release,
                fixture.Demo,
                fixture.Environment,
                fixture.AssimilationApplication
            );

            SceneReleaseLifecycleTransition transition =
                fixture.Release
                    .LifecycleTransitions[
                        fixture.Release
                            .LifecycleTransitions
                            .Count - 1
                    ];

            Assert.That(
                transition.FromState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                transition.ToState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                transition.GlobalTurn,
                Is.EqualTo(6)
            );
        }
        
        [Test]
        public void
            CanonRetainedReleaseRemainsResidentWithItsFieldPosition()
        {
            Fixture fixture =
                BuildAppliedAssimilation();

            service.Apply(
                fixture.Release,
                fixture.Demo,
                fixture.Environment,
                fixture.AssimilationApplication
            );

            Assert.That(
                fixture.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            /*
             * Retention removes the artifact from ordinary
             * Field competition, not from Scene Space.
             */
            Assert.That(
                fixture.Release.HostedSceneNodeId,
                Is.EqualTo("KVLT")
            );

            Assert.That(
                fixture.Release.HasFieldPosition,
                Is.True
            );

            Assert.That(
                fixture.Release
                    .FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(1.10f)
                    .Within(0.0001f)
            );
        }
        
        [Test]
        public void
            CanonRetainedReleaseCannotReceiveFurtherFieldMovementOrRejection()
        {
            Fixture fixture =
                BuildAppliedAssimilation();

            service.Apply(
                fixture.Release,
                fixture.Demo,
                fixture.Environment,
                fixture.AssimilationApplication
            );

            float frozenPosition =
                fixture.Release
                    .FieldPositionState
                    .CurrentPosition;

            int transitionCount =
                fixture.Release
                    .LifecycleTransitions
                    .Count;

            Assert.That(
                fixture.Release.TryApplyFieldMovement(
                    0.50f,
                    7,
                    out _
                ),
                Is.False
            );

            Assert.That(
                fixture.Release.TryReject(
                    7,
                    outerBoundary: 2f
                ),
                Is.False
            );

            Assert.That(
                fixture.Release
                    .FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(frozenPosition)
                    .Within(0.0001f)
            );

            Assert.That(
                fixture.Release
                    .LifecycleTransitions
                    .Count,
                Is.EqualTo(transitionCount)
            );

            Assert.That(
                fixture.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );
        }
        
        [Test]
        public void
            CanonRetainedReleaseCannotBeReevaluatedForCanonBreakthrough()
        {
            Fixture fixture =
                BuildAppliedAssimilation();

            SceneReleaseCanonFreezeApplication frozen =
                service.Apply(
                    fixture.Release,
                    fixture.Demo,
                    fixture.Environment,
                    fixture.AssimilationApplication
                );

            Assert.Throws<
                System.ArgumentException>(
                () =>
                    new
                            SceneReleaseCanonBreakthroughEvaluator()
                        .Evaluate(
                            fixture.Release,
                            fixture.Demo,
                            frozen.FinalLegitimacy,
                            new CanonState()
                        )
            );
        }
        
        [Test]
        public void
            CanonRetainedReleaseCannotReceiveOrdinaryFieldGravityScore()
        {
            Fixture fixture =
                BuildAppliedAssimilation();

            SceneReleaseCanonFreezeApplication frozen =
                service.Apply(
                    fixture.Release,
                    fixture.Demo,
                    fixture.Environment,
                    fixture.AssimilationApplication
                );

            SEMM91.GamePlay.Score.ScoreLedger ledger =
                new();

            bool awarded =
                new
                        SEMM91.GamePlay.Score
                        .SceneReleaseScoreAwardService()
                    .TryAwardFieldGravity(
                        fixture.Release,
                        frozen.FinalLegitimacy,
                        ledger,
                        globalTurn: 6,
                        out _
                    );

            Assert.That(
                awarded,
                Is.False
            );
        }
    }
}
