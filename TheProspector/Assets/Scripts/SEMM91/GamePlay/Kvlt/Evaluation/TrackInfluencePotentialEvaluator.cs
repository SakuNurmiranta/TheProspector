using System;
using System.Collections.Generic;
using SEMM91.Core.Recordings;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackInfluencePotentialEvaluator
    {
        private readonly TrackSurfaceExtremityEvaluator
            surfaceExtremityEvaluator =
                new TrackSurfaceExtremityEvaluator();

        private readonly TrackResonanceEvaluator
            resonanceEvaluator =
                new TrackResonanceEvaluator();

        public TrackInfluencePotentialEvaluation Evaluate(
            DemoTapeTrackSnapshot track,
            TrackEvaluationEnvironment environment,
            TrackTrveCapabilityEvaluation capability)
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

            if (capability == null)
            {
                throw new ArgumentNullException(
                    nameof(capability)
                );
            }

            ValidateIdentity(
                track,
                capability
            );

            TrackSurfaceExtremityEvaluation
                canonSurface =
                    surfaceExtremityEvaluator
                        .EvaluateAgainstNormativeCentre(
                            track,
                            environment
                                .CanonNormativeCentre
                        );

            TrackResonanceEvaluation
                canonResonance =
                    resonanceEvaluator
                        .Evaluate(
                            canonSurface
                        );

            List<IdeaInfluencePotentialEvaluation>
                ideaEvaluations =
                    new();

            float maximumTrveTotal =
                0f;

            for (int index = 0;
                 index < track.IdeaSnapshots.Count;
                 index++)
            {
                DemoTapeIdeaSnapshot idea =
                    track.IdeaSnapshots[index];

                IdeaTrveCapabilityEvaluation
                    ideaCapability =
                        capability
                            .IdeaEvaluations[index];

                IdeaSurfaceExtremityEvaluation
                    surfaceIdea =
                        canonSurface
                            .IdeaEvaluations[index];

                IdeaResonanceEvaluation
                    resonanceIdea =
                        canonResonance
                            .IdeaEvaluations[index];

                if (idea.SourceIdeaId !=
                        ideaCapability.SourceIdeaId ||
                    idea.IdeaIndex !=
                        ideaCapability.IdeaIndex)
                {
                    throw new ArgumentException(
                        "TRVE capability Idea " +
                        "identity/order does not match " +
                        "recorded Track.",
                        nameof(capability)
                    );
                }

                float maximumValidContribution =
                    ideaCapability.IsTrveCapable
                        ? ideaCapability
                            .ContextualTrvePotential
                        : 0f;

                if (maximumValidContribution < 0f ||
                    maximumValidContribution > 6f)
                {
                    throw new InvalidOperationException(
                        "Maximum valid Idea TRVE " +
                        "contribution is outside 0..6 | " +
                        $"idea={idea.SourceIdeaId} | " +
                        $"value={maximumValidContribution}"
                    );
                }

                maximumTrveTotal +=
                    maximumValidContribution;

                ideaEvaluations.Add(
                    new IdeaInfluencePotentialEvaluation(
                        idea.SourceIdeaId,
                        idea.IdeaIndex,
                        surfaceIdea
                            .SurfaceContribution,
                        surfaceIdea.Extremity,
                        resonanceIdea
                            .SignedResonanceContribution,
                        maximumValidContribution
                    )
                );
            }

            float maximumTrve =
                track.IdeaSnapshots.Count == 0
                    ? 0f
                    : maximumTrveTotal /
                      track.IdeaSnapshots.Count;

            float maximumTrveFraction =
                maximumTrve / 6f;

            float signedIp =
                canonResonance
                    .SignedResonance *
                maximumTrveFraction;

            return new TrackInfluencePotentialEvaluation(
                track.SourceTrackId,
                canonResonance.SignedResonance,
                maximumTrve,
                signedIp,
                ideaEvaluations
            );
        }

        private static void ValidateIdentity(
            DemoTapeTrackSnapshot track,
            TrackTrveCapabilityEvaluation capability)
        {
            if (track.SourceTrackId !=
                capability.SourceTrackId)
            {
                throw new ArgumentException(
                    "TRVE capability evaluation " +
                    "refers to a different Track.",
                    nameof(capability)
                );
            }

            if (track.IdeaSnapshots.Count !=
                capability.IdeaEvaluations.Count)
            {
                throw new ArgumentException(
                    "TRVE capability evaluation " +
                    "contains a different number " +
                    "of Ideas.",
                    nameof(capability)
                );
            }
        }
    }
}