using System;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// One immutable signed cause of movement for one
    /// SceneRelease during one settlement.
    ///
    /// Positive Delta = inward.
    /// Negative Delta = outward.
    /// </summary>
    public sealed class SceneReleaseMovementComponent
    {
        public string SceneReleaseId { get; }

        public string SceneId { get; }

        public int SettledTurn { get; }

        public SceneReleaseMovementComponentKind
            Kind { get; }

        public float Delta { get; }

        public string SourceId { get; }

        public SceneReleaseMovementComponent(
            string sceneReleaseId,
            string sceneId,
            int settledTurn,
            SceneReleaseMovementComponentKind kind,
            float delta,
            string sourceId)
        {
            SceneReleaseId =
                RequireText(
                    sceneReleaseId,
                    nameof(sceneReleaseId)
                );

            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            SourceId =
                RequireText(
                    sourceId,
                    nameof(sourceId)
                );

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            if (!Enum.IsDefined(
                    typeof(
                        SceneReleaseMovementComponentKind
                    ),
                    kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind)
                );
            }

            if (!IsFinite(delta))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(delta)
                );
            }

            SettledTurn =
                settledTurn;

            Kind =
                kind;

            Delta =
                delta;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Movement provenance cannot be empty.",
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