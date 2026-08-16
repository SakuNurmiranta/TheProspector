using System;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class SceneReleasePendingActivation
    {
        public string PendingActivationId { get; }

        public PendingActivationCandidate Source { get; }

        public ActivationLegitimacyCandidate
            Candidate =>
                Source.Candidate;

        public int PendingTurn =>
            Source.PendingTurn;

        public string AllegianceCrisisQuestionId =>
            Source.AllegianceCrisisQuestionId;

        public bool IsRedeemed { get; private set; }

        public int? RedeemedTurn { get; private set; }

        public string
            RedeemingAcceptedTransgressionId
            { get; private set; }

        public SceneReleasePendingActivation(
            PendingActivationCandidate source)
        {
            Source =
                source ??
                throw new ArgumentNullException(
                    nameof(source)
                );

            PendingActivationId =
                BuildPendingId(
                    source
                );
        }

        internal void MarkRedeemed(
            AcceptedTransgressionRecord precedent,
            int redeemedTurn)
        {
            if (IsRedeemed)
            {
                throw new InvalidOperationException(
                    "Pending activation has already " +
                    "been redeemed."
                );
            }

            if (precedent == null)
            {
                throw new ArgumentNullException(
                    nameof(precedent)
                );
            }

            if (redeemedTurn <
                PendingTurn)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(redeemedTurn)
                );
            }

            IsRedeemed =
                true;

            RedeemedTurn =
                redeemedTurn;

            RedeemingAcceptedTransgressionId =
                precedent.AcceptedTransgressionId;
        }

        private static string BuildPendingId(
            PendingActivationCandidate source)
        {
            ActivationLegitimacyCandidate candidate =
                source.Candidate;

            return
                "PENDING|" +
                Encode(
                    source.AllegianceCrisisQuestionId
                ) +
                "|" +
                Encode(candidate.SceneReleaseId) +
                "|" +
                Encode(candidate.SourceTrackId) +
                "|" +
                Encode(candidate.SourceIdeaId) +
                "|" +
                candidate.IdeaIndex;
        }

        private static string Encode(
            string value)
        {
            return
                value.Length +
                ":" +
                value;
        }
    }
}