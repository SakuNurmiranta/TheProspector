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
        HistoricalCanonInstitutionalGravityScoreAwardServiceTests
    {
        private readonly
            HistoricalCanonInstitutionalGravityScoreAwardService
            service =
                new();

        [Test]
        public void
            HistoricalFrontierPaysActivatorRatherThanReleaseOwner()
        {
            Fixture fixture =
                BuildFixture(
                    transitionToHistorical: true
                );

            ScoreLedger ledger =
                new();

            var awards =
                service.AwardEligibleClaims(
                    fixture.Release,
                    fixture.CurrentCanon,
                    ledger,
                    8
                );

            Assert.That(
                awards.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                awards[0]
                    .CanonicalActivatorPlayerId,
                Is.EqualTo(
                    "PLAYER_FRONTIER"
                )
            );

            Assert.That(
                awards[0]
                    .ScoreEvent
                    .BeneficiaryEntityId,
                Is.EqualTo(
                    "PLAYER_FRONTIER"
                )
            );

            Assert.That(
                awards[0]
                    .ScoreEvent
                    .SourceOwnerEntityId,
                Is.EqualTo("OWNER")
            );

            Assert.That(
                awards[0]
                    .ScoreEvent
                    .BeneficiaryEntityId,
                Is.Not.EqualTo(
                    awards[0]
                        .ScoreEvent
                        .SourceOwnerEntityId
                )
            );
        }

        [Test]
        public void
            PostTenurePaysOnlyFrontierShareNotFullReleaseGravity()
        {
            Fixture fixture =
                BuildFixture(
                    transitionToHistorical: true
                );

            ScoreLedger ledger =
                new();

            var awards =
                service.AwardEligibleClaims(
                    fixture.Release,
                    fixture.CurrentCanon,
                    ledger,
                    8
                );

            Assert.That(
                awards.Count,
                Is.EqualTo(1)
            );

            float frontierTotal =
                fixture.Release
                    .CanonGravityDecompositionState
                    .FrontierContributionTotal;

            Assert.That(
                awards[0].Amount,
                Is.EqualTo(
                    frontierTotal
                ).Within(0.0001f)
            );

            Assert.That(
                awards[0].Amount,
                Is.LessThan(
                    fixture.Release
                        .FrozenPostAssimilationGravity
                )
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    "PLAYER_FRONTIER"
                ),
                Is.EqualTo(
                    frontierTotal
                ).Within(0.0001f)
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    "OWNER"
                ),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void
            EqualCurrentCanonDegreeKeepsFrontierPayoutEligible()
        {
            Fixture fixture =
                BuildFixture(
                    transitionToHistorical: true
                );

            CanonState equalLaterCanon =
                fixture.CurrentCanon.CreateCopy();

            /*
             * Coincident/equal later Canon does not
             * supersede the old frontier.
             */
            Assert.That(
                equalLaterCanon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    CanonProvenanceKind.SceneRelease,
                    "EQUAL_RELEASE",
                    "EQUAL_TRACK",
                    "EQUAL_IDEA",
                    7
                ),
                Is.True
            );

            var awards =
                service.AwardEligibleClaims(
                    fixture.Release,
                    equalLaterCanon,
                    new ScoreLedger(),
                    8
                );

            Assert.That(
                awards.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            StrictlyHigherCurrentCanonStopsOldFrontierPayout()
        {
            Fixture fixture =
                BuildFixture(
                    transitionToHistorical: true
                );

            CanonState higherCanon =
                fixture.CurrentCanon.CreateCopy();

            Assert.That(
                higherCanon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    CanonProvenanceKind.SceneRelease,
                    "HIGHER_RELEASE",
                    "HIGHER_TRACK",
                    "HIGHER_IDEA",
                    8
                ),
                Is.True
            );

            var awards =
                service.AwardEligibleClaims(
                    fixture.Release,
                    higherCanon,
                    new ScoreLedger(),
                    9
                );

            Assert.That(
                awards,
                Is.Empty
            );
        }

        [Test]
        public void
            SameFrontierClaimCannotPayTwiceInSameTurn()
        {
            Fixture fixture =
                BuildFixture(
                    transitionToHistorical: true
                );

            ScoreLedger ledger =
                new();

            Assert.That(
                service.AwardEligibleClaims(
                    fixture.Release,
                    fixture.CurrentCanon,
                    ledger,
                    8
                ).Count,
                Is.EqualTo(1)
            );

            Assert.That(
                service.AwardEligibleClaims(
                    fixture.Release,
                    fixture.CurrentCanon,
                    ledger,
                    8
                ),
                Is.Empty
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            CanonRetainedReleaseDoesNotReceiveHistoricalFrontierPayout()
        {
            Fixture fixture =
                BuildFixture(
                    transitionToHistorical: false
                );

            Assert.That(
                fixture.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            var awards =
                service.AwardEligibleClaims(
                    fixture.Release,
                    fixture.CurrentCanon,
                    new ScoreLedger(),
                    7
                );

            Assert.That(
                awards,
                Is.Empty
            );
        }

        [Test]
        public void
            ExistingFullReleaseTenureAwardOnSameTurnBlocksClaimPayout()
        {
            Fixture fixture =
                BuildFixture(
                    transitionToHistorical: true
                );

            ScoreLedger ledger =
                new();

            /*
             * Model settlement in which the final
             * TENURE_A full-release payout was already
             * valid earlier in turn 7 before the
             * tenure transition was processed.
             */
            ScoreEvent retainedAward =
                new(
                    "TEST|RETAINED|TURN7",
                    fixture.Release
                        .SourceOwnerEntityId,
                    fixture.Release
                        .SourceOwnerEntityId,
                    fixture.Release
                        .HostedSceneNodeId,
                    fixture.Release.ReleaseId,
                    fixture.Release
                        .SourceDemoTapeId,
                    ScoreEventKind
                        .CanonRetainedGravity,
                    fixture.Release
                        .FrozenPostAssimilationGravity,
                    7
                );

            Assert.That(
                ledger.TryRecord(
                    retainedAward
                ),
                Is.True
            );

            Assert.That(
                service.AwardEligibleClaims(
                    fixture.Release,
                    fixture.CurrentCanon,
                    ledger,
                    7
                ),
                Is.Empty
            );

            /*
             * Next turn the Historical phase is free
             * to begin.
             */
            Assert.That(
                service.AwardEligibleClaims(
                    fixture.Release,
                    fixture.CurrentCanon,
                    ledger,
                    8
                ).Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            MissingCurrentCanonDoesNotPayHistoricalClaim()
        {
            Fixture fixture =
                BuildFixture(
                    transitionToHistorical: true
                );

            var awards =
                service.AwardEligibleClaims(
                    fixture.Release,
                    new CanonState(),
                    new ScoreLedger(),
                    8
                );

            Assert.That(
                awards,
                Is.Empty
            );
        }

        private static Fixture BuildFixture(
            bool transitionToHistorical)
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

            /*
             * Genuine frontier:
             * Symbolic2 over old Symbolic1.
             */
            ApplyCovered(
                release,
                "TRACK_FRONTIER",
                "IDEA_FRONTIER",
                "PLAYER_FRONTIER",
                TagAxis.Symbolic,
                TagDegree.Dominant,
                TagDegree.Dominant
            );

            /*
             * Already-canonical active material:
             * Physical1 over old Physical1.
             *
             * Contributes to full G_release but must
             * never become a historical frontier
             * entitlement.
             */
            ApplyCovered(
                release,
                "TRACK_CANONICAL",
                "IDEA_CANONICAL",
                "PLAYER_OTHER",
                TagAxis.Physical,
                TagDegree.Weak,
                TagDegree.Weak
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

            CanonState previousCanon =
                PreviousCanon();

            SceneReleaseCanonBreakthroughEvaluation
                breakthrough =
                    new
                            SceneReleaseCanonBreakthroughEvaluator()
                        .Evaluate(
                            release,
                            demo,
                            preMovement,
                            previousCanon
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

            CanonSimultaneousMergeEvaluation merge =
                new
                        CanonSimultaneousMergeEvaluator()
                    .Evaluate(
                        previousCanon,
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
                appliedAssimilation =
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
                        appliedAssimilation
                    );

            new
                    SceneReleaseCanonGravityDecompositionService()
                .Apply(
                    release,
                    freeze,
                    merge
                );

            Assert.That(
                release
                    .CanonGravityDecompositionState
                    .ActivePairContributions.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                release
                    .CanonGravityDecompositionState
                    .FrontierContributions.Count,
                Is.EqualTo(1)
            );

            if (transitionToHistorical)
            {
                Assert.That(
                    new
                            SceneReleaseCanonTenureTransitionService()
                        .Apply(
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
            }

            return new Fixture(
                release,
                merge.NewCanon
            );
        }

        private static DemoTape Demo()
        {
            DemoTapeTrackSnapshot frontierTrack =
                new(
                    "TRACK_FRONTIER",
                    "Frontier",
                    1f,
                    1f,
                    new[]
                    {
                        Pair(
                            "IDEA_FRONTIER",
                            TagAxis.Symbolic,
                            TagDegree.Dominant
                        )
                    }
                );

            DemoTapeTrackSnapshot canonicalTrack =
                new(
                    "TRACK_CANONICAL",
                    "Canonical",
                    1f,
                    1f,
                    new[]
                    {
                        Pair(
                            "IDEA_CANONICAL",
                            TagAxis.Physical,
                            TagDegree.Weak
                        )
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
                    frontierTrack,
                    canonicalTrack
                }
            );
        }

        private static DemoTapeIdeaSnapshot Pair(
            string ideaId,
            TagAxis axis,
            TagDegree dominantDegree)
        {
            return new DemoTapeIdeaSnapshot(
                ideaId,
                0,
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
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Physical,
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

            society.SetNormativeDegree(
                TagAxis.Physical,
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
                    "SYMBOLIC_SEED"
                ),
                Is.True
            );

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Physical,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind.ScenarioSeed,
                    "PHYSICAL_SEED"
                ),
                Is.True
            );

            return canon;
        }

        private static void ApplyCovered(
            SceneRelease release,
            string trackId,
            string ideaId,
            string actorId,
            TagAxis axis,
            TagDegree appliedDegree,
            TagDegree recordedDegree)
        {
            string behavior =
                "PRAXIS_" + ideaId;

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
                    0,
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

        private sealed class Fixture
        {
            public SceneRelease Release { get; }

            public CanonState CurrentCanon { get; }

            public Fixture(
                SceneRelease release,
                CanonState currentCanon)
            {
                Release =
                    release;

                CurrentCanon =
                    currentCanon;
            }
        }

        [Test]
        public void
            HigherCanonEstablishedThisTurnDoesNotClawBackTurnStartPayout()
        {
            Fixture fixture =
                BuildFixture(
                    transitionToHistorical: true
                );

            /*
             * Scene_t:
             *
             * the historical Symbolic2 frontier is still
             * current.
             */
            CanonState turnStartCanon =
                fixture.CurrentCanon;

            /*
             * Settlement t establishes Symbolic3.
             */
            CanonState postSettlementCanon =
                turnStartCanon.CreateCopy();

            Assert.That(
                postSettlementCanon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    CanonProvenanceKind.SceneRelease,
                    "NEW_FRONTIER_RELEASE",
                    "NEW_FRONTIER_TRACK",
                    "NEW_FRONTIER_IDEA",
                    8
                ),
                Is.True
            );

            ScoreLedger ledger =
                new();

            /*
             * Institutional payout is evaluated against the
             * frozen Scene_t Canon.
             *
             * Therefore the old Symbolic2 frontier still
             * receives turn 8.
             */
            var turnEightAwards =
                service.AwardEligibleClaims(
                    fixture.Release,
                    turnStartCanon,
                    ledger,
                    8
                );

            Assert.That(
                turnEightAwards.Count,
                Is.EqualTo(1)
            );

            CanonFrontierSupersessionEvaluation
                supersession =
                    new
                            CanonFrontierSupersessionEvaluator()
                        .Evaluate(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant,
                            turnStartCanon,
                            postSettlementCanon,
                            8
                        );

            Assert.That(
                supersession.Disposition,
                Is.EqualTo(
                    CanonFrontierSupersessionDisposition
                        .SupersededAfterSettlement
                )
            );

            Assert.That(
                supersession
                    .WasEligibleForTurnStartPayout,
                Is.True
            );

            /*
             * Scene_t+1 now contains Symbolic3.
             *
             * The old Symbolic2 frontier stops paying.
             */
            var turnNineAwards =
                service.AwardEligibleClaims(
                    fixture.Release,
                    postSettlementCanon,
                    ledger,
                    9
                );

            Assert.That(
                turnNineAwards,
                Is.Empty
            );

            /*
             * No rollback:
             *
             * the already-recorded turn-8 event remains.
             */
            Assert.That(
                ledger.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    "PLAYER_FRONTIER"
                ),
                Is.EqualTo(
                    turnEightAwards[0].Amount
                ).Within(0.0001f)
            );
        }
    }
}