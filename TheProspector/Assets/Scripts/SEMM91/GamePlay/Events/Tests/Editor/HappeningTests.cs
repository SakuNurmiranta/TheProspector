using NUnit.Framework;
using SEMM91.GamePlay.Actions.History;

namespace SEMM91.GamePlay.Events.Tests.Editor
{
    public class HappeningTests
    {
        [Test]
        public void NewHappening_PreservesCommittedIdentityAndProvenance()
        {
            Happening happening =
                CreateHappening();

            Assert.That(
                happening.HappeningId,
                Is.EqualTo("HAPPENING")
            );

            Assert.That(
                happening.OwningCollectiveId,
                Is.EqualTo("COLLECTIVE_KVLT")
            );

            Assert.That(
                happening.InstigatorEntityId,
                Is.EqualTo("KEEPER")
            );

            Assert.That(
                happening.Cause,
                Is.EqualTo("PROMOTE_KVLT")
            );

            Assert.That(
                happening.Crux,
                Is.EqualTo("MIDSUMMER_NIGHT")
            );

            Assert.That(
                happening.AnchorPhysicalNodeId,
                Is.EqualTo("NODE_HOLE")
            );

            Assert.That(
                happening.CommittedTurn,
                Is.EqualTo(7)
            );

            Assert.That(
                happening.SourcePromotionActionKey,
                Is.EqualTo(
                    new CharacterActionKey(
                        "KEEPER",
                        7,
                        1
                    )
                )
            );

            Assert.That(
                happening.LifecycleState,
                Is.EqualTo(
                    HappeningLifecycleState
                        .Committed
                )
            );

            Assert.That(
                happening.ParticipantEntityIds,
                Is.Empty
            );

            Assert.That(
                happening.Contexts,
                Is.Empty
            );

            Assert.That(
                happening.LifecycleTransitions,
                Is.Empty
            );
        }

        [Test]
        public void Context_PreservesPurposeWithoutEncodingAction()
        {
            HappeningContext context =
                new HappeningContext(
                    "CTX_CEMETERY",
                    "Open cemetery for moonlit " +
                    "promotional photography",
                    HappeningContextAnchorKind
                        .PhysicalNode,
                    "NODE_CEMETERY",
                    "KEEPER",
                    7
                );

            Assert.That(
                context.ContextId,
                Is.EqualTo("CTX_CEMETERY")
            );

            Assert.That(
                context.Purpose,
                Does.Contain(
                    "promotional photography"
                )
            );

            Assert.That(
                context.AnchorKind,
                Is.EqualTo(
                    HappeningContextAnchorKind
                        .PhysicalNode
                )
            );

            Assert.That(
                context.AnchorId,
                Is.EqualTo("NODE_CEMETERY")
            );

            Assert.That(
                context.HasAnchor,
                Is.True
            );
        }

