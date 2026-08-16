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

        private readonly
            List<HappeningParticipantIntent>
            participantIntents =
                new();

        private readonly
            Dictionary<
                string,
                HappeningParticipantIntent>
            participantIntentsById =
                new(
                    StringComparer.Ordinal
                );

        private readonly
            List<HappeningIntentResolution>
            intentResolutions =
                new();

        private readonly
            Dictionary<
                string,
                HappeningIntentResolution>
            intentResolutionsByIntentId =
                new(
                    StringComparer.Ordinal
                );

        private readonly
            List<BehaviorOccurrence>
            behaviorOccurrences =
                new();

        private readonly
            Dictionary<string, BehaviorOccurrence>
            behaviorOccurrencesById =
                new(
                    StringComparer.Ordinal
                );

        private readonly
            Dictionary<string, BehaviorOccurrence>
            behaviorOccurrencesBySourceIntentId =
                new(
                    StringComparer.Ordinal
                );

        private readonly
            List<HailOccurrence>
            hailOccurrences =
                new();

        private readonly
            Dictionary<string, HailOccurrence>
            hailOccurrencesById =
                new(
                    StringComparer.Ordinal
                );

        private readonly
            Dictionary<string, HailOccurrence>
            hailOccurrencesBySourceIntentId =
                new(
                    StringComparer.Ordinal
                );

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

        public IReadOnlyList<
                HappeningParticipantIntent>
            ParticipantIntents =>
            participantIntents;

        public IReadOnlyList<
                HappeningIntentResolution>
            IntentResolutions =>
            intentResolutions;

        public IReadOnlyList<BehaviorOccurrence>
            BehaviorOccurrences =>
            behaviorOccurrences;

        public IReadOnlyList<HailOccurrence>
            HailOccurrences =>
            hailOccurrences;

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

        public bool TryGetBehaviorOccurrence(
            string behaviorOccurrenceId,
            out BehaviorOccurrence occurrence)
        {
            if (string.IsNullOrWhiteSpace(
                    behaviorOccurrenceId))
            {
                occurrence = null;
                return false;
            }

            return behaviorOccurrencesById.TryGetValue(
                behaviorOccurrenceId,
                out occurrence
            );
        }

        public bool TryGetHailOccurrence(
            string hailOccurrenceId,
            out HailOccurrence occurrence)
        {
            if (string.IsNullOrWhiteSpace(
                    hailOccurrenceId))
            {
                occurrence = null;
                return false;
            }

            return hailOccurrencesById.TryGetValue(
                hailOccurrenceId,
                out occurrence
            );
        }

        public bool TryGetHailOccurrenceBySourceIntent(
            string intentId,
            out HailOccurrence occurrence)
        {
            if (string.IsNullOrWhiteSpace(intentId))
            {
                occurrence = null;
                return false;
            }

            return hailOccurrencesBySourceIntentId.TryGetValue(
                intentId,
                out occurrence);
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

            if (intentResolutionsByIntentId.Count !=
                participantIntents.Count)
            {
                return false;
            }

            foreach (HappeningParticipantIntent intent
                     in participantIntents)
            {
                if (intent is not
                        HappeningEnactBehaviorIntent &&
                    intent is not
                        HappeningHailBehaviorIntent)
                {
                    continue;
                }

                HappeningIntentResolution resolution =
                    intentResolutionsByIntentId[
                        intent.IntentId
                    ];

                if (resolution.Outcome !=
                    HappeningIntentOutcome.Succeeded)
                {
                    continue;
                }

                bool hasFact =
                    intent is HappeningEnactBehaviorIntent
                        ? behaviorOccurrencesBySourceIntentId
                            .ContainsKey(intent.IntentId)
                        : hailOccurrencesBySourceIntentId
                            .ContainsKey(intent.IntentId);

                if (!hasFact)
                {
                    return false;
                }
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

        public bool TryRecordParticipantIntent(
            HappeningParticipantIntent intent)
        {
            if (LifecycleState !=
                HappeningLifecycleState.Committed &&
                LifecycleState !=
                HappeningLifecycleState.Resolving)
            {
                return false;
            }

            if (intent == null)
            {
                return false;
            }

            if (intent.HappeningId !=
                HappeningId)
            {
                return false;
            }

            if (intent.DeclaredTurn <
                CommittedTurn)
            {
                return false;
            }

            if (!participantEntityIdSet.Contains(
                    intent.ActorEntityId))
            {
                return false;
            }

            if (!contextIds.Contains(
                    intent.ContextId))
            {
                return false;
            }

            if (participantIntentsById.ContainsKey(
                    intent.IntentId))
            {
                return false;
            }

            if (intent is HappeningOpposeIntent
                oppose)
            {
                if (!participantIntentsById.ContainsKey(
                        oppose.TargetIntentId))
                {
                    return false;
                }
            }

            if (intent is HappeningSupportIntent
                support)
            {
                if (!participantIntentsById.ContainsKey(
                        support.TargetIntentId))
                {
                    return false;
                }
            }

            if (intent is HappeningHailBehaviorIntent hail)
            {
                if (!participantIntentsById.TryGetValue(
                        hail.TargetBehaviorIntentId,
                        out HappeningParticipantIntent target) ||
                    target is not HappeningEnactBehaviorIntent)
                {
                    return false;
                }
            }

            participantIntents.Add(
                intent
            );

            participantIntentsById.Add(
                intent.IntentId,
                intent
            );

            return true;
        }

        public bool TryGetParticipantIntent(
            string intentId,
            out HappeningParticipantIntent intent)
        {
            if (string.IsNullOrWhiteSpace(
                    intentId))
            {
                intent = null;
                return false;
            }

            return participantIntentsById.TryGetValue(
                intentId,
                out intent
            );
        }

        public bool TryRecordIntentResolution(
            HappeningIntentResolution resolution)
        {
            if (LifecycleState !=
                HappeningLifecycleState.Resolving)
            {
                return false;
            }

            if (resolution == null)
            {
                return false;
            }

            if (!participantIntentsById.TryGetValue(
                    resolution.IntentId,
                    out HappeningParticipantIntent intent))
            {
                return false;
            }

            if (resolution.ResolvedTurn <
                intent.DeclaredTurn)
            {
                return false;
            }

            if (intentResolutionsByIntentId.ContainsKey(
                    resolution.IntentId))
            {
                return false;
            }

            if (resolution.Outcome ==
                HappeningIntentOutcome.Blocked)
            {
                if (!participantIntentsById.TryGetValue(
                        resolution.OpposingIntentId,
                        out HappeningParticipantIntent
                            opposingIntent))
                {
                    return false;
                }

                if (opposingIntent is not
                    HappeningOpposeIntent oppose)
                {
                    return false;
                }

                if (oppose.TargetIntentId !=
                    resolution.IntentId)
                {
                    return false;
                }
            }

            intentResolutions.Add(
                resolution
            );

            intentResolutionsByIntentId.Add(
                resolution.IntentId,
                resolution
            );

            return true;
        }

        public bool TryGetIntentResolution(
            string intentId,
            out HappeningIntentResolution resolution)
        {
            if (string.IsNullOrWhiteSpace(
                    intentId))
            {
                resolution = null;
                return false;
            }

            return intentResolutionsByIntentId.TryGetValue(
                intentId,
                out resolution
            );
        }

        public bool TryMaterializeSuccessfulEnactBehavior(
            string intentId,
            string behaviorOccurrenceId,
            string hailOccurrenceId = null)
        {
            if (LifecycleState !=
                HappeningLifecycleState.Resolving)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    intentId) ||
                string.IsNullOrWhiteSpace(
                    behaviorOccurrenceId))
            {
                return false;
            }

            if (!participantIntentsById.TryGetValue(
                    intentId,
                    out HappeningParticipantIntent
                        participantIntent))
            {
                return false;
            }

            if (participantIntent is not
                HappeningEnactBehaviorIntent intent)
            {
                return false;
            }

            if (!intentResolutionsByIntentId.TryGetValue(
                    intent.IntentId,
                    out HappeningIntentResolution
                        resolution))
            {
                return false;
            }

            if (resolution.Outcome !=
                HappeningIntentOutcome.Succeeded)
            {
                return false;
            }

            if (behaviorOccurrencesBySourceIntentId
                .ContainsKey(
                    intent.IntentId))
            {
                return false;
            }

            string normalizedBehaviorId =
                behaviorOccurrenceId.Trim();

            if (behaviorOccurrencesById.ContainsKey(
                    normalizedBehaviorId))
            {
                return false;
            }

            bool intentHasHail =
                intent.HasIntendedHail;

            bool suppliedHailId =
                !string.IsNullOrWhiteSpace(
                    hailOccurrenceId
                );

            if (intentHasHail != suppliedHailId)
            {
                return false;
            }

            string normalizedHailId =
                suppliedHailId
                    ? hailOccurrenceId.Trim()
                    : null;

            if (normalizedHailId != null &&
                hailOccurrencesById.ContainsKey(
                    normalizedHailId))
            {
                return false;
            }

            BehaviorOccurrence behavior =
                new BehaviorOccurrence(
                    normalizedBehaviorId,
                    HappeningId,
                    intent.ContextId,
                    new[]
                    {
                        intent.ActorEntityId
                    },
                    intent.BehaviorTypeId,
                    intent.Axis,
                    intent.Pole,
                    intent.IntendedDegree,
                    intent.IntentId,
                    resolution.ResolvedTurn
                );

            HailOccurrence hail =
                null;

            if (intentHasHail)
            {
                hail =
                    new HailOccurrence(
                        normalizedHailId,
                        behavior.BehaviorOccurrenceId,
                        intent.ActorEntityId,
                        intent.IntendedHailedAspectId
                    );
            }

            // Only mutate after every validation and
            // both factual records have been constructed.

            behaviorOccurrences.Add(
                behavior
            );

            behaviorOccurrencesById.Add(
                behavior.BehaviorOccurrenceId,
                behavior
            );

            behaviorOccurrencesBySourceIntentId.Add(
                intent.IntentId,
                behavior
            );

            if (hail != null)
            {
                hailOccurrences.Add(
                    hail
                );

                hailOccurrencesById.Add(
                    hail.HailOccurrenceId,
                    hail
                );

                hailOccurrencesBySourceIntentId.Add(
                    intent.IntentId,
                    hail
                );
            }

            return true;
        }

        public bool TryMaterializeSuccessfulHailBehavior(
            string intentId,
            string hailOccurrenceId)
        {
            if (LifecycleState !=
                HappeningLifecycleState.Resolving ||
                string.IsNullOrWhiteSpace(intentId) ||
                string.IsNullOrWhiteSpace(hailOccurrenceId))
            {
                return false;
            }

            if (!participantIntentsById.TryGetValue(
                    intentId,
                    out HappeningParticipantIntent source) ||
                source is not HappeningHailBehaviorIntent hailIntent)
            {
                return false;
            }

            if (!intentResolutionsByIntentId.TryGetValue(
                    intentId,
                    out HappeningIntentResolution resolution) ||
                resolution.Outcome !=
                    HappeningIntentOutcome.Succeeded)
            {
                return false;
            }

            if (!behaviorOccurrencesBySourceIntentId.TryGetValue(
                    hailIntent.TargetBehaviorIntentId,
                    out BehaviorOccurrence targetBehavior))
            {
                return false;
            }

            if (hailOccurrencesBySourceIntentId.ContainsKey(
                    intentId))
            {
                return false;
            }

            string normalizedHailId =
                hailOccurrenceId.Trim();

            if (hailOccurrencesById.ContainsKey(normalizedHailId))
                return false;

            HailOccurrence hail = new HailOccurrence(
                normalizedHailId,
                targetBehavior.BehaviorOccurrenceId,
                hailIntent.ActorEntityId,
                hailIntent.HailedAspectId);

            hailOccurrences.Add(hail);
            hailOccurrencesById.Add(
                hail.HailOccurrenceId,
                hail);
            hailOccurrencesBySourceIntentId.Add(
                intentId,
                hail);

            return true;
        }
    }
}
