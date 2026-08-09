using System;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackLegitimacyEvaluation
    {
        public string SourceReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceTrackId { get; }

        public int SettledTurn { get; }

        public TrackSurfaceExtremityEvaluation
            SurfaceExtremity { get; }

        public TrackSensationalExtremityEvaluation
            SensationalExtremity { get; }

        public TrackSocietyBackingEvaluation
            SocietyBacking { get; }

        public TrackTrveCapabilityEvaluation
            TrveCapability { get; }

        public TrackTrveEvaluation
            Trve { get; }

        public TrackResonanceEvaluation
            Resonance { get; }

        public TrackGravityEvaluation
            Gravity { get; }

        public TrackInfluencePotentialEvaluation
            InfluencePotential { get; }

        public float Surface =>
            SurfaceExtremity.Surface;

        public float Extremity =>
            SurfaceExtremity.Extremity;

        public int Es =>
            SensationalExtremity
                .SensationalExtremity;

        public float T =>
            Trve.Trve;

        public bool HasTrve =>
            Trve.HasTrve;

        public float SignedResonance =>
            Resonance.SignedResonance;

        public float ResonanceMagnitude =>
            Resonance.Resonance;

        public float SignedGravity =>
            Gravity.SignedGravity;

        public float GravityMagnitude =>
            Gravity.Gravity;

        public float SignedInfluencePotential =>
            InfluencePotential
                .SignedInfluencePotential;

        public float InfluencePotentialMagnitude =>
            InfluencePotential
                .InfluencePotential;

        public TrackLegitimacyEvaluation(
            string sourceReleaseId,
            string sourceDemoTapeId,
            string sourceTrackId,
            int settledTurn,
            TrackSurfaceExtremityEvaluation
                surfaceExtremity,
            TrackSensationalExtremityEvaluation
                sensationalExtremity,
            TrackSocietyBackingEvaluation
                societyBacking,
            TrackTrveCapabilityEvaluation
                trveCapability,
            TrackTrveEvaluation trve,
            TrackResonanceEvaluation resonance,
            TrackGravityEvaluation gravity,
            TrackInfluencePotentialEvaluation
                influencePotential)
        {
            SourceReleaseId =
                RequireText(
                    sourceReleaseId,
                    nameof(sourceReleaseId)
                );

            SourceDemoTapeId =
                RequireText(
                    sourceDemoTapeId,
                    nameof(sourceDemoTapeId)
                );

            SourceTrackId =
                RequireText(
                    sourceTrackId,
                    nameof(sourceTrackId)
                );

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn),
                    settledTurn,
                    "Settled turn cannot be negative."
                );
            }

            SettledTurn =
                settledTurn;

            SurfaceExtremity =
                surfaceExtremity ??
                throw new ArgumentNullException(
                    nameof(surfaceExtremity)
                );

            SensationalExtremity =
                sensationalExtremity ??
                throw new ArgumentNullException(
                    nameof(sensationalExtremity)
                );

            SocietyBacking =
                societyBacking ??
                throw new ArgumentNullException(
                    nameof(societyBacking)
                );

            TrveCapability =
                trveCapability ??
                throw new ArgumentNullException(
                    nameof(trveCapability)
                );

            Trve =
                trve ??
                throw new ArgumentNullException(
                    nameof(trve)
                );

            Resonance =
                resonance ??
                throw new ArgumentNullException(
                    nameof(resonance)
                );

            Gravity =
                gravity ??
                throw new ArgumentNullException(
                    nameof(gravity)
                );

            InfluencePotential =
                influencePotential ??
                throw new ArgumentNullException(
                    nameof(influencePotential)
                );

            ValidateTrackIdentity();
        }

        private void ValidateTrackIdentity()
        {
            string expected =
                SourceTrackId;

            if (SurfaceExtremity.SourceTrackId != expected ||
                SensationalExtremity.SourceTrackId != expected ||
                SocietyBacking.SourceTrackId != expected ||
                TrveCapability.SourceTrackId != expected ||
                Trve.SourceTrackId != expected ||
                Resonance.SourceTrackId != expected ||
                Gravity.SourceTrackId != expected ||
                InfluencePotential.SourceTrackId != expected)
            {
                throw new ArgumentException(
                    "Complete Track evaluation contains " +
                    "component results from different Tracks."
                );
            }

            if (Trve.SourceReleaseId !=
                    SourceReleaseId ||
                Gravity.SourceReleaseId !=
                    SourceReleaseId)
            {
                throw new ArgumentException(
                    "Complete Track evaluation contains " +
                    "different SceneRelease provenance."
                );
            }

            if (Trve.SourceDemoTapeId !=
                    SourceDemoTapeId ||
                Gravity.SourceDemoTapeId !=
                    SourceDemoTapeId)
            {
                throw new ArgumentException(
                    "Complete Track evaluation contains " +
                    "different DemoTape provenance."
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
                    "Track evaluation provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}