        [Test]
        public void CommittedHappening_AllowsInstigatorToStackDistinctContexts()
        {
            Happening happening =
                CreateHappening();

            Assert.That(
                happening.TryAddContext(
                    Context(
                        "CTX_OLD_GODS",
                        HappeningContextAnchorKind
                            .Aspect,
                        "ASPECT_OLD_GODS"
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.TryAddContext(
                    Context(
                        "CTX_RELEASE",
                        HappeningContextAnchorKind
                            .SceneRelease,
                        "RELEASE_X"
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.TryAddContext(
                    Context(
                        "CTX_JOURNALIST",
                        HappeningContextAnchorKind
                            .GameEntity,
                        "JOURNALIST_Y"
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.Contexts.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                happening.Contexts[0].AnchorId,
                Is.EqualTo("ASPECT_OLD_GODS")
            );

            Assert.That(
                happening.Contexts[1].AnchorId,
                Is.EqualTo("RELEASE_X")
            );

            Assert.That(
                happening.Contexts[2].AnchorId,
                Is.EqualTo("JOURNALIST_Y")
            );
        }

        [Test]
        public void Contexts_DoNotPermitDuplicateIdentityOrNonInstigatorAgendaAuthor()
        {
            Happening happening =
                CreateHappening();

            HappeningContext first =
                Context(
                    "CTX_ONE",
                    HappeningContextAnchorKind
                        .Aspect,
                    "ASPECT_ODIN"
                );

            Assert.That(
                happening.TryAddContext(first),
                Is.True
            );

            Assert.That(
                happening.TryAddContext(first),
                Is.False
            );

            HappeningContext outsiderAuthored =
                new HappeningContext(
                    "CTX_OUTSIDER",
                    "Change organizer agenda",
                    HappeningContextAnchorKind
                        .Aspect,
                    "ASPECT_SATAN",
                    "OTHER_PLAYER",
                    7
                );

            Assert.That(
                happening.TryAddContext(
                    outsiderAuthored
                ),
                Is.False
            );

            Assert.That(
                happening.Contexts.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void ParticipantHistory_IsDistinctFromInstigatorAndDeduplicated()
        {
            Happening happening =
                CreateHappening();

            Assert.That(
                happening.TryAddParticipant(
                    "KEEPER"
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
                    "PLAYER_B"
                ),
                Is.False
            );

            Assert.That(
                happening.ParticipantEntityIds,
                Is.EqualTo(
                    new[]
                    {
                        "KEEPER",
                        "PLAYER_B"
                    }
                )
            );
        }

        [Test]
        public void Lifecycle_MustProgressCommittedResolvingSettled()
        {
            Happening happening =
                CreateHappening();

            Assert.That(
                happening.TrySettle(7),
                Is.False
            );

            Assert.That(
                happening.TryBeginResolving(7),
                Is.True
            );

            Assert.That(
                happening.LifecycleState,
                Is.EqualTo(
                    HappeningLifecycleState
                        .Resolving
                )
            );

            Assert.That(
                happening.TryBeginResolving(7),
                Is.False
            );

            Assert.That(
                happening.TrySettle(8),
                Is.True
            );

            Assert.That(
                happening.LifecycleState,
                Is.EqualTo(
                    HappeningLifecycleState
                        .Settled
                )
            );

            Assert.That(
                happening.LifecycleTransitions.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                happening.LifecycleTransitions[0]
                    .FromState,
                Is.EqualTo(
                    HappeningLifecycleState.Committed
                )
            );

            Assert.That(
                happening.LifecycleTransitions[0]
                    .ToState,
                Is.EqualTo(
                    HappeningLifecycleState.Resolving
                )
            );

            Assert.That(
                happening.LifecycleTransitions[1]
                    .FromState,
                Is.EqualTo(
                    HappeningLifecycleState.Resolving
                )
            );

            Assert.That(
                happening.LifecycleTransitions[1]
                    .ToState,
                Is.EqualTo(
                    HappeningLifecycleState.Settled
                )
            );
        }

        [Test]
        public void Resolving_FreezesOrganizerAgendaButStillAllowsParticipantRegistration()
        {
            Happening happening =
                CreateHappening();

            Assert.That(
                happening.TryBeginResolving(7),
                Is.True
            );

            Assert.That(
                happening.TryAddContext(
                    Context(
                        "TOO_LATE",
                        HappeningContextAnchorKind
                            .Aspect,
                        "ASPECT_SATAN"
                    )
                ),
                Is.False
            );

            Assert.That(
                happening.TryAddParticipant(
                    "PLAYER_B"
                ),
                Is.True
            );

            Assert.That(
                happening.ParticipantEntityIds,
                Is.EqualTo(
                    new[]
                    {
                        "PLAYER_B"
                    }
                )
            );
        }

        [Test]
        public void SettledHappening_FreezesParticipantAndContextHistory()
        {
            Happening happening =
                CreateHappening();

            Assert.That(
                happening.TryAddParticipant(
                    "PLAYER_A"
                ),
                Is.True
            );

            Assert.That(
                happening.TryAddContext(
                    Context(
                        "CTX",
                        HappeningContextAnchorKind
                            .Aspect,
                        "ASPECT_ODIN"
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.TryBeginResolving(7),
                Is.True
            );

            Assert.That(
                happening.TrySettle(8),
                Is.True
            );

            Assert.That(
                happening.TryAddParticipant(
                    "PLAYER_B"
                ),
                Is.False
            );

            Assert.That(
                happening.TryAddContext(
                    Context(
                        "LATE_CTX",
                        HappeningContextAnchorKind
                            .Aspect,
                        "ASPECT_SATAN"
                    )
                ),
                Is.False
            );

            Assert.That(
                happening.ParticipantEntityIds.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                happening.Contexts.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void LifecycleCannotMoveBeforeCommittedTurn()
        {
            Happening happening =
                CreateHappening();

            Assert.That(
                happening.TryBeginResolving(6),
                Is.False
            );

            Assert.That(
                happening.LifecycleState,
                Is.EqualTo(
                    HappeningLifecycleState
                        .Committed
                )
            );
        }

        private static Happening
            CreateHappening()
        {
            return new Happening(
                "HAPPENING",
                "COLLECTIVE_KVLT",
                "KEEPER",
                "PROMOTE_KVLT",
                "MIDSUMMER_NIGHT",
                "NODE_HOLE",
                7,
                new CharacterActionKey(
                    "KEEPER",
                    7,
                    1
                )
            );
        }

        private static HappeningContext
            Context(
                string id,
                HappeningContextAnchorKind
                    anchorKind,
                string anchorId)
        {
            return new HappeningContext(
                id,
                $"Purpose for {id}",
                anchorKind,
                anchorId,
                "KEEPER",
                7
            );
        }
    }
}