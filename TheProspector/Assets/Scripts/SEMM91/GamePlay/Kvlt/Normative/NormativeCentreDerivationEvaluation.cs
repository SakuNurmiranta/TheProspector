using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Normative
{
    /// <summary>
    /// Immutable settled Normative Centre plus the
    /// complete per-Tag derivation that produced it.
    /// </summary>
    public sealed class NormativeCentreDerivationEvaluation
    {
        private readonly
            NormativeCentreDerivationEntry[]
            entries;

        public string SceneId { get; }

        public int SettledTurn { get; }

        public NormativeCentre Centre { get; }

        public IReadOnlyList<
                NormativeCentreDerivationEntry>
            Entries =>
            entries;

        public int EntryCount =>
            entries.Length;

        public NormativeCentreDerivationEvaluation(
            string sceneId,
            int settledTurn,
            NormativeCentre centre,
            IReadOnlyList<
                NormativeCentreDerivationEntry>
                sourceEntries)
        {
            if (string.IsNullOrWhiteSpace(sceneId))
            {
                throw new ArgumentException(
                    "Scene identity cannot be empty.",
                    nameof(sceneId)
                );
            }

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            Centre =
                centre ??
                throw new ArgumentNullException(
                    nameof(centre)
                );

            if (sourceEntries == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceEntries)
                );
            }

            SceneId =
                sceneId.Trim();

            SettledTurn =
                settledTurn;

            entries =
                new NormativeCentreDerivationEntry[
                    sourceEntries.Count
                ];

            HashSet<
                (
                    Core.Tags.TagAxis Axis,
                    Core.Tags.TagPole Pole,
                    Core.Tags.TagDegree Degree
                )>
                seen =
                    new();

            for (int index = 0;
                 index < sourceEntries.Count;
                 index++)
            {
                NormativeCentreDerivationEntry entry =
                    sourceEntries[index] ??
                    throw new ArgumentException(
                        "Normative derivation cannot " +
                        "contain null entries.",
                        nameof(sourceEntries)
                    );

                var key =
                    (
                        entry.Axis,
                        entry.Pole,
                        entry.RecordedDegree
                    );

                if (!seen.Add(key))
                {
                    throw new ArgumentException(
                        "Normative derivation contains " +
                        "duplicate affinity identity.",
                        nameof(sourceEntries)
                    );
                }

                entries[index] =
                    entry;
            }
        }

        public bool TryGetEntry(
            Core.Tags.TagAxis axis,
            Core.Tags.TagPole pole,
            Core.Tags.TagDegree recordedDegree,
            out NormativeCentreDerivationEntry entry)
        {
            foreach (
                NormativeCentreDerivationEntry candidate
                in entries)
            {
                if (candidate.Axis == axis &&
                    candidate.Pole == pole &&
                    candidate.RecordedDegree ==
                        recordedDegree)
                {
                    entry =
                        candidate;

                    return true;
                }
            }

            entry =
                null;

            return false;
        }
    }
}