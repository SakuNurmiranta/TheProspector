using System.Collections.Generic;

namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Pure Keeper-selection and transition-
    /// classification logic.
    ///
    /// Peak-2 year-end succession uses:
    ///
    /// 1. YearInfluence;
    /// 2. Scene Standing between otherwise tied
    ///    challengers;
    /// 3. lowest ClientId as final deterministic
    ///    challenger tie-break.
    ///
    /// The incumbent receives a stronger protection:
    /// a challenger must strictly exceed the
    /// incumbent's YearInfluence. Equal influence
    /// always retains the incumbent regardless of
    /// Scene Standing.
    ///
    /// Institutionally ineligible candidates are
    /// ignored for acquisition of the office.
    ///
    /// This resolver does not mutate GameCoordinator,
    /// player state, scene releases, roles,
    /// canonization, tenure or Pull.
    /// </summary>
    public sealed class KeeperTransitionResolver
    {
        public KeeperTransitionResult
            ResolveInitialAssignment(
                int resolvedRound,
                IReadOnlyList<KeeperCandidate>
                    candidates)
        {
            if (!TrySelectHighestEligibleCandidate(
                    candidates,
                    out KeeperCandidate winner
                ))
            {
                return KeeperTransitionResult.None;
            }

            return CreateResult(
                resolvedRound,
                KeeperTransitionReason
                    .InitialAssignment,
                previousKeeperClientId:
                    ulong.MaxValue,
                nextKeeperClientId:
                    winner.ClientId,
                winningInfluence:
                    winner.YearInfluence
            );
        }

        public KeeperTransitionResult
            ResolveYearEnd(
                int resolvedRound,
                ulong currentKeeperClientId,
                IReadOnlyList<KeeperCandidate>
                    candidates)
        {
            if (currentKeeperClientId ==
                ulong.MaxValue)
            {
                return ResolveInitialAssignment(
                    resolvedRound,
                    candidates
                );
            }

            bool currentKeeperIsCandidate =
                TryFindCandidate(
                    candidates,
                    currentKeeperClientId,
                    out KeeperCandidate
                        currentKeeper
                );

            /*
             * A missing incumbent is not protected.
             * This may occur through exceptional
             * runtime state; the highest eligible
             * remaining player receives the office.
             */
            if (!currentKeeperIsCandidate)
            {
                if (!TrySelectHighestEligibleCandidate(
                        candidates,
                        out KeeperCandidate winner))
                {
                    return KeeperTransitionResult.None;
                }

                return CreateResult(
                    resolvedRound,
                    KeeperTransitionReason
                        .YearEndReplaced,
                    currentKeeperClientId,
                    winner.ClientId,
                    winner.YearInfluence
                );
            }

            /*
             * Select challengers independently of the
             * incumbent.
             *
             * This matters because the incumbent's
             * retention rule is NOT an ordinary
             * challenger tie-break.
             */
            if (!TrySelectHighestEligibleChallenger(
                    candidates,
                    currentKeeperClientId,
                    out KeeperCandidate challenger))
            {
                /*
                 * No eligible challenger exists.
                 *
                 * This also covers the final rule that
                 * an incumbent remains when all
                 * challengers are institutionally
                 * ineligible.
                 */
                return CreateResult(
                    resolvedRound,
                    KeeperTransitionReason
                        .YearEndRetained,
                    currentKeeperClientId,
                    currentKeeperClientId,
                    currentKeeper.YearInfluence
                );
            }

            /*
             * Critical final Peak-2 rule:
             *
             * Scene Standing cannot dethrone an
             * incumbent on an equal YearInfluence.
             *
             * The challenger must actually outperform
             * the completed-year influence of the
             * Keeper.
             */
            if (challenger.YearInfluence >
                currentKeeper.YearInfluence)
            {
                return CreateResult(
                    resolvedRound,
                    KeeperTransitionReason
                        .YearEndReplaced,
                    currentKeeperClientId,
                    challenger.ClientId,
                    challenger.YearInfluence
                );
            }

            return CreateResult(
                resolvedRound,
                KeeperTransitionReason
                    .YearEndRetained,
                currentKeeperClientId,
                currentKeeperClientId,
                currentKeeper.YearInfluence
            );
        }

        public KeeperTransitionResult
            ResolveDisconnectionFallback(
                int resolvedRound,
                ulong disconnectedKeeperClientId,
                IReadOnlyList<KeeperCandidate>
                    remainingCandidates)
        {
            if (!TrySelectHighestEligibleCandidate(
                    remainingCandidates,
                    out KeeperCandidate winner
                ))
            {
                return CreateResult(
                    resolvedRound,
                    KeeperTransitionReason
                        .DisconnectionFallback,
                    disconnectedKeeperClientId,
                    nextKeeperClientId:
                        ulong.MaxValue,
                    winningInfluence:
                        0.0f
                );
            }

            return CreateResult(
                resolvedRound,
                KeeperTransitionReason
                    .DisconnectionFallback,
                disconnectedKeeperClientId,
                winner.ClientId,
                winner.YearInfluence
            );
        }

        public KeeperTransitionResult
            ResolveSceneCollapseLock(
                int resolvedRound,
                ulong currentKeeperClientId,
                float currentKeeperSceneOutput)
        {
            return CreateResult(
                resolvedRound,
                KeeperTransitionReason
                    .SceneCollapseLocked,
                currentKeeperClientId,
                currentKeeperClientId,
                currentKeeperSceneOutput
            );
        }

        private static bool
            TrySelectHighestEligibleCandidate(
                IReadOnlyList<KeeperCandidate>
                    candidates,
                out KeeperCandidate winner)
        {
            winner =
                default;

            if (candidates == null ||
                candidates.Count == 0)
            {
                return false;
            }

            bool foundCandidate =
                false;

            for (int index = 0;
                 index < candidates.Count;
                 index++)
            {
                KeeperCandidate candidate =
                    candidates[index];

                if (!candidate.IsEligible)
                {
                    continue;
                }

                if (!foundCandidate)
                {
                    winner =
                        candidate;

                    foundCandidate =
                        true;

                    continue;
                }

                if (IsBetterOpenSeatCandidate(
                        candidate,
                        winner))
                {
                    winner =
                        candidate;
                }
            }

            return foundCandidate;
        }

        private static bool
            TrySelectHighestEligibleChallenger(
                IReadOnlyList<KeeperCandidate>
                    candidates,
                ulong incumbentClientId,
                out KeeperCandidate winner)
        {
            winner =
                default;

            if (candidates == null ||
                candidates.Count == 0)
            {
                return false;
            }

            bool foundCandidate =
                false;

            for (int index = 0;
                 index < candidates.Count;
                 index++)
            {
                KeeperCandidate candidate =
                    candidates[index];

                if (candidate.ClientId ==
                    incumbentClientId)
                {
                    continue;
                }

                if (!candidate.IsEligible)
                {
                    continue;
                }

                if (!foundCandidate)
                {
                    winner =
                        candidate;

                    foundCandidate =
                        true;

                    continue;
                }

                if (IsBetterOpenSeatCandidate(
                        candidate,
                        winner))
                {
                    winner =
                        candidate;
                }
            }

            return foundCandidate;
        }

        private static bool
            IsBetterOpenSeatCandidate(
                KeeperCandidate candidate,
                KeeperCandidate incumbentBest)
        {
            if (candidate.YearInfluence >
                incumbentBest.YearInfluence)
            {
                return true;
            }

            if (candidate.YearInfluence <
                incumbentBest.YearInfluence)
            {
                return false;
            }

            /*
             * Equal influence between challengers or
             * candidates for an otherwise open seat:
             * current Scene Standing is the first
             * deterministic tie-break.
             */
            if (candidate.HasSceneStanding &&
                !incumbentBest.HasSceneStanding)
            {
                return true;
            }

            if (!candidate.HasSceneStanding &&
                incumbentBest.HasSceneStanding)
            {
                return false;
            }

            if (candidate.HasSceneStanding &&
                incumbentBest.HasSceneStanding)
            {
                if (candidate.SceneStanding.Value >
                    incumbentBest.SceneStanding.Value)
                {
                    return true;
                }

                if (candidate.SceneStanding.Value <
                    incumbentBest.SceneStanding.Value)
                {
                    return false;
                }
            }

            /*
             * Final deterministic tie-break.
             */
            return
                candidate.ClientId <
                incumbentBest.ClientId;
        }

        private static bool TryFindCandidate(
            IReadOnlyList<KeeperCandidate>
                candidates,
            ulong clientId,
            out KeeperCandidate result)
        {
            result =
                default;

            if (candidates == null)
            {
                return false;
            }

            for (int index = 0;
                 index < candidates.Count;
                 index++)
            {
                KeeperCandidate candidate =
                    candidates[index];

                if (candidate.ClientId !=
                    clientId)
                {
                    continue;
                }

                result =
                    candidate;

                return true;
            }

            return false;
        }

        private static KeeperTransitionResult
            CreateResult(
                int resolvedRound,
                KeeperTransitionReason reason,
                ulong previousKeeperClientId,
                ulong nextKeeperClientId,
                float winningInfluence)
        {
            float pullGrant =
                KeeperPullRules.GetTransitionGrant(
                    reason,
                    winningInfluence
                );

            return new KeeperTransitionResult(
                resolvedRound,
                reason,
                previousKeeperClientId,
                nextKeeperClientId,
                previousSubjectReleaseId:
                    string.Empty,
                canonizedReleaseId:
                    string.Empty,
                incomingSubjectReleaseId:
                    string.Empty,
                winningSceneOutput:
                    winningInfluence,
                pullGrant
            );
        }
    }
}