using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Immutable snapshot of one band's current
    /// physical location inside one Scene Field.
    ///
    /// Unlike Scene Standing, this contains no
    /// historical weighting, rejection scars or
    /// Canon legacy.
    ///
    /// It is simply the mean current ScenePosition
    /// of the band's resident Field releases.
    /// </summary>
    public sealed class
        BandScenePositionEvaluation
    {
        private readonly string[]
            residentReleaseIds;

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int SettledTurn { get; }

        public IReadOnlyList<string>
            ResidentReleaseIds =>
            residentReleaseIds;

        public int ResidentReleaseCount =>
            residentReleaseIds.Length;

        public bool HasScenePosition =>
            residentReleaseIds.Length > 0;

        public float? ScenePosition { get; }

        public BandScenePositionEvaluation(
            string sourceOwnerEntityId,
            string sceneId,
            int settledTurn,
            IReadOnlyList<string>
                sourceResidentReleaseIds,
            float? scenePosition)
        {
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

            if (sourceResidentReleaseIds == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceResidentReleaseIds)
                );
            }

            SettledTurn =
                settledTurn;

            residentReleaseIds =
                new string[
                    sourceResidentReleaseIds.Count
                ];

            HashSet<string> identities =
                new(
                    StringComparer.Ordinal
                );

            for (int index = 0;
                 index <
                 sourceResidentReleaseIds.Count;
                 index++)
            {
                string releaseId =
                    RequireText(
                        sourceResidentReleaseIds[index],
                        nameof(
                            sourceResidentReleaseIds
                        )
                    );

                if (!identities.Add(
                        releaseId))
                {
                    throw new ArgumentException(
                        "Band Scene Position cannot " +
                        "contain duplicate resident " +
                        "release identity.",
                        nameof(
                            sourceResidentReleaseIds
                        )
                    );
                }

                residentReleaseIds[index] =
                    releaseId;
            }

            if (residentReleaseIds.Length == 0)
            {
                if (scenePosition.HasValue)
                {
                    throw new ArgumentException(
                        "Band without resident releases " +
                        "cannot have a Scene Position.",
                        nameof(scenePosition)
                    );
                }

                ScenePosition =
                    null;

                return;
            }

            if (!scenePosition.HasValue ||
                !IsFinite(scenePosition.Value))
            {
                throw new ArgumentException(
                    "Resident Band Scene Position " +
                    "requires a finite mean position.",
                    nameof(scenePosition)
                );
            }

            ScenePosition =
                scenePosition.Value;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Band Scene Position identity " +
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