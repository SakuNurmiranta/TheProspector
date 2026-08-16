using System;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// A canonical frontier claim paired with the
    /// exact frozen share of G_release attributable
    /// to that active pair.
    ///
    /// This is the future post-tenure Institutional
    /// Gravity unit.
    /// </summary>
    public sealed class
        SceneReleaseCanonFrontierGravityContribution
    {
        public SceneReleaseCanonFrontierClaim
            FrontierClaim { get; }

        public
            SceneReleaseActivePairGravityContribution
            PairContribution { get; }

        public string SceneReleaseId =>
            FrontierClaim.SceneReleaseId;

        public string SourceTrackId =>
            FrontierClaim.SourceTrackId;

        public string SourceIdeaId =>
            FrontierClaim.SourceIdeaId;

        public int IdeaIndex =>
            FrontierClaim.IdeaIndex;

        public string
            CanonicalActivatorPlayerId =>
            FrontierClaim
                .CanonicalActivatorPlayerId;

        public float
            ClaimReleaseGravityContribution =>
            PairContribution
                .ClaimReleaseGravityContribution;

        public SceneReleaseCanonFrontierGravityContribution(
            SceneReleaseCanonFrontierClaim
                frontierClaim,
            SceneReleaseActivePairGravityContribution
                pairContribution)
        {
            FrontierClaim =
                frontierClaim ??
                throw new ArgumentNullException(
                    nameof(frontierClaim)
                );

            PairContribution =
                pairContribution ??
                throw new ArgumentNullException(
                    nameof(pairContribution)
                );

            if (frontierClaim.SceneReleaseId !=
                    pairContribution.SceneReleaseId ||
                frontierClaim.SourceDemoTapeId !=
                    pairContribution.SourceDemoTapeId ||
                frontierClaim.SourceOwnerEntityId !=
                    pairContribution.SourceOwnerEntityId ||
                frontierClaim.SourceTrackId !=
                    pairContribution.SourceTrackId ||
                frontierClaim.SourceIdeaId !=
                    pairContribution.SourceIdeaId ||
                frontierClaim.IdeaIndex !=
                    pairContribution.IdeaIndex ||
                frontierClaim.CanonizedTurn !=
                    pairContribution.CanonizedTurn)
            {
                throw new ArgumentException(
                    "Frontier claim and pair Gravity " +
                    "contribution have different " +
                    "provenance."
                );
            }

            if ((int)frontierClaim
                    .CanonicalDegreeEstablished >
                pairContribution
                    .FinalActivationDegree)
            {
                throw new ArgumentException(
                    "Canonical frontier degree cannot " +
                    "exceed the final frozen activation."
                );
            }
        }
    }
}