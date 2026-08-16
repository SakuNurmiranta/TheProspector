using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class SceneReleaseLegitimacyEvaluation
    {
        private readonly
            TrackLegitimacyEvaluation[]
                trackEvaluations;

        public string SourceReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public int SettledTurn { get; }

        public IReadOnlyList<
            TrackLegitimacyEvaluation>
            TrackEvaluations =>
                trackEvaluations;

        public float Surface { get; }

        public float Extremity { get; }

        public int SensationalExtremity { get; }

        public float Trve { get; }

        public float Resonance { get; }

        public float Gravity { get; }

        public float InfluencePotential { get; }

        public bool HasTrve { get; }

        public SceneReleaseLegitimacyEvaluation(
            string sourceReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            int settledTurn,
            IReadOnlyList<
                TrackLegitimacyEvaluation>
                sourceTrackEvaluations)
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

            SourceOwnerEntityId =
                RequireText(
                    sourceOwnerEntityId,
                    nameof(sourceOwnerEntityId)
                );

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn),
                    settledTurn,
                    "Settled turn cannot be negative."
                );
            }

            if (sourceTrackEvaluations == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceTrackEvaluations)
                );
            }

            SettledTurn =
                settledTurn;

            trackEvaluations =
                new TrackLegitimacyEvaluation[
                    sourceTrackEvaluations.Count
                ];

            float surfaceTotal = 0f;
            float extremityTotal = 0f;
            float trveTotal = 0f;
            float resonanceTotal = 0f;
            float gravityTotal = 0f;
            float influencePotentialTotal = 0f;

            int sensationalMaximum = 0;

            bool hasTrve = false;

            for (int index = 0;
                 index < sourceTrackEvaluations.Count;
                 index++)
            {
                TrackLegitimacyEvaluation track =
                    sourceTrackEvaluations[index] ??
                    throw new ArgumentException(
                        "Release evaluation cannot " +
                        "contain a null Track result.",
                        nameof(sourceTrackEvaluations)
                    );

                if (track.SourceReleaseId !=
                    SourceReleaseId)
                {
                    throw new ArgumentException(
                        "Track evaluation belongs to " +
                        "a different SceneRelease.",
                        nameof(sourceTrackEvaluations)
                    );
                }

                if (track.SourceDemoTapeId !=
                    SourceDemoTapeId)
                {
                    throw new ArgumentException(
                        "Track evaluation belongs to " +
                        "a different DemoTape.",
                        nameof(sourceTrackEvaluations)
                    );
                }

                if (track.SettledTurn !=
                    SettledTurn)
                {
                    throw new ArgumentException(
                        "Track evaluation belongs to " +
                        "a different settled turn.",
                        nameof(sourceTrackEvaluations)
                    );
                }

                trackEvaluations[index] =
                    track;

                surfaceTotal +=
                    track.Surface;

                extremityTotal +=
                    track.Extremity;

                sensationalMaximum =
                    Math.Max(
                        sensationalMaximum,
                        track.Es
                    );

                trveTotal +=
                    track.T;

                resonanceTotal +=
                    track.ResonanceMagnitude;

                gravityTotal +=
                    track.GravityMagnitude;

                influencePotentialTotal +=
                    track.InfluencePotentialMagnitude;

                if (track.HasTrve)
                {
                    hasTrve = true;
                }
            }

            if (trackEvaluations.Length == 0)
            {
                Surface = 0f;
                Extremity = 0f;
                SensationalExtremity = 0;
                Trve = 0f;
                Resonance = 0f;
                Gravity = 0f;
                InfluencePotential = 0f;
                HasTrve = false;

                return;
            }

            float count =
                trackEvaluations.Length;

            Surface =
                surfaceTotal / count;

            Extremity =
                extremityTotal / count;

            SensationalExtremity =
                sensationalMaximum;

            Trve =
                trveTotal / count;

            Resonance =
                resonanceTotal / count;

            Gravity =
                gravityTotal / count;

            InfluencePotential =
                influencePotentialTotal / count;

            HasTrve =
                hasTrve;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Release evaluation provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}