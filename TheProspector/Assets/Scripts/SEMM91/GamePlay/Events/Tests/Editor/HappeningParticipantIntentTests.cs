using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions.History;

namespace SEMM91.GamePlay.Events.Tests
{
    public class HappeningParticipantIntentTests
    {
        [Test]
        public void PerformIntent_ExplicitlyTargetsSceneRelease()
        {
            HappeningPerformIntent intent =
                new HappeningPerformIntent(
                    "INTENT_PERFORM",
                    "HAPPENING",
                    "CTX_STAGE",
                    "PLAYER_A",
                    7,
                    "RELEASE_X"
                );

            Assert.That(
                intent.Kind,
                Is.EqualTo(
                    HappeningIntentKind.Perform
                )
            );

            Assert.That(
                intent.TargetSceneReleaseId,
                Is.EqualTo("RELEASE_X")
            );
        }

        [Test]
        public void EnactBehaviorIntent_PreservesPraxisAndOptionalHail()
        {
            HappeningEnactBehaviorIntent intent =
                new HappeningEnactBehaviorIntent(
                    "INTENT_ARSON",
                    "HAPPENING",
                    "CTX_CEMETERY",
                    "PLAYER_A",
                    7,
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    "ASPECT_SATAN"
                );

            Assert.That(
                intent.Kind,
                Is.EqualTo(
                    HappeningIntentKind.EnactBehavior
                )
            );

            Assert.That(
                intent.BehaviorTypeId,
                Is.EqualTo("CHURCH_ARSON")
            );

            Assert.That(
                intent.Axis,
                Is.EqualTo(TagAxis.Symbolic)
            );

            Assert.That(
                intent.Pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                intent.IntendedDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                intent.HasIntendedHail,
                Is.True
            );

            Assert.That(
                intent.IntendedHailedAspectId,
                Is.EqualTo("ASPECT_SATAN")
            );
        }

        [Test]
        public void AttendanceWithoutSpecialIntent_RemainsPassive()
        {
            Happening happening =
                CreateResolvingHappening();

            Assert.That(
                happening.ParticipantEntityIds,
                Does.Contain("PLAYER_A")
            );

            Assert.That(
                happening.ParticipantIntents,
                Is.Empty
            );
        }

        [Test]
        public void IntentsCanOnlyBeRecordedDuringResolving()
        {
            Happening happening =
                CreateCommittedHappening();

            HappeningPerformIntent intent =
                Perform(
                    "INTENT"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    intent
                ),
                Is.False
            );

