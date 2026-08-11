using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Score.Tests
{
    public class
        SceneReleaseScoreAwardServiceTests
    {
        private readonly
            SceneReleaseScoreAwardService
                scoring =
                    new();

        private readonly
            SceneReleaseActivationStateService
                activation =
                    new();

        private readonly
            SceneReleaseLegitimacyEvaluator
                evaluator =
                    new();

        [Test]
        public void
            FirstFetter_AwardsReleaseResonanceToOwner()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    "KVLT",
                    4
                );

            ApplyPerformanceActivation(
                release,
                TagDegree.Weak,
                5
            );

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    Evaluate(
                        release,
                        demo
                    );

            Assert.That(
                evaluation.HasTrve,
                Is.True
            );

            Assert.That(
                release.TryFetter(5),
                Is.True
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
                        out ScoreEvent scoreEvent
                    ),
                Is.True
            );

            Assert.That(
                scoreEvent.Kind,
                Is.EqualTo(
                    ScoreEventKind
                        .FirstFetterResonance
                )
            );

            Assert.That(
                scoreEvent.BeneficiaryEntityId,
                Is.EqualTo("OWNER")
            );

            Assert.That(
                scoreEvent.SourceOwnerEntityId,
                Is.EqualTo("OWNER")
            );

            Assert.That(
                scoreEvent.SceneReleaseId,
                Is.EqualTo(
                    release.ReleaseId
                )
            );

            Assert.That(
                scoreEvent.DemoTapeId,
                Is.EqualTo(
                    demo.DemoTapeId
                )
            );

            Assert.That(
                scoreEvent.Amount,
                Is.EqualTo(
                    evaluation.Resonance
                ).Within(0.0001f)
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            FirstFetterAwardRequiresActualFetterTransitionOnThatTurn()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    "KVLT",
                    4
                );

            ApplyPerformanceActivation(
                release,
                TagDegree.Weak,
                5
            );

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    Evaluate(
                        release,
                        demo
                    );

            Assert.That(
                release.TryFetter(5),
                Is.True
            );

            ScoreLedger ledger =
                new ScoreLedger();

            /*
             * Trying to manufacture the award a turn
             * later is rejected.
             */
            Assert.That(
                scoring
                    .TryAwardFirstFetterResonance(
                        release,
                        evaluation,
                        ledger,
                        6,
                        out _
                    ),
                Is.False
            );

            Assert.That(
                ledger.History,
                Is.Empty
            );

            Assert.That(
                scoring
                    .TryAwardFirstFetterResonance(
                        release,
                        evaluation,
                        ledger,
                        5,
                        out _
                    ),
                Is.True
            );
        }

        [Test]
        public void
            FailedToFetter_EarnsNoResonanceAward()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    "KVLT",
                    4
                );

            Assert.That(
                release.TryFailToFetter(5),
                Is.True
            );

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    Evaluate(
                        release,
                        demo
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
                        out _
                    ),
                Is.False
            );

            Assert.That(
                ledger.History,
                Is.Empty
            );
        }

        [Test]
        public void
            SameDemoSameScene_CannotFarmFirstFetterResonance()
        {
            DemoTape demo =
                Demo();

            SceneRelease first =
                Release(
                    demo,
                    "KVLT",
                    4
                );

            ApplyPerformanceActivation(
                first,
                TagDegree.Weak,
                5
            );

            SceneReleaseLegitimacyEvaluation
                firstEvaluation =
                    Evaluate(
                        first,
                        demo
                    );

            Assert.That(
                first.TryFetter(5),
                Is.True
            );

            ScoreLedger ledger =
                new ScoreLedger();

            Assert.That(
                scoring
                    .TryAwardFirstFetterResonance(
                        first,
                        firstEvaluation,
                        ledger,
                        5,
                        out _
                    ),
                Is.True
            );

            /*
             * Same master released again later.
             * Public activation is separate, so this
             * release must activate independently.
             */
            SceneRelease second =
                Release(
                    demo,
                    "KVLT",
                    10
                );

            ApplyPerformanceActivation(
                second,
                TagDegree.Weak,
                11
            );

            SceneReleaseLegitimacyEvaluation
                secondEvaluation =
                    Evaluate(
                        second,
                        demo
                    );

            Assert.That(
                second.TryFetter(11),
                Is.True
            );

            Assert.That(
                scoring
                    .TryAwardFirstFetterResonance(
                        second,
                        secondEvaluation,
                        ledger,
                        11,
                        out _
                    ),
                Is.False
            );

            Assert.That(
                ledger.History.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                ledger.History[0].SceneReleaseId,
                Is.EqualTo(
                    first.ReleaseId
                )
            );
        }

        [Test]
        public void
            SameDemoDifferentScene_CanEarnFirstFetterResonanceAgain()
        {
            DemoTape demo =
                Demo();

            SceneRelease first =
                Release(
                    demo,
                    "KVLT_A",
                    4
                );

            ApplyPerformanceActivation(
                first,
                TagDegree.Weak,
                5
            );

            Assert.That(
                first.TryFetter(5),
                Is.True
            );

            SceneRelease second =
                Release(
                    demo,
                    "KVLT_B",
                    10
                );

            ApplyPerformanceActivation(
                second,
                TagDegree.Weak,
                11
            );

            Assert.That(
                second.TryFetter(11),
                Is.True
            );

            ScoreLedger ledger =
                new ScoreLedger();

            Assert.That(
                scoring
                    .TryAwardFirstFetterResonance(
                        first,
                        Evaluate(first, demo),
                        ledger,
                        5,
                        out _
                    ),
                Is.True
            );

            Assert.That(
                scoring
                    .TryAwardFirstFetterResonance(
                        second,
                        Evaluate(second, demo),
                        ledger,
                        11,
                        out _
                    ),
                Is.True
            );

            Assert.That(
                ledger.History.Count,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void
            FieldTurn_AwardsCurrentReleaseGravity()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    "KVLT",
                    4
                );

            ApplyPerformanceActivation(
                release,
                TagDegree.Weak,
                5
            );

            Assert.That(
                release.TryFetter(5),
                Is.True
            );

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    Evaluate(
                        release,
                        demo
                    );

            Assert.That(
                evaluation.Gravity,
                Is.GreaterThan(0f)
            );

            ScoreLedger ledger =
                new ScoreLedger();

            Assert.That(
                scoring.TryAwardFieldGravity(
                    release,
                    evaluation,
                    ledger,
                    6,
                    out ScoreEvent gravityEvent
                ),
                Is.True
            );

            Assert.That(
                gravityEvent.Kind,
                Is.EqualTo(
                    ScoreEventKind.FieldGravity
                )
            );

            Assert.That(
                gravityEvent.Amount,
                Is.EqualTo(
                    evaluation.Gravity
                ).Within(0.0001f)
            );

            Assert.That(
                gravityEvent.GlobalTurn,
                Is.EqualTo(6)
            );
        }

        [Test]
        public void
            FetterTurn_CanAwardResonanceAndFieldGravityAsDistinctEvents()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    "KVLT",
                    4
                );

            ApplyPerformanceActivation(
                release,
                TagDegree.Weak,
                5
            );

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    Evaluate(
                        release,
                        demo
                    );

            Assert.That(
                release.TryFetter(5),
                Is.True
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
                        out _
                    ),
                Is.True
            );

            Assert.That(
                scoring.TryAwardFieldGravity(
                    release,
                    evaluation,
                    ledger,
                    5,
                    out _
                ),
                Is.True
            );

            Assert.That(
                ledger.History.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                ledger.History[0].Kind,
                Is.EqualTo(
                    ScoreEventKind
                        .FirstFetterResonance
                )
            );

            Assert.That(
                ledger.History[1].Kind,
                Is.EqualTo(
                    ScoreEventKind.FieldGravity
                )
            );
        }

        [Test]
        public void
            SameReleaseSameTurn_CannotDuplicateFieldGravity()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    "KVLT",
                    4
                );

            ApplyPerformanceActivation(
                release,
                TagDegree.Weak,
                5
            );

            Assert.That(
                release.TryFetter(5),
                Is.True
            );

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    Evaluate(
                        release,
                        demo
                    );

            ScoreLedger ledger =
                new ScoreLedger();

            Assert.That(
                scoring.TryAwardFieldGravity(
                    release,
                    evaluation,
                    ledger,
                    6,
                    out _
                ),
                Is.True
            );

            Assert.That(
                scoring.TryAwardFieldGravity(
                    release,
                    evaluation,
                    ledger,
                    6,
                    out _
                ),
                Is.False
            );

            Assert.That(
                ledger.History.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            FringeAndFailedRelease_EarnNoGravity()
        {
            DemoTape demo =
                Demo();

            SceneRelease fringe =
                Release(
                    demo,
                    "KVLT",
                    4
                );

            SceneReleaseLegitimacyEvaluation
                fringeEvaluation =
                    Evaluate(
                        fringe,
                        demo
                    );

            ScoreLedger ledger =
                new ScoreLedger();

            Assert.That(
                scoring.TryAwardFieldGravity(
                    fringe,
                    fringeEvaluation,
                    ledger,
                    5,
                    out _
                ),
                Is.False
            );

            SceneRelease failed =
                Release(
                    demo,
                    "KVLT",
                    4
                );

            Assert.That(
                failed.TryFailToFetter(5),
                Is.True
            );

            Assert.That(
                scoring.TryAwardFieldGravity(
                    failed,
                    Evaluate(failed, demo),
                    ledger,
                    5,
                    out _
                ),
                Is.False
            );

            Assert.That(
                ledger.History,
                Is.Empty
            );
        }

        [Test]
        public void
            PendingRedemption_DoesNotBackfillEarlierGravityScore()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    "KVLT",
                    4
                );

            /*
             * Low-order public performance activates
             * the pair to A=1 and fetters the tape.
             */
            ApplyPerformanceActivation(
                release,
                TagDegree.Weak,
                5
            );

            Assert.That(
                release.TryFetter(5),
                Is.True
            );

            /*
             * Later degree-3 CHURCH_ARSON would raise
             * this recorded D=2 pair to A=2, but
             * Society victory leaves it Pending.
             */
            ActivationLegitimacyCandidate
                pendingCandidate =
                    HailCandidate(
                        release,
                        TagDegree.Dominant
                    );

            Assert.That(
                activation.StorePending(
                    release,
                    new PendingActivationCandidate(
                        pendingCandidate,
                        "CRISIS",
                        8
                    ),
                    out _
                ),
                Is.True
            );

            SceneReleaseLegitimacyEvaluation
                turnEightEvaluation =
                    Evaluate(
                        release,
                        demo
                    );

            ScoreLedger ledger =
                new ScoreLedger();

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

            float originalGravity =
                turnEightScore.Amount;

            /*
             * Later KVLT acceptance redeems Pending.
             */
            AcceptedTransgressionState
                acceptedState =
                    new AcceptedTransgressionState(
                        "KVLT"
                    );

            AcceptedTransgressionRecord
                laterPrecedent =
                    new AcceptedTransgressionRecord(
                        "AT_LATER",
                        "KVLT",
                        "CHURCH_ARSON",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive,
                        12,
                        AcceptedTransgressionSourceKind
                            .AllegianceCrisis,
                        "LATER_CRISIS"
                    );

            Assert.That(
                acceptedState.TryAccept(
                    laterPrecedent
                ),
                Is.True
            );

            Assert.That(
                activation.RedeemEligiblePending(
                    release,
                    acceptedState,
                    12
                ),
                Is.EqualTo(1)
            );

            SceneReleaseLegitimacyEvaluation
                turnTwelveEvaluation =
                    Evaluate(
                        release,
                        demo
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

            Assert.That(
                turnTwelveScore.Amount,
                Is.GreaterThan(
                    originalGravity
                )
            );

            /*
             * Redemption has not inserted or modified
             * Score for turns 9-11.
             *
             * The old turn-8 event also remains the
             * original value.
             */
            Assert.That(
                ledger.History.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                ledger.History[0],
                Is.SameAs(turnEightScore)
            );

            Assert.That(
                ledger.History[0].GlobalTurn,
                Is.EqualTo(8)
            );

            Assert.That(
                ledger.History[0].Amount,
                Is.EqualTo(
                    originalGravity
                ).Within(0.0001f)
            );

            Assert.That(
                ledger.History[1].GlobalTurn,
                Is.EqualTo(12)
            );

            Assert.That(
                ledger.GetTotalDuringTurns(
                    "OWNER",
                    9,
                    11
                ),
                Is.EqualTo(0f)
            );
        }

        private SceneReleaseLegitimacyEvaluation
            Evaluate(
                SceneRelease release,
                DemoTape demo)
        {
            return evaluator.Evaluate(
                release,
                demo,
                Environment()
            );
        }

        private void ApplyPerformanceActivation(
            SceneRelease release,
            TagDegree appliedDegree,
            int turn)
        {
            ActivationLegitimacyCandidate candidate =
                PerformanceCandidate(
                    release,
                    appliedDegree
                );

            AcceptedTransgressionRecord precedent =
                new AcceptedTransgressionRecord(
                    "AT_PERFORMANCE_" +
                    release.ReleaseId +
                    "_" +
                    turn,
                    "KVLT",
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    0,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "TEST_SCENARIO"
                );

            ActivationLegitimacyAssessment assessment =
                new ActivationLegitimacyAssessment(
                    candidate,
                    ActivationLegitimacyDisposition
                        .Covered,
                    precedent
                );

            Assert.That(
                activation.ApplyCovered(
                    release,
                    assessment,
                    turn
                ),
                Is.True
            );
        }

        private static
            ActivationLegitimacyCandidate
            PerformanceCandidate(
                SceneRelease release,
                TagDegree appliedDegree)
        {
            return new ActivationLegitimacyCandidate(
                ActivationAttemptRoute.Performance,
                "HAPPENING",
                "PERFORM",
                "PLAYER_A",
                release.ReleaseId,
                release.SourceDemoTapeId,
                "TRACK",
                "IDEA",
                0,
                "PUBLIC_PERFORMANCE",
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Weak,
                appliedDegree,
                TagDegree.Dominant
            );
        }

        private static
            ActivationLegitimacyCandidate
            HailCandidate(
                SceneRelease release,
                TagDegree appliedDegree)
        {
            return new ActivationLegitimacyCandidate(
                ActivationAttemptRoute.Hail,
                "HAPPENING",
                "HAIL_INTENT",
                "PLAYER_A",
                release.ReleaseId,
                release.SourceDemoTapeId,
                "TRACK",
                "IDEA",
                0,
                "CHURCH_ARSON",
                TagAxis.Symbolic,
                TagPole.Negative,

                TagDegree.Transgressive,

                appliedDegree,
                TagDegree.Dominant,
                "BEHAVIOR",
                "HAIL",
                "ASPECT_SATAN"
            );
        }

        private static SceneRelease Release(
            DemoTape demo,
            string sceneId,
            int releasedTurn)
        {
            return new SceneRelease(
                "Test Release",
                demo.DemoTapeId,
                "OWNER",
                sceneId,
                releasedTurn,
                1f
            );
        }

        private static DemoTape Demo()
        {
            DemoTapeIdeaSnapshot idea =
                new DemoTapeIdeaSnapshot(
                    "IDEA",
                    0,
                    "ASPECT",
                    IdeaPayloadType.TagPair,
                    "AUTHOR",
                    TagContainerType.Transient,
                    1f,
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

            DemoTapeTrackSnapshot track =
                new DemoTapeTrackSnapshot(
                    "TRACK",
                    "TRACK",
                    1f,
                    1f,
                    new[]
                    {
                        idea
                    }
                );

            return new DemoTape(
                "DEMO",
                "Test Demo",
                "SET",
                "Test Set",
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
                            TagDegree.Dominant,
                            1f
                        ),

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