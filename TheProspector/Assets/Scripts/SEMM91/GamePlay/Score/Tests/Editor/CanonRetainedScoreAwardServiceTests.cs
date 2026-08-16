using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Movement;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Score.Tests.Editor
{
    public class
        CanonRetainedScoreAwardServiceTests
    {
        private readonly
            CanonRetainedScoreAwardService service =
                new();

        [Test]
        public void
            MatchingTenureAwardsFrozenGravityOnCanonizationTurn()
        {
            Fixture fixture =
                BuildFixture(
                    attachDecomposition: true
                );

            ScoreLedger ledger =
                new();

            Assert.That(
                service.TryAward(
                    fixture.Release,
                    "TENURE_A",
                    ledger,
                    6,
                    out ScoreEvent scoreEvent
                ),
                Is.True
            );

            Assert.That(
                scoreEvent.Kind,
                Is.EqualTo(
                    ScoreEventKind
                        .CanonRetainedGravity
                )
            );

            Assert.That(
                scoreEvent.BeneficiaryEntityId,
                Is.EqualTo("OWNER")
            );

            Assert.That(
                scoreEvent.Amount,
                Is.EqualTo(
                    fixture.Release
                        .FrozenPostAssimilationGravity
                ).Within(0.0001f)
            );

            Assert.That(
                scoreEvent.GlobalTurn,
                Is.EqualTo(6)
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    "OWNER"
                ),
                Is.EqualTo(
                    fixture.Release
                        .FrozenPostAssimilationGravity
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            SameTenurePaysFullFrozenGravityAgainOnLaterTurn()
        {
            Fixture fixture =
                BuildFixture(
                    attachDecomposition: true
                );

            ScoreLedger ledger =
                new();

            Assert.That(
                service.TryAward(
                    fixture.Release,
                    "TENURE_A",
                    ledger,
                    6,
                    out _
                ),
                Is.True
            );

            Assert.That(
                service.TryAward(
                    fixture.Release,
                    "TENURE_A",
                    ledger,
                    7,
                    out _
                ),
                Is.True
            );

            float expected =
                fixture.Release
                    .FrozenPostAssimilationGravity *
                2f;

            Assert.That(
                ledger.GetLifetimeTotal(
                    "OWNER"
                ),
                Is.EqualTo(expected)
                    .Within(0.0001f)
            );

            Assert.That(
                ledger.GetTotalDuringTurns(
                    "OWNER",
                    6,
                    7
                ),
                Is.EqualTo(expected)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            SameReleaseCannotReceiveTenureGravityTwiceInOneTurn()
        {
            Fixture fixture =
                BuildFixture(
                    attachDecomposition: true
                );

            ScoreLedger ledger =
                new();

            Assert.That(
                service.TryAward(
                    fixture.Release,
                    "TENURE_A",
                    ledger,
                    6,
                    out _
                ),
                Is.True
            );

            Assert.That(
                service.TryAward(
                    fixture.Release,
                    "TENURE_A",
                    ledger,
                    6,
                    out _
                ),
                Is.False
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            DifferentKeeperTenureDoesNotPayRetainedRelease()
        {
            Fixture fixture =
                BuildFixture(
                    attachDecomposition: true
                );

            ScoreLedger ledger =
                new();

            /*
             * TENURE_A owns the release's retained
             * institutional residency.
             *
             * TENURE_B may even belong to the same
             * player returning later; tenure identity,
             * not player identity, controls payout.
             */
            Assert.That(
                service.TryAward(
                    fixture.Release,
                    "TENURE_B",
                    ledger,
                    7,
                    out _
                ),
                Is.False
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void
            ExistingFieldGravityOnSameTurnBlocksDoubleGravityPayment()
        {
            Fixture fixture =
                BuildFixture(
                    attachDecomposition: true
                );

            ScoreLedger ledger =
                new();

            ScoreEvent erroneousPriorFieldGravity =
                new(
                    "TEST|OLD_FIELD_GRAVITY",
                    fixture.Release
                        .SourceOwnerEntityId,
                    fixture.Release
                        .SourceOwnerEntityId,
                    fixture.Release
                        .HostedSceneNodeId,
                    fixture.Release.ReleaseId,
                    fixture.Release
                        .SourceDemoTapeId,
                    ScoreEventKind.FieldGravity,
                    fixture.Release
                        .FrozenPostAssimilationGravity,
                    6
                );

            Assert.That(
                ledger.TryRecord(
                    erroneousPriorFieldGravity
                ),
                Is.True
            );

            Assert.That(
                service.TryAward(
                    fixture.Release,
                    "TENURE_A",
                    ledger,
                    6,
                    out _
                ),
                Is.False
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            TenurePayoutRequiresCompletedClaimGravityDecomposition()
        {
            Fixture fixture =
                BuildFixture(
                    attachDecomposition: false
                );

            Assert.That(
                fixture.Release.IsCanonized,
                Is.True
            );

            Assert.That(
                fixture.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                fixture.Release
                    .HasCanonGravityDecomposition,
                Is.False
            );

            ScoreLedger ledger =
                new();

            Assert.That(
                service.TryAward(
                    fixture.Release,
                    "TENURE_A",
                    ledger,
                    6,
                    out _
                ),
                Is.False
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(0)
            );
        }

        private static Fixture BuildFixture(
            bool attachDecomposition)
        {
            DemoTape demo =
                Demo();

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

            ApplyCovered(
                release
            );

            TrackEvaluationEnvironment environment =
                Environment();

            SceneReleaseLegitimacyEvaluation
                preMovement =
                    new
                        SceneReleaseLegitimacyEvaluator()
                        .Evaluate(
                            release,
                            demo,
                            environment
                        );

            CanonState oldCanon =
                PreviousCanon();

            SceneReleaseCanonBreakthroughEvaluation
                breakthrough =
                    new
                        SceneReleaseCanonBreakthroughEvaluator()
                        .Evaluate(
                            release,
                            demo,
                            preMovement,
                            oldCanon
                        );

            Assert.That(
                breakthrough
                    .HasQualifyingBreakthrough,
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

            SceneReleaseNexusBoundaryEvaluation nexus =
                new
                    SceneReleaseNexusBoundaryEvaluator()
                    .Evaluate(
                        release,
                        breakthrough,
                        1f
                    );

            Assert.That(
                nexus.IsCanonCandidate,
                Is.True
            );

            CanonSimultaneousMergeEvaluation merge =
                new
                    CanonSimultaneousMergeEvaluator()
                    .Evaluate(
                        oldCanon,
                        new[]
                        {
                            new
                                SceneReleaseCanonMergeCandidate(
                                    release,
                                    nexus
                                )
                        },
                        "TENURE_A"
                    );

            SceneReleaseCanonAssimilationEvaluation
                assimilation =
                    new
                        SceneReleaseCanonAssimilationEvaluator()
                        .Evaluate(
                            release,
                            demo,
                            nexus,
                            merge
                        );

            SceneReleaseCanonAssimilationApplication
                assimilationApplication =
                    new
                        SceneReleaseCanonAssimilationService()
                        .Apply(
                            release,
                            assimilation
                        );

            SceneReleaseCanonFreezeApplication freeze =
                new SceneReleaseCanonFreezeService()
                    .Apply(
                        release,
                        demo,
                        environment,
                        assimilationApplication
                    );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            if (attachDecomposition)
            {
                new
                    SceneReleaseCanonGravityDecompositionService()
                    .Apply(
                        release,
                        freeze,
                        merge
                    );
            }

            return new Fixture(
                release
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

        private static CanonState PreviousCanon()
        {
            CanonState canon =
                new();

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind.ScenarioSeed,
                    "OLD_CANON"
                ),
                Is.True
            );

            return canon;
        }

        private static void ApplyCovered(
            SceneRelease release)
        {
            ActivationLegitimacyCandidate candidate =
                new(
                    ActivationAttemptRoute.Performance,
                    "HAPPENING",
                    "INTENT",
                    "PLAYER_ACTIVATOR",
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    "TRACK",
                    "IDEA",
                    0,
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    TagDegree.Dominant,
                    TagDegree.Dominant
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT",
                    "KVLT",
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
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

        private sealed class Fixture
        {
            public SceneRelease Release { get; }

            public Fixture(
                SceneRelease release)
            {
                Release =
                    release;
            }
        }
    }
}