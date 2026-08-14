using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Score;

namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Runtime adapter for the Peak-2 year-end Keeper
    /// sequence.
    ///
    /// It composes existing domain services for:
    ///
    /// - completed-year YearInfluence;
    /// - Keeper succession classification;
    /// - continuous-tenure identity;
    /// - CanonRetained tenure-end transition.
    ///
    /// It does not apply collective roles or networking
    /// state. GameCoordinator owns those authoritative
    /// runtime responsibilities.
    /// </summary>
    public sealed class
        KvltYearEndKeeperRuntimeSettlementService
    {
        private readonly
            YearInfluenceEvaluator
            yearInfluenceEvaluator =
                new();

        private readonly
            KeeperTransitionResolver
            transitionResolver =
                new();

        private readonly
            SceneReleaseCanonTenureTransitionService
            canonTenureTransitionService =
                new();

        public IReadOnlyList<YearInfluenceEvaluation>
            EvaluateYearInfluence(
                string sceneId,
                int settledTurn,
                int turnsPerYear,
                IReadOnlyList<string>
                    beneficiaryEntityIds,
                ScoreLedger ledger)
        {
            return yearInfluenceEvaluator.Evaluate(
                sceneId,
                settledTurn,
                turnsPerYear,
                beneficiaryEntityIds,
                ledger
            );
        }

        public KeeperTransitionResult
            ResolveSuccession(
                int resolvedRound,
                ulong currentKeeperClientId,
                IReadOnlyList<KeeperCandidate>
                    candidates,
                bool sceneCollapseLocksTransition)
        {
            if (sceneCollapseLocksTransition)
            {
                float incumbentInfluence =
                    FindIncumbentInfluence(
                        candidates,
                        currentKeeperClientId
                    );

                return transitionResolver
                    .ResolveSceneCollapseLock(
                        resolvedRound,
                        currentKeeperClientId,
                        incumbentInfluence
                    );
            }

            return transitionResolver.ResolveYearEnd(
                resolvedRound,
                currentKeeperClientId,
                candidates
            );
        }

        public KeeperTenureState CreateNextTenure(
            KeeperTransitionResult transition,
            KeeperTenureState currentTenure)
        {
            if (!transition.HasResult)
            {
                throw new ArgumentException(
                    "Year-end Keeper transition has no " +
                    "resolved result.",
                    nameof(transition)
                );
            }

            switch (transition.Reason)
            {
                case KeeperTransitionReason
                    .YearEndRetained:

                    RequireMatchingIncumbentTenure(
                        transition,
                        currentTenure
                    );

                    return currentTenure.AddPull(
                        transition.PullGrant
                    );

                case KeeperTransitionReason
                    .YearEndReplaced:

                    if (!transition.HasAssignedKeeper)
                    {
                        throw new InvalidOperationException(
                            "Year-end replacement has no " +
                            "incoming Keeper."
                        );
                    }

                    if (currentTenure == null ||
                        currentTenure.KeeperClientId !=
                            transition
                                .PreviousKeeperClientId)
                    {
                        throw new InvalidOperationException(
                            "Year-end replacement does not " +
                            "match the current Keeper tenure."
                        );
                    }

                    return KeeperTenureState.Create(
                        transition.NextKeeperClientId,
                        transition.ResolvedRound,
                        transition.PullGrant
                    );

                case KeeperTransitionReason
                    .SceneCollapseLocked:

                    RequireMatchingIncumbentTenure(
                        transition,
                        currentTenure
                    );

                    return currentTenure;

                default:

                    throw new InvalidOperationException(
                        "Unsupported Peak-2 year-end " +
                        "Keeper transition reason | " +
                        $"reason={transition.Reason}"
                    );
            }
        }

        public IReadOnlyList<
                SceneReleaseCanonTenureTransitionApplication>
            ApplyCanonTenureTransition(
                IReadOnlyList<SceneRelease> releases,
                KeeperTenureState endingTenure,
                KeeperTenureState nextTenure,
                int settledTurn)
        {
            if (endingTenure == null)
            {
                throw new ArgumentNullException(
                    nameof(endingTenure)
                );
            }

            if (nextTenure == null)
            {
                throw new ArgumentNullException(
                    nameof(nextTenure)
                );
            }

            return canonTenureTransitionService.Apply(
                releases,
                endingTenure.KeeperTenureId,
                nextTenure.KeeperTenureId,
                settledTurn
            );
        }

        private static void
            RequireMatchingIncumbentTenure(
                KeeperTransitionResult transition,
                KeeperTenureState currentTenure)
        {
            if (!transition.HasAssignedKeeper ||
                transition.PreviousKeeperClientId !=
                    transition.NextKeeperClientId ||
                currentTenure == null ||
                currentTenure.KeeperClientId !=
                    transition.NextKeeperClientId)
            {
                throw new InvalidOperationException(
                    "Retained Keeper result does not " +
                    "match the current continuous tenure."
                );
            }
        }

        private static float FindIncumbentInfluence(
            IReadOnlyList<KeeperCandidate> candidates,
            ulong currentKeeperClientId)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(
                    nameof(candidates)
                );
            }

            foreach (
                KeeperCandidate candidate
                in candidates)
            {
                if (candidate.ClientId ==
                    currentKeeperClientId)
                {
                    return candidate.YearInfluence;
                }
            }

            return 0f;
        }
    }
}