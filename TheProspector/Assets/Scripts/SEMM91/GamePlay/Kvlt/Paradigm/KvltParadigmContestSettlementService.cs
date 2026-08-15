using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Events;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class
        KvltParadigmContestSettlementService
    {
        public IReadOnlyList<
                KvltParadigmContestResult>
            Settle(
                string happeningId,
                IReadOnlyList<
                    BehaviorOccurrence>
                    behaviorOccurrences,
                IReadOnlyList<
                    HailOccurrence>
                    hailOccurrences,
                KvltParadigmOpposition opposition,
                string keeperEntityId,
                KvltParadigmState state,
                int settledTurn)
        {
            happeningId =
                RequireText(
                    happeningId,
                    nameof(happeningId)
                );

            if (behaviorOccurrences == null)
            {
                throw new ArgumentNullException(
                    nameof(behaviorOccurrences)
                );
            }

            if (hailOccurrences == null)
            {
                throw new ArgumentNullException(
                    nameof(hailOccurrences)
                );
            }

            if (opposition == null)
            {
                throw new ArgumentNullException(
                    nameof(opposition)
                );
            }

            if (state == null)
            {
                throw new ArgumentNullException(
                    nameof(state)
                );
            }

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            keeperEntityId =
                keeperEntityId?.Trim() ??
                string.Empty;

            Dictionary<string, BehaviorOccurrence>
                behaviorById =
                    new(StringComparer.Ordinal);

            foreach (
                BehaviorOccurrence behavior
                in behaviorOccurrences)
            {
                if (behavior == null)
                {
                    throw new ArgumentException(
                        "Behavior collection contains null.",
                        nameof(behaviorOccurrences)
                    );
                }

                if (behavior.HappeningId !=
                    happeningId)
                {
                    throw new ArgumentException(
                        "Behavior belongs to another " +
                        "Happening.",
                        nameof(behaviorOccurrences)
                    );
                }

                if (behavior.ResolvedTurn >
                    settledTurn)
                {
                    throw new ArgumentException(
                        "Behavior resolves after the " +
                        "contest settlement turn.",
                        nameof(behaviorOccurrences)
                    );
                }

                if (!behaviorById.TryAdd(
                        behavior.BehaviorOccurrenceId,
                        behavior))
                {
                    throw new ArgumentException(
                        "Behavior identity is duplicated.",
                        nameof(behaviorOccurrences)
                    );
                }
            }

            Dictionary<
                    string,
                    Dictionary<string, HailOccurrence>>
                firstSideByBehavior =
                    new(StringComparer.Ordinal);

            Dictionary<
                    string,
                    Dictionary<string, HailOccurrence>>
                secondSideByBehavior =
                    new(StringComparer.Ordinal);

            List<HailOccurrence> orderedHails =
                new(hailOccurrences);

            orderedHails.Sort(
                (left, right) =>
                    string.CompareOrdinal(
                        left?.HailOccurrenceId,
                        right?.HailOccurrenceId
                    )
            );

            foreach (
                HailOccurrence hail
                in orderedHails)
            {
                if (hail == null)
                {
                    throw new ArgumentException(
                        "Hail collection contains null.",
                        nameof(hailOccurrences)
                    );
                }

                if (!behaviorById.ContainsKey(
                        hail.BehaviorOccurrenceId))
                {
                    throw new ArgumentException(
                        "Hail references an unknown " +
                        "BehaviorOccurrence.",
                        nameof(hailOccurrences)
                    );
                }

                if (!opposition.Contains(
                        hail.HailedAspectId))
                {
                    continue;
                }

                /*
                 * Every actual Hail reinforces Grip,
                 * including losing and minority Hails.
                 */
                state.RecordGrip(
                    hail.DeclarerEntityId,
                    hail.HailedAspectId,
                    hail.HailOccurrenceId,
                    settledTurn
                );

                Dictionary<
                    string,
                    Dictionary<string, HailOccurrence>>
                    targetSide =
                        hail.HailedAspectId ==
                        opposition.FirstHailAspectId
                            ? firstSideByBehavior
                            : secondSideByBehavior;

                if (!targetSide.TryGetValue(
                        hail.BehaviorOccurrenceId,
                        out Dictionary<
                            string,
                            HailOccurrence>
                            sideParticipants))
                {
                    sideParticipants =
                        new Dictionary<
                            string,
                            HailOccurrence>(
                                StringComparer.Ordinal
                            );

                    targetSide.Add(
                        hail.BehaviorOccurrenceId,
                        sideParticipants
                    );
                }

                /*
                 * Social support is headcount-based.
                 * Repeated Hails reinforce Grip but do
                 * not duplicate the same participant.
                 */
                sideParticipants.TryAdd(
                    hail.DeclarerEntityId,
                    hail
                );
            }

            SortedSet<string> contestedBehaviorIds =
                new(StringComparer.Ordinal);

            foreach (string behaviorId
                     in firstSideByBehavior.Keys)
            {
                contestedBehaviorIds.Add(behaviorId);
            }

            foreach (string behaviorId
                     in secondSideByBehavior.Keys)
            {
                contestedBehaviorIds.Add(behaviorId);
            }

            List<KvltParadigmContestResult>
                results =
                    new();

            foreach (string behaviorId
                     in contestedBehaviorIds)
            {
                firstSideByBehavior.TryGetValue(
                    behaviorId,
                    out Dictionary<
                        string,
                        HailOccurrence>
                        firstSide
                );

                secondSideByBehavior.TryGetValue(
                    behaviorId,
                    out Dictionary<
                        string,
                        HailOccurrence>
                        secondSide
                );

                int firstCount =
                    firstSide?.Count ?? 0;

                int secondCount =
                    secondSide?.Count ?? 0;

                /*
                 * A single paradigm assertion is not
                 * a contest.
                 */
                if (firstCount == 0 ||
                    secondCount == 0)
                {
                    continue;
                }

                BehaviorOccurrence behavior =
                    behaviorById[behaviorId];

                int strong =
                    Math.Max(
                        firstCount,
                        secondCount
                    );

                int weak =
                    Math.Min(
                        firstCount,
                        secondCount
                    );

                /*
                 * Inclusive two-thirds threshold:
                 *
                 * weak / strong >= 2 / 3
                 * <=> 3 * weak >= 2 * strong
                 */
                bool isBeef =
                    3L * weak >=
                    2L * strong;

                if (isBeef)
                {
                    state.RecordBeef(
                        opposition,
                        behavior.BehaviorTypeId,
                        happeningId,
                        behaviorId,
                        settledTurn
                    );

                    results.Add(
                        new KvltParadigmContestResult(
                            happeningId,
                            behaviorId,
                            behavior.BehaviorTypeId,
                            opposition,
                            firstCount,
                            secondCount,
                            isBeef: true,
                            winningHailAspectId:
                                string.Empty,
                            losingHailAspectId:
                                string.Empty,
                            sourceNewPoserDeclarations:
                                Array.Empty<
                                    KvltPoserDeclaration>()
                        )
                    );

                    continue;
                }

                bool firstSideWon =
                    firstCount >
                    secondCount;

                string winningAspectId =
                    firstSideWon
                        ? opposition
                            .FirstHailAspectId
                        : opposition
                            .SecondHailAspectId;

                string losingAspectId =
                    firstSideWon
                        ? opposition
                            .SecondHailAspectId
                        : opposition
                            .FirstHailAspectId;

                Dictionary<string, HailOccurrence>
                    losingSide =
                        firstSideWon
                            ? secondSide
                            : firstSide;

                List<string> eligibleLosers =
                    new();

                int incomingTurn =
                    settledTurn + 1;

                foreach (string entityId
                         in losingSide.Keys)
                {
                    if (entityId ==
                        keeperEntityId)
                    {
                        continue;
                    }

                    if (state.HasActivePoserdom(
                            entityId,
                            incomingTurn))
                    {
                        continue;
                    }

                    eligibleLosers.Add(entityId);
                }

                eligibleLosers.Sort(
                    StringComparer.Ordinal
                );

                List<KvltPoserDeclaration>
                    declarations =
                        new();

                if (eligibleLosers.Count > 0)
                {
                    int durationTurns =
                        (4 +
                         eligibleLosers.Count -
                         1) /
                        eligibleLosers.Count;

                    foreach (string entityId
                             in eligibleLosers)
                    {
                        KvltPoserDeclaration
                            declaration =
                                new(
                                    declarationId:
                                        $"POSER|" +
                                        $"{happeningId}|" +
                                        $"{behaviorId}|" +
                                        $"{entityId}",
                                    entityId:
                                        entityId,
                                    sourceHappeningId:
                                        happeningId,
                                    sourceBehaviorOccurrenceId:
                                        behaviorId,
                                    losingHailAspectId:
                                        losingAspectId,
                                    winningHailAspectId:
                                        winningAspectId,
                                    declaredDuringTurn:
                                        settledTurn,
                                    durationTurns:
                                        durationTurns
                                );

                        if (!state.TryDeclarePoser(
                                declaration))
                        {
                            throw new
                                InvalidOperationException(
                                    "Eligible Poser declaration " +
                                    "was rejected."
                                );
                        }

                        declarations.Add(
                            declaration
                        );
                    }
                }

                results.Add(
                    new KvltParadigmContestResult(
                        happeningId,
                        behaviorId,
                        behavior.BehaviorTypeId,
                        opposition,
                        firstCount,
                        secondCount,
                        isBeef: false,
                        winningHailAspectId:
                            winningAspectId,
                        losingHailAspectId:
                            losingAspectId,
                        sourceNewPoserDeclarations:
                            declarations
                    )
                );
            }

            return results.ToArray();
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Settlement provenance cannot " +
                    "be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}