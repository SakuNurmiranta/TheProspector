using System;
using SEMM91.Core.Recordings;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackLegitimacyEvaluator
    {
        private readonly
            TrackSurfaceExtremityEvaluator
                surfaceExtremityEvaluator =
                    new();

        private readonly
            TrackSensationalExtremityEvaluator
                sensationalExtremityEvaluator =
                    new();

        private readonly
            TrackSocietyBackingEvaluator
                societyBackingEvaluator =
                    new();

        private readonly
            TrackTrveCapabilityEvaluator
                trveCapabilityEvaluator =
                    new();

        private readonly
            TrackTrveEvaluator
                trveEvaluator =
                    new();

        private readonly
            TrackResonanceEvaluator
                resonanceEvaluator =
                    new();

        private readonly
            TrackGravityEvaluator
                gravityEvaluator =
                    new();

        private readonly
            TrackInfluencePotentialEvaluator
                influencePotentialEvaluator =
                    new();

        public TrackLegitimacyEvaluation Evaluate(
            DemoTapeTrackSnapshot track,
            TrackEvaluationEnvironment environment,
            TrackActivationEvaluationSnapshot activation)
        {
            if (track == null)
            {
                throw new ArgumentNullException(
                    nameof(track)
                );
            }

            if (environment == null)
            {
                throw new ArgumentNullException(
                    nameof(environment)
                );
            }

            if (activation == null)
            {
                throw new ArgumentNullException(
                    nameof(activation)
                );
            }

            if (activation.SourceTrackId !=
                track.SourceTrackId)
            {
                throw new ArgumentException(
                    "Activation snapshot refers to " +
                    "a different Track.",
                    nameof(activation)
                );
            }

            TrackSurfaceExtremityEvaluation
                surfaceExtremity =
                    surfaceExtremityEvaluator
                        .Evaluate(
                            track,
                            environment
                        );

            TrackSensationalExtremityEvaluation
                sensationalExtremity =
                    sensationalExtremityEvaluator
                        .Evaluate(
                            track,
                            environment
                        );

            TrackSocietyBackingEvaluation
                societyBacking =
                    societyBackingEvaluator
                        .Evaluate(
                            track,
                            environment
                        );

            TrackTrveCapabilityEvaluation
                trveCapability =
                    trveCapabilityEvaluator
                        .Evaluate(
                            track,
                            societyBacking
                        );

            TrackTrveEvaluation trve =
                trveEvaluator.Evaluate(
                    track,
                    trveCapability,
                    activation
                );

            TrackResonanceEvaluation resonance =
                resonanceEvaluator.Evaluate(
                    surfaceExtremity
                );

            TrackGravityEvaluation gravity =
                gravityEvaluator.Evaluate(
                    resonance,
                    trve
                );

            TrackInfluencePotentialEvaluation
                influencePotential =
                    influencePotentialEvaluator
                        .Evaluate(
                            track,
                            environment,
                            trveCapability
                        );

            return new TrackLegitimacyEvaluation(
                activation.SourceReleaseId,
                activation.SourceDemoTapeId,
                track.SourceTrackId,
                environment.SettledTurn,
                surfaceExtremity,
                sensationalExtremity,
                societyBacking,
                trveCapability,
                trve,
                resonance,
                gravity,
                influencePotential
            );
        }
    }
}