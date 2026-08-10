using System;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// One exact activation candidate whose praxis
    /// has obtained institutional legitimacy.
    ///
    /// This is not yet applied SceneRelease activation.
    /// </summary>
    public sealed class LegitimizedActivationCandidate
    {
        public ActivationLegitimacyCandidate
            Candidate { get; }

        public string AllegianceCrisisQuestionId { get; }

        public AcceptedTransgressionRecord
            CoveringPrecedent { get; }

        public int LegitimatedTurn { get; }

        public LegitimizedActivationCandidate(
            ActivationLegitimacyCandidate candidate,
            string allegianceCrisisQuestionId,
            AcceptedTransgressionRecord
                coveringPrecedent,
            int legitimatedTurn)
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
                    "Legitimized activation requires " +
                    "Allegiance Crisis provenance.",
                    nameof(allegianceCrisisQuestionId)
                );
            }

            CoveringPrecedent =
                coveringPrecedent ??
                throw new ArgumentNullException(
                    nameof(coveringPrecedent)
                );

            if (legitimatedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(legitimatedTurn)
                );
            }

            if (CoveringPrecedent.Key.BehaviorTypeId !=
                    Candidate.BehaviorTypeId ||
                CoveringPrecedent.Key.Axis !=
                    Candidate.Axis ||
                CoveringPrecedent.Key.Pole !=
                    Candidate.Pole ||
                (int)CoveringPrecedent.AcceptedDegree <
                    (int)Candidate.PraxisDegree)
            {
                throw new ArgumentException(
                    "Covering precedent does not " +
                    "legitimate this activation praxis.",
                    nameof(coveringPrecedent)
                );
            }

            AllegianceCrisisQuestionId =
                allegianceCrisisQuestionId.Trim();

            LegitimatedTurn =
                legitimatedTurn;
        }
    }
}