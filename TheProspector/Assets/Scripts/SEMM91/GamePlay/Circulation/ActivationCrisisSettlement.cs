using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class ActivationCrisisSettlement
    {
        private readonly
            LegitimizedActivationCandidate[]
                legitimizedCandidates;

        private readonly
            PendingActivationCandidate[]
                pendingCandidates;

        public AllegianceCrisisQuestion
            Question { get; }

        public AllegianceCrisisResolution
            CrisisResolution { get; }

        public AcceptedTransgressionRecord
            AcceptedPrecedent { get; }

        public bool RaisedAcceptedPrecedent { get; }

        public IReadOnlyList<
            LegitimizedActivationCandidate>
            LegitimizedCandidates =>
                legitimizedCandidates;

        public IReadOnlyList<
            PendingActivationCandidate>
            PendingCandidates =>
                pendingCandidates;

        public ActivationCrisisSettlement(
            AllegianceCrisisQuestion question,
            AllegianceCrisisResolution
                crisisResolution,
            AcceptedTransgressionRecord
                acceptedPrecedent,
            bool raisedAcceptedPrecedent,
            IReadOnlyList<
                LegitimizedActivationCandidate>
                legitimizedCandidates,
            IReadOnlyList<
                PendingActivationCandidate>
                pendingCandidates)
        {
            Question =
                question ??
                throw new ArgumentNullException(
                    nameof(question)
                );

            CrisisResolution =
                crisisResolution ??
                throw new ArgumentNullException(
                    nameof(crisisResolution)
                );

            if (legitimizedCandidates == null)
            {
                throw new ArgumentNullException(
                    nameof(legitimizedCandidates)
                );
            }

            if (pendingCandidates == null)
            {
                throw new ArgumentNullException(
                    nameof(pendingCandidates)
                );
            }

            this.legitimizedCandidates =
                Copy(
                    legitimizedCandidates
                );

            this.pendingCandidates =
                Copy(
                    pendingCandidates
                );

            if (crisisResolution.Outcome ==
                AllegianceCrisisOutcome.Kvlt)
            {
                AcceptedPrecedent =
                    acceptedPrecedent ??
                    throw new ArgumentNullException(
                        nameof(acceptedPrecedent),
                        "KVLT victory requires accepted " +
                        "praxis provenance."
                    );

                if (this.pendingCandidates.Length != 0)
                {
                    throw new ArgumentException(
                        "KVLT victory cannot create " +
                        "Pending candidates.",
                        nameof(pendingCandidates)
                    );
                }
            }
            else
            {
                if (acceptedPrecedent != null ||
                    raisedAcceptedPrecedent)
                {
                    throw new ArgumentException(
                        "Society victory cannot create " +
                        "Accepted Transgression."
                    );
                }

                if (this.legitimizedCandidates.Length != 0)
                {
                    throw new ArgumentException(
                        "Society victory cannot directly " +
                        "legitimize candidates.",
                        nameof(legitimizedCandidates)
                    );
                }

                AcceptedPrecedent = null;
            }

            RaisedAcceptedPrecedent =
                raisedAcceptedPrecedent;
        }

        private static T[] Copy<T>(
            IReadOnlyList<T> source)
            where T : class
        {
            T[] result =
                new T[source.Count];

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                result[index] =
                    source[index] ??
                    throw new ArgumentException(
                        "Settlement collections cannot " +
                        "contain null records."
                    );
            }

            return result;
        }
    }
}