using System;
using SEMM91.GamePlay.Kvlt.Canon;

namespace SEMM91.GamePlay.Score
{
    /// <summary>
    /// Immutable proof that one historical canonical
    /// frontier claim produced one Institutional
    /// Gravity Score event.
    /// </summary>
    public sealed class
        HistoricalCanonInstitutionalGravityAward
    {
        public
            SceneReleaseCanonFrontierGravityContribution
            FrontierContribution { get; }

        public ScoreEvent ScoreEvent { get; }

        public string SceneReleaseId =>
            FrontierContribution.SceneReleaseId;

        public string CanonicalActivatorPlayerId =>
            FrontierContribution
                .CanonicalActivatorPlayerId;

        public float Amount =>
            FrontierContribution
                .ClaimReleaseGravityContribution;

        public int GlobalTurn =>
            ScoreEvent.GlobalTurn;

        public HistoricalCanonInstitutionalGravityAward(
            SceneReleaseCanonFrontierGravityContribution
                frontierContribution,
            ScoreEvent scoreEvent)
        {
            FrontierContribution =
                frontierContribution ??
                throw new ArgumentNullException(
                    nameof(frontierContribution)
                );

            ScoreEvent =
                scoreEvent ??
                throw new ArgumentNullException(
                    nameof(scoreEvent)
                );

            if (scoreEvent.Kind !=
                ScoreEventKind.InstitutionalGravity)
            {
                throw new ArgumentException(
                    "Historical frontier award requires " +
                    "InstitutionalGravity Score.",
                    nameof(scoreEvent)
                );
            }

            if (scoreEvent.SceneReleaseId !=
                    frontierContribution
                        .SceneReleaseId ||
                scoreEvent.SourceOwnerEntityId !=
                    frontierContribution
                        .FrontierClaim
                        .SourceOwnerEntityId ||
                scoreEvent.BeneficiaryEntityId !=
                    frontierContribution
                        .CanonicalActivatorPlayerId)
            {
                throw new ArgumentException(
                    "Institutional Gravity Score " +
                    "provenance does not match the " +
                    "canonical frontier claim.",
                    nameof(scoreEvent)
                );
            }

            if (Math.Abs(
                    scoreEvent.Amount -
                    frontierContribution
                        .ClaimReleaseGravityContribution) >
                0.0001f)
            {
                throw new ArgumentException(
                    "Institutional Gravity Score amount " +
                    "does not match frozen claim " +
                    "contribution.",
                    nameof(scoreEvent)
                );
            }
        }
    }
}