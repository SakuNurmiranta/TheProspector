using System;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// One canonical frontier claim.
    ///
    /// A claim may originate either from:
    ///
    /// 1. a live realized breakthrough with exact
    ///    activation-event provenance, or
    ///
    /// 2. authoritative Scenario initial state.
    ///
    /// Both forms produce the same institutional
    /// frontier identity for later Gravity accounting.
    /// </summary>
    public sealed class
        SceneReleaseCanonFrontierClaim
    {
        public
            SceneReleaseCanonFrontierProvenanceKind
            ProvenanceKind { get; }

        /// <summary>
        /// Present only for live Canon breakthroughs.
        /// </summary>
        public SceneReleaseCanonBreakthroughClaim
            BreakthroughClaim { get; }

        /// <summary>
        /// Present only for live Canon breakthroughs.
        /// </summary>
        public SceneReleaseActivationHistoryRecord
            ActivationProvenance { get; }

        public bool HasLiveBreakthroughProvenance =>
            ProvenanceKind ==
            SceneReleaseCanonFrontierProvenanceKind
                .LiveBreakthrough;

        public bool IsScenarioSeed =>
            ProvenanceKind ==
            SceneReleaseCanonFrontierProvenanceKind
                .ScenarioSeed;

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
        /// For live Canon this comes from the activation
        /// actor.
        ///
        /// For Scenario-seeded Canon it is supplied by
        /// the scenario bootstrap explicitly.
        /// </summary>
        public string
            CanonicalActivatorPlayerId { get; }

        public int CanonizedTurn { get; }

        public string KeeperTenureId { get; }

        /// <summary>
        /// Existing live-play constructor.
        ///
        /// Its behavior is intentionally unchanged.
        /// </summary>
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

            keeperTenureId =
                RequireText(
                    keeperTenureId,
                    nameof(keeperTenureId)
                );

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

            ProvenanceKind =
                SceneReleaseCanonFrontierProvenanceKind
                    .LiveBreakthrough;

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
                keeperTenureId;
        }

        /// <summary>
        /// Scenario-initial-state constructor.
        ///
        /// No breakthrough or activation-history event
        /// is invented. The release must already carry
        /// the frozen pair activation being claimed.
        /// </summary>
        public SceneReleaseCanonFrontierClaim(
            SceneRelease release,
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex,
            TagAxis axis,
            TagPole pole,
            TagDegree canonicalDegreeEstablished,
            string canonicalActivatorPlayerId,
            string keeperTenureId,
            int canonizedTurn)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            sourceTrackId =
                RequireText(
                    sourceTrackId,
                    nameof(sourceTrackId)
                );

            sourceIdeaId =
                RequireText(
                    sourceIdeaId,
                    nameof(sourceIdeaId)
                );

            canonicalActivatorPlayerId =
                RequireText(
                    canonicalActivatorPlayerId,
                    nameof(canonicalActivatorPlayerId)
                );

            keeperTenureId =
                RequireText(
                    keeperTenureId,
                    nameof(keeperTenureId)
                );

            if (ideaIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ideaIndex)
                );
            }

            ValidateDegree(
                canonicalDegreeEstablished,
                nameof(canonicalDegreeEstablished)
            );

            if (canonicalDegreeEstablished ==
                TagDegree.Neutral)
            {
                throw new ArgumentException(
                    "Scenario-seeded Canon frontier " +
                    "requires degree >= 1.",
                    nameof(canonicalDegreeEstablished)
                );
            }

            if (canonizedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(canonizedTurn)
                );
            }

            if (!release.IsCanonized ||
                release.CanonizationFreezeState == null ||
                release.LifecycleState !=
                    SceneReleaseLifecycleState
                        .CanonRetained)
            {
                throw new ArgumentException(
                    "Scenario frontier requires an " +
                    "already frozen CanonRetained release.",
                    nameof(release)
                );
            }

            if (release.CanonizedTurn !=
                    canonizedTurn ||
                !string.Equals(
                    release
                        .CanonizedUnderKeeperTenureId,
                    keeperTenureId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Scenario frontier turn/tenure does " +
                    "not match frozen SceneRelease."
                );
            }

            if (!release.TryGetPairActivationState(
                    sourceTrackId,
                    sourceIdeaId,
                    ideaIndex,
                    out
                        SceneReleasePairActivationState
                        activation))
            {
                throw new ArgumentException(
                    "Scenario frontier does not identify " +
                    "a frozen active pair on the release."
                );
            }

            if ((int)activation
                    .CurrentActivationDegree <
                (int)canonicalDegreeEstablished)
            {
                throw new ArgumentException(
                    "Scenario frontier degree exceeds " +
                    "the release's frozen activation."
                );
            }

            ProvenanceKind =
                SceneReleaseCanonFrontierProvenanceKind
                    .ScenarioSeed;

            /*
             * Deliberately absent. No fake gameplay
             * event is manufactured for initial state.
             */
            BreakthroughClaim =
                null;

            ActivationProvenance =
                null;

            SceneReleaseId =
                release.ReleaseId;

            SourceDemoTapeId =
                release.SourceDemoTapeId;

            SourceOwnerEntityId =
                release.SourceOwnerEntityId;

            SourceTrackId =
                sourceTrackId;

            SourceIdeaId =
                sourceIdeaId;

            IdeaIndex =
                ideaIndex;

            Axis =
                axis;

            Pole =
                pole;

            CanonicalDegreeEstablished =
                canonicalDegreeEstablished;

            CanonicalActivatorPlayerId =
                canonicalActivatorPlayerId;

            CanonizedTurn =
                canonizedTurn;

            KeeperTenureId =
                keeperTenureId;
        }

        private static void ValidateDegree(
            TagDegree degree,
            string parameterName)
        {
            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    degree))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Canon frontier provenance cannot " +
                    "be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}