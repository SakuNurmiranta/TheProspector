using System;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// One realized breakthrough claim that actually
    /// establishes the resulting NewCanon frontier for
    /// its Tag polarity.
    ///
    /// Multiple same-degree claims may coexist as
    /// tied/coincident frontier provenance.
    /// </summary>
    public sealed class
        SceneReleaseCanonFrontierClaim
    {
        public SceneReleaseCanonBreakthroughClaim
            BreakthroughClaim { get; }

        public SceneReleaseActivationHistoryRecord
            ActivationProvenance { get; }

        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SourceTrackId { get; }

        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree
            CanonicalDegreeEstablished { get; }

        /// <summary>
        /// Peak-2 domain player identity.
        ///
        /// Existing activation provenance identifies
        /// the acting player-agent by ActorEntityId.
        /// This deliberately does not depend on
        /// transport/client identity.
        /// </summary>
        public string
            CanonicalActivatorPlayerId { get; }

        public int CanonizedTurn { get; }

        public string KeeperTenureId { get; }

        public SceneReleaseCanonFrontierClaim(
            SceneRelease release,
            SceneReleaseCanonBreakthroughClaim
                breakthroughClaim,
            SceneReleaseActivationHistoryRecord
                activationProvenance,
            string keeperTenureId,
            int canonizedTurn)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            BreakthroughClaim =
                breakthroughClaim ??
                throw new ArgumentNullException(
                    nameof(breakthroughClaim)
                );

            ActivationProvenance =
                activationProvenance ??
                throw new ArgumentNullException(
                    nameof(activationProvenance)
                );

            if (string.IsNullOrWhiteSpace(
                    keeperTenureId))
            {
                throw new ArgumentException(
                    "Frontier Canon provenance requires " +
                    "Keeper tenure identity.",
                    nameof(keeperTenureId)
                );
            }

            if (canonizedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(canonizedTurn)
                );
            }

            if (!breakthroughClaim
                    .IsQualifyingBreakthrough)
            {
                throw new ArgumentException(
                    "Canon frontier requires a " +
                    "qualifying realized breakthrough.",
                    nameof(breakthroughClaim)
                );
            }

            if (breakthroughClaim.SceneReleaseId !=
                    release.ReleaseId ||
                breakthroughClaim.SourceDemoTapeId !=
                    release.SourceDemoTapeId ||
                breakthroughClaim.SourceOwnerEntityId !=
                    release.SourceOwnerEntityId)
            {
                throw new ArgumentException(
                    "Breakthrough claim provenance does " +
                    "not match SceneRelease.",
                    nameof(breakthroughClaim)
                );
            }

            ActivationLegitimacyCandidate activation =
                activationProvenance.Candidate;

            if (activation.SceneReleaseId !=
                    release.ReleaseId ||
                activation.SourceDemoTapeId !=
                    release.SourceDemoTapeId ||
                activation.SourceTrackId !=
                    breakthroughClaim.SourceTrackId ||
                activation.SourceIdeaId !=
                    breakthroughClaim.SourceIdeaId ||
                activation.IdeaIndex !=
                    breakthroughClaim.IdeaIndex)
            {
                throw new ArgumentException(
                    "Activation provenance does not " +
                    "belong to the frontier pair.",
                    nameof(activationProvenance)
                );
            }

            if (!activationProvenance.RaisedActivation ||
                activationProvenance.NewActivationDegree !=
                    breakthroughClaim
                        .ActiveDominantDegree)
            {
                throw new ArgumentException(
                    "Frontier activation provenance " +
                    "must establish the active degree " +
                    "that entered Canon.",
                    nameof(activationProvenance)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    activation.ActorEntityId))
            {
                throw new ArgumentException(
                    "Frontier activation has no player " +
                    "actor provenance.",
                    nameof(activationProvenance)
                );
            }

            SceneReleaseId =
                release.ReleaseId;

            SourceDemoTapeId =
                release.SourceDemoTapeId;

            SourceOwnerEntityId =
                release.SourceOwnerEntityId;

            SourceTrackId =
                breakthroughClaim.SourceTrackId;

            SourceIdeaId =
                breakthroughClaim.SourceIdeaId;

            IdeaIndex =
                breakthroughClaim.IdeaIndex;

            Axis =
                breakthroughClaim.DominantAxis;

            Pole =
                breakthroughClaim.DominantPole;

            CanonicalDegreeEstablished =
                breakthroughClaim
                    .ActiveDominantDegree;

            CanonicalActivatorPlayerId =
                activation.ActorEntityId;

            CanonizedTurn =
                canonizedTurn;

            KeeperTenureId =
                keeperTenureId.Trim();
        }
    }
}