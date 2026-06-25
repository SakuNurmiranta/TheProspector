using System.Collections.Generic;

namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Pure Keeper-selection and transition-classification logic.
    ///
    /// This resolver does not mutate GameCoordinator, player state,
    /// scene releases, roles, canonization or Pull.
    /// </summary>
    public sealed class KeeperTransitionResolver
    {
        public KeeperTransitionResult
            ResolveInitialAssignment(
                int resolvedRound,
                IReadOnlyList<KeeperCandidate>
                    candidates)
        {
            if (!TrySelectHighestCandidate(
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
                winningSceneOutput:
                    winner.SceneOutput
            );
        }

        public KeeperTransitionResult
            ResolveYearEnd(
                int resolvedRound,
                ulong currentKeeperClientId,
                IReadOnlyList<KeeperCandidate>
                    candidates)
        {
            if (!TrySelectHighestCandidate(
                    candidates,
                    out KeeperCandidate winner
                ))
            {
                return KeeperTransitionResult.None;
            }

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
                    out KeeperCandidate currentKeeper
                );

            if (!currentKeeperIsCandidate)
            {
                return CreateResult(
                    resolvedRound,
                    KeeperTransitionReason
                        .YearEndReplaced,
                    currentKeeperClientId,
                    winner.ClientId,
                    winner.SceneOutput
                );
            }

            bool challengerActuallyWins =
                winner.ClientId !=
                    currentKeeperClientId &&
                winner.SceneOutput >
                    currentKeeper.SceneOutput;

            if (challengerActuallyWins)
            {
                return CreateResult(
                    resolvedRound,
                    KeeperTransitionReason
                        .YearEndReplaced,
                    currentKeeperClientId,
                    winner.ClientId,
                    winner.SceneOutput
                );
            }

            // Ties retain the incumbent. A challenger must
            // strictly exceed the current Keeper's output.
            return CreateResult(
                resolvedRound,
                KeeperTransitionReason
                    .YearEndRetained,
                currentKeeperClientId,
                currentKeeperClientId,
                currentKeeper.SceneOutput
            );
        }

        public KeeperTransitionResult
            ResolveDisconnectionFallback(
                int resolvedRound,
                ulong disconnectedKeeperClientId,
                IReadOnlyList<KeeperCandidate>
                    remainingCandidates)
        {
            if (!TrySelectHighestCandidate(
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
                    winningSceneOutput: 0.0f
                );
            }

            return CreateResult(
                resolvedRound,
                KeeperTransitionReason
                    .DisconnectionFallback,
                disconnectedKeeperClientId,
                winner.ClientId,
                winner.SceneOutput
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

        private static bool TrySelectHighestCandidate(
            IReadOnlyList<KeeperCandidate>
                candidates,
            out KeeperCandidate winner)
        {
            winner = default;

            if (candidates == null ||
                candidates.Count == 0)
            {
                return false;
            }

            bool foundCandidate = false;

            for (int i = 0;
                 i < candidates.Count;
                 i++)
            {
                KeeperCandidate candidate =
                    candidates[i];

                if (!foundCandidate)
                {
                    winner = candidate;
                    foundCandidate = true;
                    continue;
                }

                bool hasHigherOutput =
                    candidate.SceneOutput >
                    winner.SceneOutput;

                bool winsTieBreak =
                    candidate.SceneOutput.Equals(
                        winner.SceneOutput
                    ) &&
                    candidate.ClientId <
                    winner.ClientId;

                if (hasHigherOutput ||
                    winsTieBreak)
                {
                    winner = candidate;
                }
            }

            return foundCandidate;
        }

        private static bool TryFindCandidate(
            IReadOnlyList<KeeperCandidate>
                candidates,
            ulong clientId,
            out KeeperCandidate result)
        {
            result = default;

            if (candidates == null)
                return false;

            for (int i = 0;
                 i < candidates.Count;
                 i++)
            {
                KeeperCandidate candidate =
                    candidates[i];

                if (candidate.ClientId != clientId)
                    continue;

                result = candidate;
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
                float winningSceneOutput)
        {
            float pullGrant =
                KeeperPullRules.GetTransitionGrant(
                    reason,
                    winningSceneOutput
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
                winningSceneOutput,
                pullGrant
            );
        }
    }
}