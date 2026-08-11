using System;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Frozen qualifying Field-release contribution
    /// to one settlement's competitive Surface
    /// population.
    /// </summary>
    public sealed class SceneFieldSurfaceEntry
    {
        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int SettledTurn { get; }

        public float Surface { get; }

        public float StartFieldPosition { get; }

        public SceneFieldSurfaceEntry(
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            int settledTurn,
            float surface,
            float startFieldPosition)
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

            if (!IsFinite(surface))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(surface)
                );
            }

            if (!IsFinite(startFieldPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startFieldPosition)
                );
            }

            SettledTurn =
                settledTurn;

            Surface =
                surface;

            StartFieldPosition =
                startFieldPosition;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Field Surface provenance " +
                    "cannot be empty.",
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