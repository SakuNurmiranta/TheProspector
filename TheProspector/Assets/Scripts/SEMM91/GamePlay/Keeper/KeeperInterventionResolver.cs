using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Validates and resolves one light-version Keeper
    /// intervention against authoritative server-domain state.
    ///
    /// On failure, neither tenure nor release state changes.
    /// On success, the release adjustment is staged and the
    /// returned tenure contains the reduced Pull amount.
    /// </summary>
    public sealed class KeeperInterventionResolver
    {
        public bool TryResolve(
            KeeperInterventionRequest request,
            ulong currentKeeperClientId,
            int currentTurn,
            KeeperTenureState currentTenure,
            SeededWorldState world,
            out KeeperTenureState nextTenure,
            out KeeperInterventionResult result)
        {
            KeeperInterventionFailureReason
                requestFailure =
                    KeeperPullRules
                        .ValidateInterventionRequest(
                            request
                        );

            if (requestFailure !=
                KeeperInterventionFailureReason.None)
            {
                return Fail(
                    request,
                    requestFailure,
                    currentTenure,
                    out nextTenure,
                    out result
                );
            }

            if (request.KeeperClientId !=
                currentKeeperClientId)
            {
                return Fail(
                    request,
                    KeeperInterventionFailureReason
                        .NotCurrentKeeper,
                    currentTenure,
                    out nextTenure,
                    out result
                );
            }

            if (request.RequestedTurn !=
                currentTurn)
            {
                return Fail(
                    request,
                    KeeperInterventionFailureReason
                        .InvalidTurn,
                    currentTenure,
                    out nextTenure,
                    out result
                );
            }

            if (currentTenure == null ||
                currentTenure.KeeperClientId !=
                currentKeeperClientId)
            {
                return Fail(
                    request,
                    KeeperInterventionFailureReason
                        .MissingTenure,
                    currentTenure,
                    out nextTenure,
                    out result
                );
            }
            
            if (currentTenure.HasUsedIntervention(
                    request.InterventionType,
                    currentTurn
                ))
            {
                return Fail(
                    request,
                    KeeperInterventionFailureReason
                        .AlreadyIntervenedThisTurn,
                    currentTenure,
                    out nextTenure,
                    out result
                );
            }
            
            if (world == null)
            {
                return Fail(
                    request,
                    KeeperInterventionFailureReason
                        .MissingWorldState,
                    currentTenure,
                    out nextTenure,
                    out result
                );
            }

            SceneRelease release =
                world.FindSceneRelease(
                    request.ReleaseId
                );

            if (release == null)
            {
                return Fail(
                    request,
                    KeeperInterventionFailureReason
                        .MissingRelease,
                    currentTenure,
                    out nextTenure,
                    out result
                );
            }

            float requestedVisibilityDelta =
                KeeperInterventionRules
                    .GetRequestedVisibilityDelta(
                        request.InterventionType,
                        request.PullSpend
                    );

            if (!release
                    .TryPreviewVisibilityAdjustment(
                        requestedVisibilityDelta,
                        out _
                    ))
            {
                return Fail(
                    request,
                    KeeperInterventionFailureReason
                        .NoVisibilityEffect,
                    currentTenure,
                    out nextTenure,
                    out result
                );
            }

            if (!currentTenure.TrySpendPull(
                    request.PullSpend,
                    out KeeperTenureState
                        prospectiveTenure
                ))
            {
                return Fail(
                    request,
                    KeeperInterventionFailureReason
                        .InsufficientPull,
                    currentTenure,
                    out nextTenure,
                    out result
                );
            }

            /*
             * Spending above was immutable. The authoritative
             * tenure has not changed yet.
             *
             * Staging can therefore occur now. If it fails,
             * prospectiveTenure is discarded and both domain
             * objects remain unchanged.
             */
            if (!release.TryStageVisibilityAdjustment(
                    requestedVisibilityDelta,
                    out float appliedVisibilityDelta
                ))
            {
                return Fail(
                    request,
                    KeeperInterventionFailureReason
                        .NoVisibilityEffect,
                    currentTenure,
                    out nextTenure,
                    out result
                );
            }

            nextTenure =
                prospectiveTenure
                    .RecordIntervention(
                        request.InterventionType,
                        currentTurn
                    );

            result =
                KeeperInterventionResult.Success(
                    request,
                    pullSpent:
                        request.PullSpend,
                    pullRemaining:
                        nextTenure.Pull,
                    appliedVisibilityDelta
                );

            return true;
        }

        private static bool Fail(
            KeeperInterventionRequest request,
            KeeperInterventionFailureReason
                failureReason,
            KeeperTenureState currentTenure,
            out KeeperTenureState nextTenure,
            out KeeperInterventionResult result)
        {
            nextTenure =
                currentTenure;

            result =
                KeeperInterventionResult.Failure(
                    request,
                    failureReason,
                    pullRemaining:
                        currentTenure?.Pull ??
                        0.0f
                );

            return false;
        }
    }
}