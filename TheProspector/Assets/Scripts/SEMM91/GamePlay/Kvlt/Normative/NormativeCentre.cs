using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Normative
{
    [Serializable]
    public sealed class NormativeCentre
    {
        private readonly Dictionary<
            (
                TagAxis Axis,
                TagPole Pole,
                TagDegree RecordedDegree
            ),
            float
        > affinities = new();

        public static NormativeCentre Neutral { get; } =
            new NormativeCentre(
                Array.Empty<NormativeAffinityEntry>()
            );

        public int NonZeroAffinityCount =>
            affinities.Count;

        public NormativeCentre(
            IEnumerable<NormativeAffinityEntry> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(
                    nameof(entries)
                );
            }

            HashSet<
                (
                    TagAxis Axis,
                    TagPole Pole,
                    TagDegree RecordedDegree
                )
            > seenKeys = new();

            foreach (NormativeAffinityEntry entry
                     in entries)
            {
                if (entry == null)
                {
                    throw new ArgumentException(
                        "Normative Centre cannot contain a null affinity entry.",
                        nameof(entries)
                    );
                }

                var key =
                    (
                        entry.Axis,
                        entry.Pole,
                        entry.RecordedDegree
                    );

                if (!seenKeys.Add(key))
                {
                    throw new ArgumentException(
                        "Normative Centre contains a duplicate affinity key | " +
                        $"axis={entry.Axis} | " +
                        $"pole={entry.Pole} | " +
                        $"degree={entry.RecordedDegree}",
                        nameof(entries)
                    );
                }

                if (entry.Affinity == 0f)
                {
                    continue;
                }

                affinities.Add(
                    key,
                    entry.Affinity
                );
            }
        }

        public float GetAffinity(
            TagAxis axis,
            TagPole pole,
            TagDegree recordedDegree)
        {
            if (affinities.TryGetValue(
                    (
                        axis,
                        pole,
                        recordedDegree
                    ),
                    out float affinity))
            {
                return affinity;
            }

            return 0f;
        }
    }
}