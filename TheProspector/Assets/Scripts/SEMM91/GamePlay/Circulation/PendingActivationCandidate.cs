using System;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// One exact prospective activation blocked from
    /// institutional legitimacy by Society victory.
    ///
    /// Pending does not itself activate anything.
    /// </summary>
    public sealed class PendingActivationCandidate
    {
        public ActivationLegitimacyCandidate
            Candidate { get; }

        public string AllegianceCrisisQuestionId { get; }

        public int PendingTurn { get; }

        public PendingActivationCandidate(
            ActivationLegitimacyCandidate candidate,
            string allegianceCrisisQuestionId,
            int pendingTurn)
        {
            Candidate =
                candidate ??
                throw new ArgumentNullException(
                    nameof(candidate)
                );

            if (string.IsNullOrWhiteSpace(
                    allegianceCrisisQuestionId))
            {
                throw new ArgumentException(
                    "Pending activation requires " +
                    "Allegiance Crisis provenance.",
                    nameof(allegianceCrisisQuestionId)
                );
            }

            if (pendingTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pendingTurn)
                );
            }

            AllegianceCrisisQuestionId =
                allegianceCrisisQuestionId.Trim();

            PendingTurn =
                pendingTurn;
        }
    }
}