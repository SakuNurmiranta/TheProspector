using NUnit.Framework;
using SEMM91.GamePlay.Actions.History;

namespace SEMM91.GamePlay.Events.Tests.Editor
{
    public sealed class
        HappeningIntentSettlementServiceTests
    {
        private const int Turn =
            7;

        private readonly
            HappeningIntentSettlementService service =
                new();

        [Test]
        public void
            UnopposedPerformanceSucceeds()
        {
            Happening happening =
                CreateResolving();

            HappeningPerformIntent perform =
                new(
                    "INTENT_PERFORM",
                    "HAPPENING",
                    "CTX_STAGE",
                    "PLAYER_A",
                    Turn,
                    "RELEASE_X"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    perform
                ),
                Is.True
            );

            service.Settle(
                happening,
                Turn
            );

            Assert.That(
                happening.TryGetIntentResolution(
                    perform.IntentId,
                    out HappeningIntentResolution
                        resolution
                ),
                Is.True
            );

            Assert.That(
                resolution.Outcome,
                Is.EqualTo(
                    HappeningIntentOutcome.Succeeded
                )
            );
        }

        [Test]
        public void
            GreaterOppositionBlocksPrimaryIntent()
        {
            Happening happening =
                CreateResolving();

            HappeningPerformIntent perform =
                new(
                    "INTENT_PERFORM",
                    "HAPPENING",
                    "CTX_STAGE",
                    "PLAYER_A",
                    Turn,
                    "RELEASE_X"
                );

            HappeningOpposeIntent oppose =
                new(
                    "INTENT_OPPOSE",
                    "HAPPENING",
                    "CTX_STAGE",
                    "PLAYER_B",
                    Turn,
                    perform.IntentId
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    perform
                ),
                Is.True
            );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    oppose
                ),
                Is.True
            );

            service.Settle(
                happening,
                Turn
            );

            Assert.That(
                happening.TryGetIntentResolution(
                    perform.IntentId,
                    out HappeningIntentResolution
                        performanceResolution
                ),
                Is.True
            );

            Assert.That(
                performanceResolution.Outcome,
                Is.EqualTo(
                    HappeningIntentOutcome.Blocked
                )
            );

            Assert.That(
                performanceResolution.OpposingIntentId,
                Is.EqualTo(
                    oppose.IntentId
                )
            );

            Assert.That(
                happening.TryGetIntentResolution(
                    oppose.IntentId,
                    out HappeningIntentResolution
                        oppositionResolution
                ),
                Is.True
            );

            Assert.That(
                oppositionResolution.Outcome,
                Is.EqualTo(
                    HappeningIntentOutcome.Succeeded
                )
            );
        }

        [Test]
        public void
            EqualSupportCancelsOppositionForVerticalSlice()
        {
            Happening happening =
                CreateResolving();

            HappeningPerformIntent perform =
                new(
                    "INTENT_PERFORM",
                    "HAPPENING",
                    "CTX_STAGE",
                    "PLAYER_A",
                    Turn,
                    "RELEASE_X"
                );

            HappeningOpposeIntent oppose =
                new(
                    "INTENT_OPPOSE",
                    "HAPPENING",
                    "CTX_STAGE",
                    "PLAYER_B",
                    Turn,
                    perform.IntentId
                );

            HappeningSupportIntent support =
                new(
                    "INTENT_SUPPORT",
                    "HAPPENING",
                    "CTX_STAGE",
                    "PLAYER_C",
                    Turn,
                    perform.IntentId
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    perform
                ),
                Is.True
            );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    oppose
                ),
                Is.True
            );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    support
                ),
                Is.True
            );

            service.Settle(
                happening,
                Turn
            );

            Assert.That(
                happening.TryGetIntentResolution(
                    perform.IntentId,
                    out HappeningIntentResolution
                        resolution
                ),
                Is.True
            );

            Assert.That(
                resolution.Outcome,
                Is.EqualTo(
                    HappeningIntentOutcome.Succeeded
                )
            );
        }

        [Test]
        public void
            ExistingConcreteFailureIsPreserved()
        {
            Happening happening =
                CreateResolving();

            HappeningPerformIntent perform =
                new(
                    "INTENT_PERFORM",
                    "HAPPENING",
                    "CTX_STAGE",
                    "PLAYER_A",
                    Turn,
                    "RELEASE_X"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    perform
                ),
                Is.True
            );

            HappeningIntentResolution
                concreteFailure =
                    new(
                        perform.IntentId,
                        HappeningIntentOutcome.Failed,
                        Turn
                    );

            Assert.That(
                happening.TryRecordIntentResolution(
                    concreteFailure
                ),
                Is.True
            );

            service.Settle(
                happening,
                Turn
            );

            Assert.That(
                happening.IntentResolutions,
                Has.Count.EqualTo(1)
            );

            Assert.That(
                happening.TryGetIntentResolution(
                    perform.IntentId,
                    out HappeningIntentResolution
                        preserved
                ),
                Is.True
            );

            Assert.That(
                preserved,
                Is.SameAs(
                    concreteFailure
                )
            );

            Assert.That(
                preserved.Outcome,
                Is.EqualTo(
                    HappeningIntentOutcome.Failed
                )
            );
        }

        private static Happening
            CreateResolving()
        {
            Happening happening =
                new(
                    "HAPPENING",
                    "KVLT",
                    "PLAYER_A",
                    "promotion",
                    "night",
                    "NODE",
                    Turn,
                    new CharacterActionKey(
                        "PLAYER_A",
                        Turn,
                        0
                    )
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
                happening.TryAddParticipant(
                    "PLAYER_C"
                ),
                Is.True
            );

            Assert.That(
                happening.TryAddContext(
                    new HappeningContext(
                        "CTX_STAGE",
                        "stage",
                        HappeningContextAnchorKind
                            .SceneRelease,
                        "RELEASE_X",
                        "PLAYER_A",
                        Turn
                    )
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
    }
}