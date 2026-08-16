using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions.History;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltHappeningConsequenceSettlementServiceTests
    {
        private const int Turn =
            8;

        private readonly
            KvltHappeningPreparationService
            preparationService =
                new();

        private readonly
            KvltHappeningConsequenceSettlementService
            settlementService =
                new();

        [Test]
        public void
            CoveredPerformanceActivatesFettersAndSettlesHappening()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    Turn - 1
                );

            SceneReleaseActivationSource source =
                new(
                    release,
                    demo
                );

            Happening happening =
                ResolvingHappening(
                    "HAPPENING_COVERED"
                );

            HappeningPerformIntent perform =
                new(
                    "PERFORM",
                    happening.HappeningId,
                    "CTX",
                    "PLAYER_A",
                    Turn,
                    release.ReleaseId
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    perform
                ),
                Is.True
            );

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        perform.IntentId,
                        HappeningIntentOutcome
                            .Succeeded,
                        Turn
                    )
                ),
                Is.True
            );

            AcceptedTransgressionState accepted =
                AcceptedPerformance();

            AllegianceCrisisRegistry crises =
                new();

            KvltHappeningPreparationResult preparation =
                preparationService.Prepare(
                    Turn,
                    new[]
                    {
                        happening
                    },
                    new[]
                    {
                        source
                    },
                    accepted,
                    crises,
                    Participants(),
                    "KEEPER"
                );

            KvltHappeningConsequenceSettlementResult
                result =
                    settlementService.Settle(
                        Turn,
                        preparation,
                        new[]
                        {
                            source
                        },
                        accepted,
                        crises,
                        Environment()
                    );

            Assert.That(
                result.CoveredActivationApplications,
                Is.EqualTo(1)
            );

            Assert.That(
                result.CrisisSettlements,
                Is.Empty
            );

            Assert.That(
                result.PendingStoredCount,
                Is.EqualTo(0)
            );

            Assert.That(
                release.TryGetPairActivationState(
                    "TRACK",
                    "IDEA",
                    0,
                    out SceneReleasePairActivationState
                        activation
                ),
                Is.True
            );

            Assert.That(
                activation.CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            Assert.That(
                result.LegitimacyByRelease[
                    release.ReleaseId
                ].HasTrve,
                Is.True
            );

            Assert.That(
                result.FetteringByRelease[
                    release.ReleaseId
                ],
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .Fettered
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                happening.LifecycleState,
                Is.EqualTo(
                    HappeningLifecycleState.Settled
                )
            );
        }

        [Test]
        public void
            SocietyVictoryStoresPendingAndGraceProtectsRelease()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    Turn - 1
                );

            SceneReleaseActivationSource source =
                new(
                    release,
                    demo
                );

            AcceptedTransgressionState accepted =
                new(
                    "KVLT"
                );

            AllegianceCrisisRegistry crises =
                new();

            KvltHappeningPreparationResult preparation =
                PrepareSevereHail(
                    "HAPPENING_REJECTED",
                    source,
                    accepted,
                    crises,
                    out Happening happening
                );

            AllegianceCrisis crisis =
                preparation.OpenedCrises[0];

            ResolveCrisis(
                crisis,
                AllegianceChoice.Society
            );

            KvltHappeningConsequenceSettlementResult
                result =
                    settlementService.Settle(
                        Turn,
                        preparation,
                        new[]
                        {
                            source
                        },
                        accepted,
                        crises,
                        Environment()
                    );

            Assert.That(
                result.CrisisSettlements.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.PendingStoredCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.CrisisLegitimizedApplications,
                Is.EqualTo(0)
            );

            Assert.That(
                result.AcceptedPrecedentsRaised,
                Is.EqualTo(0)
            );

            Assert.That(
                release.PendingActivations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.PendingActivations[0]
                    .IsRedeemed,
                Is.False
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );

            Assert.That(
                result.FetteringByRelease[
                    release.ReleaseId
                ],
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
                happening.LifecycleState,
                Is.EqualTo(
                    HappeningLifecycleState.Settled
                )
            );
        }

        [Test]
        public void
            KvltVictoryRaisesPrecedentActivatesAndRedeemsOlderPending()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    Turn - 1
                );

            SceneReleaseActivationSource source =
                new(
                    release,
                    demo
                );

            /*
             * Seed one older Society-rejected
             * occurrence of the same praxis.
             */
            ActivationLegitimacyCandidate
                oldCandidate =
                    new(
                        ActivationAttemptRoute.Hail,
                        "HAPPENING_OLD",
                        "INTENT_OLD",
                        "PLAYER_A",
                        release.ReleaseId,
                        demo.DemoTapeId,
                        "TRACK",
                        "IDEA",
                        0,
                        "CHURCH_ARSON",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive,
                        TagDegree.Transgressive,
                        TagDegree.Transgressive,
                        "BEHAVIOR_OLD",
                        "HAIL_OLD",
                        "ASPECT_SATAN"
                    );

            SceneReleaseActivationStateService
                activationState =
                    new();

            Assert.That(
                activationState.StorePending(
                    release,
                    new PendingActivationCandidate(
                        oldCandidate,
                        "QUESTION_OLD",
                        Turn - 1
                    ),
                    out SceneReleasePendingActivation
                        oldPending
                ),
                Is.True
            );

            AcceptedTransgressionState accepted =
                new(
                    "KVLT"
                );

            AllegianceCrisisRegistry crises =
                new();

            KvltHappeningPreparationResult preparation =
                PrepareSevereHail(
                    "HAPPENING_ACCEPTED",
                    source,
                    accepted,
                    crises,
                    out Happening happening
                );

            ResolveCrisis(
                preparation.OpenedCrises[0],
                AllegianceChoice.Kvlt
            );

            KvltHappeningConsequenceSettlementResult
                result =
                    settlementService.Settle(
                        Turn,
                        preparation,
                        new[]
                        {
                            source
                        },
                        accepted,
                        crises,
                        Environment()
                    );

            Assert.That(
                result.AcceptedPrecedentsRaised,
                Is.EqualTo(1)
            );

            Assert.That(
                accepted.History.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.CrisisLegitimizedApplications,
                Is.EqualTo(1)
            );

            Assert.That(
                result.PendingRedeemedCount,
                Is.EqualTo(1)
            );

            Assert.That(
                oldPending.IsRedeemed,
                Is.True
            );

            Assert.That(
                oldPending.RedeemedTurn,
                Is.EqualTo(
                    Turn
                )
            );

            Assert.That(
                release.TryGetPairActivationState(
                    "TRACK",
                    "IDEA",
                    0,
                    out SceneReleasePairActivationState
                        activation
                ),
                Is.True
            );

            Assert.That(
                activation.CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                result.LegitimacyByRelease[
                    release.ReleaseId
                ].HasTrve,
                Is.True
            );

            Assert.That(
                result.FetteringByRelease[
                    release.ReleaseId
                ],
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .Fettered
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                happening.LifecycleState,
                Is.EqualTo(
                    HappeningLifecycleState.Settled
                )
            );
        }

        [Test]
        public void
            OpenCrisisPreventsAnyConsequenceSettlement()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    Turn - 1
                );

            SceneReleaseActivationSource source =
                new(
                    release,
                    demo
                );

            AcceptedTransgressionState accepted =
                new(
                    "KVLT"
                );

            AllegianceCrisisRegistry crises =
                new();

            KvltHappeningPreparationResult preparation =
                PrepareSevereHail(
                    "HAPPENING_OPEN",
                    source,
                    accepted,
                    crises,
                    out Happening happening
                );

            Assert.That(
                preparation.OpenedCrises.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                preparation.OpenedCrises[0].IsOpen,
                Is.True
            );

            Assert.Throws<InvalidOperationException>(
                () =>
                    settlementService.Settle(
                        Turn,
                        preparation,
                        new[]
                        {
                            source
                        },
                        accepted,
                        crises,
                        Environment()
                    )
            );

            /*
             * Validation occurs before semantic
             * mutation.
             */
            Assert.That(
                accepted.History,
                Is.Empty
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );

            Assert.That(
                release.PendingActivations,
                Is.Empty
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Fringe
                )
            );

            Assert.That(
                happening.LifecycleState,
                Is.EqualTo(
                    HappeningLifecycleState.Resolving
                )
            );
        }

        private KvltHappeningPreparationResult
            PrepareSevereHail(
                string happeningId,
                SceneReleaseActivationSource source,
                AcceptedTransgressionState accepted,
                AllegianceCrisisRegistry crises,
                out Happening happening)
        {
            happening =
                ResolvingHappening(
                    happeningId
                );

            HappeningEnactBehaviorIntent intent =
                new(
                    "ARSON_HAIL",
                    happening.HappeningId,
                    "CTX",
                    "PLAYER_A",
                    Turn,
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

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        intent.IntentId,
                        HappeningIntentOutcome
                            .Succeeded,
                        Turn
                    )
                ),
                Is.True
            );

            KvltHappeningPreparationResult preparation =
                preparationService.Prepare(
                    Turn,
                    new[]
                    {
                        happening
                    },
                    new[]
                    {
                        source
                    },
                    accepted,
                    crises,
                    Participants(),
                    "KEEPER"
                );

            Assert.That(
                preparation.OpenedCrises.Count,
                Is.EqualTo(1)
            );

            return preparation;
        }

        private static void ResolveCrisis(
            AllegianceCrisis crisis,
            AllegianceChoice choice)
        {
            foreach (
                string voter
                in crisis.EligibleVoterEntityIds)
            {
                Assert.That(
                    crisis.TryCastVote(
                        new AllegianceCrisisVote(
                            voter,
                            choice,
                            Turn
                        )
                    ),
                    Is.True
                );
            }

            Assert.That(
                crisis.TryResolve(
                    Turn,
                    out _
                ),
                Is.True
            );
        }

        private static string[] Participants()
        {
            return new[]
            {
                "KEEPER",
                "PLAYER_A",
                "PLAYER_B"
            };
        }

        private static
            AcceptedTransgressionState
            AcceptedPerformance()
        {
            AcceptedTransgressionState state =
                new(
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
                        "TEST"
                    )
                ),
                Is.True
            );

            return state;
        }

        private static
            TrackEvaluationEnvironment
            Environment()
        {
            SocietyNormativeProfile society =
                new();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            return new TrackEvaluationEnvironment(
                Turn,
                NormativeCentre.Neutral,
                NormativeCentre.Neutral,
                society
            );
        }

        private static Happening
            ResolvingHappening(
                string happeningId)
        {
            Happening happening =
                new(
                    happeningId,
                    "KVLT",
                    "KEEPER",
                    "PROMOTION",
                    "KVLT_NIGHT",
                    "NODE_HOLE",
                    Turn,
                    new CharacterActionKey(
                        "KEEPER",
                        Turn,
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
                        Turn
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
                    Turn
                ),
                Is.True
            );

            return happening;
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
                new(
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
                new(
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
    }
}