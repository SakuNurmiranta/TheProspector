using System;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class ActivationLegitimacyAssessment
    {
        public ActivationLegitimacyCandidate
            Candidate { get; }

        public ActivationLegitimacyDisposition
            Disposition { get; }

        public AcceptedTransgressionRecord
            CoveringPrecedent { get; }

        public bool IsCovered =>
            Disposition ==
            ActivationLegitimacyDisposition.Covered;

        public ActivationLegitimacyAssessment(
            ActivationLegitimacyCandidate candidate,
            ActivationLegitimacyDisposition disposition,
            AcceptedTransgressionRecord
                coveringPrecedent)
        {
            Candidate =
                candidate ??
                throw new ArgumentNullException(
                    nameof(candidate)
                );

            if (!Enum.IsDefined(
                    typeof(
                        ActivationLegitimacyDisposition
                    ),
                    disposition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(disposition)
                );
            }

            if (disposition ==
                ActivationLegitimacyDisposition.Covered)
            {
                CoveringPrecedent =
                    coveringPrecedent ??
                    throw new ArgumentNullException(
                        nameof(coveringPrecedent),
                        "Covered activation requires " +
                        "exact precedent provenance."
                    );
            }
            else
            {
                if (coveringPrecedent != null)
                {
                    throw new ArgumentException(
                        "Uncovered activation cannot " +
                        "carry covering precedent.",
                        nameof(coveringPrecedent)
                    );
                }

                CoveringPrecedent = null;
            }

            Disposition = disposition;
        }
    }
}