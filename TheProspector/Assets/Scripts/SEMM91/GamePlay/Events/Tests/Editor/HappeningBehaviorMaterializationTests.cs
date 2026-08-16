using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions.History;

namespace SEMM91.GamePlay.Events.Tests
{
    public class
        HappeningBehaviorMaterializationTests
    {
        [Test]
        public void SuccessfulEnactBehavior_CreatesFactualBehavior()
        {
            Happening happening =
                CreateResolvingHappening();

            RecordBehaviorIntent(
                happening,
                "INTENT",
                hailedAspectId: null
            );

            Resolve(
                happening,
                "INTENT",
                HappeningIntentOutcome.Succeeded
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "INTENT",
                        "BEHAVIOR"
                    ),
                Is.True
            );

            Assert.That(
                happening.BehaviorOccurrences.Count,
                Is.EqualTo(1)
            );

            BehaviorOccurrence behavior =
                happening.BehaviorOccurrences[0];

            Assert.That(
                behavior.BehaviorOccurrenceId,
                Is.EqualTo("BEHAVIOR")
            );

            Assert.That(
                behavior.HappeningId,
                Is.EqualTo("HAPPENING")
            );

            Assert.That(
                behavior.ContextId,
                Is.EqualTo("CTX")
            );

            Assert.That(
                behavior.ActorEntityIds,
                Is.EqualTo(
                    new[]
                    {
                        "PLAYER_A"
                    }
                )
            );

            Assert.That(
                behavior.BehaviorTypeId,
                Is.EqualTo("CHURCH_ARSON")
            );

            Assert.That(
                behavior.Axis,
                Is.EqualTo(TagAxis.Symbolic)
            );

            Assert.That(
                behavior.Pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                behavior.DemonstratedDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                behavior.SourceIntentId,
                Is.EqualTo("INTENT")
            );

            Assert.That(
                behavior.ResolvedTurn,
                Is.EqualTo(8)
            );

            Assert.That(
                happening.HailOccurrences,
                Is.Empty
            );
        }

        [Test]
        public void SuccessfulBehaviorWithIntendedHail_CreatesSeparateHailFact()
        {
            Happening happening =
                CreateResolvingHappening();

            RecordBehaviorIntent(
                happening,
                "INTENT",
                "ASPECT_SATAN"
            );

            Resolve(
                happening,
                "INTENT",
                HappeningIntentOutcome.Succeeded
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "INTENT",
                        "BEHAVIOR",
                        "HAIL"
                    ),
                Is.True
            );

            Assert.That(
                happening.BehaviorOccurrences.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                happening.HailOccurrences.Count,
                Is.EqualTo(1)
            );

            HailOccurrence hail =
                happening.HailOccurrences[0];

            Assert.That(
                hail.HailOccurrenceId,
                Is.EqualTo("HAIL")
            );

            Assert.That(
                hail.BehaviorOccurrenceId,
                Is.EqualTo("BEHAVIOR")
            );

            Assert.That(
                hail.DeclarerEntityId,
                Is.EqualTo("PLAYER_A")
            );

