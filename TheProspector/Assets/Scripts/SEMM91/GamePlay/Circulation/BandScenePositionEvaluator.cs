using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Calculates one band's current Scene Position
    /// from its resident Field releases.
    ///
    /// Resident means:
    ///
    /// Lifecycle == Field
    /// AND
    /// FieldPositionState exists.
    ///
    /// Newly fettered releases waiting for initial
    /// placement are therefore excluded.
    /// </summary>
    public sealed class
        BandScenePositionEvaluator
    {
        public BandScenePositionEvaluation
            Evaluate(
                string sourceOwnerEntityId,
                string sceneId,
                int settledTurn,
                IReadOnlyList<SceneRelease>
                    releases)
        {
            sourceOwnerEntityId =
                RequireText(
                    sourceOwnerEntityId,
                    nameof(sourceOwnerEntityId)
                );

            sceneId =
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

            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            HashSet<string> allReleaseIds =
                new(
                    StringComparer.Ordinal
                );

            List<SceneRelease> resident =
                new();

            foreach (
                SceneRelease release
                in releases)
            {
                if (release == null)
                {
                    throw new ArgumentException(
                        "Band Scene Position release " +
                        "population cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!allReleaseIds.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Band Scene Position release " +
                        "population contains duplicate " +
                        "identity.",
                        nameof(releases)
                    );
                }

                if (release.SourceOwnerEntityId !=
                    sourceOwnerEntityId)
                {
                    continue;
                }

                if (release.HostedSceneNodeId !=
                    sceneId)
                {
                    continue;
                }

                if (release.LifecycleState !=
                    SceneReleaseLifecycleState.Field)
                {
                    continue;
                }

                if (!release.HasFieldPosition ||
                    release.FieldPositionState == null)
                {
                    /*
                     * Newly fettered, pre-placement
                     * releases cannot contribute to the
                     * position that determines their own
                     * entry.
                     */
                    continue;
                }

                if (release.FieldPositionState
                        .EstablishedTurn >
                    settledTurn)
                {
                    throw new InvalidOperationException(
                        "Band Scene Position cannot use " +
                        "a Field position established " +
                        "after the requested settled " +
                        "turn | " +
                        $"release={release.ReleaseId}"
                    );
                }

                resident.Add(
                    release
                );
            }

            resident.Sort(
                (
                    left,
                    right
                ) =>
                    string.CompareOrdinal(
                        left.ReleaseId,
                        right.ReleaseId
                    )
            );

            if (resident.Count == 0)
            {
                return new
                    BandScenePositionEvaluation(
                        sourceOwnerEntityId,
                        sceneId,
                        settledTurn,
                        Array.Empty<string>(),
                        scenePosition:
                            null
                    );
            }

            float total =
                0f;

            string[] sourceReleaseIds =
                new string[
                    resident.Count
                ];

            for (int index = 0;
                 index < resident.Count;
                 index++)
            {
                SceneRelease release =
                    resident[index];

                sourceReleaseIds[index] =
                    release.ReleaseId;

                total +=
                    release.FieldPositionState
                        .CurrentPosition;
            }

            float mean =
                total /
                resident.Count;

            return new
                BandScenePositionEvaluation(
                    sourceOwnerEntityId,
                    sceneId,
                    settledTurn,
                    sourceReleaseIds,
                    mean
                );
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
    }
}