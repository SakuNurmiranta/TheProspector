using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Events;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltParadigmContestSettlementService
    {
        public IReadOnlyList<KvltParadigmContestResult>
            SettleAll(
                int settledTurn,
                IReadOnlyList<Happening> happenings,
                IReadOnlyList<KvltParadigmOpposition>
                    oppositions,
                string activeKeeperEntityId,
                KvltParadigmState state)
        {
            if (settledTurn < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn));
            if (happenings == null)
                throw new ArgumentNullException(nameof(happenings));
            if (oppositions == null)
                throw new ArgumentNullException(nameof(oppositions));
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            List<KvltParadigmContestResult> results =
                new();
            HashSet<string> reinforcedHails =
                new(StringComparer.Ordinal);

            foreach (Happening happening in happenings)
            {
                if (happening == null)
                    throw new ArgumentException(
                        "Happening population cannot contain null.",
                        nameof(happenings));

                if (happening.LifecycleState !=
                    HappeningLifecycleState.Resolving)
                    throw new ArgumentException(
                        "Paradigm settlement requires a resolving Happening.",
                        nameof(happenings));

                Dictionary<string, BehaviorOccurrence>
                    behaviorById = BuildBehaviorLookup(happening);

                foreach (HailOccurrence hail
                         in happening.HailOccurrences)
                {
                    if (!behaviorById.ContainsKey(
                            hail.BehaviorOccurrenceId))
                        throw new InvalidOperationException(
                            "Hail points to a missing factual Behavior.");

                    if (reinforcedHails.Add(
                            hail.HailOccurrenceId))
                    {
                        state.ReinforceGrip(
                            hail.DeclarerEntityId,
                            hail.HailedAspectId,
                            hail.HailOccurrenceId,
                            settledTurn);
                    }
                }

                foreach (BehaviorOccurrence behavior
                         in happening.BehaviorOccurrences)
                {
                    foreach (KvltParadigmOpposition opposition
                             in oppositions)
                    {
                        if (opposition == null)
                            throw new ArgumentException(
                                "Opposition population cannot contain null.",
                                nameof(oppositions));

                        KvltParadigmContestResult result =
                            SettleBehavior(
                                settledTurn,
                                happening,
                                behavior,
                                opposition,
                                activeKeeperEntityId,
                                state);

                        if (result != null)
                            results.Add(result);
                    }
                }
            }

            return results.ToArray();
        }

        public IReadOnlyList<KvltParadigmContestResult>
            Settle(
                int settledTurn,
                IReadOnlyList<Happening> happenings,
                IReadOnlyList<KvltParadigmOpposition>
                    oppositions,
                string activeKeeperEntityId,
                KvltParadigmState state)
        {
            return SettleAll(
                settledTurn,
                happenings,
                oppositions,
                activeKeeperEntityId,
                state);
        }

        public KvltParadigmContestResult Settle(
            int settledTurn,
            Happening happening,
            BehaviorOccurrence behavior,
            KvltParadigmOpposition opposition,
            string activeKeeperEntityId,
            KvltParadigmState state)
        {
            if (happening == null)
                throw new ArgumentNullException(nameof(happening));
            if (behavior == null)
                throw new ArgumentNullException(nameof(behavior));
            if (opposition == null)
                throw new ArgumentNullException(nameof(opposition));
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            foreach (HailOccurrence hail in happening.HailOccurrences)
            {
                if (hail.BehaviorOccurrenceId ==
                    behavior.BehaviorOccurrenceId &&
                    opposition.Contains(hail.HailedAspectId))
                {
                    state.ReinforceGrip(
                        hail.DeclarerEntityId,
                        hail.HailedAspectId,
                        hail.HailOccurrenceId,
                        settledTurn);
                }
            }

            return SettleBehavior(
                settledTurn,
                happening,
                behavior,
                opposition,
                activeKeeperEntityId,
                state);
        }

        public KvltParadigmContestResult Settle(
            int settledTurn,
            string happeningId,
            BehaviorOccurrence behavior,
            IReadOnlyList<HailOccurrence> hails,
            KvltParadigmOpposition opposition,
            string activeKeeperEntityId,
            KvltParadigmState state)
        {
            if (hails == null)
                throw new ArgumentNullException(nameof(hails));
            if (behavior == null)
                throw new ArgumentNullException(nameof(behavior));
            if (opposition == null)
                throw new ArgumentNullException(nameof(opposition));
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            foreach (HailOccurrence hail in hails)
            {
                if (hail == null)
                    throw new ArgumentException(
                        "Hail population cannot contain null.",
                        nameof(hails));

                if (hail.BehaviorOccurrenceId ==
                        behavior.BehaviorOccurrenceId &&
                    opposition.Contains(hail.HailedAspectId))
                {
                    state.ReinforceGrip(
                        hail.DeclarerEntityId,
                        hail.HailedAspectId,
                        hail.HailOccurrenceId,
                        settledTurn);
                }
            }

            return SettleBehaviorCore(
                settledTurn,
                happeningId,
                behavior,
                hails,
                opposition,
                activeKeeperEntityId,
                state);
        }

        // Entry 40 exposed happening identity before the boundary turn.
        // Keep that call surface as a compatibility seam while the
        // authoritative implementation remains the int-first overload.
        public KvltParadigmContestResult Settle(
            string happeningId,
            int settledTurn,
            BehaviorOccurrence behavior,
            IReadOnlyList<HailOccurrence> hails,
            KvltParadigmOpposition opposition,
            string activeKeeperEntityId,
            KvltParadigmState state)
        {
            return Settle(
                settledTurn,
                happeningId,
                behavior,
                hails,
                opposition,
                activeKeeperEntityId,
                state);
        }

        public KvltParadigmContestResult Settle(
            string happeningId,
            BehaviorOccurrence behavior,
            IReadOnlyList<HailOccurrence> hails,
            KvltParadigmOpposition opposition,
            string activeKeeperEntityId,
            int settledTurn,
            KvltParadigmState state)
        {
            return Settle(
                settledTurn,
                happeningId,
                behavior,
                hails,
                opposition,
                activeKeeperEntityId,
                state);
        }

        public KvltParadigmContestResult Settle(
            string happeningId,
            BehaviorOccurrence behavior,
            IReadOnlyList<HailOccurrence> hails,
            int settledTurn,
            KvltParadigmOpposition opposition,
            string activeKeeperEntityId,
            KvltParadigmState state)
        {
            return Settle(
                settledTurn,
                happeningId,
                behavior,
                hails,
                opposition,
                activeKeeperEntityId,
                state);
        }

        public KvltParadigmContestResult Settle(
            int settledTurn,
            BehaviorOccurrence behavior,
            IReadOnlyList<HailOccurrence> hails,
            KvltParadigmOpposition opposition,
            string activeKeeperEntityId,
            KvltParadigmState state)
        {
            return Settle(
                settledTurn,
                behavior?.HappeningId,
                behavior,
                hails,
                opposition,
                activeKeeperEntityId,
                state);
        }

        public KvltParadigmContestResult Settle(
            KvltParadigmState state,
            int settledTurn,
            BehaviorOccurrence behavior,
            IReadOnlyList<HailOccurrence> hails,
            KvltParadigmOpposition opposition,
            string activeKeeperEntityId)
        {
            return Settle(
                settledTurn,
                behavior,
                hails,
                opposition,
                activeKeeperEntityId,
                state);
        }

        public KvltParadigmContestResult Settle(
            int settledTurn,
            KvltParadigmOpposition opposition,
            BehaviorOccurrence behavior,
            IReadOnlyList<HailOccurrence> hails,
            string activeKeeperEntityId,
            KvltParadigmState state)
        {
            return Settle(
                settledTurn,
                behavior,
                hails,
                opposition,
                activeKeeperEntityId,
                state);
        }

        public KvltParadigmContestResult Settle(
            KvltParadigmState state,
            int settledTurn,
            KvltParadigmOpposition opposition,
            BehaviorOccurrence behavior,
            IReadOnlyList<HailOccurrence> hails,
            string activeKeeperEntityId)
        {
            return Settle(
                settledTurn,
                behavior,
                hails,
                opposition,
                activeKeeperEntityId,
                state);
        }

        private static KvltParadigmContestResult
            SettleBehavior(
                int settledTurn,
                Happening happening,
                BehaviorOccurrence behavior,
                KvltParadigmOpposition opposition,
                string activeKeeperEntityId,
                KvltParadigmState state)
        {
            List<HailOccurrence> relevant = new();

            foreach (HailOccurrence hail
                     in happening.HailOccurrences)
            {
                if (hail.BehaviorOccurrenceId ==
                    behavior.BehaviorOccurrenceId)
                    relevant.Add(hail);
            }

            return SettleBehaviorCore(
                settledTurn,
                happening.HappeningId,
                behavior,
                relevant,
                opposition,
                activeKeeperEntityId,
                state);
        }

        private static KvltParadigmContestResult
            SettleBehaviorCore(
                int settledTurn,
                string happeningId,
                BehaviorOccurrence behavior,
                IReadOnlyList<HailOccurrence> hails,
                KvltParadigmOpposition opposition,
                string activeKeeperEntityId,
                KvltParadigmState state)
        {
            if (settledTurn < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn));
            if (string.IsNullOrWhiteSpace(happeningId))
                throw new ArgumentException(
                    "Happening identity cannot be empty.",
                    nameof(happeningId));
            if (behavior == null)
                throw new ArgumentNullException(nameof(behavior));

            List<HailOccurrence> first = new();
            List<HailOccurrence> second = new();

            foreach (HailOccurrence hail
                     in hails)
            {
                if (hail.BehaviorOccurrenceId !=
                    behavior.BehaviorOccurrenceId)
                    continue;

                if (hail.HailedAspectId ==
                    opposition.FirstHailAspectId)
                    first.Add(hail);
                else if (hail.HailedAspectId ==
                    opposition.SecondHailAspectId)
                    second.Add(hail);
            }

            if (first.Count == 0 || second.Count == 0)
                return null;

            int strong = Math.Max(first.Count, second.Count);
            int weak = Math.Min(first.Count, second.Count);
            bool isBeef = 3 * weak >= 2 * strong;

            if (isBeef)
            {
                state.RecordOrReinforceBeef(
                    opposition,
                    behavior.BehaviorTypeId,
                    happeningId,
                    behavior.BehaviorOccurrenceId,
                    settledTurn);

                return new KvltParadigmContestResult(
                    settledTurn,
                    happeningId,
                    behavior,
                    opposition,
                    first.Count,
                    second.Count,
                    true,
                    string.Empty,
                    string.Empty,
                    Array.Empty<KvltPoserDeclaration>());
            }

            List<HailOccurrence> losing =
                first.Count < second.Count ? first : second;
            string losingAspect = losing[0].HailedAspectId;
            string winningAspect =
                opposition.GetOpposingAspectId(losingAspect);

            List<string> eligibleLosers = new();
            HashSet<string> seen =
                new(StringComparer.Ordinal);
            int incomingTurn = settledTurn + 1;

            foreach (HailOccurrence hail in losing)
            {
                if (!seen.Add(hail.DeclarerEntityId) ||
                    hail.DeclarerEntityId ==
                        activeKeeperEntityId ||
                    state.HasActivePoserdom(
                        hail.DeclarerEntityId,
                        incomingTurn))
                    continue;

                eligibleLosers.Add(hail.DeclarerEntityId);
            }

            int duration = eligibleLosers.Count == 0
                ? 0
                : (4 + eligibleLosers.Count - 1) /
                  eligibleLosers.Count;

            List<KvltPoserDeclaration> created = new();

            foreach (string entityId in eligibleLosers)
            {
                KvltPoserDeclaration declaration =
                    new KvltPoserDeclaration(
                        $"POSER:{happeningId}:" +
                        $"{behavior.BehaviorOccurrenceId}:" +
                        $"{entityId}:{settledTurn}",
                        entityId,
                        happeningId,
                        behavior.BehaviorOccurrenceId,
                        losingAspect,
                        winningAspect,
                        settledTurn,
                        duration);

                if (state.TryDeclarePoser(declaration))
                    created.Add(declaration);
            }

            return new KvltParadigmContestResult(
                settledTurn,
                happeningId,
                behavior,
                opposition,
                first.Count,
                second.Count,
                false,
                winningAspect,
                losingAspect,
                created);
        }

        private static Dictionary<string, BehaviorOccurrence>
            BuildBehaviorLookup(Happening happening)
        {
            Dictionary<string, BehaviorOccurrence> result =
                new(StringComparer.Ordinal);

            foreach (BehaviorOccurrence behavior
                     in happening.BehaviorOccurrences)
                result.Add(behavior.BehaviorOccurrenceId, behavior);

            return result;
        }
    }
}
