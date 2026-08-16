using System;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Frozen share of release Gravity attributable to
    /// one currently active formal-pair TRVE claim.
    ///
    /// ClaimTrackGravityContribution =
    ///     abs(R_signed_track)
    ///     * (t_pair / IdeaCount_track)
    ///     / 6
    ///
    /// ClaimReleaseGravityContribution =
    ///     ClaimTrackGravityContribution
    ///     / TrackCount_release
    /// </summary>
    public sealed class
        SceneReleaseActivePairGravityContribution
    {
        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public string SourceTrackId { get; }

        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public int CanonizedTurn { get; }

        public int FinalActivationDegree { get; }

        public float ActiveTrveContribution { get; }

        public float TrackSignedResonance { get; }

        public int IdeaCountTrack { get; }

        public int TrackCountRelease { get; }

        public float
            ClaimTrackGravityContribution { get; }

        public float
            ClaimReleaseGravityContribution { get; }

        public SceneReleaseActivePairGravityContribution(
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex,
            int canonizedTurn,
            int finalActivationDegree,
            float activeTrveContribution,
            float trackSignedResonance,
            int ideaCountTrack,
            int trackCountRelease)
        {
            SceneReleaseId =
                RequireText(
                    sceneReleaseId,
                    nameof(sceneReleaseId)
                );

            SourceDemoTapeId =
                RequireText(
                    sourceDemoTapeId,
                    nameof(sourceDemoTapeId)
                );

            SourceOwnerEntityId =
                RequireText(
                    sourceOwnerEntityId,
                    nameof(sourceOwnerEntityId)
                );

            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            SourceTrackId =
                RequireText(
                    sourceTrackId,
                    nameof(sourceTrackId)
                );

            SourceIdeaId =
                RequireText(
                    sourceIdeaId,
                    nameof(sourceIdeaId)
                );

            if (ideaIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ideaIndex)
                );
            }

            if (canonizedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(canonizedTurn)
                );
            }

            if (finalActivationDegree <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalActivationDegree)
                );
            }

            if (!IsFinite(activeTrveContribution) ||
                activeTrveContribution <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activeTrveContribution)
                );
            }

            if (!IsFinite(trackSignedResonance))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(trackSignedResonance)
                );
            }

            if (ideaCountTrack <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ideaCountTrack)
                );
            }

            if (trackCountRelease <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(trackCountRelease)
                );
            }

            IdeaIndex =
                ideaIndex;

            CanonizedTurn =
                canonizedTurn;

            FinalActivationDegree =
                finalActivationDegree;

            ActiveTrveContribution =
                activeTrveContribution;

            TrackSignedResonance =
                trackSignedResonance;

            IdeaCountTrack =
                ideaCountTrack;

            TrackCountRelease =
                trackCountRelease;

            ClaimTrackGravityContribution =
                Math.Abs(trackSignedResonance) *
                (
                    activeTrveContribution /
                    ideaCountTrack
                ) /
                6f;

            ClaimReleaseGravityContribution =
                ClaimTrackGravityContribution /
                trackCountRelease;

            if (!IsFinite(
                    ClaimTrackGravityContribution) ||
                !IsFinite(
                    ClaimReleaseGravityContribution))
            {
                throw new InvalidOperationException(
                    "Claim-level Gravity decomposition " +
                    "produced a non-finite result."
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
                    "Claim Gravity provenance cannot " +
                    "be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }

        private static bool IsFinite(
            float value)
        {
            return
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}