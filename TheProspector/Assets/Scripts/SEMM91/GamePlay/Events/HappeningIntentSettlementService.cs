using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Events
{
    /// <summary>
    /// Minimal deterministic Peak-2 settlement for
    /// ordinary participant Intents.
    ///
    /// This is deliberately not the future stat-based
    /// Happening conflict system.
    ///
    /// More specific authoritative outcomes may already
    /// exist. Those are preserved.
    ///
    /// For unresolved primary Intents:
    ///
    /// direct Oppose > direct Support
    ///     => Blocked
    ///
    /// otherwise
    ///     => Succeeded
    ///
    /// Oppose and Support Intents themselves resolve as
    /// successful declarations.
    /// </summary>
    public sealed class
        HappeningIntentSettlementService
    {
        public IReadOnlyList<
                HappeningIntentResolution>
            Settle(
                Happening happening,
                int globalTurn)
        {
            if (happening == null)
            {
                throw new ArgumentNullException(
                    nameof(happening)
                );
            }

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            if (happening.LifecycleState !=
                HappeningLifecycleState.Resolving)
            {
                throw new InvalidOperationException(
                    "Ordinary Happening Intent settlement " +
                    "requires Resolving lifecycle | " +
                    $"happening={happening.HappeningId} | " +
                    $"state={happening.LifecycleState}"
                );
            }

            if (globalTurn <
                happening.CommittedTurn)
            {
                throw new InvalidOperationException(
                    "Happening Intent settlement cannot " +
                    "precede the Happening commit turn."
                );
            }

            List<HappeningParticipantIntent>
                orderedIntents =
                    new(
                        happening.ParticipantIntents
                    );

            orderedIntents.Sort(
                (
                    left,
                    right
                ) =>
                    string.CompareOrdinal(
                        left.IntentId,
                        right.IntentId
                    )
            );

            foreach (
                HappeningParticipantIntent intent
                in orderedIntents)
            {
                if (intent.DeclaredTurn >
                    globalTurn)
                {
                    throw new InvalidOperationException(
                        "Happening contains an Intent " +
                        "declared after the turn currently " +
                        "being settled | " +
                        $"happening={happening.HappeningId} | " +
                        $"intent={intent.IntentId} | " +
                        $"declared={intent.DeclaredTurn} | " +
                        $"settled={globalTurn}"
                    );
                }

                /*
                 * A more specific authoritative resolver
                 * may already have produced Succeeded,
                 * Blocked or Failed.
                 *
                 * Never overwrite it.
                 */
                if (happening.TryGetIntentResolution(
                        intent.IntentId,
                        out _))
                {
                    continue;
                }

                HappeningIntentResolution resolution =
                    ResolveFallback(
                        happening,
                        intent,
                        globalTurn
                    );

                if (!happening.TryRecordIntentResolution(
                        resolution))
                {
                    throw new InvalidOperationException(
                        "Authoritative Happening Intent " +
                        "resolution could not be recorded | " +
                        $"happening={happening.HappeningId} | " +
                        $"intent={intent.IntentId}"
                    );
                }
            }

            /*
             * Return the authoritative domain records,
             * not a parallel result population.
             */
            return happening
                .IntentResolutions;
        }

        private static
            HappeningIntentResolution
            ResolveFallback(
                Happening happening,
                HappeningParticipantIntent intent,
                int globalTurn)
        {
            switch (intent.Kind)
            {
                case HappeningIntentKind.Perform:
                case HappeningIntentKind.EnactBehavior:
                    return ResolvePrimaryIntent(
                        happening,
                        intent,
                        globalTurn
                    );

                case HappeningIntentKind.Oppose:
                case HappeningIntentKind.Support:
                    return new
                        HappeningIntentResolution(
                            intent.IntentId,
                            HappeningIntentOutcome.Succeeded,
                            globalTurn
                        );

                default:
                    throw new InvalidOperationException(
                        "Unsupported Happening Intent kind | " +
                        $"intent={intent.IntentId} | " +
                        $"kind={intent.Kind}"
                    );
            }
        }

        private static
            HappeningIntentResolution
            ResolvePrimaryIntent(
                Happening happening,
                HappeningParticipantIntent target,
                int globalTurn)
        {
            List<HappeningOpposeIntent>
                oppositions =
                    new();

            int supportCount =
                0;

            foreach (
                HappeningParticipantIntent candidate
                in happening.ParticipantIntents)
            {
                if (candidate is
                        HappeningOpposeIntent oppose &&
                    oppose.TargetIntentId ==
                        target.IntentId)
                {
                    oppositions.Add(
                        oppose
                    );

                    continue;
                }

                if (candidate is
                        HappeningSupportIntent support &&
                    support.TargetIntentId ==
                        target.IntentId)
                {
                    supportCount++;
                }
            }

            oppositions.Sort(
                (
                    left,
                    right
                ) =>
                    string.CompareOrdinal(
                        left.IntentId,
                        right.IntentId
                    )
            );

            if (oppositions.Count >
                supportCount)
            {
                /*
                 * Opposition provenance is deterministic
                 * even if network arrival order differed.
                 */
                return new
                    HappeningIntentResolution(
                        target.IntentId,
                        HappeningIntentOutcome.Blocked,
                        globalTurn,
                        oppositions[0].IntentId
                    );
            }

            return new
                HappeningIntentResolution(
                    target.IntentId,
                    HappeningIntentOutcome.Succeeded,
                    globalTurn
                );
        }
    }
}
