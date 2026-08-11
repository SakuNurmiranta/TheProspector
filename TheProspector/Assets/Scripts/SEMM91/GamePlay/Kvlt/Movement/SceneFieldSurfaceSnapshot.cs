using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Immutable qualifying Field population used by
    /// one simultaneous Natural Drift calculation.
    /// </summary>
    public sealed class SceneFieldSurfaceSnapshot
    {
        private readonly
            SceneFieldSurfaceEntry[]
            entries;

        private readonly
            Dictionary<string, SceneFieldSurfaceEntry>
            entriesByReleaseId =
                new(
                    StringComparer.Ordinal
                );

        public string SceneId { get; }

        public int SettledTurn { get; }

        public IReadOnlyList<
                SceneFieldSurfaceEntry>
            Entries =>
            entries;

        public int PopulationCount =>
            entries.Length;

        public bool HasQualifyingPopulation =>
            entries.Length > 0;

        public float? MeanSurface { get; }

        public SceneFieldSurfaceSnapshot(
            string sceneId,
            int settledTurn,
            IReadOnlyList<
                SceneFieldSurfaceEntry>
                sourceEntries)
        {
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

            if (sourceEntries == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceEntries)
                );
            }

            SettledTurn =
                settledTurn;

            entries =
                new SceneFieldSurfaceEntry[
                    sourceEntries.Count
                ];

            for (int index = 0;
                 index < sourceEntries.Count;
                 index++)
            {
                SceneFieldSurfaceEntry entry =
                    sourceEntries[index] ??
                    throw new ArgumentException(
                        "Field Surface snapshot cannot " +
                        "contain a null entry.",
                        nameof(sourceEntries)
                    );

                if (entry.SceneId !=
                    SceneId)
                {
                    throw new ArgumentException(
                        "Field Surface entry belongs to " +
                        "a different scene.",
                        nameof(sourceEntries)
                    );
                }

                if (entry.SettledTurn !=
                    SettledTurn)
                {
                    throw new ArgumentException(
                        "Field Surface entry belongs to " +
                        "a different settled turn.",
                        nameof(sourceEntries)
                    );
                }

                if (!entriesByReleaseId.TryAdd(
                        entry.SceneReleaseId,
                        entry))
                {
                    throw new ArgumentException(
                        "Field Surface snapshot contains " +
                        "duplicate SceneRelease identity.",
                        nameof(sourceEntries)
                    );
                }

                entries[index] =
                    entry;
            }

            /*
             * Stable ordering removes collection-order
             * ambiguity from later deterministic
             * settlement and logging.
             */
            Array.Sort(
                entries,
                (
                    left,
                    right
                ) =>
                    string.CompareOrdinal(
                        left.SceneReleaseId,
                        right.SceneReleaseId
                    )
            );

            if (entries.Length == 0)
            {
                MeanSurface =
                    null;

                return;
            }

            double total =
                0d;

            foreach (
                SceneFieldSurfaceEntry entry
                in entries)
            {
                total +=
                    entry.Surface;
            }

            MeanSurface =
                (float)(
                    total /
                    entries.Length
                );
        }

        public bool TryGetEntry(
            string sceneReleaseId,
            out SceneFieldSurfaceEntry entry)
        {
            if (string.IsNullOrWhiteSpace(
                    sceneReleaseId))
            {
                entry =
                    null;

                return false;
            }

            return entriesByReleaseId
                .TryGetValue(
                    sceneReleaseId,
                    out entry
                );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Field Surface snapshot identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}