            Assert.That(
                happening.TryBeginResolving(7),
                Is.True
            );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    intent
                ),
                Is.True
            );
        }

        [Test]
        public void IntentRequiresKnownParticipantAndContext()
        {
            Happening happening =
                CreateResolvingHappening();

            HappeningPerformIntent outsider =
                new HappeningPerformIntent(
                    "OUTSIDER",
                    "HAPPENING",
                    "CTX_STAGE",
                    "NOT_PARTICIPATING",
                    7,
                    "RELEASE_X"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    outsider
                ),
                Is.False
            );

            HappeningPerformIntent badContext =
                new HappeningPerformIntent(
                    "BAD_CONTEXT",
                    "HAPPENING",
                    "UNKNOWN_CONTEXT",
                    "PLAYER_A",
                    7,
                    "RELEASE_X"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    badContext
                ),
                Is.False
            );

            Assert.That(
                happening.ParticipantIntents,
                Is.Empty
            );
        }

        [Test]
        public void OpposeIntent_PreservesWhoOpposedWhom()
        {
            Happening happening =
                CreateResolvingHappening();

            HappeningEnactBehaviorIntent primary =
                Behavior(
                    "PRIMARY"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    primary
                ),
                Is.True
            );

            HappeningOpposeIntent oppose =
                new HappeningOpposeIntent(
                    "OPPOSE",
                    "HAPPENING",
                    "CTX_STAGE",
                    "KEEPER",
                    7,
                    "PRIMARY"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    oppose
                ),
                Is.True
            );

            Assert.That(
                oppose.TargetIntentId,
                Is.EqualTo("PRIMARY")
            );

            HappeningOpposeIntent unknown =
                new HappeningOpposeIntent(
                    "BAD_OPPOSE",
                    "HAPPENING",
                    "CTX_STAGE",
                    "KEEPER",
                    7,
                    "DOES_NOT_EXIST"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    unknown
                ),
                Is.False
            );
        }

        [Test]
        public void SupportIntent_PreservesSupportedIntentRelationship()
        {
            Happening happening =
                CreateResolvingHappening();

            Assert.That(
                happening.TryRecordParticipantIntent(
                    Perform("PRIMARY")
                ),
                Is.True
            );

            HappeningSupportIntent support =
                new HappeningSupportIntent(
                    "SUPPORT",
                    "HAPPENING",
                    "CTX_STAGE",
                    "KEEPER",
                    7,
                    "PRIMARY"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    support
                ),
                Is.True
            );

            Assert.That(
                support.TargetIntentId,
                Is.EqualTo("PRIMARY")
            );
        }

        [Test]
        public void BlockedResolution_RequiresActualOpposingIntent()
        {
            Happening happening =
                CreateResolvingHappening();

            Assert.That(
                happening.TryRecordParticipantIntent(
                    Behavior("PRIMARY")
                ),
                Is.True
            );

            HappeningIntentResolution invalid =
                new HappeningIntentResolution(
                    "PRIMARY",
                    HappeningIntentOutcome.Blocked,
                    7,
                    "NOT_AN_OPPOSITION"
                );

            Assert.That(
                happening.TryRecordIntentResolution(
                    invalid
                ),
                Is.False
            );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    new HappeningOpposeIntent(
                        "OPPOSE",
                        "HAPPENING",
                        "CTX_STAGE",
                        "KEEPER",
                        7,
                        "PRIMARY"
                    )
                ),
                Is.True
            );

            HappeningIntentResolution valid =
                new HappeningIntentResolution(
                    "PRIMARY",
                    HappeningIntentOutcome.Blocked,
                    7,
                    "OPPOSE"
                );

            Assert.That(
                happening.TryRecordIntentResolution(
                    valid
                ),
                Is.True
            );

            Assert.That(
                valid.WasBlocked,
                Is.True
            );

            Assert.That(
                valid.OpposingIntentId,
                Is.EqualTo("OPPOSE")
            );
        }

        [Test]
        public void IntentReceivesOnlyOneAuthoritativeResolution()
        {
            Happening happening =
                CreateResolvingHappening();

            Assert.That(
                happening.TryRecordParticipantIntent(
                    Perform("PRIMARY")
                ),
                Is.True
            );

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        "PRIMARY",
                        HappeningIntentOutcome.Succeeded,
                        7
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        "PRIMARY",
                        HappeningIntentOutcome.Failed,
                        8
                    )
                ),
                Is.False
            );

            Assert.That(
                happening.TryGetIntentResolution(
                    "PRIMARY",
                    out HappeningIntentResolution
                        stored
                ),
                Is.True
            );

            Assert.That(
                stored.Outcome,
                Is.EqualTo(
                    HappeningIntentOutcome.Succeeded
                )
            );
        }

        [Test]
        public void HappeningCannotSettleUntilRecordedIntentsAreResolved()
        {
            Happening happening =
                CreateResolvingHappening();

            Assert.That(
                happening.TryRecordParticipantIntent(
                    Perform("PRIMARY")
                ),
                Is.True
            );

            Assert.That(
                happening.TrySettle(8),
                Is.False
            );

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        "PRIMARY",
                        HappeningIntentOutcome.Succeeded,
                        8
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.TrySettle(8),
                Is.True
            );

            Assert.That(
                happening.LifecycleState,
                Is.EqualTo(
                    HappeningLifecycleState.Settled
                )
            );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    new HappeningPerformIntent(
                        "TOO_LATE",
                        "HAPPENING",
                        "CTX_STAGE",
                        "PLAYER_A",
                        8,
                        "RELEASE_X"
                    )
                ),
                Is.False
            );
        }

        private static Happening
            CreateCommittedHappening()
        {
            Happening happening =
                new Happening(
                    "HAPPENING",
                    "COLLECTIVE_KVLT",
                    "KEEPER",
                    "CAUSE",
                    "CRUX",
                    "NODE_HOLE",
                    7,
                    new CharacterActionKey(
                        "KEEPER",
                        7,
                        1
                    )
                );

            Assert.That(
                happening.TryAddContext(
                    new HappeningContext(
                        "CTX_STAGE",
                        "Stage is open for performance",
                        HappeningContextAnchorKind
                            .SceneRelease,
                        "RELEASE_X",
                        "KEEPER",
                        7
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

            return happening;
        }

        private static Happening
            CreateResolvingHappening()
        {
            Happening happening =
                CreateCommittedHappening();

            Assert.That(
                happening.TryBeginResolving(7),
                Is.True
            );

            return happening;
        }

        private static
            HappeningPerformIntent
            Perform(
                string id)
        {
            return new HappeningPerformIntent(
                id,
                "HAPPENING",
                "CTX_STAGE",
                "PLAYER_A",
                7,
                "RELEASE_X"
            );
        }

        private static
            HappeningEnactBehaviorIntent
            Behavior(
                string id)
        {
            return new HappeningEnactBehaviorIntent(
                id,
                "HAPPENING",
                "CTX_STAGE",
                "PLAYER_A",
                7,
                "CHURCH_ARSON",
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Transgressive,
                "ASPECT_SATAN"
            );
        }
    }
}