using System;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Immutable ordinary Natural Drift result for one
    /// qualifying SceneRelease against one frozen
    /// Field Surface snapshot.
    /// </summary>
    public sealed class
        SceneReleaseNaturalDriftEvaluation
    {
        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int SettledTurn { get; }

        public float StartFieldPosition { get; }

        public float ReleaseSurface { get; }

        public float FieldMeanSurface { get; }

        public float RelativeSurface { get; }

        public int FieldPopulationCount { get; }

        public float FieldDriftScale { get; }

        public float NaturalDrift { get; }

        public bool IsLoneQualifyingRelease =>
            FieldPopulationCount == 1;

        public SceneReleaseNaturalDriftEvaluation(
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            int settledTurn,
            float startFieldPosition,
            float releaseSurface,
            float fieldMeanSurface,
            int fieldPopulationCount,
            float fieldDriftScale,
            float naturalDrift)
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

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            if (!IsFinite(startFieldPosition) ||
                !IsFinite(releaseSurface) ||
                !IsFinite(fieldMeanSurface) ||
                !IsFinite(fieldDriftScale) ||
                !IsFinite(naturalDrift))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(naturalDrift),
                    "Natural Drift evaluation values " +
                    "must be finite."
                );
            }

            if (fieldPopulationCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fieldPopulationCount)
                );
            }

            if (fieldDriftScale < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fieldDriftScale),
                    "Field Drift Scale cannot be negative."
                );
            }

            SettledTurn =
                settledTurn;

            StartFieldPosition =
                startFieldPosition;

            ReleaseSurface =
                releaseSurface;

            FieldMeanSurface =
                fieldMeanSurface;

            RelativeSurface =
                releaseSurface -
                fieldMeanSurface;

            FieldPopulationCount =
                fieldPopulationCount;

            FieldDriftScale =
                fieldDriftScale;

            NaturalDrift =
                naturalDrift;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Natural Drift provenance cannot " +
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