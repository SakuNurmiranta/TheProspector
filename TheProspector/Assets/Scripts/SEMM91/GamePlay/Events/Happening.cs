using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Actions.History;

namespace SEMM91.GamePlay.Events
{
    public sealed class Happening
    {
        private readonly
            List<string>
                participantEntityIds =
                    new();

        private readonly
            HashSet<string>
                participantEntityIdSet =
                    new(
                        StringComparer.Ordinal
                    );

        private readonly
            List<HappeningContext>
                contexts =
                    new();

        private readonly
            HashSet<string>
                contextIds =
                    new(
                        StringComparer.Ordinal
                    );

        private readonly
            List<HappeningLifecycleTransition>
                lifecycleTransitions =
                    new();

        public string HappeningId { get; }

        public string OwningCollectiveId { get; }

        public string InstigatorEntityId { get; }

        public string Cause { get; }

        public string Crux { get; }

        public string AnchorPhysicalNodeId { get; }

        public int CommittedTurn { get; }

        public CharacterActionKey
            SourcePromotionActionKey { get; }

        public HappeningLifecycleState
            LifecycleState { get; private set; }

        public IReadOnlyList<string>
            ParticipantEntityIds =>
                participantEntityIds;

        public IReadOnlyList<HappeningContext>
            Contexts =>
                contexts;

        public IReadOnlyList<
            HappeningLifecycleTransition>
            LifecycleTransitions =>
                lifecycleTransitions;

        public Happening(
            string happeningId,
            string owningCollectiveId,
            string instigatorEntityId,
            string cause,
            string crux,
            string anchorPhysicalNodeId,
            int committedTurn,
            CharacterActionKey
                sourcePromotionActionKey)
        {
            HappeningId =
                RequireText(
                    happeningId,
                    nameof(happeningId)
                );

            OwningCollectiveId =
                RequireText(
                    owningCollectiveId,
                    nameof(owningCollectiveId)
                );

            InstigatorEntityId =
                RequireText(
                    instigatorEntityId,
                    nameof(instigatorEntityId)
                );

            Cause =
                RequireText(
                    cause,
                    nameof(cause)
                );

            Crux =
                RequireText(
                    crux,
                    nameof(crux)
                );

            AnchorPhysicalNodeId =
                RequireText(
                    anchorPhysicalNodeId,
                    nameof(anchorPhysicalNodeId)
                );

            if (committedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(committedTurn),
                    committedTurn,
                    "Happening commit turn cannot be negative."
                );
            }

            if (string.IsNullOrWhiteSpace(
                    sourcePromotionActionKey
                        .CharacterEntityId))
            {
                throw new ArgumentException(
                    "Happening requires valid Promotion " +
                    "action provenance.",
                    nameof(sourcePromotionActionKey)
                );
            }

            if (sourcePromotionActionKey
                    .CharacterEntityId !=
                InstigatorEntityId)
            {
                throw new ArgumentException(
                    "Promotion action provenance must " +
                    "belong to the Happening instigator.",
                    nameof(sourcePromotionActionKey)
                );
            }

            if (sourcePromotionActionKey.GlobalTurn !=
                committedTurn)
            {
                throw new ArgumentException(
                    "Promotion action turn must equal " +
                    "the Happening committed turn.",
                    nameof(sourcePromotionActionKey)
                );
            }

            CommittedTurn =
                committedTurn;

            SourcePromotionActionKey =
                sourcePromotionActionKey;

            LifecycleState =
                HappeningLifecycleState.Committed;
        }

        public bool TryAddParticipant(
            string entityId)
        {
            if (LifecycleState ==
                HappeningLifecycleState.Settled)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    entityId))
            {
                return false;
            }

            string normalized =
                entityId.Trim();

            if (!participantEntityIdSet.Add(
                    normalized))
            {
                return false;
            }

            participantEntityIds.Add(
                normalized
            );

            return true;
        }

        public bool TryAddContext(
            HappeningContext context)
        {
            if (LifecycleState !=
                HappeningLifecycleState.Committed)
            {
                return false;
            }

            if (context == null)
            {
                return false;
            }

            // Peak 2: original instigator remains
            // the sole agenda author.

            if (context.AuthorEntityId !=
                InstigatorEntityId)
            {
                return false;
            }

            if (context.AuthoredTurn <
                CommittedTurn)
            {
                return false;
            }

            if (!contextIds.Add(
                    context.ContextId))
            {
                return false;
            }

            contexts.Add(
                context
            );

            return true;
        }

        public bool TryBeginResolving(
            int globalTurn)
        {
            if (LifecycleState !=
                HappeningLifecycleState.Committed)
            {
                return false;
            }

            if (globalTurn < CommittedTurn)
            {
                return false;
            }

            TransitionTo(
                HappeningLifecycleState.Resolving,
                globalTurn
            );

            return true;
        }

        public bool TrySettle(
            int globalTurn)
        {
            if (LifecycleState !=
                HappeningLifecycleState.Resolving)
            {
                return false;
            }

            if (lifecycleTransitions.Count == 0)
            {
                return false;
            }

            int resolvingTurn =
                lifecycleTransitions[
                    lifecycleTransitions.Count - 1
                ].GlobalTurn;

            if (globalTurn < resolvingTurn)
            {
                return false;
            }

            TransitionTo(
                HappeningLifecycleState.Settled,
                globalTurn
            );

            return true;
        }

        private void TransitionTo(
            HappeningLifecycleState target,
            int globalTurn)
        {
            HappeningLifecycleState previous =
                LifecycleState;

            LifecycleState =
                target;

            lifecycleTransitions.Add(
                new HappeningLifecycleTransition(
                    previous,
                    target,
                    globalTurn
                )
            );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Happening identity/provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}