using System;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class SceneReleaseActivationStateService
    {
        public bool ApplyCovered(
            SceneRelease release,
            ActivationLegitimacyAssessment assessment,
            int appliedTurn)
        {
            if (release == null ||
                assessment == null)
            {
                return false;
            }

            if (assessment.Disposition !=
                    ActivationLegitimacyDisposition
                        .Covered ||
                assessment.CoveringPrecedent == null)
            {
                return false;
            }

            return release.TryApplyLegitimateActivation(
                assessment.Candidate,
                assessment.CoveringPrecedent,
                SceneReleaseActivationHistoryKind
                    .CoveredApplied,
                appliedTurn
            );
        }

        public bool ApplyCrisisLegitimized(
            SceneRelease release,
            LegitimizedActivationCandidate
                legitimized)
        {
            if (release == null ||
                legitimized == null)
            {
                return false;
            }

            return release.TryApplyLegitimateActivation(
                legitimized.Candidate,
                legitimized.CoveringPrecedent,
                SceneReleaseActivationHistoryKind
                    .CrisisAcceptedApplied,
                legitimized.LegitimatedTurn,
                legitimized.AllegianceCrisisQuestionId
            );
        }

        public bool StorePending(
            SceneRelease release,
            PendingActivationCandidate candidate,
            out SceneReleasePendingActivation pending)
        {
            pending = null;

            if (release == null ||
                candidate == null)
            {
                return false;
            }

            return release.TryStorePendingActivation(
                candidate,
                out pending
            );
        }

        public int RedeemEligiblePending(
            SceneRelease release,
            AcceptedTransgressionState
                acceptedTransgressions,
            int redeemedTurn)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (acceptedTransgressions == null)
            {
                throw new ArgumentNullException(
                    nameof(acceptedTransgressions)
                );
            }

            if (redeemedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(redeemedTurn)
                );
            }

            int redeemedCount =
                0;

            // Snapshot protects iteration from any
            // future changes to Pending storage logic.

            SceneReleasePendingActivation[]
                pendingSnapshot =
                    new SceneReleasePendingActivation[
                        release.PendingActivations.Count
                    ];

            for (int index = 0;
                 index < pendingSnapshot.Length;
                 index++)
            {
                pendingSnapshot[index] =
                    release.PendingActivations[index];
            }

            foreach (
                SceneReleasePendingActivation pending
                in pendingSnapshot)
            {
                if (pending.IsRedeemed)
                {
                    continue;
                }

                ActivationLegitimacyCandidate candidate =
                    pending.Candidate;

                if (!acceptedTransgressions
                        .TryGetCoveringPrecedent(
                            candidate.BehaviorTypeId,
                            candidate.Axis,
                            candidate.Pole,
                            candidate.PraxisDegree,
                            out AcceptedTransgressionRecord
                                precedent
                        ))
                {
                    continue;
                }

                if (precedent.AcceptedTurn >
                    redeemedTurn)
                {
                    continue;
                }

                if (release
                    .TryRedeemPendingActivation(
                        pending,
                        precedent,
                        redeemedTurn
                    ))
                {
                    redeemedCount++;
                }
            }

            return redeemedCount;
        }
    }
}