            Assert.That(
                hail.HailedAspectId,
                Is.EqualTo("ASPECT_SATAN")
            );
        }

        [Test]
        public void BlockedEnactBehavior_ProducesNoBehaviorOrHail()
        {
            Happening happening =
                CreateResolvingHappening();

            RecordBehaviorIntent(
                happening,
                "PRIMARY",
                "ASPECT_SATAN"
            );

            HappeningOpposeIntent oppose =
                new HappeningOpposeIntent(
                    "OPPOSE",
                    "HAPPENING",
                    "CTX",
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
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        "PRIMARY",
                        HappeningIntentOutcome.Blocked,
                        8,
                        "OPPOSE"
                    )
                ),
                Is.True
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "PRIMARY",
                        "BEHAVIOR",
                        "HAIL"
                    ),
                Is.False
            );

            Assert.That(
                happening.BehaviorOccurrences,
                Is.Empty
            );

            Assert.That(
                happening.HailOccurrences,
                Is.Empty
            );

            // Attempt/contest history survives.

            Assert.That(
                happening.ParticipantIntents.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                happening.IntentResolutions.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void FailedEnactBehavior_ProducesNoBehaviorOrHail()
        {
            Happening happening =
                CreateResolvingHappening();

            RecordBehaviorIntent(
                happening,
                "INTENT",
                "ASPECT_ODIN"
            );

            Resolve(
                happening,
                "INTENT",
                HappeningIntentOutcome.Failed
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "INTENT",
                        "BEHAVIOR",
                        "HAIL"
                    ),
                Is.False
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
        public void IntendedHailAndFactualHailMustRemainConsistent()
        {
            Happening withHail =
                CreateResolvingHappening();

            RecordBehaviorIntent(
                withHail,
                "WITH_HAIL",
                "ASPECT_SATAN"
            );

            Resolve(
                withHail,
                "WITH_HAIL",
                HappeningIntentOutcome.Succeeded
            );

            // Cannot silently drop an intended Hail.

            Assert.That(
                withHail
                    .TryMaterializeSuccessfulEnactBehavior(
                        "WITH_HAIL",
                        "BEHAVIOR"
                    ),
                Is.False
            );

            Assert.That(
                withHail.BehaviorOccurrences,
                Is.Empty
            );

            Happening withoutHail =
                CreateResolvingHappening();

            RecordBehaviorIntent(
                withoutHail,
                "NO_HAIL",
                null
            );

            Resolve(
                withoutHail,
                "NO_HAIL",
                HappeningIntentOutcome.Succeeded
            );

            // Cannot manufacture a Hail that was
            // never part of the Intent.

            Assert.That(
                withoutHail
                    .TryMaterializeSuccessfulEnactBehavior(
                        "NO_HAIL",
                        "BEHAVIOR_2",
                        "HAIL_2"
                    ),
                Is.False
            );

            Assert.That(
                withoutHail.BehaviorOccurrences,
                Is.Empty
            );
        }

        [Test]
        public void OneIntentCanMaterializeOnlyOneFactualBehavior()
        {
            Happening happening =
                CreateResolvingHappening();

            RecordBehaviorIntent(
                happening,
                "INTENT",
                null
            );

            Resolve(
                happening,
                "INTENT",
                HappeningIntentOutcome.Succeeded
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "INTENT",
                        "BEHAVIOR_A"
                    ),
                Is.True
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "INTENT",
                        "BEHAVIOR_B"
                    ),
                Is.False
            );

            Assert.That(
                happening.BehaviorOccurrences.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void SuccessfulBehaviorMustBecomeFactualBeforeHappeningCanSettle()
        {
            Happening happening =
                CreateResolvingHappening();

            RecordBehaviorIntent(
                happening,
                "INTENT",
                null
            );

            Resolve(
                happening,
                "INTENT",
                HappeningIntentOutcome.Succeeded
            );

            Assert.That(
                happening.TrySettle(8),
                Is.False
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "INTENT",
                        "BEHAVIOR"
                    ),
                Is.True
            );

            Assert.That(
                happening.TrySettle(8),
                Is.True
            );
        }

        [Test]
        public void BlockedOrFailedBehaviorDoesNotPreventSettlement()
        {
            Happening happening =
                CreateResolvingHappening();

            RecordBehaviorIntent(
                happening,
                "FAILED",
                null
            );

            Resolve(
                happening,
                "FAILED",
                HappeningIntentOutcome.Failed
            );

            Assert.That(
                happening.TrySettle(8),
                Is.True
            );

            Assert.That(
                happening.BehaviorOccurrences,
                Is.Empty
            );
        }

        [Test]
        public void OrganizerParticipationAndBehaviorAuthorshipRemainDistinct()
        {
            Happening happening =
                CreateResolvingHappening();

            RecordBehaviorIntent(
                happening,
                "INTENT",
                "ASPECT_SATAN"
            );

            Resolve(
                happening,
                "INTENT",
                HappeningIntentOutcome.Succeeded
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "INTENT",
                        "BEHAVIOR",
                        "HAIL"
                    ),
                Is.True
            );

            BehaviorOccurrence behavior =
                happening.BehaviorOccurrences[0];

            HailOccurrence hail =
                happening.HailOccurrences[0];

            Assert.That(
                happening.InstigatorEntityId,
                Is.EqualTo("KEEPER")
            );

            Assert.That(
                behavior.ActorEntityIds,
                Is.EqualTo(
                    new[]
                    {
                        "PLAYER_A"
                    }
                )
            );

            Assert.That(
                behavior.ActorEntityIds,
                Does.Not.Contain("KEEPER")
            );

            Assert.That(
                hail.DeclarerEntityId,
                Is.EqualTo("PLAYER_A")
            );
        }

        private static Happening
            CreateResolvingHappening()
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
                        "CTX",
                        "Cemetery photo shoot",
                        HappeningContextAnchorKind
                            .PhysicalNode,
                        "NODE_CEMETERY",
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

            Assert.That(
                happening.TryBeginResolving(7),
                Is.True
            );

            return happening;
        }

        private static void
            RecordBehaviorIntent(
                Happening happening,
                string intentId,
                string hailedAspectId)
        {
            Assert.That(
                happening.TryRecordParticipantIntent(
                    new HappeningEnactBehaviorIntent(
                        intentId,
                        "HAPPENING",
                        "CTX",
                        "PLAYER_A",
                        7,
                        "CHURCH_ARSON",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive,
                        hailedAspectId
                    )
                ),
                Is.True
            );
        }

        private static void Resolve(
            Happening happening,
            string intentId,
            HappeningIntentOutcome outcome)
        {
            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        intentId,
                        outcome,
                        8
                    )
                ),
                Is.True
            );
        }
    }
}