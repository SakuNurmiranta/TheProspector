using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions.History;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Score;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Circulation.Tests
{
    public class Camp2FetterTapeAcceptanceTests
    {
        private readonly
            ActivationAttemptDiscoveryService
                attemptDiscovery =
                    new();

        private readonly
            ActivationLegitimacyClassificationService
                legitimacyClassifier =
                    new();

        private readonly
            ActivationLegitimacyCrisisGroupingService
                crisisGrouping =
                    new();

        private readonly
            ActivationCrisisSettlementService
                crisisSettlement =
                    new();

        private readonly
            SceneReleaseActivationStateService
                activationState =
                    new();

        private readonly
            SceneReleaseFetteringService
                fettering =
                    new();

        private readonly
            SceneReleaseLegitimacyEvaluator
                evaluator =
                    new();

        private readonly
            SceneReleaseScoreAwardService
                scoring =
                    new();

        [Test]
        public void
            FringeWithoutGraceAttempt_FailsToFetter()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    releasedTurn: 5
                );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Fringe
                )
            );

            Assert.That(
                fettering.Resolve(
                    release,
                    hasActiveTrve: false,
                    globalTurn: 5
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .GraceOpen
                )
            );

            Assert.That(
                fettering.Resolve(
                    release,
                    hasActiveTrve: false,
                    globalTurn: 6
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .FailedToFetter
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .FailedToFetter
                )
            );

            Assert.That(
                release.ActivationAttempts,
                Is.Empty
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );
        }

        [Test]
        public void
            BlockedPerformanceAttempt_ProtectsGraceWithoutActivating()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    releasedTurn: 5
                );

            SceneReleaseActivationSource source =
                new SceneReleaseActivationSource(
                    release,
                    demo
                );

            Happening happening =
                ResolvingHappening(
                    "HAPPENING_BLOCKED",
                    6
                );

            HappeningPerformIntent perform =
                new HappeningPerformIntent(
                    "PERFORM",
                    happening.HappeningId,
                    "CTX",
                    "PLAYER_A",
                    6,
                    release.ReleaseId
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    perform
                ),
                Is.True
            );

            var attempts =
                attemptDiscovery.Discover(
                    happening,
                    perform.IntentId,
                    new[]
                    {
                        source
                    }
                );

            Assert.That(
                attempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.TryRecordActivationAttempt(
                    attempts[0]
                ),
                Is.True
            );

            HappeningOpposeIntent oppose =
                new HappeningOpposeIntent(
                    "OPPOSE",
                    happening.HappeningId,
                    "CTX",
                    "KEEPER",
                    6,
                    perform.IntentId
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    oppose
                ),
                Is.True
            );

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        perform.IntentId,
                        HappeningIntentOutcome.Blocked,
                        6,
                        oppose.IntentId
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        oppose.IntentId,
                        HappeningIntentOutcome.Succeeded,
                        6
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.TrySettle(6),
                Is.True
            );

            Assert.That(
                fettering.Resolve(
                    release,
                    hasActiveTrve: false,
                    globalTurn: 6
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .GraceProtectedByAttempt
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Fringe
                )
            );

            Assert.That(
                release.ActivationAttempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );

            Assert.That(
                happening.BehaviorOccurrences,
                Is.Empty
            );

            Assert.That(
                happening.HailOccurrences,
                Is.Empty
            );
        }

        [Test]
        public void
            CoveredPerformance_FettersAndScoresResonanceAndGravity()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    releasedTurn: 4
                );

            SceneReleaseActivationSource source =
                new SceneReleaseActivationSource(
                    release,
                    demo
                );

            AcceptedTransgressionState
                accepted =
                    AcceptedStateWithPerformance();

            Happening happening =
                ResolvingHappening(
                    "HAPPENING_GIG",
                    5
                );

            ApplySuccessfulPerformance(
                happening,
                source,
                accepted,
                turn: 5
            );

            Assert.That(
                happening.TrySettle(5),
                Is.True
            );

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    evaluator.Evaluate(
                        release,
                        demo,
                        Environment()
                    );

            Assert.That(
                evaluation.HasTrve,
                Is.True
            );

            Assert.That(
                evaluation.Trve,
                Is.GreaterThan(0f)
            );

            Assert.That(
                evaluation.Resonance,
                Is.GreaterThan(0f)
            );

            Assert.That(
                evaluation.Gravity,
                Is.GreaterThan(0f)
            );

            Assert.That(
                fettering.Resolve(
                    release,
                    evaluation.HasTrve,
                    5
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .Fettered
                )
            );

            ScoreLedger ledger =
                new ScoreLedger();

            Assert.That(
                scoring
                    .TryAwardFirstFetterResonance(
                        release,
                        evaluation,
                        ledger,
                        5,
                        out ScoreEvent resonanceEvent
                    ),
                Is.True
            );

            Assert.That(
                scoring.TryAwardFieldGravity(
                    release,
                    evaluation,
                    ledger,
                    5,
                    out ScoreEvent gravityEvent
                ),
                Is.True
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                release.PairActivationStates.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.PairActivationStates[0]
                    .CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            Assert.That(
                ledger.History.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                resonanceEvent.Amount,
                Is.EqualTo(
                    evaluation.Resonance
                ).Within(0.0001f)
            );

            Assert.That(
                gravityEvent.Amount,
                Is.EqualTo(
                    evaluation.Gravity
                ).Within(0.0001f)
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    "OWNER"
                ),
                Is.EqualTo(
                    evaluation.Resonance +
                    evaluation.Gravity
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            RejectedSevereHail_BecomesPending_ThenLaterAcceptanceRedeemsWithoutRetroactiveScore()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    releasedTurn: 4
                );

            SceneReleaseActivationSource source =
                new SceneReleaseActivationSource(
                    release,
                    demo
                );

            AcceptedTransgressionState
                accepted =
                    AcceptedStateWithPerformance();

            /*
             * ------------------------------------------------
             * TURN 5
             *
             * Low-order public performance is already
             * legitimate in this scenario.
             *
             * It activates the Profane pair to A=1 and
             * fetters the release.
             * ------------------------------------------------
             */

            Happening gig =
                ResolvingHappening(
                    "HAPPENING_GIG",
                    5
                );

            ApplySuccessfulPerformance(
                gig,
                source,
                accepted,
                turn: 5
            );

            Assert.That(
                gig.TrySettle(5),
                Is.True
            );

            SceneReleaseLegitimacyEvaluation
                turnFiveEvaluation =
                    evaluator.Evaluate(
                        release,
                        demo,
                        Environment()
                    );

            Assert.That(
                fettering.Resolve(
                    release,
                    turnFiveEvaluation.HasTrve,
                    5
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .Fettered
                )
            );

            ScoreLedger ledger =
                new ScoreLedger();

            Assert.That(
                scoring
                    .TryAwardFirstFetterResonance(
                        release,
                        turnFiveEvaluation,
                        ledger,
                        5,
                        out _
                    ),
                Is.True
            );

            Assert.That(
                scoring.TryAwardFieldGravity(
                    release,
                    turnFiveEvaluation,
                    ledger,
                    5,
                    out _
                ),
                Is.True
            );

            /*
             * ------------------------------------------------
             * TURN 8
             *
             * PLAYER_A performs degree-3 CHURCH_ARSON
             * and Hails Satan.
             *
             * The recording has a matching intact
             * Profane3 formal pair.
             *
             * No CHURCH_ARSON precedent exists.
             * ------------------------------------------------
             */

            Happening rejectedHappening;

            ActivationLegitimacyCrisisGroup
                rejectedGroup =
                    CreateUncoveredSuccessfulHail(
                        "HAPPENING_REJECTED",
                        8,
                        source,
                        accepted,
                        out rejectedHappening
                    );

            AllegianceCrisis rejectedCrisis =
                ResolveCrisis(
                    rejectedGroup.Question,
                    AllegianceChoice.Society,
                    8
                );

            ActivationCrisisSettlement
                rejectedSettlement =
                    crisisSettlement.Settle(
                        rejectedGroup,
                        rejectedCrisis,
                        accepted
                    );

            Assert.That(
                rejectedSettlement
                    .AcceptedPrecedent,
                Is.Null
            );

            Assert.That(
                rejectedSettlement
                    .PendingCandidates.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                activationState.StorePending(
                    release,
                    rejectedSettlement
                        .PendingCandidates[0],
                    out SceneReleasePendingActivation
                        oldPending
                ),
                Is.True
            );

            Assert.That(
                rejectedHappening.TrySettle(8),
                Is.True
            );

            Assert.That(
                oldPending.IsRedeemed,
                Is.False
            );

            Assert.That(
                release.TryGetPairActivationState(
                    "TRACK",
                    "IDEA",
                    0,
                    out SceneReleasePairActivationState
                        afterRejection
                ),
                Is.True
            );

            /*
             * Existing degree-1 activation survives.
             * Pending degree-3 does not increase A.
             */

            Assert.That(
                afterRejection
                    .CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            SceneReleaseLegitimacyEvaluation
                turnEightEvaluation =
                    evaluator.Evaluate(
                        release,
                        demo,
                        Environment()
                    );

            Assert.That(
                scoring.TryAwardFieldGravity(
                    release,
                    turnEightEvaluation,
                    ledger,
                    8,
                    out ScoreEvent turnEightScore
                ),
                Is.True
            );

            float turnEightGravity =
                turnEightScore.Amount;

            /*
             * ------------------------------------------------
             * TURN 12
             *
             * Same previously-unaccepted praxis occurs
             * again.
             *
             * This time KVLT wins the Allegiance Crisis.
             * ------------------------------------------------
             */

            Happening acceptedHappening;

            ActivationLegitimacyCrisisGroup
                acceptedGroup =
                    CreateUncoveredSuccessfulHail(
                        "HAPPENING_ACCEPTED",
                        12,
                        source,
                        accepted,
                        out acceptedHappening
                    );

            AllegianceCrisis acceptedCrisis =
                ResolveCrisis(
                    acceptedGroup.Question,
                    AllegianceChoice.Kvlt,
                    12
                );

            ActivationCrisisSettlement
                acceptedSettlement =
                    crisisSettlement.Settle(
                        acceptedGroup,
                        acceptedCrisis,
                        accepted
                    );

            Assert.That(
                acceptedSettlement
                    .RaisedAcceptedPrecedent,
                Is.True
            );

            Assert.That(
                acceptedSettlement
                    .AcceptedPrecedent,
                Is.Not.Null
            );

            Assert.That(
                acceptedSettlement
                    .AcceptedPrecedent
                    .AcceptedDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            /*
             * The current accepted occurrence now
             * legitimately activates every candidate
             * it itself answered.
             */

            foreach (
                LegitimizedActivationCandidate
                    legitimate
                in acceptedSettlement
                    .LegitimizedCandidates)
            {
                Assert.That(
                    activationState
                        .ApplyCrisisLegitimized(
                            release,
                            legitimate
                        ),
                    Is.True
                );
            }

            /*
             * The newly-created precedent also redeems
             * the older Society-rejected Pending record.
             */

            Assert.That(
                activationState
                    .RedeemEligiblePending(
                        release,
                        accepted,
                        12
                    ),
                Is.EqualTo(1)
            );

            Assert.That(
                acceptedHappening.TrySettle(12),
                Is.True
            );

            Assert.That(
                oldPending.IsRedeemed,
                Is.True
            );

            Assert.That(
                oldPending.RedeemedTurn,
                Is.EqualTo(12)
            );

            Assert.That(
                release.TryGetPairActivationState(
                    "TRACK",
                    "IDEA",
                    0,
                    out SceneReleasePairActivationState
                        afterAcceptance
                ),
                Is.True
            );

            Assert.That(
                afterAcceptance
                    .CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            SceneReleaseLegitimacyEvaluation
                turnTwelveEvaluation =
                    evaluator.Evaluate(
                        release,
                        demo,
                        Environment()
                    );

            Assert.That(
                turnTwelveEvaluation.Trve,
                Is.GreaterThan(
                    turnEightEvaluation.Trve
                )
            );

            Assert.That(
                turnTwelveEvaluation.Gravity,
                Is.GreaterThan(
                    turnEightEvaluation.Gravity
                )
            );

            Assert.That(
                scoring.TryAwardFieldGravity(
                    release,
                    turnTwelveEvaluation,
                    ledger,
                    12,
                    out ScoreEvent turnTwelveScore
                ),
                Is.True
            );

            /*
             * ------------------------------------------------
             * PROSPECTIVE-ONLY REDEMPTION
             * ------------------------------------------------
             */

            Assert.That(
                turnTwelveScore.Amount,
                Is.GreaterThan(
                    turnEightGravity
                )
            );

            Assert.That(
                turnEightScore.Amount,
                Is.EqualTo(
                    turnEightGravity
                ).Within(0.0001f)
            );

            Assert.That(
                ledger.GetTotalDuringTurns(
                    "OWNER",
                    9,
                    11
                ),
                Is.EqualTo(0f)
            );

            /*
             * turn 5:
             *     Resonance + Gravity
             *
             * turn 8:
             *     lower Gravity
             *
             * turn 12:
             *     higher current Gravity
             */

            Assert.That(
                ledger.History.Count,
                Is.EqualTo(4)
            );

            Assert.That(
                ledger.History[2].GlobalTurn,
                Is.EqualTo(8)
            );

            Assert.That(
                ledger.History[3].GlobalTurn,
                Is.EqualTo(12)
            );
        }

        private void ApplySuccessfulPerformance(
            Happening happening,
            SceneReleaseActivationSource source,
            AcceptedTransgressionState accepted,
            int turn)
        {
            HappeningPerformIntent intent =
                new HappeningPerformIntent(
                    $"PERFORM_{turn}",
                    happening.HappeningId,
                    "CTX",
                    "PLAYER_A",
                    turn,
                    source.Release.ReleaseId
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    intent
                ),
                Is.True
            );

            var attempts =
                attemptDiscovery.Discover(
                    happening,
                    intent.IntentId,
                    new[]
                    {
                        source
                    }
                );

            Assert.That(
                attempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                source.Release
                    .TryRecordActivationAttempt(
                        attempts[0]
                    ),
                Is.True
            );

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        intent.IntentId,
                        HappeningIntentOutcome.Succeeded,
                        turn
                    )
                ),
                Is.True
            );

            var assessments =
                legitimacyClassifier.Classify(
                    happening,
                    attempts[0],
                    source,
                    accepted
                );

            Assert.That(
                assessments.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                assessments[0].Disposition,
                Is.EqualTo(
                    ActivationLegitimacyDisposition
                        .Covered
                )
            );

            Assert.That(
                activationState.ApplyCovered(
                    source.Release,
                    assessments[0],
                    turn
                ),
                Is.True
            );
        }

        private ActivationLegitimacyCrisisGroup
            CreateUncoveredSuccessfulHail(
                string happeningId,
                int turn,
                SceneReleaseActivationSource source,
                AcceptedTransgressionState accepted,
                out Happening happening)
        {
            happening =
                ResolvingHappening(
                    happeningId,
                    turn
                );

            HappeningEnactBehaviorIntent intent =
                new HappeningEnactBehaviorIntent(
                    $"HAIL_INTENT_{turn}",
                    happening.HappeningId,
                    "CTX",
                    "PLAYER_A",
                    turn,
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    "ASPECT_SATAN"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    intent
                ),
                Is.True
            );

            /*
             * Attempt discovery happens from committed
             * activation-capable Intent, before outcome
             * is known.
             */

            var attempts =
                attemptDiscovery.Discover(
                    happening,
                    intent.IntentId,
                    new[]
                    {
                        source
                    }
                );

            Assert.That(
                attempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                source.Release
                    .TryRecordActivationAttempt(
                        attempts[0]
                    ),
                Is.True
            );

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        intent.IntentId,
                        HappeningIntentOutcome.Succeeded,
                        turn
                    )
                ),
                Is.True
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        intent.IntentId,
                        $"BEHAVIOR_{turn}",
                        $"HAIL_{turn}"
                    ),
                Is.True
            );

            var assessments =
                legitimacyClassifier.Classify(
                    happening,
                    attempts[0],
                    source,
                    accepted
                );

            Assert.That(
                assessments.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                assessments[0].Disposition,
                Is.EqualTo(
                    ActivationLegitimacyDisposition
                        .RequiresAllegianceCrisis
                )
            );

            var groups =
                crisisGrouping.Group(
                    assessments,
                    turn
                );

            Assert.That(
                groups.Count,
                Is.EqualTo(1)
            );

            return groups[0];
        }

        private static AllegianceCrisis
            ResolveCrisis(
                AllegianceCrisisQuestion question,
                AllegianceChoice choice,
                int turn)
        {
            AllegianceCrisis crisis =
                new AllegianceCrisis(
                    question,
                    new[]
                    {
                        "KEEPER",
                        "PLAYER_B"
                    },
                    "KEEPER"
                );

            Assert.That(
                crisis.TryCastVote(
                    new AllegianceCrisisVote(
                        "KEEPER",
                        choice,
                        turn
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryCastVote(
                    new AllegianceCrisisVote(
                        "PLAYER_B",
                        choice,
                        turn
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryResolve(
                    turn,
                    out _
                ),
                Is.True
            );

            return crisis;
        }

        private static Happening
            ResolvingHappening(
                string happeningId,
                int turn)
        {
            Happening happening =
                new Happening(
                    happeningId,
                    "COLLECTIVE_KVLT",
                    "KEEPER",
                    "PROMOTION",
                    "KVLT_NIGHT",
                    "NODE_HOLE",
                    turn,
                    new CharacterActionKey(
                        "KEEPER",
                        turn,
                        1
                    )
                );

            Assert.That(
                happening.TryAddContext(
                    new HappeningContext(
                        "CTX",
                        "Public KVLT gathering",
                        HappeningContextAnchorKind
                            .Circumstance,
                        "KVLT_NIGHT",
                        "KEEPER",
                        turn
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.TryAddParticipant(
                    "KEEPER"
                ),
                Is.True
            );

            Assert.That(
                happening.TryAddParticipant(
                    "PLAYER_A"
                ),
                Is.True
            );

            Assert.That(
                happening.TryAddParticipant(
                    "PLAYER_B"
                ),
                Is.True
            );

            Assert.That(
                happening.TryBeginResolving(
                    turn
                ),
                Is.True
            );

            return happening;
        }

        private static
            AcceptedTransgressionState
            AcceptedStateWithPerformance()
        {
            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            Assert.That(
                state.TryAccept(
                    new AcceptedTransgressionRecord(
                        "AT_PUBLIC_PERFORMANCE",
                        "KVLT",
                        "PUBLIC_PERFORMANCE",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak,
                        0,
                        AcceptedTransgressionSourceKind
                            .ScenarioSeed,
                        "TEST_SCENARIO"
                    )
                ),
                Is.True
            );

            return state;
        }

        private static SceneRelease Release(
            DemoTape demo,
            int releasedTurn)
        {
            return new SceneRelease(
                "Test Release",
                demo.DemoTapeId,
                "OWNER",
                "KVLT_SCENE",
                releasedTurn,
                1f
            );
        }

        private static DemoTape Demo()
        {
            DemoTapeIdeaSnapshot pair =
                new DemoTapeIdeaSnapshot(
                    "IDEA",
                    0,
                    "ASPECT_GUITAR",
                    IdeaPayloadType.TagPair,
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

            DemoTapeTrackSnapshot track =
                new DemoTapeTrackSnapshot(
                    "TRACK",
                    "Track",
                    1f,
                    1f,
                    new[]
                    {
                        pair
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

        private static TrackEvaluationEnvironment
            Environment()
        {
            NormativeCentre centre =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            1f
                        )
                    }
                );

            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            /*
             * Recorded submissive is Symbolic/Positive.
             * Society backs it strongly enough for the
             * formal pair to be TRVE-capable.
             */
            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            return new TrackEvaluationEnvironment(
                12,
                centre,
                centre,
                society
            );
        }
    }
}