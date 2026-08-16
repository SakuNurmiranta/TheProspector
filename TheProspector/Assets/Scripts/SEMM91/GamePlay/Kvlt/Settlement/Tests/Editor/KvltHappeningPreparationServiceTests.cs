using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions.History;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltHappeningPreparationServiceTests
    {
        private const int Turn =
            8;

        private readonly
            KvltHappeningPreparationService
            service =
                new();

        [Test]
        public void
            CoveredPerformanceRecordsAttemptButDoesNotApplyActivationYet()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    releasedTurn:
                        Turn - 1
                );

            Happening happening =
                ResolvingHappening(
                    "HAPPENING_PERFORMANCE"
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

            KvltHappeningPreparationResult result =
                service.Prepare(
                    Turn,
                    new[]
                    {
                        happening
                    },
                    new[]
                    {
                        new SceneReleaseActivationSource(
                            release,
                            demo
                        )
                    },
                    accepted,
                    crises,
                    Participants(),
                    "KEEPER"
                );

            Assert.That(
                result.ActivationAttempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.ActivationAttempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.LegitimacyAssessments.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.LegitimacyAssessments[0]
                    .Disposition,
                Is.EqualTo(
                    ActivationLegitimacyDisposition
                        .Covered
                )
            );

            Assert.That(
                result.OpenedCrises,
                Is.Empty
            );

            Assert.That(
                crises.Count,
                Is.EqualTo(0)
            );

            /*
             * Preparation does not prematurely apply
             * semantic activation.
             */
            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );

            Assert.That(
                happening.LifecycleState,
                Is.EqualTo(
                    HappeningLifecycleState.Resolving
                )
            );
        }

        [Test]
        public void
            SevereSuccessfulHailMaterializesFactsAndOpensOneCrisis()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    releasedTurn:
                        Turn - 1
                );

            Happening happening =
                ResolvingHappening(
                    "HAPPENING_ARSON"
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

            AcceptedTransgressionState accepted =
                new(
                    "KVLT"
                );

            AllegianceCrisisRegistry crises =
                new();

            KvltHappeningPreparationResult result =
                service.Prepare(
                    Turn,
                    new[]
                    {
                        happening
                    },
                    new[]
                    {
                        new SceneReleaseActivationSource(
                            release,
                            demo
                        )
                    },
                    accepted,
                    crises,
                    Participants(),
                    "KEEPER"
                );

            Assert.That(
                result.MaterializedBehaviorCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.MaterializedHailCount,
                Is.EqualTo(1)
            );

            Assert.That(
                happening.BehaviorOccurrences.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                happening.HailOccurrences.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.ActivationAttempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.LegitimacyAssessments.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.LegitimacyAssessments[0]
                    .Disposition,
                Is.EqualTo(
                    ActivationLegitimacyDisposition
                        .RequiresAllegianceCrisis
                )
            );

            Assert.That(
                result.CrisisGroups.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.OpenedCrises.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                crises.Count,
                Is.EqualTo(1)
            );

            AllegianceCrisis crisis =
                result.OpenedCrises[0];

            Assert.That(
                crisis.Question.TriggeringActorEntityId,
                Is.EqualTo(
                    "PLAYER_A"
                )
            );

            Assert.That(
                crisis.EligibleVoterEntityIds,
                Does.Not.Contain(
                    "PLAYER_A"
                )
            );

            Assert.That(
                crisis.EligibleVoterEntityIds,
                Does.Contain(
                    "KEEPER"
                )
            );

            Assert.That(
                crisis.EligibleVoterEntityIds,
                Does.Contain(
                    "PLAYER_B"
                )
            );

            /*
             * The Crisis has been opened, not decided.
             * Activation waits for HappeningSettlement.
             */
            Assert.That(
                crisis.IsOpen,
                Is.True
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );

            Assert.That(
                happening.LifecycleState,
                Is.EqualTo(
                    HappeningLifecycleState.Resolving
                )
            );
        }

        [Test]
        public void
            BlockedPerformanceStillRecordsGraceProtectingAttempt()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                Release(
                    demo,
                    releasedTurn:
                        Turn - 1
                );

            Happening happening =
                ResolvingHappening(
                    "HAPPENING_BLOCKED"
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

            HappeningOpposeIntent oppose =
                new(
                    "OPPOSE",
                    happening.HappeningId,
                    "CTX",
                    "KEEPER",
                    Turn,
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
                        Turn,
                        oppose.IntentId
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        oppose.IntentId,
                        HappeningIntentOutcome
                            .Succeeded,
                        Turn
                    )
                ),
                Is.True
            );

            KvltHappeningPreparationResult result =
                service.Prepare(
                    Turn,
                    new[]
                    {
                        happening
                    },
                    new[]
                    {
                        new SceneReleaseActivationSource(
                            release,
                            demo
                        )
                    },
                    AcceptedPerformance(),
                    new AllegianceCrisisRegistry(),
                    Participants(),
                    "KEEPER"
                );

            Assert.That(
                result.ActivationAttempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.LegitimacyAssessments,
                Is.Empty
            );

            Assert.That(
                result.OpenedCrises,
                Is.Empty
            );

            Assert.That(
                happening.BehaviorOccurrences,
                Is.Empty
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );

            /*
             * Camp-2 grace semantics are preserved:
             * the attempt exists even though the
             * performance was blocked.
             */
            Assert.That(
                new SceneReleaseFetteringService()
                    .Resolve(
                        release,
                        hasActiveTrve:
                            false,
                        globalTurn:
                            Turn
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
        }

        [Test]
        public void SeveralHailsShareOneBehaviorOneCrisisAndLockKvlt()
        {
            DemoTape demo = Demo();
            SceneRelease release = Release(
                demo,
                releasedTurn: Turn - 1);
            Happening happening = ResolvingHappening(
                "HAPPENING_SHARED_HAIL");

            HappeningEnactBehaviorIntent primary =
                new HappeningEnactBehaviorIntent(
                    "PRIMARY_HAIL",
                    happening.HappeningId,
                    "CTX",
                    "PLAYER_A",
                    Turn,
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    "ASPECT_SATAN");

            HappeningHailBehaviorIntent secondary =
                new HappeningHailBehaviorIntent(
                    "SECONDARY_HAIL",
                    happening.HappeningId,
                    "CTX",
                    "PLAYER_B",
                    Turn,
                    primary.IntentId,
                    "ASPECT_ODIN");

            Assert.That(
                happening.TryRecordParticipantIntent(primary),
                Is.True);
            Assert.That(
                happening.TryRecordParticipantIntent(secondary),
                Is.True);

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        primary.IntentId,
                        HappeningIntentOutcome.Succeeded,
                        Turn)),
                Is.True);
            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        secondary.IntentId,
                        HappeningIntentOutcome.Succeeded,
                        Turn)),
                Is.True);

            KvltHappeningPreparationResult result =
                service.Prepare(
                    Turn,
                    new[] { happening },
                    new[]
                    {
                        new SceneReleaseActivationSource(
                            release,
                            demo)
                    },
                    new AcceptedTransgressionState("KVLT"),
                    new AllegianceCrisisRegistry(),
                    Participants(),
                    "KEEPER");

            Assert.That(
                happening.BehaviorOccurrences.Count,
                Is.EqualTo(1));
            Assert.That(
                happening.HailOccurrences.Count,
                Is.EqualTo(2));
            Assert.That(
                result.ActivationAttempts.Count,
                Is.EqualTo(2));
            Assert.That(
                result.LegitimacyAssessments.Count,
                Is.EqualTo(2));
            Assert.That(
                result.CrisisGroups.Count,
                Is.EqualTo(1));
            Assert.That(
                result.OpenedCrises.Count,
                Is.EqualTo(1));
            Assert.That(
                result.OpenedCrises[0].Votes.Count,
                Is.EqualTo(1));
            Assert.That(
                result.OpenedCrises[0].Votes[0]
                    .VoterEntityId,
                Is.EqualTo("PLAYER_B"));
            Assert.That(
                result.OpenedCrises[0].Votes[0].Choice,
                Is.EqualTo(AllegianceChoice.Kvlt));
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
